# Wild Shape Progression Implementation Plan

> **Superseded** by `2026-09-23-wild-shape-talent-tree-design.md`: per-druid talent tree, no research, no SkillDef.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gate which animal forms a colony can use behind research, and scale how strong each form is
by a per-druid wild shaping skill.

**Architecture:** Two independent gates on structures the mod already owns. A nullable
`requiredResearch` field on `DruidkinAnimalFormDef` plus one `ResearchProjectDef` per gated form
decides availability, checked in `Dialog_ChooseAnimalForm`. A thirteenth `SkillDef` decides power: a
new `HediffComp_WildShapeMastery` awards XP every tick the shift hediff is active, and the hediff
snapshots a curve-derived multiplier into the single `boost` local that `BuildStage` already funnels
both derived stats through.

**Tech Stack:** C# against .NET Framework 4.7.2, RimWorld 1.6.4871 (`Assembly-CSharp.dll` referenced
by `HintPath`), Harmony 2.x via annotation patches, RimWorld XML defs.

**Spec:** `docs/superpowers/specs/2026-09-21-wild-shape-progression-design.md`

## Global Constraints

- **RimWorld 1.6**, Biotech required. Build with `/home/trog/.dotnet/dotnet build` from
  `Druidkin/Source/Druidkin`; a post-build step copies the DLL into `Druidkin/Assemblies/`.
- **No automated test harness exists** and none is being added. See Verification Model below.
- **Def naming:** forms are `Druidkin_Form_<Animal>`, research projects are
  `Druidkin_Research_<Animal>`. Never mix the two prefixes.
- **All user-facing strings** go in `Languages/English/Keyed/Druidkin_Keys.xml` and are used via
  `.Translate()`. No literal strings in C#.
- **Harmony patches** are annotation-based `public static class` in namespace
  `Druidkin.HarmonyPatches`, one file per patch, matching `Patch_CanEquip.cs`. `PatchAll()` is already
  called in `DruidkinMod`.
- **Comment style:** `///` comments explain *why* a choice was made, not what the code does. Match the
  density of the surrounding files.
- `masteryFactorByLevel` is anchored at `1.0` for level 10, because every `statBoostFactor` in
  `AnimalForms_Druidkin.xml` was tuned against an unscaled form.

## Verification Model

This mod cannot be exercised by an automated harness, so the usual red/green cycle is replaced by two
gates that every task must pass:

1. **Compile gate.** `dotnet build` must report `0 Warning(s) 0 Error(s)`. This is a real test, not a
   formality: it is the only thing that confirms a RimWorld API member exists with the name, arity and
   accessibility assumed. During the work-priority fix it validated four member guesses at once.
2. **In-game gate.** Each task appends numbered steps to the Testing checklist in `Druidkin/README.md`
   and those steps are then performed. The player log at
   `~/snap/steam/common/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log` carries
   def config errors at load, Harmony patch failures at startup, and exceptions during play, and is
   the primary evidence channel.

A task is not complete until both gates pass. Do not proceed to the next task on a compile gate alone.

## File Structure

| File | Status | Responsibility |
| --- | --- | --- |
| `Defs/ResearchProjectDefs/Research_Druidkin.xml` | create | The five unlock projects and their prerequisite chain |
| `Defs/SkillDefs/Skills_Druidkin.xml` | create | The `Druidkin_WildShaping` skill |
| `Source/Druidkin/WildShape/HediffComp_WildShapeMastery.cs` | create | Awards XP while shifted; XML home for `xpPerDay` and the curve |
| `Source/Druidkin/HarmonyPatches/Patch_PawnGenerator_GenerateSkills.cs` | create | Stops pawn generation spending a passion on wild shaping for non-carriers |
| `Source/Druidkin/DruidkinAnimalFormDef.cs` | modify | Gains `requiredResearch` and `IsUnlocked` |
| `Source/Druidkin/WildShape/WildShapeUtility.cs` | modify | Gains `MasteryFactorFor`, the single curve lookup |
| `Source/Druidkin/WildShape/Hediff_WildShapeForm.cs` | modify | Snapshots the factor; applies it in `BuildStage` |
| `Source/Druidkin/WildShape/Dialog_ChooseAnimalForm.cs` | modify | Greys locked rows; shows the mastery summary |
| `Source/Druidkin/DruidkinDefOf.cs` | modify | Resolves the skill and gene defs |
| `Defs/HediffDefs/Hediffs_Druidkin.xml` | modify | Attaches the mastery comp and holds the curve |
| `Defs/DruidkinAnimalFormDefs/AnimalForms_Druidkin.xml` | modify | Names each form's research project |
| `Languages/English/Keyed/Druidkin_Keys.xml` | modify | Two new keys |
| `README.md` | modify | Checklist steps for each task |

---

### Task 1: Verify the baseline in play

No code. This task exists because everything after it is unfalsifiable without it: if a grizzly's
numbers come out wrong in Task 5, the cause could be the mastery curve or `BuildStage`'s existing
health-scale arithmetic, and there is no way to tell which if the baseline was never confirmed.

**Files:**
- Modify: `Druidkin/README.md` (only if the run reveals defects worth recording)

- [ ] **Step 1: Build the current tree**

```bash
cd /home/trog/code/rimworld-mods/Druidkin/Source/Druidkin && /home/trog/.dotnet/dotnet build
```

Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 2: Run the full existing checklist**

Work through all 14 steps of the Testing checklist in `Druidkin/README.md`. Steps 11 and 14 cover the
work-priority restoration, which is implemented but has never been observed working.

- [ ] **Step 3: Record the outcome**

For each step, note pass or fail. For step 11 specifically, confirm the log contains no
`Tried to change priority on disabled worktype` — that error means the restore ran while work types
were still disabled and silently did nothing, and the fix is to defer the restore by one tick.

- [ ] **Step 4: Stop if anything failed**

Fix baseline defects before starting Task 2. Report what failed rather than working around it.

- [ ] **Step 5: Commit any README corrections**

```bash
cd /home/trog/code/rimworld-mods
git add Druidkin/README.md
git commit -m "docs: record baseline in-game verification results"
```

---

### Task 2: Research gating

Availability only. No skill, no mastery. At the end of this task a fresh colony can shift into a rat
and sees the other five forms greyed out with the project that would unlock them.

**Files:**
- Modify: `Druidkin/Source/Druidkin/DruidkinAnimalFormDef.cs`
- Create: `Druidkin/Defs/ResearchProjectDefs/Research_Druidkin.xml`
- Modify: `Druidkin/Defs/DruidkinAnimalFormDefs/AnimalForms_Druidkin.xml`
- Modify: `Druidkin/Source/Druidkin/WildShape/Dialog_ChooseAnimalForm.cs`
- Modify: `Druidkin/Languages/English/Keyed/Druidkin_Keys.xml`
- Modify: `Druidkin/README.md`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `DruidkinAnimalFormDef.requiredResearch` (field, type `ResearchProjectDef`, nullable) and
  `DruidkinAnimalFormDef.IsUnlocked` (get-only `bool`). Task 5 does not use these; only the dialog does.

- [ ] **Step 1: Add the field and the unlock check**

In `DruidkinAnimalFormDef.cs`, add `using RimWorld;` to the using block, then insert after the
`statBoostFactor` field:

```csharp
        /// The project this form waits on. Null means available from the start, which is what
        /// keeps the rat free and what lets a form added by another mod work without that mod
        /// knowing this field exists. Making absence mean "locked" would silently break them.
        public ResearchProjectDef requiredResearch;
```

Then add beside the existing `Race` and `BodyGraphicData` properties:

```csharp
        public bool IsUnlocked => requiredResearch == null || requiredResearch.IsFinished;
```

- [ ] **Step 2: Build**

```bash
cd /home/trog/code/rimworld-mods/Druidkin/Source/Druidkin && /home/trog/.dotnet/dotnet build
```

Expected: `0 Error(s)`. A failure here means `ResearchProjectDef` or `IsFinished` resolved
differently than assumed; report the exact compiler message rather than guessing a replacement.

- [ ] **Step 3: Write the research defs**

Create `Druidkin/Defs/ResearchProjectDefs/Research_Druidkin.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!--
  One project per gated form. The rat has none, so a druid can shift on the day the gene
  appears rather than waiting on a research bench they may not have built.

  techLevel is Neolithic on every project deliberately. RimWorld multiplies research cost
  when a project's techLevel sits above the colony's own, so mixing levels here would make
  these prices swing with colony tech instead of following the flat curve baseCost sets.
-->
<Defs>

  <ResearchProjectDef>
    <defName>Druidkin_Research_TimberWolf</defName>
    <label>wolf shape</label>
    <description>Learn to hold the shape of a pack hunter: fast, lean, and built to run prey down over distance.</description>
    <baseCost>500</baseCost>
    <techLevel>Neolithic</techLevel>
    <researchViewX>0.0</researchViewX>
    <researchViewY>0.2</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>Druidkin_Research_Cougar</defName>
    <label>cougar shape</label>
    <description>Learn the shape of an ambush killer, trading the wolf's endurance for a burst of lethal speed.</description>
    <baseCost>900</baseCost>
    <techLevel>Neolithic</techLevel>
    <prerequisites>
      <li>Druidkin_Research_TimberWolf</li>
    </prerequisites>
    <researchViewX>1.0</researchViewX>
    <researchViewY>0.0</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>Druidkin_Research_Muffalo</defName>
    <label>muffalo shape</label>
    <description>Learn the shape of a herd beast: heavy, placid, and strong enough to carry a load as easily as it takes a hit.</description>
    <baseCost>1200</baseCost>
    <techLevel>Neolithic</techLevel>
    <prerequisites>
      <li>Druidkin_Research_TimberWolf</li>
    </prerequisites>
    <researchViewX>1.0</researchViewX>
    <researchViewY>0.4</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>Druidkin_Research_GrizzlyBear</defName>
    <label>bear shape</label>
    <description>Learn the shape of a bear, where the cat's precision gives way to mass and the simple authority of claws.</description>
    <baseCost>1600</baseCost>
    <techLevel>Neolithic</techLevel>
    <prerequisites>
      <li>Druidkin_Research_Cougar</li>
    </prerequisites>
    <researchViewX>2.0</researchViewX>
    <researchViewY>0.0</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>Druidkin_Research_Megasloth</defName>
    <label>megasloth shape</label>
    <description>Learn the shape of a megasloth, which asks the druid to hold something far larger than themselves and keep hold of it.</description>
    <baseCost>2800</baseCost>
    <techLevel>Neolithic</techLevel>
    <prerequisites>
      <li>Druidkin_Research_GrizzlyBear</li>
      <li>Druidkin_Research_Muffalo</li>
    </prerequisites>
    <researchViewX>3.0</researchViewX>
    <researchViewY>0.2</researchViewY>
  </ResearchProjectDef>

</Defs>
```

- [ ] **Step 4: Point each form at its project**

In `Druidkin/Defs/DruidkinAnimalFormDefs/AnimalForms_Druidkin.xml`, add one line to five of the six
forms, immediately after each `<statBoostFactor>` line. Leave `Druidkin_Form_Rat` untouched.

```xml
    <requiredResearch>Druidkin_Research_TimberWolf</requiredResearch>
```

```xml
    <requiredResearch>Druidkin_Research_Cougar</requiredResearch>
```

```xml
    <requiredResearch>Druidkin_Research_GrizzlyBear</requiredResearch>
```

```xml
    <requiredResearch>Druidkin_Research_Muffalo</requiredResearch>
```

```xml
    <requiredResearch>Druidkin_Research_Megasloth</requiredResearch>
```

Also extend the file's header comment to say that a form with no `requiredResearch` is available from
the start.

- [ ] **Step 5: Add the tooltip string**

In `Druidkin/Languages/English/Keyed/Druidkin_Keys.xml`, add before the closing `</LanguageData>`:

```xml
  <Druidkin_FormLockedBy>Locked. Requires research: {0}.</Druidkin_FormLockedBy>
```

- [ ] **Step 6: Grey the locked rows in the dialog**

In `Dialog_ChooseAnimalForm.cs`, replace the body of the `foreach` loop in `DoWindowContents` with:

```csharp
                Rect rowRect = new Rect(0f, y, inRect.width, 32f);
                bool unlocked = form.IsUnlocked;

                // A locked form is drawn rather than hidden. A form the player cannot see
                // teaches them nothing; one they can see and cannot use is a research goal.
                if (!unlocked)
                {
                    GUI.color = Color.gray;
                    TooltipHandler.TipRegion(rowRect,
                        "Druidkin_FormLockedBy".Translate(form.requiredResearch.LabelCap));
                }

                if (Widgets.ButtonText(rowRect, form.LabelCap, drawBackground: true,
                        doMouseoverSound: true, active: unlocked))
                {
                    if (WildShapeUtility.TryTransform(pawn, form, durationTicks, out string failReason))
                    {
                        Close();
                    }
                    else
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, historical: false);
                    }
                }

                GUI.color = Color.white;

                y += 36f;
```

`GUI.color` is global render state, so resetting it on every iteration rather than only after a locked
row is what stops one locked form greying every row beneath it.

- [ ] **Step 7: Build**

```bash
cd /home/trog/code/rimworld-mods/Druidkin/Source/Druidkin && /home/trog/.dotnet/dotnet build
```

Expected: `0 Error(s)`.

- [ ] **Step 8: Add the checklist steps**

Append to the Testing checklist in `Druidkin/README.md`:

```markdown
15. Start a fresh colony with a druid. Confirm the choose-form window offers **only the rat** as a usable button, and that the other five rows are greyed and inactive with a tooltip naming the research project each one needs.
16. Check the research tab. Confirm the five wolf-through-megasloth projects appear, that wolf shape has no prerequisite, and that megasloth shape sits behind both bear shape and muffalo shape.
17. Complete **wolf shape** via the dev-mode research finisher. Confirm the timber wolf row becomes usable, that cougar and muffalo stay locked, and that a second druid in the same colony also sees the wolf unlocked.
```

- [ ] **Step 9: Run steps 1, 15, 16 and 17 in game**

Step 1 of the checklist matters as much as the new steps: five new `ResearchProjectDef`s and a new
field are exactly the kind of change that produces def config errors at load, and those are only
visible in the log before a save is loaded.

- [ ] **Step 10: Commit**

```bash
cd /home/trog/code/rimworld-mods
git add Druidkin/Defs/ResearchProjectDefs/Research_Druidkin.xml \
        Druidkin/Defs/DruidkinAnimalFormDefs/AnimalForms_Druidkin.xml \
        Druidkin/Source/Druidkin/DruidkinAnimalFormDef.cs \
        Druidkin/Source/Druidkin/WildShape/Dialog_ChooseAnimalForm.cs \
        Druidkin/Languages/English/Keyed/Druidkin_Keys.xml \
        Druidkin/README.md Druidkin/Assemblies/Druidkin.dll
git commit -m "feat: gate animal forms behind research projects"
```

---

### Task 3: The wild shaping skill

The skill exists and behaves itself. It does nothing yet — no XP source, no effect on power.

**Files:**
- Create: `Druidkin/Defs/SkillDefs/Skills_Druidkin.xml`
- Create: `Druidkin/Source/Druidkin/HarmonyPatches/Patch_PawnGenerator_GenerateSkills.cs`
- Modify: `Druidkin/Source/Druidkin/DruidkinDefOf.cs`
- Modify: `Druidkin/README.md`

**Interfaces:**
- Consumes: nothing from Task 2.
- Produces: `DruidkinDefOf.Druidkin_WildShaping` (`SkillDef`) and `DruidkinDefOf.Druidkin_WildShape`
  (`GeneDef`). Tasks 4 and 5 both read the first.

- [ ] **Step 1: Write the SkillDef**

Create `Druidkin/Defs/SkillDefs/Skills_Druidkin.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!--
  Core's twelve skills occupy listOrder 10 through 120, verified against
  Data/Core/Defs/SkillDefs/Skills.xml, so 130 places wild shaping last in the Skills tab.

  neverDisabledBasedOnWorkTypes is required rather than cosmetic: this skill drives no work
  type, and without it vanilla reads it as disabled for every pawn - druids included.

  A SkillDef exists for every humanlike pawn, so non-druids will carry this skill too. The
  passion that pawn generation would otherwise spend on it is undone by
  Patch_PawnGenerator_GenerateSkills; the leftover tab row is accepted.
-->
<Defs>

  <SkillDef>
    <defName>Druidkin_WildShaping</defName>
    <label>wild shaping</label>
    <skillLabel>wild shaping</skillLabel>
    <description>Skill at wearing an animal's shape: holding a body that is not yours, and making it move as though it were. A practised druid wears a form more completely, and gets more out of it.</description>
    <listOrder>130</listOrder>
    <pawnCreatorSummaryVisible>false</pawnCreatorSummaryVisible>
    <neverDisabledBasedOnWorkTypes>true</neverDisabledBasedOnWorkTypes>
  </SkillDef>

</Defs>
```

- [ ] **Step 2: Resolve the new defs**

In `DruidkinDefOf.cs`, add to the field list:

```csharp
        public static SkillDef Druidkin_WildShaping;

        /// Needed only to tell a druid from any other colonist, which pawn generation has to
        /// do before it decides whether wild shaping deserves a passion.
        public static GeneDef Druidkin_WildShape;
```

- [ ] **Step 3: Stop pawn generation wasting passions**

Create `Druidkin/Source/Druidkin/HarmonyPatches/Patch_PawnGenerator_GenerateSkills.cs`:

```csharp
using HarmonyLib;
using RimWorld;
using Verse;

namespace Druidkin.HarmonyPatches
{
    /// A SkillDef belongs to every humanlike pawn, so pawn generation will roll a level and
    /// sometimes spend a passion on wild shaping for colonists who can never use it. The
    /// leftover Skills tab row is only untidy, but passions are a limited budget and a wasted
    /// one is a real balance change to every pawn in the game, so that half is undone here.
    [HarmonyPatch(typeof(PawnGenerator), "GenerateSkills")]
    public static class Patch_PawnGenerator_GenerateSkills
    {
        public static void Postfix(Pawn pawn)
        {
            if (pawn?.skills == null
                || pawn.genes?.HasActiveGene(DruidkinDefOf.Druidkin_WildShape) == true)
            {
                return;
            }

            SkillRecord skill = pawn.skills.GetSkill(DruidkinDefOf.Druidkin_WildShaping);
            if (skill == null)
            {
                return;
            }

            skill.Level = 0;
            skill.xpSinceLastLevel = 0f;
            skill.passion = Passion.None;
        }
    }
}
```

`GenerateSkills` is private, which is why it is named by string rather than by `nameof`. The postfix
parameter must be named `pawn` to match the original's parameter name; if Harmony reports that it
cannot find a parameter called `pawn`, rename it to `__0` rather than changing its type.

- [ ] **Step 4: Build**

```bash
cd /home/trog/code/rimworld-mods/Druidkin/Source/Druidkin && /home/trog/.dotnet/dotnet build
```

Expected: `0 Error(s)`. `HasActiveGene` is the 1.5-and-later name; if it fails to resolve, the older
name is `HasGene` and the log message will say so plainly.

- [ ] **Step 5: Add the checklist steps**

Append to the Testing checklist in `Druidkin/README.md`:

```markdown
18. Check the log at startup for a Harmony patch failure naming `GenerateSkills`. A patch that fails to apply is reported once at startup and never again, so this is the only chance to see it.
19. Open a druid's Skills tab. Confirm **wild shaping** appears as the last skill in the list, and that it is not shown as disabled.
20. Generate several non-druid colonists. Confirm none has a passion in wild shaping, and that all show it at level 0.
```

- [ ] **Step 6: Run steps 1, 18, 19 and 20 in game**

- [ ] **Step 7: Commit**

```bash
cd /home/trog/code/rimworld-mods
git add Druidkin/Defs/SkillDefs/Skills_Druidkin.xml \
        Druidkin/Source/Druidkin/DruidkinDefOf.cs \
        Druidkin/Source/Druidkin/HarmonyPatches/Patch_PawnGenerator_GenerateSkills.cs \
        Druidkin/README.md Druidkin/Assemblies/Druidkin.dll
git commit -m "feat: add wild shaping skill"
```

---

### Task 4: Earn XP while shifted

The skill now rises. It still has no effect on power, so this task is independently observable: a
druid's wild shaping level climbs across a shift and does not change while human.

**Files:**
- Create: `Druidkin/Source/Druidkin/WildShape/HediffComp_WildShapeMastery.cs`
- Modify: `Druidkin/Source/Druidkin/WildShape/WildShapeUtility.cs`
- Modify: `Druidkin/Defs/HediffDefs/Hediffs_Druidkin.xml`
- Modify: `Druidkin/README.md`

**Interfaces:**
- Consumes: `DruidkinDefOf.Druidkin_WildShaping` from Task 3.
- Produces: `HediffCompProperties_WildShapeMastery` with public fields `xpPerDay` (`float`) and
  `masteryFactorByLevel` (`SimpleCurve`); `HediffComp_WildShapeMastery`; and
  `WildShapeUtility.MasteryFactorFor(Pawn pawn)` returning `float`. Task 5 calls `MasteryFactorFor`
  from two places and never evaluates the curve itself.

- [ ] **Step 1: Write the comp**

Create `Druidkin/Source/Druidkin/WildShape/HediffComp_WildShapeMastery.cs`:

```csharp
using RimWorld;
using Verse;

namespace Druidkin
{
    public class HediffCompProperties_WildShapeMastery : HediffCompProperties
    {
        /// XP for one full day spent shifted. The ability's durationTicks is 60000, which is
        /// one day, so this reads directly as "XP for one full-length shift" and can be
        /// retuned without doing arithmetic first.
        public float xpPerDay = 2000f;

        /// Skill level to a multiplier on the form's statBoostFactor. Lives in XML rather
        /// than in C# because this is the number that will be retuned most often, and a
        /// constant here would cost a build and a game restart per tweak.
        public SimpleCurve masteryFactorByLevel;

        public HediffCompProperties_WildShapeMastery()
        {
            compClass = typeof(HediffComp_WildShapeMastery);
        }
    }

    /// Awards the XP. The curve it carries is read through WildShapeUtility.MasteryFactorFor
    /// rather than here, so that the dialog's preview and a shift's actual power cannot come
    /// from two different pieces of arithmetic.
    public class HediffComp_WildShapeMastery : HediffComp
    {
        private const int TicksPerDay = 60000;

        public HediffCompProperties_WildShapeMastery Props =>
            (HediffCompProperties_WildShapeMastery)props;

        /// XP accrues only while this hediff is on the pawn, and that is the entire anti-grind
        /// rule: a shifted druid has every work type disabled and no gear, so farming XP
        /// already costs a colonist's whole working day.
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            SkillRecord skill = Pawn?.skills?.GetSkill(DruidkinDefOf.Druidkin_WildShaping);
            if (skill == null)
            {
                return;
            }

            // Not direct, so passion multipliers and vanilla's daily XP saturation both apply.
            skill.Learn(Props.xpPerDay / TicksPerDay, direct: false);
        }
    }
}
```

- [ ] **Step 2: Add the single curve lookup**

In `WildShapeUtility.cs`, add after `IsShifted`:

```csharp
        /// One place evaluates the curve, so the multiplier previewed in the choose-form
        /// window and the one a shift actually runs at cannot drift apart. The curve lives on
        /// the hediff def's comp properties, which is why this reads the def rather than a
        /// live hediff: the preview is needed before any hediff exists.
        public static float MasteryFactorFor(Pawn pawn)
        {
            HediffCompProperties_WildShapeMastery props =
                DruidkinDefOf.Druidkin_WildShapeForm
                    .CompProps<HediffCompProperties_WildShapeMastery>();
            SkillRecord skill = pawn?.skills?.GetSkill(DruidkinDefOf.Druidkin_WildShaping);

            if (props?.masteryFactorByLevel == null || skill == null)
            {
                return 1f;
            }

            return props.masteryFactorByLevel.Evaluate(skill.Level);
        }
```

- [ ] **Step 3: Attach the comp and the curve**

In `Druidkin/Defs/HediffDefs/Hediffs_Druidkin.xml`, replace the `<comps>` block with:

```xml
    <comps>
      <li Class="Druidkin.HediffCompProperties_WildShapeVerbs" />
      <li Class="Druidkin.HediffCompProperties_WildShapeMastery">
        <xpPerDay>2000</xpPerDay>
        <!--
          Level 10 is 1.00 on purpose. Every statBoostFactor in AnimalForms_Druidkin.xml was
          tuned against an unscaled form, so anchoring the midpoint at neutral keeps all six
          of those numbers meaningful instead of invalidating them at once. A novice is worse
          than the animal they are copying; a master is half again better than today.

          All four points are opening positions, expected to move during play testing.
        -->
        <masteryFactorByLevel>
          <li>(0, 0.80)</li>
          <li>(10, 1.00)</li>
          <li>(15, 1.15)</li>
          <li>(20, 1.30)</li>
        </masteryFactorByLevel>
      </li>
    </comps>
```

- [ ] **Step 4: Build**

```bash
cd /home/trog/code/rimworld-mods/Druidkin/Source/Druidkin && /home/trog/.dotnet/dotnet build
```

Expected: `0 Error(s)`. This build is the check on four assumed members at once: `HediffComp.Pawn`,
`Pawn_SkillTracker.GetSkill`, `SkillRecord.Learn(float, bool)` and `HediffDef.CompProps<T>()`.

- [ ] **Step 5: Add the checklist steps**

Append to the Testing checklist in `Druidkin/README.md`:

```markdown
21. Note a druid's wild shaping XP, shift, and let a full shift run out. Confirm the XP rose, and that a level was gained if the rate allows it. Confirm no exception appears in the log during the shift, since this code runs every tick and a fault would flood the file.
22. Leave the druid human for a full day and confirm wild shaping XP does not move. Shifting is the only source.
23. Note whether the XP gained per shift feels like sane pacing, and record the number. Task 6 tunes `xpPerDay` against this observation.
```

- [ ] **Step 6: Run steps 1, 21, 22 and 23 in game**

- [ ] **Step 7: Commit**

```bash
cd /home/trog/code/rimworld-mods
git add Druidkin/Source/Druidkin/WildShape/HediffComp_WildShapeMastery.cs \
        Druidkin/Source/Druidkin/WildShape/WildShapeUtility.cs \
        Druidkin/Defs/HediffDefs/Hediffs_Druidkin.xml \
        Druidkin/README.md Druidkin/Assemblies/Druidkin.dll
git commit -m "feat: earn wild shaping xp while shifted"
```

---

### Task 5: Mastery scales form power

The two halves meet. Level now changes what a form is worth, and the player can see it before
choosing.

**Files:**
- Modify: `Druidkin/Source/Druidkin/WildShape/Hediff_WildShapeForm.cs`
- Modify: `Druidkin/Source/Druidkin/WildShape/Dialog_ChooseAnimalForm.cs`
- Modify: `Druidkin/Languages/English/Keyed/Druidkin_Keys.xml`
- Modify: `Druidkin/README.md`

**Interfaces:**
- Consumes: `WildShapeUtility.MasteryFactorFor(Pawn)` from Task 4 and
  `DruidkinDefOf.Druidkin_WildShaping` from Task 3.
- Produces: nothing later tasks consume.

- [ ] **Step 1: Add the snapshotted factor**

In `Hediff_WildShapeForm.cs`, add after the `workPriorityValuesWorking` field:

```csharp
        /// The mastery multiplier as it stood when this shift began, not as it stands now.
        /// XP accrues only while shifted, so every level-up a druid ever has happens mid-shift,
        /// and CurStage is cached precisely because stat calculation consults it constantly. A
        /// live read would therefore mean invalidating that cache from inside a level-up during
        /// stat calculation, which is the re-entrancy BuildStage already takes care to avoid.
        /// One float, and a form's power is fixed when you put it on.
        private float masteryFactor = 1f;
```

- [ ] **Step 2: Scribe it**

In `ExposeData`, add after the `storedWorkPriorities` scribe call:

```csharp
            Scribe_Values.Look(ref masteryFactor, "masteryFactor", 1f);
```

The `1f` default is what makes existing saves load unchanged: a hediff written before this field
existed comes back at neutral and behaves exactly as it does today.

- [ ] **Step 3: Snapshot on add**

Replace `PostAdd` with:

```csharp
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            // PostAdd runs when a shift begins and not when a save is loaded, which is exactly
            // the distinction wanted: a saved shift keeps the power it started with rather than
            // silently re-rolling against the druid's current level. Ordered before
            // RefreshDerivedState because that is what clears the cached stage.
            masteryFactor = WildShapeUtility.MasteryFactorFor(pawn);
            RefreshDerivedState();
        }
```

- [ ] **Step 4: Apply it to the derived stats**

In `BuildStage`, replace the `boost` assignment:

```csharp
            float boost = Mathf.Max(0.01f, form.statBoostFactor * masteryFactor);
```

That is the whole change. `MoveSpeed` scales by `boost` and `IncomingDamageFactor` divides by it, so
both pick up mastery from this one multiplication and neither needs to know the skill exists.

- [ ] **Step 5: Add the summary string**

In `Druidkin/Languages/English/Keyed/Druidkin_Keys.xml`, add before `</LanguageData>`:

```xml
  <Druidkin_MasterySummary>Wild shaping {0} — forms at {1} power.</Druidkin_MasterySummary>
```

- [ ] **Step 6: Show it in the dialog**

In `Dialog_ChooseAnimalForm.DoWindowContents`, after the `Text.Font = GameFont.Small;` line and before
`float y = 45f;`:

```csharp
            // The skill needs somewhere to visibly pay off, and the moment the player is
            // picking a form is the moment they care what their level is worth.
            SkillRecord shaping = pawn.skills?.GetSkill(DruidkinDefOf.Druidkin_WildShaping);
            if (shaping != null)
            {
                Widgets.Label(new Rect(0f, 42f, inRect.width, 24f),
                    "Druidkin_MasterySummary".Translate(
                        shaping.Level, WildShapeUtility.MasteryFactorFor(pawn).ToStringPercent()));
            }
```

Then change `float y = 45f;` to:

```csharp
            float y = 70f;
```

Add `using RimWorld;` to the file's using block if it is not already present, for `SkillRecord`.

- [ ] **Step 7: Build**

```bash
cd /home/trog/code/rimworld-mods/Druidkin/Source/Druidkin && /home/trog/.dotnet/dotnet build
```

Expected: `0 Error(s)`.

- [ ] **Step 8: Add the checklist steps**

Append to the Testing checklist in `Druidkin/README.md`:

```markdown
24. With a druid at wild shaping 0, shift into the rat and record move speed and incoming damage factor from the pawn's stat inspector. Set the skill to 20 with dev mode, shift again, and confirm both numbers improved.
25. Confirm the level and percentage in the choose-form window header match the Skills tab, and that the percentage changes when the level does.
26. Shift, then raise the druid's wild shaping level with dev mode **without reverting**. Confirm the active form's numbers do **not** change, and that they do change on the next shift. This is the snapshot behaving as designed, not a bug.
27. Shift, save, reload, and confirm the form's numbers are unchanged by the round trip rather than re-derived from the current level.
28. Load a save made before this feature existed. Confirm a mid-shift druid in it loads at neutral power with no error in the log.
```

- [ ] **Step 9: Run steps 1 and 24 through 28 in game**

Step 28 needs a save created before this branch. Make one from the Task 1 baseline run if none exists.

- [ ] **Step 10: Commit**

```bash
cd /home/trog/code/rimworld-mods
git add Druidkin/Source/Druidkin/WildShape/Hediff_WildShapeForm.cs \
        Druidkin/Source/Druidkin/WildShape/Dialog_ChooseAnimalForm.cs \
        Druidkin/Languages/English/Keyed/Druidkin_Keys.xml \
        Druidkin/README.md Druidkin/Assemblies/Druidkin.dll
git commit -m "feat: scale form power by wild shaping level"
```

---

### Task 6: Tune in play

The numbers shipped so far are opening positions, and this is the task where they stop being
guesses. It is deliberately last: tuning before Task 5 means tuning a curve with no observable effect.

**Files:**
- Modify: `Druidkin/Defs/HediffDefs/Hediffs_Druidkin.xml`
- Modify: `Druidkin/README.md`

- [ ] **Step 1: Judge the XP rate against the recorded observation**

Using the number recorded in checklist step 23, decide how many full-length shifts a druid should need
to reach level 10. Adjust `xpPerDay` to suit. XML only, so no rebuild is needed — restart the game or
use dev mode to reload defs.

- [ ] **Step 2: Judge the curve's endpoints**

Confirm a level-0 druid's form is weak but still worth using, and that a level-20 form feels earned
rather than mandatory. Move the four curve points as needed, keeping level 10 at `1.00`.

- [ ] **Step 3: Decide what to do about skill decay**

Vanilla skills rust when unused above a threshold, so a druid kept home for a season gets worse at
shifting with no way to train other than shifting. Play far enough to see whether this reads as "a
druid should practise" or as punishment, and record the decision either way.

- [ ] **Step 4: Record the outcome**

Add a short "Progression tuning" note to the Known gaps section of `Druidkin/README.md` covering the
values chosen, why, and the skill-decay decision.

- [ ] **Step 5: Commit**

```bash
cd /home/trog/code/rimworld-mods
git add Druidkin/Defs/HediffDefs/Hediffs_Druidkin.xml Druidkin/README.md
git commit -m "balance: tune wild shaping xp rate and mastery curve"
```

---

## Self-Review

**Spec coverage.** Every section of the spec maps to a task. Research gating, the `requiredResearch`
field and its null default, and the greyed dialog rows are Task 2. The `SkillDef` with `listOrder`
130, `pawnCreatorSummaryVisible`, `neverDisabledBasedOnWorkTypes`, the `DruidkinDefOf` entries and the
`GenerateSkills` postfix are Task 3. XP and the XML curve are Task 4. The snapshot, the one-line
`BuildStage` change and the dialog header are Task 5. Save compatibility is verified by checklist step
28 rather than implemented, which is correct: it holds by construction through the `1f` scribe default.
The spec's decision to accept the cosmetic Skills tab row is carried as a comment in
`Skills_Druidkin.xml` and deliberately has no task. The skill-decay risk is a Task 6 decision.

**Placeholder scan.** No TBDs. The two values the spec labelled placeholders, `xpPerDay` and the curve
points, ship as concrete numbers with a task that revisits them against a recorded observation, rather
than as blanks for an implementer to invent. `researchViewX` and `researchViewY` carry real
coordinates.

**Type consistency.** `MasteryFactorFor(Pawn)` is defined in Task 4 and called under that exact name
in two places in Task 5. `IsUnlocked` is defined and used only within Task 2. `Druidkin_WildShaping`
and `Druidkin_WildShape` are declared in Task 3 and consumed by name in Tasks 4 and 5.
`HediffCompProperties_WildShapeMastery` is spelled identically in the C# and in the XML `Class`
attribute. The `Druidkin_Research_` prefix is used consistently across the research defs, the form
wiring and the checklist.

**One gap accepted knowingly.** Task 3's Harmony postfix depends on `GenerateSkills`' parameter being
named `pawn`, which could not be confirmed without a decompiler. The step says so and names the `__0`
fallback rather than leaving it to be discovered at runtime.
