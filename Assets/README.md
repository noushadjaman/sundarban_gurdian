# Sundarban Guardian — Unity Scaffold

## Folder structure
```
Assets/
  Scripts/
    Data/LevelData.cs          — JSON data classes (levels, hazard zones, Hilsha corridor)
    Core/LevelLoader.cs        — reads level_*.json, spawns hazard zones (Tiger/Deer/Dolphin)
    Core/HazardZoneTrigger.cs  — per-zone damage + reports real data value to HUD
    Core/HudEvents.cs          — decoupled event bus (zone entered/exited)
    Core/SpeciesHealth.cs      — shared health component for all species
    Player/LandAnimalController.cs   — SHARED by Tiger and Deer (tune speed per prefab)
    Player/DolphinSwimController.cs  — Gangetic Dolphin, river-plane swim + breathing mechanic
    Player/HilshaMigrationController.cs — the differentiator: real salinity/SST corridor
  StreamingAssets/
    Levels/level_tiger_01.json
    Levels/level_hilsha_bongoposagor_to_meghna.json
data_prep/
  data_prep.py — pulls real NASA data to replace the placeholder JSON values
```

## Setup steps in Unity
1. Create a new **3D (URP)** project, import this `Assets/` folder into it.
2. Import an **Animator Controller** per species with parameters: `Speed` (float), and for Dolphin also `Surfaced` (bool), for Hilsha also `Stressed` (bool).
3. Build the **HazardZone prefab**: a flat cylinder (or plane) with a trigger `CapsuleCollider`/`BoxCollider` set to `Is Trigger`, plus the `HazardZoneTrigger` component.
4. For **Tiger/Deer**: put `CharacterController` + `Animator` + `LandAnimalController` on each prefab. Set `moveSpeed` differently per prefab (Deer faster, Tiger heavier).
5. For **Gangetic Dolphin**: `Rigidbody` (gravity off, constrain rotation X/Z if it looks unstable) + `Animator` + `DolphinSwimController`.
6. For **Hilsha**: `Rigidbody` + `Animator` + `SpeciesHealth` + `HilshaMigrationController`. Build the level as a long river-shaped mesh/spline; the controller doesn't need the mesh to match exactly since movement is code-driven, but visuals should follow the same path.
7. Drop `LevelLoader` on an empty `LevelManager` GameObject for Tiger/Deer/Dolphin scenes; call `LoadLevel("level_tiger_01")` from your GameManager's `Start()`.
8. Run `data_prep/data_prep.py` (after filling in the three `NotImplementedError` functions with real `earthaccess`/`xarray` granule reads) to overwrite the placeholder JSON numbers with real satellite readings before your final demo build.

## Suggested build order (matches your stated priority)
1. Deer (simplest — reuses `LandAnimalController`, get the hazard-zone loop working end-to-end first)
2. Royal Bengal Tiger (same script, just different prefab tuning + NDVI-based level)
3. Gangetic Dolphin (new controller, river-bounded swim + breathing mechanic)
4. Hilsha migration (the differentiator — save enough time for this one, it has the most unique logic)

## Not yet included (next scripts to ask for when ready)
- GameManager (level select, win/lose screen, score)
- Cloudflare Worker for live data fetch + Bengali fact-card generation via LLM
- Firebase Firestore leaderboard wiring
- Mobile on-screen joystick/button UI (referenced as `FixedJoystick` stub — swap for a real asset)
