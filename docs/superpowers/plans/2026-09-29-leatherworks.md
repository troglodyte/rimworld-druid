# Leatherworks Implementation Plan

> **For agentic workers:** execute phase by phase with superpowers:executing-plans or
> superpowers:subagent-driven-development. Checkboxes track progress. Each phase ends with a commit.

**Spec:** `docs/superpowers/specs/2026-09-29-leatherworks-design.md` is the source of truth for all
numbers (multipliers, costs, work, StudFactor). This plan does not repeat them.

**Goal:** Add a standalone mod `Leatherworks/` next to `Druidkin/`. It adds six leather armor
pieces (jerkin, cap and greaves, each plain and studded). Studded pieces get a bonus that depends on the stud metal.

## Global constraints

- RimWorld 1.6, core only (no DLC dependency). packageId `throwingfish.leatherworks`.
- Build env: `export DOTNET_ROOT=~/.dotnet DOTNET_ROLL_FORWARD=Major PATH=~/.dotnet:~/.dotnet/tools:$PATH`.
  Build from `Leatherworks/Source/Leatherworks`, test from `Leatherworks/Source/Leatherworks.Tests`.
- Copy the csproj layout from `Druidkin/Source/Druidkin/Druidkin.csproj` (same `RimWorldManagedDir`,
  Harmony 2.3.3, and a post-build copy into `Leatherworks/Assemblies/`). Copy the test csproj from
  `Druidkin.Tests.csproj`, with its reference pointing at `Leatherworks.dll`.
- Namespaces are `Leatherworks` and `Leatherworks.HarmonyPatches`, one patch per file. User-facing strings
  go in `Languages/English/Keyed/Leatherworks_Keys.xml`.
- Def prefix is `LW_` (for example `LW_Apparel_LeatherJerkin`, `LW_Apparel_StuddedLeatherJerkin`, `LW_Make_StuddedLeatherJerkin`).
- Gate for every phase: build with 0 warnings and 0 errors, and `dotnet test` green. Commit on main at
  the end of each phase.
- Read test and run output from tee files in `/tmp` (the user can't paste output).

---

## Phase 0: Verify engine assumptions (read-only, no commit)

Decompile the classes below with ilspycmd (`-t` per type) and write the answers into a scratch note.
- [ ] `GenRecipe.MakeRecipeProducts` and `Toils_Recipe` / `RecipeWorkerCounter` → how the dominant
      ingredient becomes the product stuff when the recipe has two ingredient slots. **Decision:** if
      the metal can end up as stuff, the Phase 3 postfix must force the leather as stuff (or the recipe
      must set `productHasIngredientStuff` false and the postfix sets stuff explicitly).
- [ ] `StatDef.parts` and `StatPart.TransformValue` / `ExplanationPart` signatures. Also where quality
      applies to armor (`StatPart_Quality` order in `ArmorRating_*` parts). The stud bonus must apply
      before quality, so the patch must insert before it, not append after it.
- [ ] Vanilla apparelTags for tribal and medieval/outlander pawn kinds. Grep
      `Data/Core/Defs/PawnKindDefs*` for `apparelTags` (for example `Neolithic`, `MedievalMilitary`?). Record the exact
      tags.
- [ ] Vanilla stuff categories and ThingCategories: `Leathery` (stuff), `Leathers` (ThingCategory), and
      the metal side (`Metallic` stuff category, and which ThingCategory holds steel, plasteel and gold, for
      the recipe filter).
- [ ] Vanilla texture paths for flak vest, simple helmet and pants (the texture sources for Phase 5).

## Phase 1: Scaffold and plain pieces (XML only, plus an empty assembly)

- [ ] `Leatherworks/About/About.xml`, `Source/Leatherworks/Leatherworks.csproj`, and a `LeatherworksMod.cs`
      that calls `new Harmony("throwingfish.leatherworks").PatchAll()`.
- [ ] `Source/Leatherworks.Tests` project with a `ContentValidationTests` skeleton that finds the mod
      root the same way Druidkin's does.
- [ ] `Defs/ThingDefs/Apparel_Leatherworks.xml`: three abstract parents (`LW_JerkinBase`, `LW_CapBase`,
      `LW_GreavesBase`) holding layer, bodyPartGroups, `stuffCategories`=Leathery, and insulation. Add
      three plain children with `recipeMaker` (tailoring benches, Neolithic tech level, no research) and
      tribal apparelTags.
- [ ] Symlink `…/RimWorld/Mods/Leatherworks -> ~/code/rimworld-mods/Leatherworks`.
- [ ] Content tests: defs parse, and each plain def has the expected parent and a non-zero `StuffEffectMultiplierArmor`.
- [ ] Commit.

## Phase 2: Stud logic (TDD, pure code first)

- [ ] Test first: `StudMath.Bonus(float metalStuffPower, float studFactor)` → the product. Also a
      label test: `StudLabel.Format("bear leather studded jerkin", "plasteel")` →
      `"bear leather studded jerkin (plasteel)"`. Both are pure static methods with no Verse types in
      their signatures, so NUnit can call them without a running game.
- [ ] `CompProperties_StuddedMetal` / `CompStuddedMetal`: a `ThingDef metal` field saved with
      `Scribe_Defs.Look`. `PostPostMake` (or `Initialize` if the metal is null) picks a random metal, weighted toward steel.
      Also `TransformLabel` via `StudLabel`, and `CompInspectStringExtra`.
- [ ] `StatPart_StuddedMetal`: `TransformValue` adds `StudMath.Bonus(metal.GetStatValueAbstract(
      StuffPower_Armor_X), 0.3)`. Pick X from `parentStat`. `ExplanationPart` uses the keyed string.
- [ ] `Patches/StatParts_Leatherworks.xml`: insert the part into `ArmorRating_Sharp` and
      `ArmorRating_Blunt`. Place it before quality, as decided in Phase 0.
- [ ] Commit.

## Phase 3: Studded defs, recipes, crafting hook

- [ ] Three studded children with the comp, MoveSpeed offsets, Medieval tech level, and medieval
      apparelTags. Leave out `recipeMaker`.
- [ ] `Defs/RecipeDefs/Recipes_Leatherworks.xml`: three hand-written RecipeDefs. Ingredient slots are
      leather (ThingCategory from Phase 0) and metal (filter from Phase 0). Research is `Smithing`, and the recipe uses the same
      benches as the plain pieces.
- [ ] `HarmonyPatches/Patch_MakeRecipeProducts.cs`: postfix on the returned `IEnumerable<Thing>`.
      Wrap it, and for each product with `CompStuddedMetal`, set `metal` to the consumed ingredient whose def
      `IsStuff` with `Metallic`. Apply the stuff fix from Phase 0 if one is needed.
- [ ] Content tests: each studded def has the comp, each has exactly one recipe producing it, and each recipe
      has both slots.
- [ ] Commit.

## Phase 4: Textures

- [ ] `Leatherworks/tools/make_textures.py` (Pillow): reads the vanilla textures from Phase 0 and
      recolors them to leather tones. For studded pieces it adds a stud dot overlay. Outputs worn
      graphics for Male/Female/Thin/Fat/Hulk × north/south/east (the cap uses no body types), plus item
      icons, into `Textures/Things/Apparel/…`.
- [ ] Point `graphicData` and `wornGraphicPath` at the generated textures.
- [ ] Content test: every expected texture path exists, for each body type and facing.
- [ ] Commit (script and PNGs).

## Phase 5: In-game verification

Append the checklist to `Leatherworks/README.md`, then run it and check `Player.log` for errors:
- [ ] No def or config errors on load. The Harmony patch applies.
- [ ] Craft each plain piece. Info card sharp and blunt values match the spec's worked examples (leather and bear).
- [ ] Craft studded jerkins with steel and with plasteel. Check that the label suffix, inspect string and
      explanation line are correct, the bonus matches the spec, and the stuff is leather, not metal.
- [ ] Dev-spawn a studded piece and confirm a random metal is assigned.
- [ ] Tribal raid wears the plain pieces. Medieval/outlander pawns wear the studded ones.
- [ ] Jerkin, cap and greaves layer correctly with a shirt, pants and a duster.
- [ ] Save, reload, and confirm the stud metal persisted.
- [ ] Commit fixes as they land. Record any tuning changes in the spec.
