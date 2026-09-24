# Wild Shape progression: a per-druid talent tree

Supersedes `2026-09-21-wild-shape-progression-design.md` and its plan. The level-to-power curve and
the snapshot-at-shift-start rule carry over from that spec; research gating and the `SkillDef` do not.

## Problem

Every druid can become a megasloth on the day they are born. `Dialog_ChooseAnimalForm` lists
`DefDatabase<DruidkinAnimalFormDef>.AllDefsListForReading` without a filter, so the six forms are all
available at once and there is nothing to earn.

The previous spec answered this with colony research for unlocks and a vanilla skill for power. That
makes every druid in a colony identical apart from one number. This spec makes progression personal
instead, in the shape Vanilla Psycasts Expanded uses: a druid levels up, earns points, and spends them
in a tree shown in its own inspect tab. Two druids in one colony can end up with different forms and
different strengths.

## Goals

- Each druid has their own level, XP and set of learned nodes.
- Levelling grants points; points buy forms, per-form upgrades and druid-wide perks.
- XP comes from shifting, from fighting while shifted, and from handling animals as a human.
- A dedicated inspect tab shows level, XP, points and the tree, and is where learning happens.
- Every tuning number lives in XML.

## Non-goals

- Colony research. No `ResearchProjectDef`s; levels and points are the only gate.
- A vanilla `SkillDef`. No passions, no skill decay, no Skills tab row.
- Refunds or respecs. Points spent are spent.
- Meditation or a work job that trains wild shaping at home.
- Retroactive progression for existing saves. See Save compatibility.

## Architecture

| Component | Responsibility |
| --- | --- |
| `Gene_Druid : Gene` | Holds and saves level, XP and learned nodes; the only entry point for XP and learning |
| `WildShapeProgressionExtension` | `DefModExtension` on the gene def holding every tuning number |
| `WildShapeNodeDef : Def` | One purchasable node: cost, level gate, prerequisites, tree position, effects |
| `NodeEffect` and subclasses | What a learned node does: unlock a form, upgrade a form, or apply a perk |
| `HediffComp_WildShapeXp` | Awards XP for time shifted and damage taken while shifted |
| Harmony postfixes | `Thing.TakeDamage` for damage dealt; `SkillRecord.Learn` for animal handling |
| `ITab_Pawn_WildShape` | The progression pane: header, tree, learning |
| `Hediff_WildShapeForm` snapshot | Mastery factor and learned nodes as they stood when the shift began |

Progression state lives on the gene because the gene already means "this pawn is a druid". The tab
is visible exactly when the gene is, losing the gene loses the progression with it, and vanilla
already saves genes, so there is no new game component and no second "is a druid" check to keep in
sync.

## Gene_Druid

Set as `geneClass` on the existing `Druidkin_WildShape` GeneDef. No new gene.

Saved state:

- `int level`, 0 to 20.
- `float xp`, progress toward the next level.
- `List<WildShapeNodeDef> learned`, scribed with `LookMode.Def`.

Unspent points are derived, never stored: `pointsPerLevel * level - sum(cost of learned)`. A stored
counter could drift from the learned list; a derived one cannot.

Public surface:

- `GainXp(float amount)` adds XP and levels up while XP covers the next threshold, capped at 20.
  Each level-up sends a positive message: "Aria reached wild shape level 4. 1 point to spend."
- `Knows(DruidkinAnimalFormDef form)`: true if any learned node's effect unlocks it.
- `CanLearn(WildShapeNodeDef node, out string reason)`: already learned, level too low, missing
  prerequisite, or not enough points. The reason is the tab's tooltip.
- `Learn(WildShapeNodeDef node)`: checks `CanLearn` and adds the node.
- Aggregators over learned effects: `ShiftDurationTicks(int baseTicks)`,
  `CooldownTicks(int baseTicks)`, `NotifyReverted()`.

On `PostAdd`, and on load when `learned` is null, every node with `cost 0` and `minLevel 0` is learned
automatically. That is how the rat stays free on day one.

## WildShapeProgressionExtension

```xml
<modExtensions>
  <li Class="Druidkin.WildShapeProgressionExtension">
    <pointsPerLevel>1</pointsPerLevel>
    <maxLevel>20</maxLevel>
    <xpToNextLevel>        <!-- x: current level, y: XP to reach the next -->
      <points>
        <li>(0, 1000)</li>
        <li>(19, 8000)</li>
      </points>
    </xpToNextLevel>
    <masteryFactorByLevel>
      <points>
        <li>(0, 0.80)</li>
        <li>(10, 1.00)</li>
        <li>(20, 1.30)</li>
      </points>
    </masteryFactorByLevel>
    <xpPerDayShifted>600</xpPerDayShifted>
    <xpPerDamageDealt>4</xpPerDamageDealt>
    <xpPerDamageTaken>2</xpPerDamageTaken>
    <animalsXpFraction>0.5</animalsXpFraction>
  </li>
</modExtensions>
```

Every number here is an opening position for tuning in play. The mastery curve is anchored at 1.00
for level 10, as in the previous spec, so the hand-tuned `statBoostFactor` values in
`AnimalForms_Druidkin.xml` keep their meaning.

## Nodes

```csharp
public class WildShapeNodeDef : Def
{
    public int cost = 1;
    public int minLevel;
    public List<WildShapeNodeDef> prerequisites;
    public Vector2 treePosition;          // grid cell in the tab
    public List<NodeEffect> effects;
}
```

`effects` is a polymorphic list, `<li Class="...">`, the pattern vanilla uses for comps. `NodeEffect`
is an abstract class whose hooks all default to doing nothing:

| Hook | Used by |
| --- | --- |
| `UnlocksForm(DruidkinAnimalFormDef form)` | `NodeEffect_UnlockForm` |
| `ApplyToStage(DruidkinAnimalFormDef form, HediffStage stage)` | `NodeEffect_FormUpgrade` |
| `ModifyShiftDuration(ref float ticks)` | `NodeEffect_Perk` |
| `ModifyCooldown(ref float ticks)` | `NodeEffect_Perk` |
| `OnRevert(Pawn pawn)` | `NodeEffect_Perk` |

- `NodeEffect_UnlockForm` names one `DruidkinAnimalFormDef`.
- `NodeEffect_FormUpgrade` names a form, a `StatDef`, and an `offset` or `factor`. It appends a
  `StatModifier` to the stage only when the shift is into that form.
- `NodeEffect_Perk` carries `shiftDurationFactor`, `cooldownFactor` and `healOnRevert`, all defaulting
  to neutral, so one class covers the three starter perks. A perk needing new behaviour gets its own
  subclass.

Callers never check an effect's type. The gene loops and calls hooks; a new kind of effect is a new
subclass with no change to the gene, the hediff or the tab.

`ConfigErrors` rejects a node with no effects, a negative cost, or a prerequisite cycle.

### Starter content

Fifteen nodes in `Defs/WildShapeNodeDefs/WildShapeNodes_Druidkin.xml`, prefixed `Druidkin_Node_`.
The whole tree costs 24 points against 20 available at level 20, so a druid cannot take everything
and builds differ. Upgrades and perks cost 2 so that deepening a form competes with learning the next
one.

```text
Forms (cost 1 unless noted)
  Rat              cost 0, level 0 (auto-learned)
  Timber wolf      level 2,  requires Rat
  Cougar           level 4,  requires Timber wolf
  Muffalo          level 6,  requires Rat
  Grizzly bear     level 8,  requires Muffalo
  Megasloth        level 12, requires Grizzly bear, cost 2

Form upgrades (cost 2, one each, requires its form)
  Rat, Timber wolf, Cougar, Muffalo, Grizzly bear, Megasloth

Perks (cost 2)
  Enduring shape   shift duration x1.5,  level 3
  Swift return     cooldown x0.5,        level 5
  Mending revert   heals on revert,      level 10
```

The upgrade stats are chosen when the defs are written, one stat per form matching its role (speed
for wolf and cougar, armour for bear and muffalo, and so on).

## XP sources

Every source calls `Gene_Druid.GainXp`, and every rate comes from the extension.

**Time shifted.** `HediffComp_WildShapeXp` on the shift hediff awards
`xpPerDayShifted * 250 / 60000` every 250 ticks rather than every tick. The ability's `durationTicks`
is 60000, so `xpPerDayShifted` reads as "XP from one full-length shift".

**Damage taken while shifted.** The same comp's `Notify_PawnPostApplyDamage` awards
`xpPerDamageTaken` per point of damage.

**Damage dealt while shifted.** A postfix on `Thing.TakeDamage` awards `xpPerDamageDealt` per point of
`DamageResult.totalDamageDealt`, when the instigator is a pawn with the shift hediff and the target is
a pawn outside the druid's faction. The faction filter stops sparring with colonists or colony
animals from becoming an XP farm. The postfix runs on every damage event in the game, so it returns
on the first failed type check before touching anything else.

**Animal handling.** A postfix on `SkillRecord.Learn`: when the skill is `Animals` and the pawn has
`Gene_Druid`, `animalsXpFraction` of the XP goes to wild shaping. One hook covers taming, training,
milking, shearing, and any other mod's work that trains Animals. This is also how a druid kept home
still progresses.

## Applying learned nodes

**Snapshot.** `Hediff_WildShapeForm.PostAdd` stores two things, both scribed:

- `masteryFactor`, read off `masteryFactorByLevel` at the druid's current level.
- `snapshotNodes`, a copy of the gene's learned list, scribed by def reference.

Defs are stored rather than computed `StatModifier`s because `StatModifier` is not `IExposable`;
recomputing from defs on load is cheap. As before, the snapshot belongs in `PostAdd`, which runs when
a shift begins and not when a save loads, so a saved shift is never re-rolled against a later level. A
level or node gained mid-shift applies to the next shift.

**Stage.** `BuildStage` multiplies its existing `boost` local by `masteryFactor`, then calls
`ApplyToStage` for every effect of every snapshot node. `MoveSpeed` and `IncomingDamageFactor` pick
up mastery from the one multiplication, as in the previous spec.

**Duration.** The shift's `ticksRemaining` comes from `gene.ShiftDurationTicks(Props.durationTicks)`
rather than the raw prop.

**Cooldown.** After a successful cast, `CompAbilityEffect_WildShape` restarts the ability's cooldown at
`gene.CooldownTicks(base)`.

**Revert.** The hediff's removal path calls `gene.NotifyReverted()`, which runs `OnRevert` on learned
effects. This reads the live learned list, not the snapshot, since the revert is its own moment.

## UI

### ITab_Pawn_WildShape

Added by an XML patch to `Human`'s `inspectorTabs`. `IsVisible` is true only when the selected pawn
has `Gene_Druid`.

```text
┌ Wild Shape ─────────────────────────────────────────────┐
│ Aria · Level 7   [████████░░░░] 1,340 / 2,000 XP         │
│ Power x0.94                    Points: 2 unspent         │
├─────────────────────────────────────────────────────────┤
│  FORMS                         PERKS                     │
│  (Rat)──(Wolf)──(Cougar)       [Enduring shape]          │
│    │      └─[Wolf upgrade]     [Swift return]            │
│  (Muffalo)──(Grizzly)          [Mending revert]          │
│    │            └─[Bear upgrade]                         │
│  (Megasloth) level 12, locked                            │
└─────────────────────────────────────────────────────────┘
```

- **Header:** level, XP bar, current power multiplier, unspent points.
- **Tree:** nodes drawn at `treePosition`, with a line to each prerequisite. A border colour marks
  form, upgrade and perk.
- **Node states:** learned (filled), learnable (highlighted), locked (greyed). The tooltip shows the
  description, cost, and when locked, the reason from `CanLearn`.
- **Learning:** clicking a learnable node opens a confirmation, since there are no refunds.

### Dialog_ChooseAnimalForm

Lists only forms the druid `Knows`. Unlearned forms belong in the tab, not greyed out in the picker,
so the picker stays quick. The header shows the druid's level and power multiplier.

### Forms from other mods

A form with no node unlocking it cannot be learned. On load, a log warning lists every such form, so
the gap is visible instead of silent. Adding one is a single node def in the other mod or a patch.

## Save compatibility

Migration is a no-op by construction.

- An existing shift hediff has no scribed `masteryFactor` or `snapshotNodes`, so it loads with 1.0
  and no nodes and behaves exactly as it does today.
- An existing druid's gene has no progression state, so it loads at level 0 with the auto-learned
  nodes, which means the rat only.
- A druid saved mid-shift in a form they have not learned keeps it until they revert.

Existing druids lose access to forms they could use before. This is accepted rather than solved.

## Risks

**Harmony signatures are unverified.** `Thing.TakeDamage`, `SkillRecord.Learn`,
`HediffComp.Notify_PawnPostApplyDamage` and `Ability.StartCooldown` must be checked against the 1.6
assemblies before the stages that use them.

**`TakeDamage` is hot.** The postfix sees every damage event. A type check and early return should
make it negligible; if profiling says otherwise, move damage-dealt XP onto the druid's own verbs.

**Animal-handling XP may outpace shifting.** A dedicated handler trains Animals all day. The
`animalsXpFraction` knob exists for this, and whether it needs lowering is a question for play.

**No refunds is harsh if node tuning is wrong.** A bad upgrade number strands a player's point. A
dev-mode "reset druid" action would cover this during testing without adding a respec to the game.

**Tree drawing is new UI code** with no vanilla widget to lean on. Keeping positions on a coarse grid
in XML keeps the drawing to boxes and straight lines.

## Implementation order

1. **Verify the existing README checklist first.** Progression layered onto unverified mechanics means
   a wrong grizzly number could be the mastery curve, an upgrade, or `BuildStage`, with no way to tell.
2. `Gene_Druid`, the extension, `WildShapeNodeDef`, `NodeEffect_UnlockForm`, the six form nodes, and
   the picker filtering on `Knows`. A new druid offers only the rat. Includes a dev-mode action to add
   XP, so later stages can be tested without grinding.
3. `ITab_Pawn_WildShape`: header, tree, learning.
4. XP sources: the hediff comp, then the two postfixes.
5. Snapshot, mastery factor in `BuildStage`, `NodeEffect_FormUpgrade` and the six upgrade nodes.
6. `NodeEffect_Perk`, the three perks, and the duration, cooldown and revert hooks.
7. Tune the extension's numbers and the upgrade values in play.

Stages 2 and 3 together already make the tab usable with forms only.

## Testing

In-game, appended to the README checklist, with the player log as the primary evidence channel.

- A fresh druid has only the rat in the picker; the tab shows the rest locked with reasons.
- The tab appears for druids and not for other colonists.
- Dev-mode XP levels a druid up, a message appears, and points go up by one per level.
- Learning a form spends a point and adds it to the picker; locked nodes cannot be clicked.
- A full shift, a fight while shifted, and taming as a human each raise XP; sparring with a colonist
  does not.
- A rat at level 0 is measurably weaker than at level 20 in move speed and damage taken.
- A form upgrade changes its stat only while in that form.
- Each perk changes duration, cooldown, or health on revert as described.
- A shift saved and reloaded keeps the power and upgrades it started with.
- A pre-update save loads without errors, and its druids start at level 0 with the rat.
