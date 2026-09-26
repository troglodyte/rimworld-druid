# Wild Shape Talent Tree Verification Checklist

> **Context:** Verification checklist for commit `2d00847` (*Implement per-druid wild shape talent tree progression*), implementing the design spec [`2026-09-23-wild-shape-talent-tree-design.md`](../specs/2026-09-23-wild-shape-talent-tree-design.md).
>
> **Automated Status:** 28 NUnit tests in [`Druidkin.Tests`](../../../Druidkin/Source/Druidkin.Tests/Druidkin.Tests.csproj) pass cleanly, validating progression math, curve interpolation, graph cycle detection, prerequisite reachability, polymorphic node effects, and XML starter content integrity.
>
> **Purpose:** Engine hooks, Harmony patches, IMGUI inspect tabs, and save/load serialization cannot be verified in standalone unit tests. This checklist records what must be verified in-game in RimWorld 1.6.

---

## Primary Evidence Channel

- **Player log location:** `~/snap/steam/common/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
- Check on startup for def configuration errors, XML patch failures, and Harmony patch injection errors.
- Check after shifting, leveling, fighting, and reverting for any runtime exceptions.

---

## In-Game Verification Checklist

### 1. Inspect Tab UI ([`ITab_Pawn_WildShape`](../../../Druidkin/Source/Druidkin/WildShape/ITab_Pawn_WildShape.cs))

- [ ] **Tab visibility:** Select a colonist with the Druid xenotype (`Gene_Druid`). Verify the **Wild Shape** inspect tab appears on the inspect pane.
- [ ] **Tab hiding:** Select a non-druid colonist, prisoner, or animal. Verify the Wild Shape tab is hidden.
- [ ] **Header rendering:**
  - [ ] Pawn name and level (`<PawnName> · Level 0`) render cleanly with no overlap.
  - [ ] XP fillable bar displays `0 / 1,000 XP` with green bar fill. Tooltip on XP bar shows current and required XP.
  - [ ] Power multiplier displays `Power x0.80` (at level 0).
  - [ ] Unspent points counter displays `0 unspent points` (greyed out when 0, amber when > 0).
- [ ] **Talent tree rendering:**
  - [ ] Node boxes render at their configured `treePosition` grid coordinates.
  - [ ] Category border colors: Blue for animal forms, Orange for form upgrades, Purple for perks.
  - [ ] State backgrounds: Dark green for learned, amber/gold for learnable, dark charcoal for locked.
  - [ ] Prerequisite lines: Green for learned, yellow for learnable, grey for locked.
  - [ ] Scrollview operates smoothly if window size requires scrolling.
- [ ] **Tooltips & lock reasons:**
  - [ ] Hover over a locked node (e.g. Cougar shape, Grizzly Bear armor upgrade, or Megasloth shape).
  - [ ] Verify tooltip lists node description, cost, and explicit lock reason (e.g. required level, missing prerequisite).
- [ ] **Learning confirmation:**
  - [ ] Click a learnable node (e.g. Wolf shape at level 2).
  - [ ] Verify a confirmation modal (`Dialog_MessageBox`) appears stating points cannot be refunded.
  - [ ] Confirm learning: verify node immediately transitions to green (Learned), unspent points decrement, and connecting lines update.

---

### 2. Form Picker Filtering ([`Dialog_ChooseAnimalForm`](../../../Druidkin/Source/Druidkin/WildShape/Dialog_ChooseAnimalForm.cs))

- [ ] **Level 0 baseline:** Open the Wild Shape ability on a fresh druid. Confirm **only the Rat form** is displayed.
- [ ] **Picker header:** Confirm the dialog header shows the druid's current level and mastery power percentage (e.g. `Level 0 (80% power)`).
- [ ] **Dynamic unlocking:** Learn a new form node in the tree (e.g. Wolf shape). Reopen the ability and confirm the newly learned form is now selectable in the list.

---

### 3. XP Generation Sources

- [ ] **Time shifted (`xpPerDayShifted = 600`):**
  - [ ] Shift into an animal form.
  - [ ] Observe XP in the Wild Shape inspect tab incrementing every 250 ticks (~4 seconds on 1x speed, ~2.5 XP per tick interval).
- [ ] **Damage taken while shifted (`xpPerDamageTaken = 2`):**
  - [ ] While in animal form, take damage from combat or a trap.
  - [ ] Verify XP increases by `2.0 × totalDamageDealt`.
- [ ] **Damage dealt while shifted (`xpPerDamageDealt = 4`):**
  - [ ] Attack an enemy raider or hostile wild animal while shifted.
  - [ ] Verify XP increases by `4.0 × totalDamageDealt` via [`Patch_Thing_TakeDamage`](../../../Druidkin/Source/Druidkin/HarmonyPatches/Patch_Thing_TakeDamage.cs).
- [ ] **Anti-exploit check (friendly-fire filtering):**
  - [ ] Have a shifted druid attack a friendly colonist, tame colony pet, or punch a door/wall.
  - [ ] Verify **no** wild shape XP is awarded.
- [ ] **Animal handling in human form (`animalsXpFraction = 0.5`):**
  - [ ] As human, have the druid tame, train, milk, or shear an animal to gain Animals skill XP.
  - [ ] Verify wild shape progression XP increases by 50% of the Animals skill XP earned via [`Patch_SkillRecord_Learn`](../../../Druidkin/Source/Druidkin/HarmonyPatches/Patch_SkillRecord_Learn.cs).

---

### 4. Mastery Scaling & Form Upgrades ([`Hediff_WildShapeForm`](../../../Druidkin/Source/Druidkin/WildShape/Hediff_WildShapeForm.cs))

- [ ] **Mastery curve scaling:**
  - [ ] Level 0: verify move speed and incoming damage reflect `x0.80` mastery factor.
  - [ ] Level 10: verify stats reflect `x1.00` mastery factor (matching raw `statBoostFactor`).
  - [ ] Level 20: verify stats reflect `x1.30` mastery factor.
- [ ] **Form upgrades:**
  - [ ] Learn an upgrade node (e.g. Wolf MoveSpeed +0.6 c/s or Grizzly Bear Sharp Armor +25%).
  - [ ] Shift into that form; inspect the pawn's stat sheet to verify the upgrade stat modifier is applied.
  - [ ] Shift into a different animal form or revert to human; verify the stat modifier is removed.

---

### 5. Perks & Revert Hooks ([`NodeEffects.cs`](../../../Druidkin/Source/Druidkin/WildShape/NodeEffects.cs))

- [ ] **Enduring shape perk:**
  - [ ] Learn Enduring Shape (`shiftDurationFactor = 1.5`).
  - [ ] Shift into an animal form; verify duration is 90,000 ticks (1.5 in-game days) instead of 60,000 ticks.
- [ ] **Swift return perk:**
  - [ ] Learn Swift Return (`cooldownFactor = 0.5`).
  - [ ] Shift into an animal; verify the ability cooldown is reduced to 1,250 ticks instead of 2,500 ticks.
- [ ] **Mending revert perk:**
  - [ ] Learn Mending Revert (`healOnRevert = 20`).
  - [ ] Sustain injuries while in animal form.
  - [ ] Revert to human; verify up to 20 HP across existing injuries heal immediately upon reverting.

---

### 6. Dev Mode Tools ([`Gene_Druid`](../../../Druidkin/Source/Druidkin/WildShape/Gene_Druid.cs))

- [ ] Enable Dev Mode in options, select a druid, and test all 3 debug gizmos:
  - [ ] `DEV: +1000 Wild Shape XP` increments XP by 1,000.
  - [ ] `DEV: +1 Wild Shape Level` pushes druid to next level threshold, awards 1 point, and triggers level-up message.
  - [ ] `DEV: Reset Druid` resets level to 0, clears learned nodes, and auto-learns Rat form.

---

### 7. Persistence & Save Compatibility

- [ ] **Mid-shift save/reload snapshot:**
  - [ ] Shift into an upgraded form at a specific level.
  - [ ] Save game, reload save.
  - [ ] Verify `masteryFactor` and `snapshotNodes` survive unchanged and are not recalculated against live level.
- [ ] **Pre-update save compatibility:**
  - [ ] Load a save created before the talent tree update with a druid colonist.
  - [ ] Confirm save loads without null errors.
  - [ ] Confirm the druid initializes at level 0 with Rat form auto-learned and 0 unspent points.

---

### 8. Third-Party Animal Forms ([`DruidkinStartup`](../../../Druidkin/Source/Druidkin/DruidkinStartup.cs))

- [ ] Verify `Player.log` on startup:
  - [ ] No warning for the 6 core forms (Rat, Timber Wolf, Cougar, Muffalo, Grizzly Bear, Megasloth).
  - [ ] If a modded animal form is added without an unlock node, confirm the startup warning fires:
    `[Druidkin] Form '<form>' has no WildShapeNodeDef unlocking it and cannot be learned by druids.`
