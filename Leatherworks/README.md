# Leatherworks

Standalone RimWorld 1.6 mod (core only). Adds a leather jerkin, cap and greaves, each plain and studded.
Design: `docs/superpowers/specs/2026-09-29-leatherworks-design.md`.

## Build and test

```
export DOTNET_ROOT=~/.dotnet DOTNET_ROLL_FORWARD=Major PATH=~/.dotnet:~/.dotnet/tools:$PATH
cd Source/Leatherworks && dotnet build          # copies Leatherworks.dll + 0Harmony.dll into Assemblies/
cd ../Leatherworks.Tests && dotnet test
```

## Notes

- Textures reuse vanilla art (flak vest, simple helmet, flak pants), tinted by the leather's stuff color.
  Vanilla textures ship packed in `resources.assets`, so there are no loose PNGs to recolor; custom art
  can replace the `texPath`/`wornGraphicPath` values later. Greaves (like vanilla pants) have no worn graphic.
- The stud bonus is `StatPart_StuddedMetal`, inserted at priority 50 on `ArmorRating_Sharp/Blunt`:
  after `StatPart_Stuff` (100), before `StatPart_Quality` (0), so quality scales it.
- Vanilla may pick the metal as the product's stuff; `Patch_MakeRecipeProducts` forces the leather back.
- Plain pieces use apparel tag `Neolithic` (tribals). Studded pieces use `IndustrialBasic`
  (outlanders, pirates, drifters). Core has no medieval pawn kinds.

## In-game verification checklist

- [ ] No def or config errors in `Player.log` on load. The Harmony patch applies.
- [ ] Craft each plain piece. Info card sharp/blunt match the spec (plain leather jerkin ≈ 0.45 / 0.13, bear ≈ 0.62 sharp).
- [ ] Craft studded jerkins with steel and with plasteel. Label suffix `(steel)`, inspect string, and explanation
      line are correct; bonus +0.27/+0.14 (steel), +0.34/+0.17 (plasteel); stuff is the leather, not the metal.
- [ ] Dev-spawn a studded piece: a random metal is assigned.
- [ ] Tribal raid wears the plain pieces. Outlander/pirate pawns wear the studded ones.
- [ ] Jerkin, cap and greaves layer correctly with a shirt, pants and a duster.
- [ ] Save, reload: the stud metal persists.
