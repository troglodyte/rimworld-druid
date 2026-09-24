# Druidkin - Wild Shape

RimWorld 1.6 mod (Biotech required).
Adds a "Druid" xenotype whose gene grants a Wild Shape ability: take on an animal's shape for combat, keep your gear safely stashed, and shift back whenever you like.

## Status

Builds clean against the real RimWorld 1.6.4871 assemblies, verified on this machine at `~/snap/steam/common/.local/share/Steam/steamapps/common/RimWorld`.
Every Def field name and API signature used here was read out of `Assembly-CSharp.dll` metadata and cross-checked against the vanilla Defs in `Data/Core`, `Data/Biotech` and `Data/Anomaly`.

**Nothing here is yet tested in-game.** Mechanics and rendering are both implemented and both unverified in play.
See [Testing checklist](#testing-checklist) for what to exercise and [Known gaps](#known-gaps) for what is missing.

The mod is symlinked into the local Mods folder:

```text
~/snap/steam/common/.local/share/Steam/steamapps/common/RimWorld/Mods/Druidkin
  -> ~/code/rimworld-mods/Druidkin
```

## How it works

The druid is never replaced by another pawn.
The same `Pawn` object stays on the map, in the player faction, for the whole shift, which is why drafting, faction and designator handling need no patching at all.
A single hediff carries the entire transformed state, and removing it ends the transformation by any route.

- `GeneDef Druidkin_WildShape` grants `AbilityDef Druidkin_WildShapeAbility`.
- `XenotypeDef Druidkin_Druid` bundles that gene into a selectable xenotype at character creation, or via a xenogerm later.
- Using the ability opens a form picker.
  Picking a form strips the druid's weapon, worn apparel and inventory into `Hediff_WildShapeForm` (not dropped on the ground) and applies that hediff to the druid.
- The hediff holds the chosen form, the stashed gear, and the remaining duration.
  `HediffComp_WildShapeVerbs` sources melee attacks from the animal, and the hediff's stage supplies move speed and durability.
- Reverting — by the gizmo or by the timer running out — removes the hediff, and `PostRemoved` hands the gear back.
  Both routes are the same code path.

Two Harmony patches remain, both small.
A postfix on `Pawn.GetGizmos` adds the "revert to human" button, since the ability itself is unavailable mid-shift.
A postfix on `EquipmentUtility.CanEquip` refuses equipment while shifted, so a bear cannot be ordered to pick up a rifle.

### Deriving the animal

Everything that differs per form is computed at shift time from the animal's own defs, so adding a form stays a single `PawnKindDef` reference and forms from animal mods work with no new code.
`statBoostFactor` on the form def scales all of it, so a druid's bear beats a wild bear.

| Aspect | Source |
| --- | --- |
| Melee attacks | The animal's `tools`, copied and rescaled by `statBoostFactor` |
| Move speed | The animal's `MoveSpeed` as a factor on the druid's own species base |
| Durability | `IncomingDamageFactor`, scaled by the ratio of the two races' health scales |
| Work and equipment | Fixed for every form, so they live in XML on the hediff stage |

## Design choices worth knowing about

**Work is blocked by work tag, not by zeroing Manipulation.**
Zeroing Manipulation is the obvious lever and the wrong one: `MeleeHitChance` takes a capacity offset from Manipulation (scale 12, max 1.5), so zeroing it would cripple the combat form the mod exists to create.
`WorkTags.AllWork` is its own flag and does not imply `Violent`, so the druid stays able to fight while unable to work.

**Per-form numbers come from an overridden `CurStage`, not a `StatPart`.**
`Hediff.CurStage` is virtual, so the stage does not have to come from static XML.
A `StatPart` would need a patch into each vanilla `StatDef`'s `parts` list, would run for every pawn's stat calculation in the game, and could not express capacity mods or damage factors at all, which live on the stage.
The stage builder reads species values off `ThingDef`s rather than off the pawn, because asking the pawn for a stat would re-enter the stat calculation that consulted `CurStage` in the first place.

**Durability is incoming damage, not health scale.**
A pawn's body part hit points are fixed when the pawn is generated, from `RaceProperties.baseHealthScale`, so no hediff can raise them afterwards.
Scaling incoming damage by the same ratio is the reachable equivalent of a tougher body.

**Animal tools are copied, never referenced.**
An animal's tools are pinned to body part groups its own body has — `FrontLeftPaw` on a bear — which a human body lacks, so the link is dropped to make each tool body-agnostic.
That edit is made on a field-by-field copy: the originals belong to the animal's shared `ThingDef`, and mutating them would retune every wild animal of that species for the rest of the game.

**Injuries need no special handling.**
The pawn that takes damage while shifted is the same pawn that carries the injuries afterwards.
This replaced the old `Druidkin_ResidualWounds` proxy hediff entirely.

**The human is hidden with skip flags, not by rebuilding the render tree.**
Vanilla has no mechanism for rendering a humanlike pawn as something else, and the Anomaly mutant hediffs that look like precedent are not: they only add overlays on top of a human that still draws.
What vanilla does have is `RenderSkipFlagDef`, the mechanism apparel uses to hide hair under a helmet.
A flagged node is dropped before it draws and takes its children with it, so skipping `Body` and `Head` removes the whole human: apparel hangs off both, and hair, beard, eyes and tattoos hang off `Head`.
The carried-thing node is a root sibling and survives, so a hauled item stays visible.

**The animal graphic comes from a node class, not a `texPath`.**
Same constraint that forced `CurStage` to be overridden: XML cannot name an animal that is chosen at runtime.
An animal's graphic also lives per life stage rather than on its `ThingDef`, so a form draws from its last life stage - the adult - rather than whatever a pup would look like.

**Duration** defaults to 60,000 ticks (one in-game day) per shift, set on `CompProperties_AbilityWildShape.durationTicks` in `Defs/AbilityDefs/Abilities_Druidkin.xml`.
The ability cooldown is a separate and much shorter 2,500 ticks.
Tune both to taste.

## Building

```bash
export PATH="$HOME/.dotnet:$PATH"
cd Source/Druidkin
dotnet build
```

The csproj references RimWorld's managed DLLs directly via `RimWorldManagedDir` in `Druidkin.csproj`; update that path if RimWorld is ever reinstalled elsewhere.
A post-build step copies the built DLL straight into `Assemblies/`.

## Automated Unit Tests

A suite of 28 NUnit tests exercises the progression mechanics, curve calculations, level thresholds, unspent points logic, talent tree graph validation (cycle detection, prerequisites, gates), polymorphic node effects, and XML starter content on disk.

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test Source/Druidkin.Tests
```

## In-game testing checklist

Interactive UI rendering and engine hooks must be confirmed in-game.
The player log at `~/snap/steam/common/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log` carries def config errors at load and exceptions during play, and is the primary evidence channel.

1. Enable **Druidkin - Wild Shape** alongside Biotech, restart, and check the log for def config errors before loading a save.
2. Create a pawn with the **Druid** xenotype, give them a weapon, apparel and something in inventory. Set distinctive work priorities - say Cooking 1, Doctor 2, Hauling 4 - and write them down, so steps 11 to 13 have something to check against.
3. Shift into the grizzly bear form and confirm the pawn stays a player-faction colonist: still on the colonist bar, still selectable, no hostility.
4. **Confirm the draft gizmo appears**, with no think-tree or gizmo patching involved. This is the defect that drove the redesign.
5. Confirm the work tab refuses everything, and that ordering the druid to equip a weapon is refused with the "cannot hold equipment" message.
6. Draft the druid and attack something. Confirm bear claws are used rather than fists, and that hit chance is not obviously crippled.
7. Confirm move speed and incoming damage differ between the rat and megasloth forms.
8. Save and reload mid-shift, then confirm the form, gear, remaining duration and melee tools all survive.
9. **Confirm the druid is drawn as the animal**, with no human body, head, hair or apparel showing underneath, from all four facings.
10. Check the colonist bar portrait and the selection shadow, both of which are expected trouble spots rather than certainties.
11. Revert via the gizmo and confirm weapon, apparel and inventory all come back intact, and that the human is drawn again. **Confirm the work priorities from step 2 are back**, and that the log carries no "Tried to change priority on disabled worktype" error - that error means the restore ran while the work types were still disabled and silently did nothing.
12. Shift again and let the timer expire on its own. Confirm the gear and the work priorities both return the same way.
13. Down the druid while shifted and confirm the gear is not lost.
14. Shift, save and reload mid-shift, then revert. Confirm the work priorities survive the round trip through the save file, not just the in-memory shift.
15. Check that a fresh druid starts at Wild Shape level 0 and has only the **rat** form available in the choose form dialog.
16. Select a druid pawn and verify that the **Wild Shape** inspect tab is visible. Select a non-druid pawn and confirm the tab is hidden.
17. In the Wild Shape inspect tab, verify the header displays pawn name, level (0), XP bar (0 / 1,000), power multiplier (x0.80), and unspent points (0).
18. Use Dev Mode gizmo to add XP / level up. Confirm a positive message appears ("... reached wild shape level 1. 1 point to spend."), unspent points increase by 1 per level, and power multiplier scales according to the mastery curve.
19. In the talent tree, click on the **wolf shape** node (level 2, requires rat, cost 1). Confirm a confirmation prompt appears warning that points cannot be refunded. Confirm learning, verify points decrement, wolf form node turns green (Learned), and the choose form dialog now includes timber wolf.
20. Confirm that locked nodes (e.g., cougar before wolf is learned, or megasloth at level < 12) cannot be learned and their tooltip clearly indicates the reason (level, prerequisite, or insufficient points).
21. Verify XP generation sources:
    - **Time shifted**: observe XP incrementing every 250 ticks while shifted (`xpPerDayShifted`).
    - **Damage taken**: take damage in animal form, confirm XP increases by `xpPerDamageTaken` per damage point.
    - **Damage dealt**: attack a hostile pawn (outside faction), confirm XP increases by `xpPerDamageDealt` per damage dealt. Confirm sparring with a colonist or colony animal does not award XP.
    - **Animal handling**: as human, tame or train an animal, confirm wild shape XP increases by `animalsXpFraction` of the Animals XP earned.
22. Test form upgrades: Learn a form upgrade node (e.g., Wolf MoveSpeed +0.6 c/s or Grizzly Bear Sharp Armor +25%). Shift into that form and confirm the stat offset appears in the stat inspector. Shift into a different form or revert to human and confirm the upgrade stat applies only while in the upgraded form.
23. Test perks:
    - **Enduring shape**: Confirm shift duration increases by x1.5 (90,000 ticks).
    - **Swift return**: Confirm ability cooldown is reduced by x0.5 (1,250 ticks).
    - **Mending revert**: Injure the pawn while shifted, revert to human, and confirm up to 20 injury hit points are healed upon revert.
24. Save and reload mid-shift: Confirm snapshotted mastery power factor and form upgrades persist across save/reload without re-rolling against current level. Load a pre-update save and confirm druids start cleanly at level 0 with the rat form.

## Known gaps

- **Work-priority restoration is implemented but unverified in-game.** `AllWork` on the hediff stage disables every work type, and vanilla zeroes a work type's priority as it becomes disabled rather than remembering it, so the hediff takes the priorities into custody at the shift and writes them back in `PostRemoved`. The open question is ordering: if the pawn's disabled-work cache has not refreshed by the time `PostRemoved` runs, every `SetPriority` call is rejected with a logged error and the restore does nothing. `Pawn.Notify_DisabledWorkTypesChanged()` is called first to force that refresh; steps 11 and 14 are what prove it.
- **Rendering is implemented but unverified in-game.** The two things worth watching are the colonist-bar portrait, which may still show a human because portraits may not route through `AdjustParms`, and the shadow, which is still the human's. Neither blocks the combat form.
- The gene, xenotype and ability icons under `Textures/` are placeholder 128x128 PNGs. Overwrite them in place; the xenotype icon should stay a white silhouette, since the UI tints it.
- Six forms are wired up: rat, timber wolf, cougar, grizzly bear, muffalo and megasloth. Add more by dropping `Druidkin.DruidkinAnimalFormDef` entries into `Defs/DruidkinAnimalFormDefs/`.
