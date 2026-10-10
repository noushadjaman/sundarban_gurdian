# Sundarban Guardian — Real Data Reference Sheet

Quick-reference document of all real datasets, population statistics, government policies, coordinates, third-party asset licenses, and data gaps currently implemented in **Sundarban Guardian**.

---

## 1. NASA Satellite Data

### A. MODIS NDVI — Vegetation Index (Product: MOD13Q1.061, 250m, 16-Day)
* **Katka Tiger Range (Healthy Canopy)**: `NDVI = 0.8193` (Date: 2025-12-03) | **[CONFIRMED REAL]** | Source: NASA AppEEARS / Terra MODIS | Used in: [`Assets/level_tiger_01.json`](file:///f:/SundarbanGuardian/Assets/level_tiger_01.json) & CSV (`tigerkatka`).
* **Katka Tiger Deforestation Zone**: `NDVI = 0.11` | **[PLACEHOLDER / ANOMALY SIMULATION]** | Simulated deforestation threshold | Used in: [`Assets/StreamingAssets/Levels/level_tiger_01.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_tiger_01.json) (`deforested_wasteland_01`).
* **Kotka Deer Deforestation Clearing**: `NDVI = 0.12` | **[PLACEHOLDER / ANOMALY SIMULATION]** | Simulated forest clearing threshold (real CSV coord `deerkotka` hit water mask fill `-3000`) | Used in: [`Assets/StreamingAssets/Levels/level_deer_01.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_deer_01.json) & [`NDVIVegetationPlacer.cs`](file:///f:/SundarbanGuardian/Assets/Editor/NDVIVegetationPlacer.cs).
* **Meghna Estuary Transect Points (Hilsha)**: Lower Meghna (`0.6316`), Confluence (`0.3286`), Goalondo (`0.2434`) (Date: 2025-12-19) | **[CONFIRMED REAL IN CSV]** | Source: NASA AppEEARS CSV | Used in: Raw CSV dataset; not bound to active Hilsha gameplay mechanics.

### B. MODIS Land Surface Temperature — Thermal Proxy (Product: MOD11A2.061, 1km, 8-Day)
* **Lower Meghna (Bhola / Monpura)**: `22.71°C` (295.86 K - 273.15, Date: 2025-12-19) | **[CONFIRMED REAL]** | Source: NASA AppEEARS / Terra MODIS | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) (Corridor 20 km).
* **Mid Meghna (Barisal / Mehendiganj)**: `21.17°C` (294.32 K - 273.15, Date: 2025-12-19) | **[CONFIRMED REAL]** | Source: NASA AppEEARS / Terra MODIS | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) (Corridor 45 km).
* **Meghna-Padma Confluence (Chandpur)**: `21.53°C` (294.68 K - 273.15, Date: 2025-12-19) | **[CONFIRMED REAL]** | Source: NASA AppEEARS / Terra MODIS | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) (Corridor 70 km).
* **Padma Upstream (Mawa / Munshiganj)**: `19.91°C` (293.06 K - 273.15, Date: 2025-12-19) | **[CONFIRMED REAL]** | Source: NASA AppEEARS / Terra MODIS | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) (Corridor 100 km).
* **Padma-Jamuna Confluence (Goalondo)**: `19.79°C` (292.94 K - 273.15, Date: 2025-12-19) | **[CONFIRMED REAL]** | Source: NASA AppEEARS / Terra MODIS | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) (Corridor 130 km).
* **Bay of Bengal Mouth (Hatiya offshore)**: `23.50°C` | **[PLACEHOLDER / INTERPOLATED PROXY]** | CSV returned `0.0` fill (open ocean mask); 23.50°C was interpolated to complete gradient | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) (Corridor 0 km).
* **Katka Tiger Range Ground LST**: `19.35°C` (292.50 K - 273.15, Date: 2025-12-19) | **[CONFIRMED REAL IN CSV]** | Source: NASA AppEEARS CSV (`tigerkatka`); recorded in CSV, not injected into Tiger level JSON.

### C. Other NASA Datasets Referenced
* **Landsat 1/7/8/9 Multispectral (30m, 1973–2024)**: Canopy loss time-series (94.2% -> 91.5%) | **[CONFIRMED REAL STUDY]** | Source: NASA Landsat Earth Observation & The Change Initiative | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) (Slide 2).
* **NASA Worldview Satellite Imagery**: Optical True-Color / NIR composite mosaic texture (`nasa_sundarbans_landsat.jpg`) | **[CONFIRMED REAL IMAGERY]** | Source: NASA Worldview (`worldview.earthdata.nasa.gov`) | Used in: Radar minimap background in [`MinimapController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/MinimapController.cs) and [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).
* **NASA PO.DAAC SMAP L3 Sea Surface Salinity (CAP v5.0)**: Cited for estuarine salinity baselines | **[PROXY / LITERATURE BACKED]** | Real SMAP product cited, but numbers are literature approximations due to coastal grid-scale limits | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) & [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).
* **NASA Carbon Monitoring System (CMS) & UNESCO**: Blue carbon storage metric (mangroves sequester 4x carbon/ha vs terrestrial forests) | **[CONFIRMED REAL RESEARCH]** | Source: NASA CMS & UNESCO | Used in: Secret Kotka Sanctuary Exploration Trigger in [`BuildSundarbanWorldCorrected.cs`](file:///f:/SundarbanGuardian/Assets/Editor/BuildSundarbanWorldCorrected.cs).
* **ISS Orbital Optical Spectrometry Feed**: Altitude 408 km, 21°56'N, 89°11'E telemetry header | **[FORMATTED CONTEXT]** | Source: NASA Earth Observation ISS orbital parameters | Used in: Telemetry badge in [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs).

---

## 2. Wildlife Population Statistics

* **Royal Bengal Tiger Population Milestones**:
  - `440` (2004, Historical Pugmark Estimate)
  - `106` (2015, First Camera-Trap Survey crash / poaching crisis)
  - `114` (2018, Camera-Trap Survey initial rebound)
  - `125` (2024, Latest Official Survey, +19 rebound from 2015)
  - Source: Bangladesh Forest Department official camera-trap census (2024) | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) (Slide 3) & [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).
* **Spotted Deer (Chital) Population**:
  - `83,000` (2004 Baseline) -> `140,357` (2023 Survey, +69% net growth / 57,000+ increase)
  - Source: Bangladesh Forest Department Wildlife Survey (2023) | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) (Slide 4).
* **Ganges River Dolphin (*Platanista gangetica*)**:
  - `225` (2001 Baseline) -> `159` (Recent Survey, -29% decline)
  - Source: Wildlife Conservation Society (WCS) Bangladesh survey data | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) (Slide 5).
* **Irrawaddy Dolphin (*Orcaella brevirostris*)**:
  - `451` (2006 Baseline) -> `198` (Recent Survey, -56% collapse)
  - Source: Wildlife Conservation Society (WCS) Bangladesh survey data | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) (Slide 5).
* **Dolphin Net Entanglement Mortality**:
  - `108` confirmed dolphin deaths documented between 2007–2019, with >33% directly caused by illegal gillnet entanglement
  - Source: Wildlife Conservation Society (WCS) Bangladesh | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) (Slide 5 alert quote box).
* **Sundarbans Dense Mangrove Canopy Coverage**:
  - `94.2%` (1973) -> `91.5%` (2024) (-2.7% net loss, representing over 160 sq km of primary dense canopy degraded)
  - Source: NASA Landsat satellite sensor archive (1973–2024) & The Change Initiative study | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) (Slide 2).

---

## 3. Government Policy Facts

* **Mother Hilsa Conservation Campaign (মা ইলিশ সংরক্ষণ অভিযান)**:
  - Exact Duration: 22-day total nationwide ban every October during peak Ashwin full-moon spawning cycle.
  - Restrictions: Complete prohibition on catching, landing, transporting, marketing, selling, and possessing Hilsa.
  - Source: Ministry of Fisheries and Livestock, Bangladesh | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) & [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).
* **Jatka Protection Program (জাটকা সংরক্ষণ কর্মসূচি)**:
  - Exact Duration: 8-month ban annually from November 1 to June 30.
  - Scope: Total prohibition on catching juvenile Hilsa (Jatka) under 25 cm (10 inches).
  - Source: Protection and Conservation of Fish Act (amended) / Dept. of Fisheries | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) & [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).
* **Legal Penalties for Violations**:
  - Minimum 1 to maximum 2 years rigorous imprisonment, and/or up to 500,000 BDT (৫ লক্ষ টাকা) financial fine.
  - Source: Protection and Conservation of Fish Rules | Used in: [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs) & Hilsha Level Fact Card.
* **2024 Spawning Impact Metric**:
  - `52%` of mother Hilsa successfully released eggs safely during the 2024 conservation campaign window.
  - Source: Bangladesh Fisheries Research Institute (BFRI) & Ministry of Fisheries and Livestock 2024 Evaluation Report | Used in: [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json) & [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).

---

## 4. Real-World Coordinates

* **Katka / Kotka Range (Tiger Level)**: `21.87°N, 89.75°E` — Kotka Sanctuary, Sundarbans East Wildlife Sanctuary | Used in: [`level_tiger_01.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_tiger_01.json), [`Assets/level_tiger_01.json`](file:///f:/SundarbanGuardian/Assets/level_tiger_01.json), CSVs.
* **Kotka Wildlife Sanctuary (Deer Level)**: `21.87°N, 89.75°E` (Level JSON) / `21.83°N, 89.73°E` (CSV query point) — Primary Spotted Deer grazing sanctuary | Used in: [`level_deer_01.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_deer_01.json) & CSV (`deerkotka`).
* **Passur River Estuary (Dolphin Level)**: `21.82°N, 89.60°E` — Confluence of Passur River & Bay of Bengal outer channels | Used in: [`level_dolphin_01.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_dolphin_01.json).
* **Chandpur Confluence (Dolphin Survey & Hilsha Migration Point 4)**: `23.23°N, 90.66°E` — Padma-Meghna confluence, key Gangetic dolphin sanctuary and Hilsa migration hub | Used in: CSVs (`dolphinchandpur`, `hilshapoint4confluence`) & Hilsha corridor (70 km).
* **Bay of Bengal Mouth (Hilsha Migration Point 1)**: `22.05°N, 91.05°E` — Hatiya offshore marine entry corridor (0 km) | Used in: CSV (`hilshapoint1mouth`) & [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json).
* **Lower Meghna (Hilsha Migration Point 2)**: `22.45°N, 90.75°E` — Bhola / Monpura estuarine channel (20 km) | Used in: CSV (`hilshapoint2lowermeghna`) & [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json).
* **Mid Meghna (Hilsha Migration Point 3)**: `22.85°N, 90.65°E` — Barisal / Mehendiganj sanctuary corridor (45 km) | Used in: CSV (`hilshapoint3midmeghna`) & [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json).
* **Padma Upstream (Hilsha Migration Point 5)**: `23.45°N, 90.35°E` — Mawa / Munshiganj riverine stretch (100 km) | Used in: CSV (`hilshapoint5padma`) & [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json).
* **Padma-Jamuna Confluence (Hilsha Migration Point 6)**: `23.85°N, 89.75°E` — Goalondo terminal spawning grounds (130 km) | Used in: CSV (`hilshapoint6goalondo`) & [`level_hilsha_bongoposagor_to_meghna.json`](file:///f:/SundarbanGuardian/Assets/StreamingAssets/Levels/level_hilsha_bongoposagor_to_meghna.json).
* **Minimap Bounding Box**: South: `21.80°N`, North: `23.90°N`, West: `89.50°E`, East: `91.00°E` — Calibrated GIS bounding box for the entire interactive GPS radar | Used in: [`MinimapController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/MinimapController.cs) & [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).
* **ISS Orbital Header Telemetry**: `21°56'N, 89°11'E` (`21.93°N, 89.18°E`) — Approximate geographic center of the Bangladesh Sundarbans mangrove reserve | Used in: [`IntroSequenceController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/UI/IntroSequenceController.cs) orbital telemetry overlay.

---

## 5. Imported Assets with Licensing Requirements

* **Poly by Google Assets (License: CC-BY 3.0 / 4.0 — Requires author attribution)**:
  - *Saltwater Crocodile* (`Mesh_Crocodile.obj`, `Tex_Crocodile.png`, ID: `2an6E2WjW3z`) | Credit: "Crocodile by Poly by Google (CC-BY)" | ⚠️ **NOT displayed in UI — needs addition to in-game credits screen**.
  - *King Cobra* (`cobra.obj`, `cobraTxt.png`, ID: `40_5Xq467-U`) | Credit: "Cobra by Poly by Google (CC-BY)" | ⚠️ **NOT displayed in UI — needs addition**.
  - *Mangrove Bird* (`Bird_01.obj`, ID: `8Ph79kHbt9s`) | Credit: "Bird by Poly by Google (CC-BY)" | ⚠️ **NOT displayed in UI — needs addition**.
  - *Lady Palm Tree* (`LadyPalmTree.obj`, ID: `e0AbYA90ux5`) | Credit: "Lady palm tree by Poly by Google (CC-BY)" | ⚠️ **NOT displayed in UI — needs addition**.
  - *Thatch Palm Tree* (`KeyThatchPalmTree.obj`, ID: `74GpKCkUqcn`) | Credit: "Thatch palm tree by Poly by Google (CC-BY)" | ⚠️ **NOT displayed in UI — needs addition**.
  - *Freshwater Pond* (`Pond.obj` / `Pond.glb`, ID: `5rf3YuZfJAW`) | Credit: "Pond by Poly by Google (CC-BY)" | ⚠️ **NOT displayed in UI — needs addition**.
* **Quaternius Assets (License: CC0 1.0 Universal — Public Domain / Attribution Recommended)**:
  - *Trees & Flora*: Common Tree 3 (`QVOop92WmG`), Twisted Tree 4 (`7PDBpElkQr`), Dead Tree 2 (`Mcd2zYqyww`) | Credit: "Nature Kit by Quaternius (CC0)" | ⚠️ **NOT displayed in UI — needs addition**.
  - *Aquatic Pack*: Dolphin, Manta ray, Shark, Whale, Fish 1-3 (`Assets/ExternalModels/Animals/fish/`) | Credit: "Fish Pack by Quaternius (CC0)" | ⚠️ **NOT displayed in UI — needs addition**.
* **Madtrollstudio (License: Sketchfab Basic / CC-BY)**:
  - *Village Dog* (`dogFinal (5).fbx`, ID: `H2SkxSJCfI`) | Credit: "Dog by madtrollstudio (CC-BY)" | ⚠️ **NOT displayed in UI — needs addition**.
* **NASA Earth Science Data & Worldview (License: Public Domain / Open Data)**:
  - *Landsat-7/8 Delta Mosaic & ISS Space Photography* | Credit: "Satellite imagery: NASA Worldview (worldview.earthdata.nasa.gov)" | ✅ **DISPLAYED IN-GAME** on Minimap header/footer & Data Sources modal in [`HudController.cs`](file:///f:/SundarbanGuardian/Assets/Scripts/Core/HudController.cs).
* **TreePackVol.1 (License: Unity Standard Asset Store EULA)**:
  - Birch/palm/pine trunks, foliage meshes, Textures.com textures | Used for forest foliage | Game runtime deployment permitted; no mandatory in-game credit required by store EULA.
* **Fonts (Open Font Licenses)**:
  - *Liberation Sans*: SIL Open Font License 1.1 | License file at [`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`](file:///f:/SundarbanGuardian/Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt) | Bundled in project.
  - *Kalpurush Bangla Font*: GNU GPL / Free Bengali Font by Ekushey | Bundled in project.

---

## 6. Gaps (Honest Unbacked / Flavor Items)

* **Salinity Numeric Hazard Values (PSU)**: `28.5 PSU` (Tiger/Deer mudflats) and `28.2`, `12.5`, `4.8`, `1.4`, `0.3`, `0.1 PSU` (Hilsha corridor) are **literature/report approximations**, not direct satellite pulls (NASA AppEEARS lacks ocean salinity; SMAP SSS resolution of 40–70 km is too coarse for river channels).
* **Deforestation Anomaly NDVI Values (`0.11`, `0.12`, `0.22`)**: The hazard zone NDVI values in `level_tiger_01.json`, `level_deer_01.json`, and `BuildSundarbanWorldCorrected.cs` are **gameplay-tuned anomaly thresholds**, not separately queried 250m satellite pixels. The real pixel pulled for Katka was `0.8193` (healthy canopy).
* **Sound Pollution Decibel Reading (`88.0 dB`)**: In `level_dolphin_01.json` (`motorized_trawler_01`), `88.0 dB` is an **acoustic literature estimate** for diesel outboard motors, not in-situ acoustic hydrophone telemetry.
* **Channel Water Depth (`1.2 m`)**: In `level_dolphin_01.json` (`shallow_mudbank_01`), `1.2 m` is a **gameplay mechanic parameter**, not hydrographic bathymetric chart data.
* **Crocodile Territory Danger Rating (`40.0`)**: In `level_deer_01.json` & `level_dolphin_01.json`, `40.0` is an **internal combat damage value**, formatted into the raw reading field for UI display.
* **Poacher Wire Snare Hazard Value (`0.18`)**: In `level_tiger_01.json` & `level_deer_01.json`, `0.18` is an **arbitrary placeholder score**, not a biological or satellite metric.
* **Bay of Bengal River Mouth SST (`23.50°C`)**: In `level_hilsha_bongoposagor_to_meghna.json`, the 0 km offshore point was **manually interpolated** because the MODIS MOD11A2 land algorithm masked the open water pixel to fill value `0.0`.
* **Live Dynamic Streaming vs Static Extracts**: All satellite and survey data in the current build are **static pre-baked extracts** (Dec 2024 – Dec 2025 MODIS records and historical census benchmarks); there is currently no live REST/CMR streaming API active at runtime.
