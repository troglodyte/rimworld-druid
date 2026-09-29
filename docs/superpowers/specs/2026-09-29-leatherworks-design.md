# Leatherworks — Leather Armor Mod Design

Date: 2026-09-29
Status: Approved in brainstorming, pending spec review

## Intent

A standalone, vanilla-style RimWorld 1.6 mod (no Druidkin tie-in) adding
leather armor any colonist can wear.

- **Niche:** early-game / tribal protection — clearly better than clothing,
  clearly worse than flak or plate.
- **Material matters:** stats scale with the leather used, so rare leathers
  (bear, thrumbo) are a meaningful upgrade.
- **Studded tier:** a stronger variant whose protection also scales with the
  metal used for the studs.

Success: all six pieces craftable, stats in the info card match the formulas
below, NPC tribals/medieval pawns spawn wearing them, saves round-trip.

## Pieces

Six ThingDefs: three pieces, each plain and studded. Three abstract parents
(jerkin, cap, greaves) hold shared fields.

| Piece | Layer | bodyPartGroups | Coexists with |
|---|---|---|---|
| Leather jerkin | Middle | Torso, Neck, Shoulders, Arms | Shirt under; duster/parka over |
| Leather cap | Overhead | UpperHead | Occupies the simple-helmet slot |
| Leather greaves | Middle | Legs | Pants under; no conflict with jerkin (no shared body part) |

Plain and studded are separate defs (own textures, base stats, recipes).

## Stats

Vanilla stuffed-armor rule: `armor = stuff StuffPower_Armor_X × StuffEffectMultiplierArmor`.
Reference: plain leather sharp 0.81 / blunt 0.24; bear sharp 1.12; flak vest
1.00 / 0.36; steel plate ≈ 0.81 / 0.41.

**All pieces:** `stuffCategories` = `Leathery` only.

| | Jerkin | Cap | Greaves |
|---|---|---|---|
| StuffEffectMultiplierArmor | 0.55 | 0.45 | 0.55 |
| MoveSpeed offset (studded only) | −0.05 | 0 | −0.05 |

Insulation: moderate cold multiplier, low heat multiplier (tune in XML).

**Studded bonus** (added by `StatPart_StuddedMetal`):
`bonus_X = metal.StuffPower_Armor_X × StudFactor`, `StudFactor = 0.3`,
applied to `ArmorRating_Sharp` and `ArmorRating_Blunt`, before quality.

Worked examples (jerkin): plain leather ≈ 0.45 sharp / 0.13 blunt;
bear ≈ 0.62 sharp; steel studs +0.27 / +0.14; plasteel +0.34 / +0.17;
gold/silver +0.22 / +0.11; plasteel-studded bear ≈ 0.96 / 0.30.

## Costs, research, benches

| Piece | Leather | Studded extra metal | Work plain / studded |
|---|---|---|---|
| Jerkin | 70 | 25 | 6000 / 9000 |
| Cap | 25 | 10 | 2000 / 3000 |
| Greaves | 45 | 15 | 4000 / 6000 |

- Plain: no research, Neolithic tech level, hand + electric tailoring bench,
  generated via `recipeMaker`.
- Studded: `Smithing` research, Medieval tech level, same benches,
  hand-written RecipeDefs (no `recipeMaker`) with two ingredient slots: any
  leather (`Leathers` category) and any metal (`Metallic` stuff category).

## NPC generation

- Plain pieces: apparelTags matching tribal pawn kinds (e.g. `Neolithic`).
- Studded pieces: apparelTags matching medieval / outlander militia kinds
  (e.g. `MedievalMilitary`). Exact tags confirmed against vanilla PawnKindDefs
  during implementation.

## Code

New assembly `Leatherworks.dll`, Harmony-based, same csproj pattern as
Druidkin (auto-detected RimWorld path, Harmony dropped into `Assemblies/`).

1. **`CompProperties_StuddedMetal` / `CompStuddedMetal`**
   - Field `ThingDef metal`, saved with `Scribe_Defs`.
   - On `Initialize`, picks a random metal weighted toward steel (covers
     raider gear, traders, dev spawns). Crafting overwrites it.
   - `TransformLabel` appends `(metal)`: "bear leather studded jerkin (plasteel)".
   - `CompInspectStringExtra` names the stud metal.
2. **`StatPart_StuddedMetal`**
   - Added to `ArmorRating_Sharp` and `ArmorRating_Blunt` via XML
     `PatchOperationAdd` on the StatDefs' `parts`.
   - `TransformValue`: if `req.Thing` has the comp with a metal, add the bonus.
   - `ExplanationPart`: "Plasteel studs: +0.34".
   - Bonus math is a pure static function (unit-testable).
3. **Harmony postfix on `GenRecipe.MakeRecipeProducts`**
   - For each product with `CompStuddedMetal`, find the consumed ingredient
     whose def is Metallic stuff and store it on the comp.

**Assumption to verify first** (decompile `Toils_Recipe`/`GenRecipe`): with
studded defs listing only `Leathery`, the leather is chosen as the dominant
ingredient/stuff, and the metal is not. If false, adjust by setting the
product stuff explicitly in the postfix.

## Textures

Placeholder art tinted from vanilla (flak vest → jerkin, simple helmet → cap,
pants → greaves) in leather tones, plus a stud overlay for studded variants.
All body types (Male, Female, Thin, Fat, Hulk) and facings (north, south,
east) for worn graphics, plus item icons. The generating script is committed
so art can be regenerated or replaced. Workshop note: derived vanilla art is
common practice but is Ludeon's art.

## Save safety

Removing the mod drops unknown items/comp data with standard warnings. The comp
stores a single def reference; no other persistent state.

## Testing

- **Unit:** stud bonus math; label formatting.
- **Content validation** (mirrors `Druidkin.Tests/ContentValidationTests`):
  every studded def has the comp and a recipe; every texture path exists for
  each body type and facing; defs parse.
- **Manual in-game:** craft each piece; check info-card stats plain vs studded
  and steel vs plasteel; spawn tribal and medieval raids; save and reload.

## Out of scope

- Studs tinted by metal color.
- Druidkin integration.
- Balance beyond the starting numbers (tune after playtesting).
