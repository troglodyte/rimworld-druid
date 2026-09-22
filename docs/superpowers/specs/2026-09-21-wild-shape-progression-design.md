# Wild Shape progression: research gates and a mastery skill

## Problem

Every druid can become a megasloth on the day they are born. `Dialog_ChooseAnimalForm` lists
`DefDatabase<DruidkinAnimalFormDef>.AllDefsListForReading` without a filter, so the six forms are all
available at once and the `statBoostFactor` spread between a rat and a megasloth is a pure dominance
ordering rather than a curve anyone climbs. There is nothing to earn and nothing to look forward to.

The gene already promises otherwise. Its description says a druid takes "the form of an animal they
have attuned to", so the fiction of a form being earned is written and unimplemented.

## Goals

Two independent gates, answering two different questions.

- **Which forms exist** is a colony question, answered by research, shared by every druid.
- **How strong a form is** is a personal question, answered by a per-druid skill.

Neither gate needs the other to ship, and the second is worthless without the first only in the sense
that a colony with one form has a short menu.

## Non-goals

- Per-form mastery. One skill covers every form; a druid does not level their bear separately.
- Scaling shift duration or ability cooldown. Mastery moves power, nothing else.
- Per-pawn attunement as a second unlock step. Research is the only unlock gate.
- New artwork, and no new UI window. Both gates surface in windows that already exist.
- Retroactive progression for existing saves. See Save compatibility.

## Architecture

| Component | Responsibility |
| --- | --- |
| `ResearchProjectDef` per gated form | Colony-wide unlock, using vanilla's own persistence and UI |
| `requiredResearch` on `DruidkinAnimalFormDef` | Names the project a form waits on; null means available from the start |
| `SkillDef Druidkin_WildShaping` | The per-druid level, with vanilla supplying XP, passions and the Skills tab |
| `HediffComp_WildShapeMastery` | Awards XP while shifted and reads the level to a power factor |
| `masteryFactor` on `Hediff_WildShapeForm` | The factor as it stood when this shift began |

No new tick path, no new saved game component, and no new class holding progression state. Both gates
attach to structures the mod already has.

## Research gating

One project per gated form, rather than tiered projects covering several. A project maps one-to-one
onto the def it unlocks, a mod adding its own form declares its own project without touching this
one, and six discrete unlock moments give the player more decisions than three do.

Rat stays free. A druid with no research behind them should still be able to shift on the day the
gene shows up, and the rat's `statBoostFactor` of 1.4 already makes it the form that stays useful
longest per point of investment.

The remaining five chain by power, so the megasloth sits behind real investment:

Research defNames take a `Druidkin_Research_` prefix, so that a project is never mistaken for the
`Druidkin_Form_` def it unlocks:

```text
Druidkin_Research_TimberWolf   500   (no prerequisite)
  |-- Druidkin_Research_Cougar        900
  |     `-- Druidkin_Research_GrizzlyBear   1600 --.
  `-- Druidkin_Research_Muffalo       1200 --------+-- Druidkin_Research_Megasloth   2800
```

The trailing numbers are opening `baseCost` values. They live in
`Defs/ResearchProjectDefs/Research_Druidkin.xml`, with no `<tab>`, which puts them on the main
research tab. `researchViewX` and `researchViewY` are laid out when the defs are written, since they
only matter relative to each other and to whatever else shares that tab.

`DruidkinAnimalFormDef` gains one field:

```csharp
public ResearchProjectDef requiredResearch;
```

Null is the meaningful default: a form with no project named is available immediately. That makes the
field safe to omit, so forms added by other mods work unchanged rather than becoming unreachable, and
it is what keeps the rat free without a special case.

`ConfigErrors` needs no addition. A named project that does not exist is already a def-resolution
error that RimWorld reports at load.

## The mastery skill

A thirteenth `SkillDef`, `Druidkin_WildShaping`, in `Defs/SkillDefs/Skills_Druidkin.xml`.

Vanilla's twelve core skills use `listOrder` 10 through 120, so 130 places wild shaping last in the
Skills tab. `pawnCreatorSummaryVisible` is false, since a skill that only matters to gene carriers
does not belong in the pawn creator's summary line. `neverDisabledBasedOnWorkTypes` is true, because
the skill maps to no work type and would otherwise read as disabled for everyone.

The reason for a real `SkillDef` rather than a hand-rolled counter is that the expensive half of a
progression system is the part vanilla gives away: twenty levels, XP thresholds, the learning-rate
curve, passions, the tab row, the tooltip, and save and load. What the mod writes is one call to
`Learn` and one curve lookup.

### XP

XP accrues only while shifted, at `xpPerDay / 60000` per tick. The ability's `durationTicks` is
already 60000, which is one in-game day, so `xpPerDay` reads directly as "XP earned by one
full-length shift" and can be tuned without arithmetic.

A shifted druid has every work type disabled and no gear, so idling in animal form already costs a
colonist's entire working day. The opportunity cost is the balance, which is why no anti-grind rule
is needed beyond it.

`Learn` is called without `direct`, so passion multipliers and vanilla's daily XP saturation both
apply. Saturation is a second brake on very long shifts, though its exact ceiling should be confirmed
in play rather than assumed.

The `2000` above is a placeholder. The real rate is calibrated at implementation by reading the level
thresholds off `SkillRecord`.
They could not be read while writing this spec, because no decompiler is installed and `strings` over
`Assembly-CSharp.dll` yields member names and string literals but not method bodies.

### Level to power

A `SimpleCurve` in XML on the comp properties, not constants in C#. The mod is entirely unverified in
play, so this number will be re-tuned many times, and a constant in C# makes each tune cost a build
and a game restart.

```xml
<li Class="Druidkin.HediffCompProperties_WildShapeMastery">
  <xpPerDay>2000</xpPerDay>
  <masteryFactorByLevel>
    <li>(0, 0.80)</li>
    <li>(10, 1.00)</li>
    <li>(20, 1.30)</li>
  </masteryFactorByLevel>
</li>
```

Level 10 is deliberately 1.00. Every `statBoostFactor` in `AnimalForms_Druidkin.xml` was tuned
against an unscaled form, so anchoring the midpoint at neutral keeps that tuning meaningful instead
of invalidating all six numbers at once. A novice is worse than the animal they are copying; a master
is half again better than today's balance. All three points are opening positions.

## Stat derivation

`BuildStage` already funnels both derived stats through one local:

```csharp
float boost = Mathf.Max(0.01f, form.statBoostFactor * masteryFactor);
```

That is the entire change. `MoveSpeed` scales by `boost` and `IncomingDamageFactor` divides by it, so
both pick up mastery from the single multiplication and neither needs to know the skill exists.

### Why the factor is snapshotted

`masteryFactor` is read once when the shift starts, stored on the hediff, and scribed. It is not
recomputed while shifted.

XP comes only from being shifted, so every level-up a druid ever has happens mid-shift. `CurStage` is
cached in `cachedStage` precisely because stat calculation consults it constantly, and a live read
would mean invalidating that cache from inside a level-up during stat calculation. The existing
comments in `Hediff_WildShapeForm` already warn about re-entering stat calculation from `BuildStage`.

Snapshotting costs one float and reads honestly: the power of a shape is fixed when you put it on. A
level earned mid-shift applies to the next one.

The snapshot belongs in `PostAdd`, which runs when a shift begins and not when a save is loaded,
before `RefreshDerivedState` clears the cached stage. Putting it in `ExposeData`'s post-load path
instead would silently re-roll a saved shift's power against the pawn's current level.

## UI

`Dialog_ChooseAnimalForm` draws locked forms greyed out and inactive rather than omitting them, with
a tooltip naming the project that would unlock them. A form the player cannot see teaches nothing; a
form they can see and cannot use is a research goal. `Widgets.ButtonText` takes an `active` argument,
so this needs no new widget.

The dialog header gains the druid's wild shaping level and the resulting multiplier, so the skill has
somewhere to visibly pay off at the moment the player is choosing a form.

`DruidkinDefOf` gains `SkillDef Druidkin_WildShaping`.

## Save compatibility

Migration is a no-op by construction. An existing save's hediff has no scribed `masteryFactor`, so it
loads as the 1.0 default and behaves exactly as it does today, and `requiredResearch` is null on
every form def until the research defs are added. Nothing needs converting and nothing breaks.

Druids in an existing save start at wild shaping 0, which makes their forms weaker than before rather
than stronger. This is accepted rather than solved.

## Risks

**A thirteenth skill appears on every human, not just druids.** Two separate problems wear this one
face. The cosmetic one is a row in the Skills tab for pawns who can never use it; the real one is
pawn generation spending a passion on it, because passions are a limited budget and a wasted one is a
balance change to every colonist in the game.

The passion half is worth fixing and is straightforward: a postfix on `PawnGenerator.GenerateSkills`
zeroing the skill and clearing its passion for pawns without the gene. Both that method and
`SkillUI.DrawSkillsOf` exist in 1.6 metadata.

The cosmetic half is not clearly worth fixing. `DrawSkillsOf` draws from a list it builds itself, so
suppressing one row means a transpiler or a reimplementation, which is a poor trade for a row of
text. The fallback is to accept the row. Nothing about it is load-bearing.

**Skill decay erodes mastery between shifts.** Vanilla skills above a threshold rust when unused, so
a druid who stops shifting gets worse at it. This is arguably correct — a druid should practise — but
it interacts with the fact that XP has exactly one source, and a druid kept home for a season loses
ground with no way to train. Whether to leave decay alone is a tuning question for play, not a
question this spec should answer.

**Research completed for a form def that later disappears** leaves a finished project unlocking
nothing. Harmless, and the inverse of the dangerous case.

## Implementation order

Research first. It is the half with no unverified assumptions in it, and it delivers a visible
progression system on its own, so if mastery stalls on the skill leakage above there is still
something shipped.

1. **Verify the existing checklist first**, including the new work-priority steps. Layering
   progression onto unverified mechanics means a wrong grizzly number could be the mastery curve or
   `BuildStage`'s health-scale maths, with no way to tell which.
2. `requiredResearch` field, the five research defs, and dialog gating with greyed rows.
3. `SkillDef`, `DruidkinDefOf` entry, and the `GenerateSkills` postfix. Confirm the skill appears and
   that non-druids are not spending passions on it.
4. `HediffComp_WildShapeMastery`, the `PostAdd` snapshot, and the one-line `BuildStage` change.
5. Tune `xpPerDay` and the curve in play.

## Testing

In-game, as with everything else in this mod, appended to the README checklist. The player log stays
the primary evidence channel.

The checks that matter, in the order of the stages above: a fresh colony offers only the rat, and the
other five rows are greyed with their project named; completing a project makes exactly its own form
available to every druid at once; a generated non-druid colonist has no passion in wild shaping; a
druid's wild shaping level rises across a full shift and not at all while human; the level shown in
the dialog header matches the Skills tab; a rat at level 0 is measurably weaker than a rat at level
20 in both move speed and damage taken; and a shift saved and reloaded keeps the power it started
with rather than re-reading the level.
