using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SundarbanGuardian.Editor
{
    /// <summary>
    /// Complete Sundarban Map Generator.
    /// Key Features:
    /// 1. DANGER ZONES: Contains ONLY sparse dead trees and open empty space (zero green trees/foliage).
    /// 2. FOREST: Uses clean decorator trees (Maple, Birch, Pine, Cherry, Palms) with NO strange hanging weeping willows.
    /// 3. MOUNTAIN PERIMETER: Giant 3D mountains and 2D panoramic mountain backdrops enclose the map edges.
    /// 4. INVISIBLE WALLS: Tall boundary colliders completely prevent falling off the map into the void.
    /// 5. ZERO ROADS OVER RIVER: All roads and canals stay strictly on inland banks.
    /// 6. BENGAL TIGER: Properly oriented and attached on all fours.
    /// </summary>
    public static class BuildSundarbanFullMap
    {
        // Map Dimensions
        public const float MapXMin = -125f;
        public const float MapXMax =  125f;
        public const float MapZMin = -105f;
        public const float MapZMax =  285f;
        public const float BankY   =  0.0f;
        public const float WaterY  =  0.04f;

        // Hazard Zones (Expanded Danger Zone Sizes): Deforested Wasteland, Poacher Trap Zone, Hypersaline Flat, Crocodile Crossing
        public static readonly (float x, float z, float r)[] HazardZones =
        {
            (-65f,  75f, 30f), // Deforested wasteland / clear-cut zone (West bank, far inland)
            (-52f,  45f, 22f), // Poacher wire-trap zone (West bank)
            (-48f, 130f, 24f), // Hypersaline mudflat (West bank)
            ( 48f,  55f, 20f), // Crocodile crossing threat zone (East bank)
        };

        public static bool IsInHazardZone(float x, float z, float buffer = 3.5f)
        {
            foreach (var hz in HazardZones)
            {
                float totalR = hz.r + buffer;
                float dx = x - hz.x;
                float dz = z - hz.z;
                if (dx * dx + dz * dz <= totalR * totalR)
                    return true;
            }
            return false;
        }

        // River curve: Authentic serpentine meandering S-curves with pronounced curves matching Landsat radar
        public static float RiverCenterX(float z)
        {
            float t = Mathf.InverseLerp(MapZMin, MapZMax, z);
            // Harmonic sinuosity creating distinct river bends and meanders
            return Mathf.Sin(t * Mathf.PI * 4.2f) * 28f
                 + Mathf.Sin(t * Mathf.PI * 2.1f + 0.4f) * 16f
                 + Mathf.Sin(t * Mathf.PI * 8.4f) * 4.5f;
        }

        public static float RiverHalfWidth(float z)
        {
            float t = Mathf.InverseLerp(MapZMin, MapZMax, z);
            // Authentic river channel width: 21m - 27m total (half-width 10.5m - 13.5m), widening to 22m at southern estuary
            float u = Mathf.Clamp01(t / 0.35f);
            float ss = u * u * (3.0f - 2.0f * u);
            float estuary = Mathf.Lerp(22f, 10.5f, ss);
            float bendSwell = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4.2f)) * 2.8f;
            return estuary + bendSwell;
        }

        // Shoreline intertidal beach / sand shelf width
        public static float BeachWidth(float z)
        {
            float t = Mathf.InverseLerp(MapZMin, MapZMax, z);
            float u = Mathf.Clamp01(t / 0.30f);
            float ss = u * u * (3.0f - 2.0f * u);
            float baseW = Mathf.Lerp(14f, 7.5f, ss);
            float wave = Mathf.Sin(t * Mathf.PI * 5.0f) * 1.5f;
            return Mathf.Clamp(baseW + wave, 6f, 16f);
        }

        // Exact physical ground surface elevation for seamless object placement
        public static float GetGroundY(float x, float z)
        {
            float rxC = RiverCenterX(z);
            float rhw = RiverHalfWidth(z);
            float beachW = BeachWidth(z);
            float dist = Mathf.Abs(x - rxC);

            // Inside River channel
            if (dist <= rhw)
            {
                float u = dist / rhw;
                return Mathf.Lerp(-1.4f, WaterY, u * u);
            }
            // On sloping shoreline beach
            if (dist <= rhw + beachW)
            {
                float tSlope = (dist - rhw) / beachW;
                return Mathf.Lerp(WaterY, 0.48f, 0.5f - 0.5f * Mathf.Cos(tSlope * Mathf.PI));
            }
            // On solid elevated riverbank land
            float outerLimit = (x < rxC) ? Mathf.Abs(MapXMin - (rxC - rhw - beachW)) : Mathf.Abs(MapXMax - (rxC + rhw + beachW));
            float tInland = Mathf.Clamp01((dist - rhw - beachW) / Mathf.Max(outerLimit, 1f));
            float baseH = Mathf.Lerp(0.48f, 0.76f, Mathf.Sqrt(tInland));
            float rip1 = Mathf.Sin(x * 0.35f + z * 0.28f) * 0.042f;
            float rip2 = Mathf.Cos(x * 0.18f - z * 0.45f) * 0.030f;
            return baseH + rip1 + rip2;
        }

        // Canals: strictly stay far inland on their respective banks, leaving a massive 70m+ continuous land island after the river!
        private static readonly (float sz, float ez, float bx, float amp, float freq, float hw)[] Canals =
        {
            (-80f, 180f, -88f,  5f, 2.1f, 3.2f),
            (  0f, 115f, -76f,  4f, 2.5f, 2.8f),
            (-60f, 200f,  88f,  5f, 1.8f, 3.2f),
            ( 40f, 230f,  96f,  4f, 3.5f, 2.5f),
            (150f, 275f,  85f,  4f, 2.2f, 2.8f),
            (-90f,  20f, -95f,  4f, 4.0f, 2.5f),
        };

        public static float CanalCenterX(int idx, float z)
        {
            var c = Canals[idx];
            float t = Mathf.InverseLerp(c.sz, c.ez, z);
            return c.bx + Mathf.Sin(t * Mathf.PI * c.freq) * c.amp;
        }

        // Roads: ONLY on far inland banks, NEVER near the river
        private static readonly (float sz, float ez, float bx, float amp, float freq)[] Roads =
        {
            (-90f, 270f, -98f, 5f, 2.4f), // Far inland West Bank
            (-50f, 250f,  98f, 5f, 3.1f), // Far inland East Bank
        };

        public static float RoadCenterX(int idx, float z)
        {
            var r = Roads[idx];
            float t = Mathf.InverseLerp(r.sz, r.ez, z);
            return r.bx + Mathf.Sin(t * Mathf.PI * r.freq) * r.amp;
        }

        [MenuItem("Sundarban/Build Full Sundarban Map")]
        public static void BuildMap()
        {
            Debug.Log("[BuildSundarbanFullMap] Starting map generation with mountains, clean danger zones, and boundary limits...");
            Random.InitState(42);

            // 1. Ensure Prefabs
            var prefabs = EnsureAllPrefabs();
            if (prefabs == null || prefabs.Count == 0)
            {
                Debug.LogError("[BuildSundarbanFullMap] Failed to load prefabs! Aborting.");
                return;
            }

            // 2. Load & update materials
            var mats = LoadOrCreateMaterials();

            // 3. Clean previous map objects
            ClearOldMapObjects();

            // 4. Build Riverbed and River Water
            var riverParent = new GameObject("Main_River_Shibsa");
            BuildRiverSystem(riverParent.transform, mats);

            // 5. Build Land Banks (West and East of River)
            var landParent = new GameObject("Sundarban_Riverbanks_Land");
            BuildBankLand(landParent.transform, mats);

            // 5.5 Build Sea Beach Surfaces (Kotka / Jamtola Golden Sand Shorelines)
            var beachParent = new GameObject("Sea_Beach_Shorelines");
            BuildSeaBeachShorelines(beachParent.transform, mats);
            DecorateSeaBeaches(beachParent.transform, prefabs);

            // 6. Build Tidal Canals (Khari - purely on bank land)
            var canalParent = new GameObject("Tidal_Canals_Khari");
            BuildAllCanals(canalParent.transform, mats);

            // 7. Build Curvy Mud Roads (STRICTLY on land, ZERO roads over river)
            var roadParent = new GameObject("Forest_Mud_Paths");
            BuildMudRoads(roadParent.transform, mats);

            // 8. Build Mountain Perimeter & Boundary Colliders (Enclose the map, prevent falling off)
            var mountainParent = new GameObject("Map_Boundaries_and_Mountains");
            BuildMountainsAndBoundaries(mountainParent.transform, mats);

            // 8.5 Build Ocean System (Bay of Bengal Open Horizon to the South)
            var oceanParent = new GameObject("BayOfBengal_Ocean_System");
            BuildOceanSystem(oceanParent.transform, mats);

            // 9. Plant Forest: Lush clean trees in forest, ONLY sparse dead trees in hazard zones
            var forestParent = new GameObject("Dense_Mangrove_Forest");
            PlantForest(forestParent.transform, prefabs, mats);

            // 9.5 Fill Ocean Sight with Coastal Palm Groves & Offshore Islets
            PlantCoastalPalmGroves(forestParent.transform, prefabs);

            // 10. Place Saltwater Crocodile
            PlaceCrocodile();

            // 11. Ensure Royal Bengal Tiger character visual
            EnsureBengalTiger();

            // 12. Disable any old flat planes that interfere
            DisableOldPlanes();

            // 13. Atmospheric lighting & mangrove mist
            SetupAtmosphere();

            // 14. Save scene
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            // 15. Focus scene view
            FocusSceneView();

            Debug.Log("[BuildSundarbanFullMap] Map successfully built with perimeter mountains, boundary colliders, clean danger zones, and zero roads over water!");
        }

        private static Dictionary<string, GameObject> EnsureAllPrefabs()
        {
            var dict = new Dictionary<string, GameObject>();
            string[] names = {
                // Authentic Mangrove Forest Trees with Stilt Roots (Low-Poly LOD1)
                "Tree_Stilt_Mangrove_LOD1",
                "Tree_Stilt_Mangrove_Tall_LOD1",
                "Tree_Mangrove_Canopy_LOD1",
                "Tree_Mangrove_Bush_LOD1",
                "Tree_LowPoly_Mangrove_Stilt",
                "Tree_LowPoly_Mangrove_Overhanging",
                // Nature Pack (Quaternius + FallTree)
                "Tree_New_CommonTree",
                "Tree_New_Pine",
                "Tree_New_Maple_1",
                "Tree_New_Maple_2",
                "Tree_New_Maple_3",
                "Tree_New_FallTree",
                "Tree_New_DeadTree",
                // Low-Poly Tropical Coastal Palms
                "Tree_Lightweight_CoconutPalm",
                "Tree_Lightweight_EvergladesPalm",
                "Tree_Lightweight_TrianglePalm",
                "Tree_Deco_DeadTree",
                // Authentic Mangrove Undergrowth (Ferns, Pencil Roots, Seedlings, Logs)
                "Detail_Mangrove_Fern",
                "Detail_Mangrove_Pneumatophore",
                "Detail_Mangrove_Seedling",
                "Detail_Mangrove_FallenLog",
                "GrassPatch",
                "Detail_Mangrove_Grass"
            };

            foreach (var n in names)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Environment/{n}.prefab");
                if (p == null)
                    p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Trees/{n}.prefab");
                if (p == null)
                    p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/NatureNew/{n}.prefab");
                if (p == null)
                    p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/LightweightNature/{n}.prefab");

                if (p != null) dict[n] = p;
            }

            return dict;
        }

        private static Dictionary<string, Material> LoadOrCreateMaterials()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            var mats = new Dictionary<string, Material>();

            // River Water
            // River Water: Vibrant marine blue close to the sea matching Bay of Bengal
            var riverMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
            if (riverMat == null)
            {
                riverMat = new Material(urpLit);
                AssetDatabase.CreateAsset(riverMat, "Assets/Materials/Sundarban_RiverWater.mat");
            }
            riverMat.shader = urpLit;
            riverMat.SetColor("_BaseColor", new Color(0.08f, 0.38f, 0.56f, 0.96f));
            riverMat.SetFloat("_Smoothness", 0.92f);
            riverMat.SetFloat("_Metallic", 0.05f);
            var waterNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Ocean_Water_Normal.tga");
            if (waterNor != null)
            {
                riverMat.SetTexture("_BumpMap", waterNor);
                riverMat.SetFloat("_BumpScale", 1.20f);
                riverMat.SetTextureScale("_BumpMap", new Vector2(25f, 50f));
                riverMat.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(riverMat);
            mats["river"] = riverMat;

            // Canal Water
            var canalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_CanalWater.mat");
            if (canalMat == null)
            {
                canalMat = new Material(urpLit);
                AssetDatabase.CreateAsset(canalMat, "Assets/Materials/Sundarban_CanalWater.mat");
            }
            canalMat.SetColor("_BaseColor", new Color(0.14f, 0.26f, 0.20f, 0.90f));
            canalMat.SetFloat("_Smoothness", 0.91f);
            EditorUtility.SetDirty(canalMat);
            mats["canal"] = canalMat;

            // Authentic Intertidal Mangrove Mud Surface (matching trial asset and user reference image)
            Material intertidalMat = null;
            var glbAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/ExternalModels/mangrove_environment.glb");
            foreach (var a in glbAssets)
            {
                if (a is Material && a.name == "Mangrove_Mud")
                {
                    intertidalMat = (Material)a;
                    break;
                }
            }
            if (intertidalMat == null)
            {
                intertidalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Intertidal_Mud.mat");
                if (intertidalMat == null)
                {
                    intertidalMat = new Material(urpLit);
                    AssetDatabase.CreateAsset(intertidalMat, "Assets/Materials/Mangrove_Intertidal_Mud.mat");
                }
                intertidalMat.shader = urpLit;
                intertidalMat.SetColor("_BaseColor", new Color(1.0f, 0.98f, 0.94f, 1.0f));
                intertidalMat.SetFloat("_Smoothness", 0.30f);
                intertidalMat.SetFloat("_Metallic", 0.0f);
                var interBaseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/MangroveMud_BaseMap.png");
                if (interBaseTex != null)
                {
                    intertidalMat.SetTexture("_BaseMap", interBaseTex);
                    intertidalMat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
                }
                var interNormTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/MangroveMud_NormalMap.png");
                if (interNormTex != null)
                {
                    intertidalMat.SetTexture("_BumpMap", interNormTex);
                    intertidalMat.SetFloat("_BumpScale", 1.5f);
                    intertidalMat.SetTextureScale("_BumpMap", new Vector2(2.6f, 2.6f));
                    intertidalMat.EnableKeyword("_NORMALMAP");
                }
                intertidalMat.enableInstancing = true;
                EditorUtility.SetDirty(intertidalMat);
            }

            // Unified surface: every ground surface in the world uses the authentic ripple mud material
            mats["mud_intertidal"] = intertidalMat;
            mats["mud_west"]       = intertidalMat;
            mats["mud_east"]       = intertidalMat;
            mats["beach_sand"]     = intertidalMat;
            mats["mud"]            = intertidalMat;

            // River Bed
            var bedMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverBed.mat");
            if (bedMat == null)
            {
                bedMat = new Material(urpLit);
                AssetDatabase.CreateAsset(bedMat, "Assets/Materials/Sundarban_RiverBed.mat");
            }
            bedMat.SetColor("_BaseColor", new Color(0.18f, 0.14f, 0.09f));
            bedMat.SetFloat("_Smoothness", 0.35f);
            mats["riverbed"] = bedMat;

            // 3D Mountains Material
            var mtnMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Mountains.mat");
            if (mtnMat == null)
            {
                mtnMat = new Material(urpLit);
                AssetDatabase.CreateAsset(mtnMat, "Assets/Materials/Sundarban_Mountains.mat");
            }
            mtnMat.SetColor("_BaseColor", new Color(0.24f, 0.30f, 0.22f));
            mtnMat.SetFloat("_Smoothness", 0.18f);
            if (intertidalMat != null && intertidalMat.mainTexture != null)
            {
                mtnMat.SetTexture("_BaseMap", intertidalMat.mainTexture);
                mtnMat.SetTextureScale("_BaseMap", new Vector2(8f, 8f));
            }
            EditorUtility.SetDirty(mtnMat);
            mats["mountains"] = mtnMat;

            // 2D Mountain Backdrop Panorama Material
            var backdropMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Mountain_Backdrop.mat");
            if (backdropMat == null)
            {
                backdropMat = new Material(urpUnlit);
                AssetDatabase.CreateAsset(backdropMat, "Assets/Materials/Sundarban_Mountain_Backdrop.mat");
            }
            var backdropTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/sundarban_mountains_backdrop.jpg");
            if (backdropTex != null) backdropMat.SetTexture("_BaseMap", backdropTex);
            backdropMat.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f, 1.0f));
            EditorUtility.SetDirty(backdropMat);
            mats["backdrop"] = backdropMat;

            // Ocean Water (Bay of Bengal - Infinite South Horizon)
            var oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_OceanWater.mat");
            if (oceanMat == null)
            {
                oceanMat = new Material(urpLit);
                AssetDatabase.CreateAsset(oceanMat, "Assets/Materials/Sundarban_OceanWater.mat");
            }
            oceanMat.shader = urpLit;
            oceanMat.SetColor("_BaseColor", new Color(0.10f, 0.36f, 0.52f, 0.96f));
            oceanMat.SetFloat("_Smoothness", 0.92f);
            oceanMat.SetFloat("_Metallic", 0.05f);
            var oceanNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Ocean_Water_Normal.tga");
            if (oceanNor != null)
            {
                oceanMat.SetTexture("_BumpMap", oceanNor);
                oceanMat.SetFloat("_BumpScale", 1.25f);
                oceanMat.SetTextureScale("_BumpMap", new Vector2(80f, 80f));
                oceanMat.EnableKeyword("_NORMALMAP");
            }
            oceanMat.enableInstancing = true;
            EditorUtility.SetDirty(oceanMat);
            mats["ocean"] = oceanMat;

            // Sea Beach Sand: Photorealistic golden sand with fine wave ripples & wet sand sheen
            var beachMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_BeachSand.mat");
            if (beachMat == null)
            {
                beachMat = new Material(urpLit);
                AssetDatabase.CreateAsset(beachMat, "Assets/Materials/Sundarban_BeachSand.mat");
            }
            var beachTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/sundarban_beach_sand.jpg");
            if (beachTex != null)
            {
                beachMat.SetTexture("_BaseMap", beachTex);
                beachMat.SetTextureScale("_BaseMap", new Vector2(12f, 18f));
            }
            var beachNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/sundarban_beach_sand_normal.png");
            if (beachNor != null)
            {
                beachMat.SetTexture("_BumpMap", beachNor);
                beachMat.SetFloat("_BumpScale", 1.25f);
                beachMat.SetTextureScale("_BumpMap", new Vector2(12f, 18f));
                beachMat.EnableKeyword("_NORMALMAP");
            }
            beachMat.SetColor("_BaseColor", new Color(0.96f, 0.90f, 0.78f, 1f));
            beachMat.SetFloat("_Smoothness", 0.40f);
            beachMat.SetFloat("_Metallic", 0.02f);
            EditorUtility.SetDirty(beachMat);
            mats["beach_sand"] = beachMat;

            return mats;
        }

        private static void ClearOldMapObjects()
        {
            string[] names = {
                "Main_River_Shibsa", "River_System", "Tidal_Canals_Khari", "Forest_Mud_Paths",
                "Dense_Mangrove_Forest", "Mangrove_Forest", "Eastern_Riverbank_Land",
                "Sundarban_Riverbanks_Land", "Sundarban_Ground", "Sundarban_Ground_Plane",
                "River_Bed_Underlay", "River_Saltwater_Crocodile", "Mangrove_Ponds_Group",
                "Map_Boundaries_and_Mountains", "Map_Boundaries_Colliders", "Perimeter_Mountains",
                "BayOfBengal_Ocean_System", "BayOfBengal_Ocean", "Coastal_Palm_Groves", "Coastal_Sandbar_Chars",
                "Sea_Beach_Shorelines", "Coastal_Beach_Decorations", "Kotka_Grand_SeaBeach"
            };
            foreach (var n in names)
            {
                var g = GameObject.Find(n);
                if (g != null) UnityEngine.Object.DestroyImmediate(g);
            }
        }

        private static void BuildRiverSystem(Transform parent, Dictionary<string, Material> mats)
        {
            const float zStep = 2.5f;
            const int xSegs = 16;
            int zCount = Mathf.CeilToInt((MapZMax - MapZMin) / zStep) + 1;

            var waterGO = new GameObject("River_Water_Surface");
            waterGO.transform.SetParent(parent, false);
            waterGO.AddComponent<MeshFilter>().sharedMesh = BuildRiverMesh(MapZMin, zStep, zCount, xSegs, true);
            waterGO.AddComponent<MeshRenderer>().sharedMaterial = mats["river"];

            var bedGO = new GameObject("River_Bed");
            bedGO.transform.SetParent(parent, false);
            bedGO.AddComponent<MeshFilter>().sharedMesh = BuildRiverMesh(MapZMin, zStep, zCount, xSegs, false);
            bedGO.AddComponent<MeshRenderer>().sharedMaterial = mats["riverbed"];
            bedGO.AddComponent<MeshCollider>();
        }

        private static Mesh BuildRiverMesh(float zStart, float zStep, int zCount, int xSegs, bool isWater)
        {
            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var colors = new List<Color>();
            var tris  = new List<int>();

            int w = xSegs + 1;
            for (int zi = 0; zi < zCount; zi++)
            {
                float z  = zStart + zi * zStep;
                float cx = RiverCenterX(z);
                float hw = RiverHalfWidth(z);
                // Water surface extends under the sloping sand beach shelf so there are zero seams
                float effectiveHw = isWater ? (hw + 3.0f) : hw;

                // Color gradient along river flow towards the sea (North green -> South ocean blue)
                float tSea = Mathf.InverseLerp(110f, -40f, z);
                tSea = Mathf.SmoothStep(0f, 1f, tSea);
                Color inlandCol = new Color(0.14f, 0.32f, 0.28f, 0.94f);
                Color seaCol    = new Color(0.08f, 0.38f, 0.58f, 0.96f);
                Color vertCol   = Color.Lerp(inlandCol, seaCol, tSea);

                for (int xi = 0; xi <= xSegs; xi++)
                {
                    float u = (float)xi / xSegs;
                    float x = Mathf.Lerp(cx - effectiveHw, cx + effectiveHw, u);
                    float y = isWater ? WaterY : (-1.4f * 4f * u * (1f - u) - 0.28f);

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, zi * 0.15f));
                    colors.Add(vertCol);
                }
            }

            for (int zi = 0; zi < zCount - 1; zi++)
            {
                for (int xi = 0; xi < xSegs; xi++)
                {
                    int i0 = zi * w + xi;
                    tris.Add(i0); tris.Add(i0 + w); tris.Add(i0 + 1);
                    tris.Add(i0 + 1); tris.Add(i0 + w); tris.Add(i0 + w + 1);
                }
            }

            var m = new Mesh { name = isWater ? "RiverWater" : "RiverBed" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            if (isWater) m.SetColors(colors);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void BuildBankLand(Transform parent, Dictionary<string, Material> mats)
        {
            var intertidalMat = mats.ContainsKey("mud_intertidal") ? mats["mud_intertidal"] : mats["mud_west"];

            var westGO = new GameObject("West_Riverbank_Land");
            westGO.transform.SetParent(parent, false);
            westGO.AddComponent<MeshFilter>().sharedMesh = BuildBankMesh(true);
            westGO.AddComponent<MeshRenderer>().sharedMaterial = intertidalMat;
            westGO.AddComponent<MeshCollider>();

            var eastGO = new GameObject("East_Riverbank_Land");
            eastGO.transform.SetParent(parent, false);
            eastGO.AddComponent<MeshFilter>().sharedMesh = BuildBankMesh(false);
            eastGO.AddComponent<MeshRenderer>().sharedMaterial = intertidalMat;
            eastGO.AddComponent<MeshCollider>();
        }

        private static Mesh BuildBankMesh(bool isWest)
        {
            const float zStep = 2.0f;
            const int uSegs = 28;
            int zCount = Mathf.CeilToInt((MapZMax - MapZMin) / zStep) + 1;
            int w = uSegs + 1;

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            for (int zi = 0; zi < zCount; zi++)
            {
                float z = MapZMin + zi * zStep;
                float riverEdgeX   = isWest ? (RiverCenterX(z) - RiverHalfWidth(z)) : (RiverCenterX(z) + RiverHalfWidth(z));
                float beachW       = BeachWidth(z);
                float beachInlandX = isWest ? (riverEdgeX - beachW) : (riverEdgeX + beachW);
                float outerX       = isWest ? MapXMin : MapXMax;

                for (int ui = 0; ui <= uSegs; ui++)
                {
                    float u = (float)ui / uSegs;
                    float x = isWest ? Mathf.Lerp(outerX, beachInlandX, u) : Mathf.Lerp(beachInlandX, outerX, u);

                    // Solid elevated land: starts at 0.48m at river crest, gently undulates up to 0.76m inland
                    float tInland = isWest ? (1f - u) : u; // 0 at shoreline, 1 at far boundary
                    float baseHeight = Mathf.Lerp(0.48f, 0.76f, Mathf.Sqrt(Mathf.Clamp01(tInland)));

                    // 3D mud ripple micro-relief matching normal map
                    float rip1 = Mathf.Sin(x * 0.35f + z * 0.28f) * 0.042f;
                    float rip2 = Mathf.Cos(x * 0.18f - z * 0.45f) * 0.030f;
                    float y = baseHeight + rip1 + rip2;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(x * 0.35f, z * 0.35f));
                }
            }

            for (int zi = 0; zi < zCount - 1; zi++)
            {
                for (int ui = 0; ui < uSegs; ui++)
                {
                    int i0 = zi * w + ui;
                    tris.Add(i0); tris.Add(i0 + w); tris.Add(i0 + 1);
                    tris.Add(i0 + 1); tris.Add(i0 + w); tris.Add(i0 + w + 1);
                }
            }

            var m = new Mesh { name = isWest ? "BankWest" : "BankEast" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void BuildSeaBeachShorelines(Transform parent, Dictionary<string, Material> mats)
        {
            var intertidalMat = mats.ContainsKey("mud_intertidal") ? mats["mud_intertidal"] : mats["beach_sand"];

            var westBeach = new GameObject("West_Sea_Beach_Surface");
            westBeach.transform.SetParent(parent, false);
            westBeach.AddComponent<MeshFilter>().sharedMesh = BuildBeachMesh(true);
            westBeach.AddComponent<MeshRenderer>().sharedMaterial = intertidalMat;
            westBeach.AddComponent<MeshCollider>();

            var eastBeach = new GameObject("East_Sea_Beach_Surface");
            eastBeach.transform.SetParent(parent, false);
            eastBeach.AddComponent<MeshFilter>().sharedMesh = BuildBeachMesh(false);
            eastBeach.AddComponent<MeshRenderer>().sharedMaterial = intertidalMat;
            eastBeach.AddComponent<MeshCollider>();
        }

        private static Mesh BuildBeachMesh(bool isWest)
        {
            const float zStep = 2.0f;
            const int uSegs = 24;
            int zCount = Mathf.CeilToInt((MapZMax - MapZMin) / zStep) + 1;
            int w = uSegs + 1;

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            for (int zi = 0; zi < zCount; zi++)
            {
                float z = MapZMin + zi * zStep;
                float riverEdgeX   = isWest ? (RiverCenterX(z) - RiverHalfWidth(z)) : (RiverCenterX(z) + RiverHalfWidth(z));
                float beachW       = BeachWidth(z);
                float beachInlandX = isWest ? (riverEdgeX - beachW) : (riverEdgeX + beachW);
                float shelfEdgeX   = isWest ? (riverEdgeX + 3.0f)   : (riverEdgeX - 3.0f);

                for (int ui = 0; ui <= uSegs; ui++)
                {
                    float u = (float)ui / uSegs;
                    float x, y;

                    if (isWest)
                    {
                        // West Beach: u=0 at beachInlandX (crest 0.48m), u=0.8 at waterline (0.04m), u=1 at submerged shelf (-0.25m)
                        if (u <= 0.80f)
                        {
                            float tSlope = u / 0.80f;
                            x = Mathf.Lerp(beachInlandX, riverEdgeX, tSlope);
                            y = Mathf.Lerp(0.48f, WaterY, 0.5f - 0.5f * Mathf.Cos(tSlope * Mathf.PI));
                        }
                        else
                        {
                            float tShelf = (u - 0.80f) / 0.20f;
                            x = Mathf.Lerp(riverEdgeX, shelfEdgeX, tShelf);
                            y = Mathf.Lerp(WaterY, -0.25f, tShelf);
                        }
                    }
                    else
                    {
                        // East Beach: u=0 at submerged shelf (-0.25m), u=0.20 at waterline (0.04m), u=1 at beachInlandX (crest 0.48m)
                        if (u <= 0.20f)
                        {
                            float tShelf = u / 0.20f;
                            x = Mathf.Lerp(shelfEdgeX, riverEdgeX, tShelf);
                            y = Mathf.Lerp(-0.25f, WaterY, tShelf);
                        }
                        else
                        {
                            float tSlope = (u - 0.20f) / 0.80f;
                            x = Mathf.Lerp(riverEdgeX, beachInlandX, tSlope);
                            y = Mathf.Lerp(WaterY, 0.48f, 0.5f - 0.5f * Mathf.Cos(tSlope * Mathf.PI));
                        }
                    }

                    float rip = Mathf.Sin(x * 0.35f + z * 0.28f) * 0.02f;
                    verts.Add(new Vector3(x, y + rip, z));
                    uvs.Add(new Vector2(x * 0.35f, z * 0.35f));
                }
            }

            for (int zi = 0; zi < zCount - 1; zi++)
            {
                for (int ui = 0; ui < uSegs; ui++)
                {
                    int i0 = zi * w + ui;
                    tris.Add(i0); tris.Add(i0 + w); tris.Add(i0 + 1);
                    tris.Add(i0 + 1); tris.Add(i0 + w); tris.Add(i0 + w + 1);
                }
            }

            var m = new Mesh { name = isWest ? "SeaBeach_West" : "SeaBeach_East" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void DecorateSeaBeaches(Transform parent, Dictionary<string, GameObject> prefabs)
        {
            var decoGroup = new GameObject("Coastal_Beach_Decorations").transform;
            decoGroup.SetParent(parent, false);

            GameObject coconut = prefabs.ContainsKey("Tree_Lightweight_CoconutPalm") ? prefabs["Tree_Lightweight_CoconutPalm"] : null;
            GameObject logPrefab = prefabs.ContainsKey("Detail_Fallen_MangroveLog") ? prefabs["Detail_Fallen_MangroveLog"] : null;
            GameObject grassPrefab = prefabs.ContainsKey("GrassPatch") ? prefabs["GrassPatch"] : null;

            int logCount = 0;
            int palmCount = 0;
            int grassCount = 0;

            Vector3 playerPos = new Vector3(-26f, 0.08f, 30f);

            // Scatter washed-up mangrove driftwood logs and leaning palms along both shorelines
            for (float z = MapZMin + 5f; z < MapZMax - 5f; z += 7.5f)
            {
                float rxC = RiverCenterX(z);
                float rhw = RiverHalfWidth(z);
                float beachW = BeachWidth(z);

                for (int side = -1; side <= 1; side += 2)
                {
                    float edgeX = rxC + side * rhw;
                    float inlandX = rxC + side * (rhw + beachW);

                    // 1. Driftwood logs on the dry sand berm (upper beach)
                    if (logPrefab != null && Random.value < 0.45f)
                    {
                        float logT = Random.Range(0.20f, 0.55f);
                        float lx = Mathf.Lerp(inlandX, edgeX, logT);
                        float lz = z + Random.Range(-2.0f, 2.0f);

                        float dSq = (lx - playerPos.x) * (lx - playerPos.x) + (lz - playerPos.z) * (lz - playerPos.z);
                        if (dSq > 4.5f * 4.5f && !IsInHazardZone(lx, lz, buffer: 3.5f))
                        {
                            var log = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab, decoGroup);
                            log.name = $"Beach_Driftwood_{logCount++}";
                            log.transform.position = new Vector3(lx, BankY + 0.065f, lz);
                            log.transform.rotation = Quaternion.Euler(Random.Range(-3f, 3f), Random.Range(0f, 360f), Random.Range(-3f, 3f));
                            log.transform.localScale = Vector3.one * Random.Range(0.40f, 0.70f);
                        }
                    }

                    // 2. Leaning coastal coconut palms along the beach upper ridge
                    if (coconut != null && Random.value < 0.35f)
                    {
                        float palmT = Random.Range(0.08f, 0.28f);
                        float px = Mathf.Lerp(inlandX, edgeX, palmT);
                        float pz = z + Random.Range(-2.0f, 2.0f);

                        float dSq = (px - playerPos.x) * (px - playerPos.x) + (pz - playerPos.z) * (pz - playerPos.z);
                        if (dSq > 6.0f * 6.0f && !IsInHazardZone(px, pz, buffer: 4.0f))
                        {
                            var pInst = (GameObject)PrefabUtility.InstantiatePrefab(coconut, decoGroup);
                            pInst.name = $"Beach_LeaningPalm_{palmCount++}";
                            pInst.transform.position = new Vector3(px, BankY + 0.075f, pz);
                            float leanAngle = Random.Range(8f, 18f);
                            float yaw = (side == -1) ? Random.Range(60f, 120f) : Random.Range(240f, 300f);
                            pInst.transform.rotation = Quaternion.Euler(leanAngle, yaw, Random.Range(-4f, 4f));
                            pInst.transform.localScale = Vector3.one * Random.Range(0.95f, 1.35f);
                        }
                    }

                    // 3. Sparse coastal dune grass patches strictly at the upper boundary where sand meets mud
                    if (grassPrefab != null && Random.value < 0.25f)
                    {
                        float gx = inlandX + side * Random.Range(-1.0f, 1.0f);
                        float gz = z + Random.Range(-2.0f, 2.0f);

                        float dSq = (gx - playerPos.x) * (gx - playerPos.x) + (gz - playerPos.z) * (gz - playerPos.z);
                        if (dSq > 5.0f * 5.0f && !IsInHazardZone(gx, gz, buffer: 3.5f))
                        {
                            var gInst = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, decoGroup);
                            gInst.name = $"Beach_DuneGrass_{grassCount++}";
                            gInst.transform.position = new Vector3(gx, BankY + 0.078f, gz);
                            gInst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                            gInst.transform.localScale = Vector3.one * Random.Range(0.45f, 0.75f);
                        }
                    }
                }
            }
        }

        private static void BuildAllCanals(Transform parent, Dictionary<string, Material> mats)
        {
            for (int i = 0; i < Canals.Length; i++)
            {
                var c = Canals[i];
                var cGO = new GameObject($"Canal_{i + 1}");
                cGO.transform.SetParent(parent, false);

                var water = new GameObject("Water");
                water.transform.SetParent(cGO.transform, false);
                water.AddComponent<MeshFilter>().sharedMesh = BuildCanalMesh(i, c.sz, c.ez, c.hw, true);
                water.AddComponent<MeshRenderer>().sharedMaterial = mats["canal"];

                var bed = new GameObject("Bed");
                bed.transform.SetParent(cGO.transform, false);
                bed.AddComponent<MeshFilter>().sharedMesh = BuildCanalMesh(i, c.sz, c.ez, c.hw, false);
                bed.AddComponent<MeshRenderer>().sharedMaterial = mats["riverbed"];
                bed.AddComponent<MeshCollider>();
            }
        }

        private static Mesh BuildCanalMesh(int idx, float sz, float ez, float hw, bool isWater)
        {
            const float zStep = 3.5f;
            const int xSegs = 6;
            int zCount = Mathf.CeilToInt((ez - sz) / zStep) + 1;
            int w = xSegs + 1;

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            for (int zi = 0; zi < zCount; zi++)
            {
                float z  = Mathf.Min(sz + zi * zStep, ez);
                float cx = CanalCenterX(idx, z);

                // Never encroach into river
                float rx = RiverCenterX(z);
                float rhw = RiverHalfWidth(z) + 2.0f;
                if (Mathf.Abs(cx - rx) < rhw)
                {
                    cx = (cx < rx) ? (rx - rhw) : (rx + rhw);
                }

                for (int xi = 0; xi <= xSegs; xi++)
                {
                    float u = (float)xi / xSegs;
                    float x = Mathf.Lerp(cx - hw, cx + hw, u);
                    float y = isWater ? (WaterY - 0.02f) : (-0.6f * 4f * u * (1f - u) - 0.15f);

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u, zi * 0.15f));
                }
            }

            for (int zi = 0; zi < zCount - 1; zi++)
            {
                for (int xi = 0; xi < xSegs; xi++)
                {
                    int i0 = zi * w + xi;
                    tris.Add(i0); tris.Add(i0 + w); tris.Add(i0 + 1);
                    tris.Add(i0 + 1); tris.Add(i0 + w); tris.Add(i0 + w + 1);
                }
            }

            var m = new Mesh { name = $"Canal_{idx}_{(isWater ? "Water" : "Bed")}" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void BuildMudRoads(Transform parent, Dictionary<string, Material> mats)
        {
            // Roads are strictly 2 trails (West Bank & East Bank), NEVER crossing the river!
            for (int i = 0; i < Roads.Length; i++)
            {
                var r = Roads[i];
                var rGO = new GameObject($"Mud_Trail_{i + 1}");
                rGO.transform.SetParent(parent, false);
                rGO.AddComponent<MeshFilter>().sharedMesh = BuildRoadMesh(i, r.sz, r.ez, 2.8f);
                rGO.AddComponent<MeshRenderer>().sharedMaterial = mats["mud"];
                rGO.AddComponent<MeshCollider>();
            }
        }

        private static Mesh BuildRoadMesh(int idx, float sz, float ez, float halfWidth)
        {
            const float zStep = 3.0f;
            int zCount = Mathf.CeilToInt((ez - sz) / zStep) + 1;

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            for (int zi = 0; zi < zCount; zi++)
            {
                float z  = Mathf.Min(sz + zi * zStep, ez);
                float cx = RoadCenterX(idx, z);

                // Strict river clearance guarantee: road must NEVER touch the river
                float rx = RiverCenterX(z);
                float rhw = RiverHalfWidth(z) + 4.0f;
                if (Mathf.Abs(cx - rx) < rhw)
                {
                    cx = (cx < rx) ? (rx - rhw) : (rx + rhw);
                }

                verts.Add(new Vector3(cx - halfWidth, BankY + 0.03f, z));
                verts.Add(new Vector3(cx,             BankY + 0.05f, z));
                verts.Add(new Vector3(cx + halfWidth, BankY + 0.03f, z));

                uvs.Add(new Vector2(0.0f, zi * 0.20f));
                uvs.Add(new Vector2(0.5f, zi * 0.20f));
                uvs.Add(new Vector2(1.0f, zi * 0.20f));
            }

            int w = 3;
            for (int zi = 0; zi < zCount - 1; zi++)
            {
                int b = zi * w;
                tris.Add(b); tris.Add(b + w); tris.Add(b + 1);
                tris.Add(b + 1); tris.Add(b + w); tris.Add(b + w + 1);
                tris.Add(b + 1); tris.Add(b + w + 1); tris.Add(b + 2);
                tris.Add(b + 2); tris.Add(b + w + 1); tris.Add(b + w + 2);
            }

            var m = new Mesh { name = $"Road_{idx}" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void BuildMountainsAndBoundaries(Transform parent, Dictionary<string, Material> mats)
        {
            // 1. INVISIBLE PHYSICAL BARRIER WALLS (Impenetrable, prevents character from falling off)
            var collidersGroup = new GameObject("Colliders_Invisible_Walls").transform;
            collidersGroup.SetParent(parent, false);

            float totalZ = MapZMax - MapZMin;
            float totalX = MapXMax - MapXMin;
            float midZ = (MapZMin + MapZMax) * 0.5f;
            float midX = (MapXMin + MapXMax) * 0.5f;

            CreateBoundaryWall(collidersGroup, "Wall_West",  new Vector3(MapXMin - 2f, 30f, midZ), new Vector3(4f, 70f, totalZ + 40f));
            CreateBoundaryWall(collidersGroup, "Wall_East",  new Vector3(MapXMax + 2f, 30f, midZ), new Vector3(4f, 70f, totalZ + 40f));
            CreateBoundaryWall(collidersGroup, "Wall_South", new Vector3(midX, 30f, MapZMin - 2f), new Vector3(totalX + 40f, 70f, 4f));
            CreateBoundaryWall(collidersGroup, "Wall_North", new Vector3(midX, 30f, MapZMax + 2f), new Vector3(totalX + 40f, 70f, 4f));

            // 2. PROCEDURAL 3D MOUNTAINS (Steep rugged mountain ridges enclosing all 4 sides)
            var ridgesGroup = new GameObject("3D_Perimeter_Mountains").transform;
            ridgesGroup.SetParent(parent, false);

            // West Mountain Ridge
            BuildMountainRidge(ridgesGroup, mats["mountains"], "Mountain_West",
                MapXMin, MapXMin - 70f, MapZMin - 20f, MapZMax + 20f, true);

            // East Mountain Ridge
            BuildMountainRidge(ridgesGroup, mats["mountains"], "Mountain_East",
                MapXMax, MapXMax + 70f, MapZMin - 20f, MapZMax + 20f, true);

            // North Mountain Ridge (Inland boundary to the North)
            BuildMountainRidge(ridgesGroup, mats["mountains"], "Mountain_North",
                MapZMax, MapZMax + 70f, MapXMin - 70f, MapXMax + 70f, false);

            // Note: South is the open Bay of Bengal Ocean horizon!
            // No mountain wall at the south — wide open marine coastline!

            // 3. 2D PANORAMIC MOUNTAIN BACKDROP BILLBOARDS (Lush distant misty peaks on inland horizons)
            var backdropGroup = new GameObject("2D_Mountain_Backdrop_Panoramas").transform;
            backdropGroup.SetParent(parent, false);

            // West Backdrop (Facing East)
            CreateBackdropBillboard(backdropGroup, mats["backdrop"], "Backdrop_West",
                new Vector3(MapXMin - 75f, 45f, midZ), new Vector3(totalZ + 80f, 95f, 1f), Quaternion.Euler(0f, 90f, 0f));

            // East Backdrop (Facing West)
            CreateBackdropBillboard(backdropGroup, mats["backdrop"], "Backdrop_East",
                new Vector3(MapXMax + 75f, 45f, midZ), new Vector3(totalZ + 80f, 95f, 1f), Quaternion.Euler(0f, -90f, 0f));

            // North Backdrop (Facing South)
            CreateBackdropBillboard(backdropGroup, mats["backdrop"], "Backdrop_North",
                new Vector3(midX, 45f, MapZMax + 75f), new Vector3(totalX + 80f, 95f, 1f), Quaternion.Euler(0f, 180f, 0f));
        }

        private static void CreateBoundaryWall(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
        }

        private static void BuildMountainRidge(Transform parent, Material mat, string name, float innerCoord, float outerCoord, float startAlong, float endAlong, bool alongZ)
        {
            const int radialSegs = 10;
            const int alongSegs = 36;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int ai = 0; ai <= alongSegs; ai++)
            {
                float along = Mathf.Lerp(startAlong, endAlong, (float)ai / alongSegs);

                for (int ri = 0; ri <= radialSegs; ri++)
                {
                    float u = (float)ri / radialSegs;
                    float across = Mathf.Lerp(innerCoord, outerCoord, u);

                    // Height profile: 0 at boundary edge, rising sharply to 35m-52m
                    float profile = Mathf.Sin(u * Mathf.PI * 0.5f);
                    float wave1 = Mathf.Sin(along * 0.05f) * 9f;
                    float wave2 = Mathf.Cos(along * 0.09f + 1.2f) * 5f;
                    float peak = Mathf.Sin(u * Mathf.PI * 1.5f + along * 0.03f) * 7f;
                    float h = profile * (38f + wave1 + wave2 + peak);
                    if (ri == 0) h = BankY; // Flush with mud ground at the boundary!

                    Vector3 v = alongZ ? new Vector3(across, h, along) : new Vector3(along, h, across);
                    verts.Add(v);
                    uvs.Add(new Vector2(u * 5f, (float)ai / alongSegs * 18f));
                }
            }

            int w = radialSegs + 1;
            for (int ai = 0; ai < alongSegs; ai++)
            {
                for (int ri = 0; ri < radialSegs; ri++)
                {
                    int i0 = ai * w + ri;
                    tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + w);
                    tris.Add(i0 + 1); tris.Add(i0 + w + 1); tris.Add(i0 + w);
                }
            }

            var m = new Mesh { name = name };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>();
        }

        private static void CreateBackdropBillboard(Transform parent, Material mat, string name, Vector3 pos, Vector3 size, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = size;
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        private static void PlantForest(Transform parent, Dictionary<string, GameObject> prefabs, Dictionary<string, Material> mats)
        {
            // Living trees pool - ONLY authentic Sundarban mangrove species & tropical coastal palms
            var livingTrees = new List<(string name, GameObject prefab, float weight)>();
            if (prefabs.ContainsKey("Tree_Stilt_Mangrove_LOD1"))        livingTrees.Add(("Tree_Stilt_Mangrove_LOD1",        prefabs["Tree_Stilt_Mangrove_LOD1"],        35f));
            if (prefabs.ContainsKey("Tree_Stilt_Mangrove_Tall_LOD1"))   livingTrees.Add(("Tree_Stilt_Mangrove_Tall_LOD1",   prefabs["Tree_Stilt_Mangrove_Tall_LOD1"],   25f));
            if (prefabs.ContainsKey("Tree_Mangrove_Canopy_LOD1"))       livingTrees.Add(("Tree_Mangrove_Canopy_LOD1",       prefabs["Tree_Mangrove_Canopy_LOD1"],       30f));
            if (prefabs.ContainsKey("Tree_Mangrove_Bush_LOD1"))         livingTrees.Add(("Tree_Mangrove_Bush_LOD1",         prefabs["Tree_Mangrove_Bush_LOD1"],         20f));
            if (prefabs.ContainsKey("Tree_LowPoly_Mangrove_Stilt"))     livingTrees.Add(("Tree_LowPoly_Mangrove_Stilt",     prefabs["Tree_LowPoly_Mangrove_Stilt"],     20f));
            if (prefabs.ContainsKey("Tree_Lightweight_CoconutPalm"))    livingTrees.Add(("Tree_Lightweight_CoconutPalm",    prefabs["Tree_Lightweight_CoconutPalm"],    15f));
            if (prefabs.ContainsKey("Tree_Lightweight_EvergladesPalm")) livingTrees.Add(("Tree_Lightweight_EvergladesPalm", prefabs["Tree_Lightweight_EvergladesPalm"], 15f));

            if (livingTrees.Count == 0) return;

            GameObject fallTreePrefab = prefabs.ContainsKey("Tree_New_FallTree") ? prefabs["Tree_New_FallTree"] : null;
            GameObject deadTreePrefab = prefabs.ContainsKey("Tree_New_DeadTree") ? prefabs["Tree_New_DeadTree"] : (prefabs.ContainsKey("Tree_Deco_DeadTree") ? prefabs["Tree_Deco_DeadTree"] : null);

            System.Func<GameObject> pickLivingTree = () =>
            {
                float total = 0f;
                foreach (var item in livingTrees) total += item.weight;
                float roll = Random.Range(0f, total);
                float acc = 0f;
                foreach (var item in livingTrees)
                {
                    acc += item.weight;
                    if (roll <= acc) return item.prefab;
                }
                return livingTrees[0].prefab;
            };

            // Player position & safe clearance
            var tiger = GameObject.Find("Tiger");
            Vector3 playerPos = tiger != null ? tiger.transform.position : new Vector3(-45f, 0.48f, 30f);

            int plantedLiving = 0;
            var forestGroup = new GameObject("Inland_Trees").transform;
            forestGroup.SetParent(parent, false);

            var hazardGroup = new GameObject("Hazard_Dead_Trees").transform;
            hazardGroup.SetParent(parent, false);

            const float gridStep = 10.5f;
            const float jitter   = 2.8f;

            for (float z = MapZMin + 8f; z < MapZMax - 8f; z += gridStep)
            {
                for (float x = MapXMin + 8f; x < MapXMax - 8f; x += gridStep)
                {
                    float jx = x + Random.Range(-jitter, jitter);
                    float jz = z + Random.Range(-jitter, jitter);

                    // 8.5m radius around player: open view for player character
                    float distPlayerSq = (jx - playerPos.x) * (jx - playerPos.x) + (jz - playerPos.z) * (jz - playerPos.z);
                    if (distPlayerSq < 8.5f * 8.5f) continue;

                    // Skip main river
                    float rxC = RiverCenterX(jz);
                    float rhw = RiverHalfWidth(jz) + 2.8f;
                    if (jx > rxC - rhw && jx < rxC + rhw) continue;

                    // Skip canals
                    bool inCanal = false;
                    for (int ci = 0; ci < Canals.Length && !inCanal; ci++)
                    {
                        var c = Canals[ci];
                        if (jz < c.sz || jz > c.ez) continue;
                        if (Mathf.Abs(jx - CanalCenterX(ci, jz)) < c.hw + 2.5f) inCanal = true;
                    }
                    if (inCanal) continue;

                    // Road clearance
                    bool onRoad = false;
                    for (int ri = 0; ri < Roads.Length && !onRoad; ri++)
                    {
                        var r = Roads[ri];
                        if (jz < r.sz || jz > r.ez) continue;
                        if (Mathf.Abs(jx - RoadCenterX(ri, jz)) < 3.2f) onRoad = true;
                    }
                    if (onRoad) continue;

                    // STRICT HAZARD ZONE RULE:
                    // Inside or near a hazard zone: ABSOLUTELY ZERO living green trees!
                    if (IsInHazardZone(jx, jz, buffer: 3.5f))
                    {
                        continue; // Do NOT plant green trees in or near hazard zones!
                    }

                    // Plant authentic mangrove tree on the elevated mud ground
                    var prefab = pickLivingTree();
                    float scale = Random.Range(0.88f, 1.25f);
                    var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    var treeInst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, forestGroup);
                    treeInst.name = $"{prefab.name}_{plantedLiving++}";
                    treeInst.transform.position = new Vector3(jx, GetGroundY(jx, jz), jz);
                    treeInst.transform.rotation = rot;
                    treeInst.transform.localScale = Vector3.one * scale;
                }
            }
            Debug.Log($"[BuildSundarbanFullMap] Planted {plantedLiving} authentic mangrove trees in clean forest.");

            // 2. EXPANDED HAZARD ZONES: Plant Fall Trees and Dead Trees in desolate danger zones
            int plantedDangerTrees = 0;
            foreach (var hz in HazardZones)
            {
                // Scatter Fall Trees across the expanded danger zone
                int numFall = Random.Range(7, 11);
                for (int f = 0; f < numFall && fallTreePrefab != null; f++)
                {
                    float angle = (float)f / numFall * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                    float dist = Random.Range(3.0f, hz.r * 0.82f);
                    float fx = hz.x + Mathf.Cos(angle) * dist;
                    float fz = hz.z + Mathf.Sin(angle) * dist;

                    // River clearance
                    float rxC = RiverCenterX(fz);
                    float rhw = RiverHalfWidth(fz) + 2.0f;
                    if (Mathf.Abs(fx - rxC) < rhw) continue;

                    var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    float scale = Random.Range(0.85f, 1.25f);

                    var treeInst = (GameObject)PrefabUtility.InstantiatePrefab(fallTreePrefab, hazardGroup);
                    treeInst.name = $"Hazard_FallTree_{plantedDangerTrees++}";
                    treeInst.transform.position = new Vector3(fx, GetGroundY(fx, fz), fz);
                    treeInst.transform.rotation = rot;
                    treeInst.transform.localScale = Vector3.one * scale;
                }

                // Scatter sparse Dead Trees among the fall trees
                int numDead = Random.Range(4, 7);
                for (int d = 0; d < numDead && deadTreePrefab != null; d++)
                {
                    float angle = (float)d / numDead * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f) + 0.5f;
                    float dist = Random.Range(4.0f, hz.r * 0.80f);
                    float dx = hz.x + Mathf.Cos(angle) * dist;
                    float dz = hz.z + Mathf.Sin(angle) * dist;

                    float rxC = RiverCenterX(dz);
                    float rhw = RiverHalfWidth(dz) + 2.0f;
                    if (Mathf.Abs(dx - rxC) < rhw) continue;

                    var rot = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                    float scale = Random.Range(0.85f, 1.25f);

                    var treeInst = (GameObject)PrefabUtility.InstantiatePrefab(deadTreePrefab, hazardGroup);
                    treeInst.name = $"Hazard_DeadTree_{plantedDangerTrees++}";
                    treeInst.transform.position = new Vector3(dx, GetGroundY(dx, dz), dz);
                    treeInst.transform.rotation = rot;
                    treeInst.transform.localScale = Vector3.one * scale;
                }
            }
            Debug.Log($"[BuildSundarbanFullMap] Planted {plantedDangerTrees} danger zone trees (Fall Trees & Dead Trees) inside expanded hazard zones.");

            // 3. Dense Mangrove Riverbank Forest (Authentic Stilt-Root Mangroves, Overhanging Canopies, and Palms)
            var bankGroup = new GameObject("Dense_Riverbank_Mangrove_Canopy").transform;
            bankGroup.SetParent(parent, false);
            int bankCount = 0;

            GameObject stiltMangrovePrefab = prefabs.ContainsKey("Tree_Stilt_Mangrove_LOD1") ? prefabs["Tree_Stilt_Mangrove_LOD1"] 
                : (prefabs.ContainsKey("Tree_LowPoly_Mangrove_Stilt") ? prefabs["Tree_LowPoly_Mangrove_Stilt"] : null);
            GameObject stiltTallPrefab = prefabs.ContainsKey("Tree_Stilt_Mangrove_Tall_LOD1") ? prefabs["Tree_Stilt_Mangrove_Tall_LOD1"] : stiltMangrovePrefab;
            GameObject canopyMangrovePrefab = prefabs.ContainsKey("Tree_Mangrove_Canopy_LOD1") ? prefabs["Tree_Mangrove_Canopy_LOD1"] : null;
            GameObject bushMangrovePrefab = prefabs.ContainsKey("Tree_Mangrove_Bush_LOD1") ? prefabs["Tree_Mangrove_Bush_LOD1"] : null;
            GameObject overhangingMangrovePrefab = prefabs.ContainsKey("Tree_LowPoly_Mangrove_Overhanging") ? prefabs["Tree_LowPoly_Mangrove_Overhanging"] : null;
            GameObject coconutPalmPrefab = prefabs.ContainsKey("Tree_Lightweight_CoconutPalm") ? prefabs["Tree_Lightweight_CoconutPalm"] : null;
            GameObject evergladesPalmPrefab = prefabs.ContainsKey("Tree_Lightweight_EvergladesPalm") ? prefabs["Tree_Lightweight_EvergladesPalm"] : null;

            for (float z = MapZMin + 6f; z <= MapZMax - 6f; z += 3.2f)
            {
                float cx = RiverCenterX(z);
                float hw = RiverHalfWidth(z);

                for (int side = -1; side <= 1; side += 2)
                {
                    // Check meander curvature to plant dense clusters at river bends
                    float prevCx = RiverCenterX(z - 2.5f);
                    float nextCx = RiverCenterX(z + 2.5f);
                    float curvature = Mathf.Abs(nextCx - 2f * cx + prevCx);
                    bool isSharpBend = curvature > 0.10f;

                    // ── Row 1: Waterline Stilt Mangroves (prop roots resting on mud slope) ──
                    if (isSharpBend || Random.value < 0.55f)
                    {
                        float px1 = cx + side * (hw + Random.Range(0.6f, 2.4f));
                        if (px1 >= MapXMin + 4f && px1 <= MapXMax - 4f)
                        {
                            float dSq = (px1 - playerPos.x) * (px1 - playerPos.x) + (z - playerPos.z) * (z - playerPos.z);
                            if (dSq >= 7.0f * 7.0f && !IsInHazardZone(px1, z, buffer: 3.5f))
                            {
                                GameObject prefab1 = (Random.value < 0.60f && stiltMangrovePrefab != null) ? stiltMangrovePrefab 
                                    : (stiltTallPrefab != null ? stiltTallPrefab : pickLivingTree());

                                float rotY = (side == -1) ? Random.Range(60f, 120f) : Random.Range(240f, 300f);
                                float tiltX = Random.Range(-3f, 3f);
                                float tiltZ = -side * Random.Range(3f, 8f);
                                var rot1 = Quaternion.Euler(tiltX, rotY, tiltZ);
                                float scale1 = Random.Range(1.10f, 1.45f);

                                var tree1 = (GameObject)PrefabUtility.InstantiatePrefab(prefab1, bankGroup);
                                tree1.name = $"Mangrove_Stilt_{bankCount++}";
                                float gy1 = GetGroundY(px1, z);
                                tree1.transform.position = new Vector3(px1, gy1 - 0.04f, z + Random.Range(-0.5f, 0.5f));
                                tree1.transform.rotation = rot1;
                                tree1.transform.localScale = Vector3.one * scale1;
                            }
                        }
                    }

                    // ── Row 2: Bank Crest Stilt Roots & Mangrove Canopy Wall ──
                    if (Random.value < 0.80f)
                    {
                        float px2 = cx + side * (hw + BeachWidth(z) + Random.Range(1.5f, 5.0f));
                        if (px2 >= MapXMin + 4f && px2 <= MapXMax - 4f)
                        {
                            float dSq2 = (px2 - playerPos.x) * (px2 - playerPos.x) + (z - playerPos.z) * (z - playerPos.z);
                            if (dSq2 >= 7.0f * 7.0f && !IsInHazardZone(px2, z, buffer: 3.5f))
                            {
                                GameObject prefab2;
                                float roll = Random.value;
                                if (roll < 0.35f && stiltTallPrefab != null) prefab2 = stiltTallPrefab;
                                else if (roll < 0.55f && canopyMangrovePrefab != null) prefab2 = canopyMangrovePrefab;
                                else if (roll < 0.72f && coconutPalmPrefab != null) prefab2 = coconutPalmPrefab;
                                else if (roll < 0.85f && bushMangrovePrefab != null) prefab2 = bushMangrovePrefab;
                                else prefab2 = pickLivingTree();

                                float scale2 = Random.Range(0.95f, 1.30f);
                                var rot2 = Quaternion.Euler(Random.Range(-3f, 3f), Random.Range(0f, 360f), -side * Random.Range(2f, 8f));

                                var tree2 = (GameObject)PrefabUtility.InstantiatePrefab(prefab2, bankGroup);
                                tree2.name = $"Mangrove_Bank_{bankCount++}";
                                float gy2 = GetGroundY(px2, z);
                                tree2.transform.position = new Vector3(px2, gy2, z + Random.Range(-0.8f, 0.8f));
                                tree2.transform.rotation = rot2;
                                tree2.transform.localScale = Vector3.one * scale2;
                            }
                        }
                    }
                }
            }
            Debug.Log($"[BuildSundarbanFullMap] Planted {bankCount} authentic stilt-root mangroves along the riverbanks!");

            // 4. AUTHENTIC MANGROVE MUD SURFACE UNDERGROWTH: Ferns, Pneumatophores, Seedlings, Driftwood Logs, & Grass
            var detailGroup = new GameObject("Lush_Greenery_and_Grass").transform;
            detailGroup.SetParent(parent, false);

            GameObject fernPrefab = prefabs.ContainsKey("Detail_Mangrove_Fern") ? prefabs["Detail_Mangrove_Fern"] : null;
            GameObject pneumatophorePrefab = prefabs.ContainsKey("Detail_Mangrove_Pneumatophore") ? prefabs["Detail_Mangrove_Pneumatophore"] : null;
            GameObject seedlingPrefab = prefabs.ContainsKey("Detail_Mangrove_Seedling") ? prefabs["Detail_Mangrove_Seedling"] : null;
            GameObject logPrefab = prefabs.ContainsKey("Detail_Mangrove_FallenLog") ? prefabs["Detail_Mangrove_FallenLog"] 
                : (prefabs.ContainsKey("Detail_Fallen_MangroveLog") ? prefabs["Detail_Fallen_MangroveLog"] : null);
            GameObject grassPatchPrefab = prefabs.ContainsKey("GrassPatch") ? prefabs["GrassPatch"] : null;

            int fernCount = 0;
            int pneumatoCount = 0;
            int seedlingCount = 0;
            int grassPatchCount = 0;
            int logCount = 0;

            // A. Intertidal Mud Flat & Shoreline Undergrowth (Dense along both riverbanks)
            for (float z = MapZMin + 5f; z <= MapZMax - 5f; z += 3.5f)
            {
                float rxC = RiverCenterX(z);
                float rhw = RiverHalfWidth(z);
                float beachW = BeachWidth(z);

                for (int side = -1; side <= 1; side += 2)
                {
                    // 1. Waterline & Shoreline Pneumatophores (pencil roots sticking up from wet silt)
                    if (pneumatophorePrefab != null && Random.value < 0.75f)
                    {
                        int pNum = Random.Range(4, 9);
                        for (int pi = 0; pi < pNum; pi++)
                        {
                            float px = rxC + side * (rhw + Random.Range(0.6f, beachW + 5.0f));
                            float pz = z + Random.Range(-1.8f, 1.8f);
                            if (IsInHazardZone(px, pz, buffer: 3.0f)) continue;

                            var pRoot = (GameObject)PrefabUtility.InstantiatePrefab(pneumatophorePrefab, detailGroup);
                            pRoot.name = $"Pneumatophore_{pneumatoCount++}";
                            float gy = GetGroundY(px, pz);
                            pRoot.transform.position = new Vector3(px, gy, pz);
                            pRoot.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                            pRoot.transform.localScale = Vector3.one * Random.Range(0.85f, 1.45f);
                        }
                    }

                    // 2. Mangrove Ferns on the mud banks (matching reference screenshot)
                    if (fernPrefab != null && Random.value < 0.75f)
                    {
                        float fx = rxC + side * (rhw + Random.Range(1.2f, beachW + 16.0f));
                        float fz = z + Random.Range(-1.5f, 1.5f);
                        float dSq = (fx - playerPos.x) * (fx - playerPos.x) + (fz - playerPos.z) * (fz - playerPos.z);
                        if (dSq >= 4.0f * 4.0f && !IsInHazardZone(fx, fz, buffer: 3.5f))
                        {
                            var fern = (GameObject)PrefabUtility.InstantiatePrefab(fernPrefab, detailGroup);
                            fern.name = $"Mangrove_Fern_{fernCount++}";
                            float gy = GetGroundY(fx, fz);
                            fern.transform.position = new Vector3(fx, gy, fz);
                            fern.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                            fern.transform.localScale = Vector3.one * Random.Range(0.95f, 1.50f);
                        }
                    }

                    // 3. Mangrove Seedlings
                    if (seedlingPrefab != null && Random.value < 0.50f)
                    {
                        float sx = rxC + side * (rhw + Random.Range(0.5f, beachW + 10.0f));
                        float sz = z + Random.Range(-1.5f, 1.5f);
                        if (!IsInHazardZone(sx, sz, buffer: 3.0f))
                        {
                            var seedling = (GameObject)PrefabUtility.InstantiatePrefab(seedlingPrefab, detailGroup);
                            seedling.name = $"Seedling_{seedlingCount++}";
                            float gy = GetGroundY(sx, sz);
                            seedling.transform.position = new Vector3(sx, gy, sz);
                            seedling.transform.rotation = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));
                            seedling.transform.localScale = Vector3.one * Random.Range(0.80f, 1.30f);
                        }
                    }

                    // 4. Washed-up Driftwood logs
                    if (logPrefab != null && Random.value < 0.28f)
                    {
                        float lx = rxC + side * (rhw + Random.Range(1.8f, beachW + 12.0f));
                        float lz = z + Random.Range(-1.5f, 1.5f);
                        if (!IsInHazardZone(lx, lz, buffer: 3.5f))
                        {
                            var log = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab, detailGroup);
                            log.name = $"FallenLog_{logCount++}";
                            float gy = GetGroundY(lx, lz);
                            log.transform.position = new Vector3(lx, gy, lz);
                            log.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                            log.transform.localScale = Vector3.one * Random.Range(0.40f, 0.75f);
                        }
                    }
                }
            }

            // B. Inland Forest Floor Undergrowth across the ENTIRE MAP (matching reference image everywhere!)
            const float inlandStep = 5.5f;
            for (float iz = MapZMin + 8f; iz < MapZMax - 8f; iz += inlandStep)
            {
                float rxC = RiverCenterX(iz);
                float rhw = RiverHalfWidth(iz);
                float beachW = BeachWidth(iz);

                for (float ix = MapXMin + 8f; ix < MapXMax - 8f; ix += inlandStep)
                {
                    float distRiver = Mathf.Abs(ix - rxC);
                    // Skip if inside river channel or inner beach (handled by shoreline pass)
                    if (distRiver < rhw + beachW) continue;
                    if (IsInHazardZone(ix, iz, buffer: 3.5f)) continue;

                    float jx = ix + Random.Range(-1.8f, 1.8f);
                    float jz = iz + Random.Range(-1.8f, 1.8f);
                    float distPlayerSq = (jx - playerPos.x) * (jx - playerPos.x) + (jz - playerPos.z) * (jz - playerPos.z);
                    if (distPlayerSq < 4.0f * 4.0f) continue;

                    // Ferns across the mud floor
                    if (fernPrefab != null && Random.value < 0.55f)
                    {
                        var fern = (GameObject)PrefabUtility.InstantiatePrefab(fernPrefab, detailGroup);
                        fern.name = $"Inland_Fern_{fernCount++}";
                        float gy = GetGroundY(jx, jz);
                        fern.transform.position = new Vector3(jx, gy, jz);
                        fern.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        fern.transform.localScale = Vector3.one * Random.Range(0.85f, 1.40f);
                    }

                    // Pencil roots cluster in the mud
                    if (pneumatophorePrefab != null && Random.value < 0.60f)
                    {
                        int pNum = Random.Range(3, 7);
                        for (int pi = 0; pi < pNum; pi++)
                        {
                            float px = jx + Random.Range(-1.2f, 1.2f);
                            float pz = jz + Random.Range(-1.2f, 1.2f);
                            var pRoot = (GameObject)PrefabUtility.InstantiatePrefab(pneumatophorePrefab, detailGroup);
                            pRoot.name = $"Inland_Pneumatophore_{pneumatoCount++}";
                            float gy = GetGroundY(px, pz);
                            pRoot.transform.position = new Vector3(px, gy, pz);
                            pRoot.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                            pRoot.transform.localScale = Vector3.one * Random.Range(0.80f, 1.35f);
                        }
                    }

                    // Seedlings
                    if (seedlingPrefab != null && Random.value < 0.35f)
                    {
                        var seedling = (GameObject)PrefabUtility.InstantiatePrefab(seedlingPrefab, detailGroup);
                        seedling.name = $"Inland_Seedling_{seedlingCount++}";
                        float gy = GetGroundY(jx, jz);
                        seedling.transform.position = new Vector3(jx, gy, jz);
                        seedling.transform.rotation = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));
                        seedling.transform.localScale = Vector3.one * Random.Range(0.75f, 1.25f);
                    }

                    // Fallen driftwood logs
                    if (logPrefab != null && Random.value < 0.12f)
                    {
                        var log = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab, detailGroup);
                        log.name = $"Inland_FallenLog_{logCount++}";
                        float gy = GetGroundY(jx, jz);
                        log.transform.position = new Vector3(jx, gy, jz);
                        log.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        log.transform.localScale = Vector3.one * Random.Range(0.40f, 0.70f);
                    }
                }
            }

            // B. Kotka Wildlife Sanctuary Grass Glade (around Chitra Deer start zone: 85, 48)
            float kotkaX = 85f, kotkaZ = 48f, kotkaR = 26f;
            for (int i = 0; i < 55; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(1.5f, kotkaR);
                float gx = kotkaX + Mathf.Cos(angle) * dist;
                float gz = kotkaZ + Mathf.Sin(angle) * dist;

                float rxC = RiverCenterX(gz);
                float rhw = RiverHalfWidth(gz) + 2.5f;
                if (Mathf.Abs(gx - rxC) < rhw) continue;
                if (IsInHazardZone(gx, gz, buffer: 3.5f)) continue;

                var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.85f, 1.35f);

                var foliagePrefab = fernPrefab != null ? fernPrefab : grassPatchPrefab;
                if (foliagePrefab != null)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(foliagePrefab, detailGroup);
                    g.name = $"Kotka_FernGlade_{fernCount++}";
                    float gy = GetGroundY(gx, gz);
                    g.transform.position = new Vector3(gx, gy, gz);
                    g.transform.rotation = rot;
                    g.transform.localScale = Vector3.one * scale;
                }
            }

            // C. Mandarbaria Glade (around -40, 155)
            float mandX = -40f, mandZ = 155f, mandR = 24f;
            for (int i = 0; i < 40; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(1.5f, mandR);
                float gx = mandX + Mathf.Cos(angle) * dist;
                float gz = mandZ + Mathf.Sin(angle) * dist;

                float rxC = RiverCenterX(gz);
                float rhw = RiverHalfWidth(gz) + 2.5f;
                if (Mathf.Abs(gx - rxC) < rhw) continue;
                if (IsInHazardZone(gx, gz, buffer: 3.5f)) continue;

                var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.85f, 1.30f);

                var foliagePrefab = fernPrefab != null ? fernPrefab : grassPatchPrefab;
                if (foliagePrefab != null)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(foliagePrefab, detailGroup);
                    g.name = $"Mandarbaria_FernGlade_{fernCount++}";
                    float gy = GetGroundY(gx, gz);
                    g.transform.position = new Vector3(gx, gy, gz);
                    g.transform.rotation = rot;
                    g.transform.localScale = Vector3.one * scale;
                }
            }

            // D. Inland Forest Floor Green Carpets & Ferns
            const float grassGrid = 8.5f;
            for (float z = MapZMin + 12f; z < MapZMax - 12f; z += grassGrid)
            {
                for (float x = MapXMin + 12f; x < MapXMax - 12f; x += grassGrid)
                {
                    float gx = x + Random.Range(-2.5f, 2.5f);
                    float gz = z + Random.Range(-2.5f, 2.5f);

                    float dSq = (gx - playerPos.x) * (gx - playerPos.x) + (gz - playerPos.z) * (gz - playerPos.z);
                    if (dSq < 6.0f * 6.0f) continue;

                    float rxC = RiverCenterX(gz);
                    float rhw = RiverHalfWidth(gz) + 2.2f;
                    if (gx > rxC - rhw && gx < rxC + rhw) continue;

                    bool inCanal = false;
                    for (int ci = 0; ci < Canals.Length && !inCanal; ci++)
                    {
                        var c = Canals[ci];
                        if (gz < c.sz || gz > c.ez) continue;
                        if (Mathf.Abs(gx - CanalCenterX(ci, gz)) < c.hw + 2.0f) inCanal = true;
                    }
                    if (inCanal) continue;

                    bool onRoad = false;
                    for (int ri = 0; ri < Roads.Length && !onRoad; ri++)
                    {
                        var r = Roads[ri];
                        if (gz < r.sz || gz > r.ez) continue;
                        if (Mathf.Abs(gx - RoadCenterX(ri, gz)) < 2.5f) onRoad = true;
                    }
                    if (onRoad) continue;

                    // CRITICAL: NEVER PLANT IN DANGER ZONES!
                    if (IsInHazardZone(gx, gz, buffer: 5.0f)) continue;

                    float roll = Random.value;
                    if (roll < 0.35f && fernPrefab != null)
                    {
                        var fern = (GameObject)PrefabUtility.InstantiatePrefab(fernPrefab, detailGroup);
                        fern.name = $"Forest_Fern_{fernCount++}";
                        float gy = GetGroundY(gx, gz);
                        fern.transform.position = new Vector3(gx, gy, gz);
                        fern.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        fern.transform.localScale = Vector3.one * Random.Range(0.90f, 1.40f);
                    }
                    else if (roll < 0.65f && pneumatophorePrefab != null)
                    {
                        int pNum = Random.Range(3, 7);
                        for (int pi = 0; pi < pNum; pi++)
                        {
                            float px = gx + Random.Range(-1.0f, 1.0f);
                            float pz = gz + Random.Range(-1.0f, 1.0f);
                            var pRoot = (GameObject)PrefabUtility.InstantiatePrefab(pneumatophorePrefab, detailGroup);
                            pRoot.name = $"Forest_Pneumatophore_{pneumatoCount++}";
                            float gy = GetGroundY(px, pz);
                            pRoot.transform.position = new Vector3(px, gy, pz);
                            pRoot.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                            pRoot.transform.localScale = Vector3.one * Random.Range(0.80f, 1.35f);
                        }
                    }
                    else if (roll < 0.85f && seedlingPrefab != null)
                    {
                        var seedling = (GameObject)PrefabUtility.InstantiatePrefab(seedlingPrefab, detailGroup);
                        seedling.name = $"Forest_Seedling_{seedlingCount++}";
                        float gy = GetGroundY(gx, gz);
                        seedling.transform.position = new Vector3(gx, gy, gz);
                        seedling.transform.rotation = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));
                        seedling.transform.localScale = Vector3.one * Random.Range(0.75f, 1.25f);
                    }
                }
            }

            Debug.Log($"[BuildSundarbanFullMap] Planted {fernCount} Mangrove Ferns, {pneumatoCount} Pencil Roots, {seedlingCount} Seedlings, and {logCount} Logs!");
        }

        private static void BuildOceanSystem(Transform parent, Dictionary<string, Material> mats)
        {
            var oceanGO = new GameObject("BayOfBengal_Ocean");
            oceanGO.transform.SetParent(parent, false);

            // Procedural Ocean Mesh extending south into the horizon (Bay of Bengal)
            const float startZ = -104f;
            const float endZ   = -900f;
            const float minX   = -500f;
            const float maxX   =  500f;
            const int segsX    = 40;
            const int segsZ    = 40;

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            for (int zi = 0; zi <= segsZ; zi++)
            {
                float tz = (float)zi / segsZ;
                float z = Mathf.Lerp(startZ, endZ, tz);

                for (int xi = 0; xi <= segsX; xi++)
                {
                    float tx = (float)xi / segsX;
                    float x = Mathf.Lerp(minX, maxX, tx);

                    verts.Add(new Vector3(x, WaterY, z));
                    uvs.Add(new Vector2((x - minX) * 0.05f, (z - startZ) * 0.05f));
                }
            }

            int row = segsX + 1;
            for (int zi = 0; zi < segsZ; zi++)
            {
                for (int xi = 0; xi < segsX; xi++)
                {
                    int i0 = zi * row + xi;
                    int i1 = i0 + 1;
                    int i2 = i0 + row;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i1); tris.Add(i3);
                    tris.Add(i0); tris.Add(i3); tris.Add(i2);
                }
            }

            var m = new Mesh { name = "Ocean_Mesh" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();

            oceanGO.AddComponent<MeshFilter>().sharedMesh = m;
            var rend = oceanGO.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mats.ContainsKey("ocean") ? mats["ocean"] : mats["river"];

            // Add OceanWaterAnimator for animated flowing wave currents
            var anim = oceanGO.GetComponent<SundarbanGuardian.Environment.OceanWaterAnimator>();
            if (anim == null) anim = oceanGO.AddComponent<SundarbanGuardian.Environment.OceanWaterAnimator>();
            anim.bumpSpeed = new Vector2(0.018f, 0.028f);
            anim.baseMapSpeed = new Vector2(0.008f, 0.012f);

            // Coastal Sandbars (offshore chars) in the ocean sight
            BuildCoastalChars(oceanGO.transform, mats);
            Debug.Log("[BuildSundarbanFullMap] Built Bay of Bengal Ocean System spanning 1000m x 800m with animated tidal currents.");
        }

        private static void BuildCoastalChars(Transform parent, Dictionary<string, Material> mats)
        {
            var charsGroup = new GameObject("Coastal_Sandbar_Chars");
            charsGroup.transform.SetParent(parent, false);

            var charDefs = new (float cx, float cz, float rx, float rz)[]
            {
                (-65f, -135f, 24f, 16f), // West offshore char
                ( 22f, -155f, 28f, 18f), // Central river delta char
                ( 80f, -130f, 22f, 15f), // East offshore char
                (-15f, -180f, 20f, 14f), // Outer horizon char
            };

            for (int i = 0; i < charDefs.Length; i++)
            {
                var cd = charDefs[i];
                var cGO = new GameObject($"Coastal_Char_{i + 1}");
                cGO.transform.SetParent(charsGroup.transform, false);

                const int segs = 24;
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();

                verts.Add(new Vector3(cd.cx, BankY + 0.16f, cd.cz)); // center mound apex
                uvs.Add(new Vector2(0.5f, 0.5f));

                for (int s = 0; s <= segs; s++)
                {
                    float angle = (float)s / segs * Mathf.PI * 2f;
                    float x = cd.cx + Mathf.Cos(angle) * cd.rx;
                    float z = cd.cz + Mathf.Sin(angle) * cd.rz;
                    verts.Add(new Vector3(x, BankY - 0.06f, z)); // underwater edge
                    uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
                }

                for (int s = 1; s <= segs; s++)
                {
                    tris.Add(0);
                    tris.Add(s);
                    tris.Add(s + 1);
                }

                var m = new Mesh { name = $"CharMesh_{i + 1}" };
                m.SetVertices(verts);
                m.SetUVs(0, uvs);
                m.SetTriangles(tris, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();

                cGO.AddComponent<MeshFilter>().sharedMesh = m;
                cGO.AddComponent<MeshRenderer>().sharedMaterial = mats.ContainsKey("beach_sand") ? mats["beach_sand"] : mats["mud"];
                cGO.AddComponent<MeshCollider>();
            }
        }

        private static void PlantCoastalPalmGroves(Transform parent, Dictionary<string, GameObject> prefabs)
        {
            var coastalGroup = new GameObject("Coastal_Palm_Groves").transform;
            coastalGroup.SetParent(parent, false);

            GameObject coconut = prefabs.ContainsKey("Tree_Lightweight_CoconutPalm") ? prefabs["Tree_Lightweight_CoconutPalm"] : null;
            GameObject everglades = prefabs.ContainsKey("Tree_Lightweight_EvergladesPalm") ? prefabs["Tree_Lightweight_EvergladesPalm"] : null;
            GameObject triangle = prefabs.ContainsKey("Tree_Lightweight_TrianglePalm") ? prefabs["Tree_Lightweight_TrianglePalm"] : null;
            GameObject grassPatch = prefabs.ContainsKey("GrassPatch") ? prefabs["GrassPatch"] : null;

            if (coconut == null && everglades == null) return;

            int palmCount = 0;
            int grassCount = 0;

            System.Func<GameObject> pickPalm = () =>
            {
                float r = Random.value;
                if (r < 0.50f && coconut != null) return coconut;
                if (r < 0.80f && everglades != null) return everglades;
                if (triangle != null) return triangle;
                return coconut != null ? coconut : everglades;
            };

            // 1. MAINLAND COASTAL PALM GROVE (Ocean sightline along south coast, Z from -60 to -103)
            for (float z = -60f; z >= -103f; z -= 5.5f)
            {
                for (float x = MapXMin + 6f; x <= MapXMax - 6f; x += 6.5f)
                {
                    float jx = x + Random.Range(-2.0f, 2.0f);
                    float jz = z + Random.Range(-1.8f, 1.8f);

                    // Skip River
                    float rxC = RiverCenterX(jz);
                    float rhw = RiverHalfWidth(jz) + 2.0f;
                    if (jx > rxC - rhw && jx < rxC + rhw) continue;

                    // Skip Canals
                    bool inCanal = false;
                    for (int ci = 0; ci < Canals.Length && !inCanal; ci++)
                    {
                        var c = Canals[ci];
                        if (jz < c.sz || jz > c.ez) continue;
                        if (Mathf.Abs(jx - CanalCenterX(ci, jz)) < c.hw + 2.2f) inCanal = true;
                    }
                    if (inCanal) continue;

                    // Skip Roads
                    bool onRoad = false;
                    for (int ri = 0; ri < Roads.Length && !onRoad; ri++)
                    {
                        var r = Roads[ri];
                        if (jz < r.sz || jz > r.ez) continue;
                        if (Mathf.Abs(jx - RoadCenterX(ri, jz)) < 2.5f) onRoad = true;
                    }
                    if (onRoad) continue;

                    // Strictly skip hazard zones
                    if (IsInHazardZone(jx, jz, buffer: 5.0f)) continue;

                    // 65% probability to plant a coastal palm
                    if (Random.value < 0.65f)
                    {
                        var pPrefab = pickPalm();
                        if (pPrefab != null)
                        {
                            var pInst = (GameObject)PrefabUtility.InstantiatePrefab(pPrefab, coastalGroup);
                            pInst.name = $"Coastal_Palm_{palmCount++}";

                            // Palms near the shoreline lean southwards towards the ocean water (-Z)
                            float leanFactor = Mathf.InverseLerp(-60f, -103f, jz);
                            float leanAngle = Mathf.Lerp(4f, 20f, leanFactor);
                            float yRot = Random.Range(0f, 360f);
                            var rot = Quaternion.Euler(leanAngle * Random.Range(0.8f, 1.2f), yRot, Random.Range(-6f, 6f));

                            float scale = Random.Range(0.90f, 1.35f);
                            pInst.transform.position = new Vector3(jx, BankY, jz);
                            pInst.transform.rotation = rot;
                            pInst.transform.localScale = Vector3.one * scale;
                        }
                    }

                    // Lush green grass carpets under the coastal palms
                    if (Random.value < 0.35f && grassPatch != null)
                    {
                        var g = (GameObject)PrefabUtility.InstantiatePrefab(grassPatch, coastalGroup);
                        g.name = $"Coastal_Grass_{grassCount++}";
                        g.transform.position = new Vector3(jx, BankY, jz);
                        g.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        g.transform.localScale = Vector3.one * Random.Range(0.75f, 1.20f);
                    }
                }
            }

            // 2. OFFSHORE COASTAL SANDBAR / ISLET PALMS (Standing in the ocean sight!)
            var charDefs = new (float cx, float cz, float rx, float rz, int count)[]
            {
                (-65f, -135f, 18f, 12f, 7), // West offshore char
                ( 22f, -155f, 22f, 14f, 9), // Central river delta char
                ( 80f, -130f, 16f, 11f, 6), // East offshore char
                (-15f, -180f, 15f, 10f, 5), // Outer horizon char
            };

            foreach (var cd in charDefs)
            {
                for (int i = 0; i < cd.count; i++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist = Random.Range(0.2f, 0.85f);
                    float px = cd.cx + Mathf.Cos(angle) * (cd.rx * dist);
                    float pz = cd.cz + Mathf.Sin(angle) * (cd.rz * dist);

                    var pPrefab = pickPalm();
                    if (pPrefab != null)
                    {
                        var pInst = (GameObject)PrefabUtility.InstantiatePrefab(pPrefab, coastalGroup);
                        pInst.name = $"Char_Ocean_Palm_{palmCount++}";
                        pInst.transform.position = new Vector3(px, BankY + 0.12f, pz);
                        pInst.transform.rotation = Quaternion.Euler(Random.Range(5f, 16f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                        pInst.transform.localScale = Vector3.one * Random.Range(0.95f, 1.35f);
                    }
                }
            }

            Debug.Log($"[BuildSundarbanFullMap] Planted {palmCount} coastal & offshore palms filling the ocean sight with tropical coconut groves!");
        }

        private static void PlaceCrocodile()
        {
            var crocPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/animal/American+Alligator.fbx");
            if (crocPrefab == null) return;

            float crocZ = 68f;
            float cx = RiverCenterX(crocZ);
            float hw = RiverHalfWidth(crocZ);
            Vector3 pos = new Vector3(cx + hw + 1.2f, 0.02f, crocZ);

            var croc = (GameObject)UnityEngine.Object.Instantiate(crocPrefab, pos, Quaternion.Euler(0f, 215f, 0f));
            croc.name = "River_Saltwater_Crocodile";
            croc.transform.localScale = Vector3.one * 1.0f;

            var col = croc.GetComponent<Collider>();
            if (col == null) croc.AddComponent<BoxCollider>();

            var bodyMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Body.mat");
            var teethMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Teeth.mat");
            var eyesMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Eyes.mat");

            var renderers = croc.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var sharedMats = r.sharedMaterials;
                for (int m = 0; m < sharedMats.Length; m++)
                {
                    string mn = sharedMats[m] != null ? sharedMats[m].name.ToLower() : "";
                    if (mn.Contains("eye") && eyesMat != null) sharedMats[m] = eyesMat;
                    else if (mn.Contains("teeth") && teethMat != null) sharedMats[m] = teethMat;
                    else if (bodyMat != null) sharedMats[m] = bodyMat;
                }
                r.sharedMaterials = sharedMats;
            }
        }

        private static void EnsureBengalTiger()
        {
            var tiger = GameObject.Find("Tiger");
            if (tiger == null) return;

            // Remove any unwanted children (including old low-poly BengalTiger_Model)
            for (int i = tiger.transform.childCount - 1; i >= 0; i--)
            {
                var child = tiger.transform.GetChild(i);
                if (child.name != "VisualModel")
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            // Disable placeholder capsule renderer
            var rootMr = tiger.GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;

            // Tune CharacterController for the realistic tiger anatomy
            var cc = tiger.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.center = new Vector3(0f, 0.55f, 0f);
                cc.radius = 0.40f;
                cc.height = 1.10f;
                cc.stepOffset = 0.4f;
            }

            // Ensure rigged visual model with SkinnedMeshRenderer
            var smr = tiger.GetComponentInChildren<SkinnedMeshRenderer>();
            var visualChild = tiger.transform.Find("VisualModel");
            if (smr == null || visualChild == null)
            {
                TigerRigBuilder.BuildRig();
            }
            else
            {
                visualChild.localPosition = Vector3.zero;
                visualChild.localRotation = Quaternion.identity;
                visualChild.localScale = Vector3.one * 0.72f;

                var tigerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Realistic_Tiger.mat");
                if (tigerMat != null && smr.sharedMaterial != tigerMat)
                {
                    smr.sharedMaterial = tigerMat;
                }
            }

            // Position Tiger safely on the elevated West Bank mud crest overlooking the river
            float spawnX = -45f;
            float spawnZ = 30f;
            float spawnY = GetGroundY(spawnX, spawnZ);
            tiger.transform.position = new Vector3(spawnX, spawnY + 0.05f, spawnZ);
            tiger.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing East toward the river

            EditorUtility.SetDirty(tiger);
        }

        private static void DisableOldPlanes()
        {
            var gt = GameObject.Find("Ground_Terrain");
            if (gt != null) { gt.SetActive(false); EditorUtility.SetDirty(gt); }

            var spit = GameObject.Find("Estuary_Mud_Spit");
            if (spit != null) { spit.SetActive(false); EditorUtility.SetDirty(spit); }
        }

        private static void SetupAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 950f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.88f); // coastal sea mist

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.72f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.50f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.26f, 0.18f);

            var sun = UnityEngine.Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.color = new Color(1.0f, 0.95f, 0.85f);
                sun.intensity = 1.25f;
                sun.transform.rotation = Quaternion.Euler(50f, 225f, 0f); // diagonal sunlight matching trial asset
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.60f;
            }
        }

        private static void FocusSceneView()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.pivot = new Vector3(-60f, 15f, 75f);
                sv.rotation = Quaternion.Euler(22f, 40f, 0f);
                sv.size = 50f;
                sv.Repaint();
            }
        }
    }
}
