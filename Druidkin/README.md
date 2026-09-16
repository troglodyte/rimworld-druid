# Druidkin - Wild Shape

RimWorld 1.6 mod (Biotech required). Adds a "Druid" xenotype whose gene grants a
Wild Shape ability: transform into an animal's body, keep your gear safely stashed,
and shift back whenever you like.

## Status

Builds clean against the real RimWorld 1.6.4871 assemblies (verified on this
machine at `~/snap/steam/common/.local/share/Steam/steamapps/common/RimWorld`).
All Def XML field names were cross-checked by reflecting on `Assembly-CSharp.dll`
and against the vanilla Defs shipped in `Data/Core` and `Data/Biotech`.

**Not yet tested in-game** - this environment can't drive the actual game UI.
The mod is symlinked into your local Mods folder:

```
~/snap/steam/common/.local/share/Steam/steamapps/common/RimWorld/Mods/Druidkin
  -> ~/code/rimworld-mods/Druidkin
```

## How it works

- `GeneDef Druidkin_WildShape` grants `AbilityDef Druidkin_WildShapeAbility`.
- `XenotypeDef Druidkin_Druid` bundles that gene into a selectable xenotype at
  character creation (or via a xenogerm later).
- Using the ability opens a form-picker; picking a form:
  1. Strips your weapon, worn apparel, and inventory into a holding list (not
     dropped on the ground) - see `WildShapeUtility.StripGear`.
  2. Generates the chosen animal, copies your name and needs (hunger/rest) over,
     force-trains it so it's fully obedient/loyal, and spawns it in your place.
  3. Despawns your human body into `Find.WorldPawns` (kept alive, not deleted).
  4. Tags the animal with `Hediff_WildShapeForm`, which remembers your original
     pawn + stashed gear and counts down the shift's duration.
- A Harmony postfix on `Pawn.GetGizmos` adds a "revert to human" button to any
  pawn wearing that hediff (the animal has no gene of its own to hang an ability
  off, so this is injected directly). Reverting respawns your human body and
  hands your gear back via `WildShapeUtility.RestoreGear`.
- A second Harmony patch blocks the Slaughter designator from targeting a
  shifted druid, since it'll otherwise sit in the Animals tab looking like
  livestock.

## Design choices worth knowing about

- **Combat model**: the animal form is a fully player-owned, obedience-trained
  animal (auto-defends, can be given "attack" orders the way any trained combat
  animal can) - **not** a draftable colonist. Making a non-humanlike pawn draft
  and job-queue like a colonist requires cloning each animal's race def with a
  custom think tree, which is a much larger and more fragile undertaking. This
  was a deliberate scope cut for v1; flag it if you want to push further.
- **Health**: injuries don't map organ-by-organ between a human body and, say,
  a bear's. Instead we carry over overall health loss as one proxy
  `Druidkin_ResidualWounds` hediff that heals over time.
- **Duration**: default 60,000 ticks (1 in-game day) per shift, set on
  `CompProperties_AbilityWildShape.durationTicks` in
  `Defs/AbilityDefs/Abilities_Druidkin.xml`. Ability cooldown is a separate,
  much shorter 2,500 ticks - tune both to taste.

## Building

```
export PATH="$HOME/.dotnet:$PATH"   # if using the sandbox-installed SDK
cd Source/Druidkin
dotnet build
```

The csproj references RimWorld's managed DLLs directly via `RimWorldManagedDir`
in `Druidkin.csproj` - update that path if you ever reinstall RimWorld elsewhere.
A post-build step copies the built DLL straight into `Assemblies/`.

## Testing checklist (do this in-game)

1. Launch RimWorld, enable **Druidkin - Wild Shape** in the Mods menu (Biotech
   must also be enabled), restart when prompted.
2. Start a new colony, create or edit a pawn with the **Druid** xenotype (or
   spawn one via Dev Mode).
3. Equip that pawn with a weapon + apparel + something in inventory, then use
   the Wild Shape ability and confirm:
   - the animal appears in your place, keeps the pawn's name;
   - no errors in the dev console (`~` key) about missing PawnKindDefs/icons;
   - gear is *not* on the ground - it's being held by the hediff.
4. Use the "revert to human" gizmo on the animal and confirm the human pawn
   reappears with weapon/apparel/inventory intact.
5. Try slaughtering the shifted animal from the Animals tab - it should be
   blocked with the "not livestock" message.
6. Save and reload mid-shift to confirm the hediff's stored pawn/gear survive
   a save (this exercises `Scribe_References`/`Scribe_Collections`).

## Known gaps / art needed

- The gene, xenotype, and ability icons under `Textures/` are placeholder leaf
  PNGs (128x128). Overwrite them in place when you're ready for real art; the
  xenotype icon should stay a white silhouette since the UI tints it.
- Only 6 forms are wired up (Rat, Timber Wolf, Cougar, Grizzly Bear, Muffalo,
  Megasloth). Add more by dropping additional `Druidkin.DruidkinAnimalFormDef`
  entries in `Defs/DruidkinAnimalFormDefs/`.
