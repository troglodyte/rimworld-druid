# Wild Shape redesign: hediff-based transformation

Replace the pawn-swap implementation of Wild Shape with a hediff applied to the colonist themselves.
The colonist never leaves the map and is never substituted, so the faction, drafting, and designator
problems that dominated the first implementation stop existing rather than being worked around.

## Problem

The original design transformed a druid by generating a new animal pawn, spawning it in the
colonist's place, and pushing the colonist into `Find.WorldPawns`. Three of the four defects found
in testing trace directly to that substitution.

Instrumented logging showed the colonist's faction being corrupted during the shift, not the revert:

```text
shift/before-passtoworld:  faction=New Arrivals    isColonist=True   inWorldPawns=False
shift/after-passtoworld:   faction=Pack of Bakgen  isColonist=False  inWorldPawns=True
```

The revert path faithfully restored a pawn whose faction had already been destroyed, which is why
reverted druids returned hostile.

Drafting failed for a separate but related reason. The animal pawn is not humanlike, and vanilla
gates the draft gizmo on `IsColonistPlayerControlled` upstream of `Pawn_DraftController.ShowDraftGizmo`.
A diagnostic confirmed the property is never consulted for a non-humanlike pawn, so no patch at that
level can succeed.

## Goals

The animal form exists for combat duty. A shifted druid can move and attack and nothing else.

- The pawn remains the same `Pawn` object, in the player faction, for the entire shift.
- Drafting works through the vanilla path with no think-tree or gizmo patching.
- Combat numbers derive from the vanilla animal, scaled by a configurable boost.
- The pawn is rendered as the animal, reusing textures the game already ships.

## Non-goals

- Work of any kind while shifted: no hauling, crafting, building, or ranged weapons.
- Per-organ injury mapping between human and animal bodies.
- New artwork. If a form cannot be drawn from existing assets, it is not supported.

## Architecture

A single hediff on the colonist carries the entire transformed state. Nothing else holds shift state,
and removing the hediff is sufficient to end the transformation.

| Component | Responsibility |
| --- | --- |
| `Hediff_WildShapeForm` | Holds the active form def, remaining ticks, and stashed gear |
| `HediffComp_WildShapeVerbs` | Sources melee tools from the animal's `ThingDef` and hosts them on the pawn |
| `StatPart_WildShape` | Applies derived move speed and durability |
| `PawnRenderNode` on the hediff | Draws the animal graphic and suppresses the human one |
| `capMods` on the hediff stage | Sets Manipulation to zero, blocking work and ranged attacks |

`StatPart` is required rather than `HediffStage.statOffsets` because the offsets differ per form and
are computed at shift time. `statOffsets` is static XML and cannot express that.

Manipulation is a single static value shared by every form, so it belongs in XML on the hediff stage
rather than in derivation code.

## Lifecycle

On shift, the ability resolves the chosen `DruidkinAnimalFormDef`, strips the pawn's weapon and
apparel into the hediff, and adds the hediff to the caster. No pawn is generated, despawned, or
passed to the world.

On revert, whether by the gizmo or by the timer expiring, the hediff returns the stashed gear and
removes itself. Because the pawn was never substituted, there is no faction, spawn, or world-pawn
state to restore.

Injuries need no special handling. The pawn taking damage while shifted is the same pawn that carries
the injuries afterward, which replaces the `Druidkin_ResidualWounds` proxy entirely.

## Stat derivation

At shift time, read `form.pawnKind.race` and copy the animal's melee tools, move speed, and body size.
Multiply the result by a `statBoostFactor` carried on the form def, so a druid's bear form is stronger
than a wild bear.

Adding a form remains a single `PawnKindDef` reference. Forms defined by other animal mods work
without new entries, which a hand-authored stat table could not offer.

## Rendering

*Revised after investigation. The original text is corrected on two points, both recorded here
because the corrections are the useful part.*

The hediff carries `renderNodeProperties`, but it cannot point at a texture path: the animal is
chosen at runtime and XML cannot name it. The entry names a `nodeClass` instead, which resolves the
graphic from the form's `PawnKindDef`. Animal graphics live per life stage rather than on the
`ThingDef`, so the lookup goes through the adult life stage.

**The Anomaly precedent does not hold.** `Hediffs_Mutants.xml` only ever adds overlays on top of a
human that still draws; vanilla has no mechanism anywhere for rendering a humanlike pawn as
something else. Reading that file as support for appearance replacement was wrong.

Suppression instead reuses `RenderSkipFlagDef`, the draw-time mechanism apparel uses to hide hair
under a helmet. A flagged node is dropped before drawing and takes its children with it, so skipping
`Body` and `Head` is sufficient for the whole human — apparel parents hang off both, and hair,
beard, eyes and tattoos off `Head`. One Harmony postfix on the private `PawnRenderTree.AdjustParms`
ORs those flags in while the hediff is present. The carried-thing node is a root sibling and
survives, which is wanted.

Two things remain unverified until the mod is run: whether portraits route through `AdjustParms`,
and the pawn's shadow.

## Code removed

The redesign deletes more than it adds. The following become dead and should be removed rather than
left in place:

- `Patches/ThinkTree_AnimalDraft.xml` and `ThinkNode_ConditionalWildShapedDrafted`
- `Patch_ShowDraftGizmo`
- `Patch_AnimalDesignators`, since a shifted druid never appears in the Animals tab
- `TrainFully`, `ApplyResidualWounds`, and all `Find.WorldPawns` handling in `WildShapeUtility`
- The `[Druidkin][DIAG]` instrumentation, once both open questions are closed

## Risks

**Render suppression is unproven.** Drawing the animal is well supported; hiding the human underneath
is not yet demonstrated. If it proves unworkable, the fallback is a stylised treatment that keeps the
human silhouette with a visual tell. This risk is the reason visuals come last.

**Animal tools may not re-host onto a human body.** A `Tool` can name a `linkedBodyPartsGroup` that
exists on an animal body and not on a human one. Mitigation is a mapping layer, or dropping the body
part group so the tool is body-agnostic. This needs checking early, since it affects whether
derivation is viable at all.

## Implementation order

Mechanics precede visuals, so that a working and unbroken combat form exists even if rendering forces
a compromise.

1. Hediff holding form state, gear stashing, and timed reversion on the same pawn.
2. `capMods` blocking work and ranged attacks; confirm the pawn is still draftable.
3. Tool derivation and re-hosting, including the body-part-group question.
4. `StatPart` for move speed and durability, with the boost factor.
5. Rendering.
6. Removal of the dead pawn-swap code.

## Testing

Each stage is verified in-game, since the mod cannot be exercised by an automated harness. The player
log at `~/snap/steam/common/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
carries def config errors at load and exceptions during play, and is the primary evidence channel.

The checks that matter, in order of the stages above: the pawn stays in the player faction across a
full shift and revert; the draft gizmo appears without any patching; work is refused while shifted;
gear returns intact; and the shift survives a save and reload.
