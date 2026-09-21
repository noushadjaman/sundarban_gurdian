using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Editor
{
    public static class BuildSundarbanWorldCorrected
    {
        [MenuItem("Sundarban/Build Authentic Natural World")]
        public static void BuildWorld()
        {
            Debug.Log("[BuildSundarbanWorldCorrected] Redesigning level terrain layout with clear navigable trail, satellite braided river delta, and on-path hazards...");

            // ── 1. Atmosphere & Lighting (Bengal Coastal Climate) ────────────────
            var sun = RenderSettings.sun;
            if (sun == null)
            {
                var sunGo = GameObject.Find("Directional Light");
                if (sunGo != null) sun = sunGo.GetComponent<Light>();
            }
            if (sun != null)
            {
                sun.color = new Color(1.0f, 0.96f, 0.88f); // warm natural tropical sunlight
                sun.intensity = 1.22f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
                EditorUtility.SetDirty(sun);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 1350f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.88f); // coastal sea mist

            // ── 2. Materials & Textures ──────────────────────────────────────────
            var richGroundTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sundarban_Ground_Rich.jpg");
            var norMud = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_Mud/brown_mud_nor_1k.jpg");
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Ground.mat");
            if (groundMat != null)
            {
                groundMat.shader = Shader.Find("Universal Render Pipeline/Lit");
                if (richGroundTex != null)
                {
                    groundMat.SetTexture("_BaseMap", richGroundTex);
                    groundMat.SetTexture("_MainTex", richGroundTex);
                }
                groundMat.SetColor("_BaseColor", Color.white);
                groundMat.SetFloat("_Smoothness", 0.52f);
                groundMat.SetTextureScale("_BaseMap", new Vector2(24f, 48f));
                if (norMud != null)
                {
                    groundMat.SetTexture("_BumpMap", norMud);
                    groundMat.SetTextureScale("_BumpMap", new Vector2(24f, 48f));
                    groundMat.SetFloat("_BumpScale", 0.85f);
                    groundMat.EnableKeyword("_NORMALMAP");
                }
                groundMat.enableInstancing = true;
                EditorUtility.SetDirty(groundMat);
            }

            var eastSwampMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_EastSwamp.mat");
            if (eastSwampMat != null)
            {
                eastSwampMat.shader = Shader.Find("Universal Render Pipeline/Lit");
                if (richGroundTex != null) eastSwampMat.SetTexture("_BaseMap", richGroundTex);
                eastSwampMat.SetColor("_BaseColor", new Color(0.65f, 0.72f, 0.58f, 1f));
                eastSwampMat.SetFloat("_Smoothness", 0.70f);
                eastSwampMat.SetTextureScale("_BaseMap", new Vector2(12f, 32f));
                eastSwampMat.enableInstancing = true;
                EditorUtility.SetDirty(eastSwampMat);
            }

            var riverMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
            if (riverMat != null)
            {
                riverMat.shader = Shader.Find("Universal Render Pipeline/Lit");
                riverMat.SetColor("_BaseColor", new Color(0.24f, 0.35f, 0.19f, 0.95f));
                riverMat.SetFloat("_Smoothness", 0.85f);
                riverMat.enableInstancing = true;
                EditorUtility.SetDirty(riverMat);
            }

            var oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_OceanWater.mat");
            var oceanNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Ocean_Water_Normal.tga");
            if (oceanMat != null)
            {
                oceanMat.shader = Shader.Find("Universal Render Pipeline/Lit");
                oceanMat.SetColor("_BaseColor", new Color(0.10f, 0.36f, 0.52f, 0.96f));
                oceanMat.SetFloat("_Smoothness", 0.90f);
                oceanMat.SetFloat("_Metallic", 0.05f);
                if (oceanNor != null)
                {
                    oceanMat.SetTexture("_BumpMap", oceanNor);
                    oceanMat.SetFloat("_BumpScale", 1.2f);
                    oceanMat.SetTextureScale("_BumpMap", new Vector2(80f, 80f));
                    oceanMat.EnableKeyword("_NORMALMAP");
                }
                oceanMat.enableInstancing = true;
                EditorUtility.SetDirty(oceanMat);
            }

            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Bark.mat");
            if (barkMat != null) { barkMat.enableInstancing = true; EditorUtility.SetDirty(barkMat); }
            var grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Grass.mat");
            if (grassMat != null) { grassMat.enableInstancing = true; EditorUtility.SetDirty(grassMat); }
            var flowerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Flowers.mat");
            if (flowerMat != null) { flowerMat.enableInstancing = true; EditorUtility.SetDirty(flowerMat); }

            // ── 3. Models & Prefabs ──────────────────────────────────────────────
            var commonTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Tree by Quaternius - QVOop92WmG/CommonTree_3.fbx");
            var twistedTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Twisted Tree by Quaternius - 7PDBpElkQr/TwistedTree_4.fbx");
            var deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Dead Tree by Quaternius - Mcd2zYqyww/DeadTree_2.fbx");
            var ladyPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Lady palm tree by Poly by Google - e0AbYA90ux5/LadyPalmTree.obj");
            var thatchPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Thatch palm tree by Poly by Google - 74GpKCkUqcn/KeyThatchPalmTree.obj");
            var pondPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Pond.obj");
            var pondMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Pond.mat");
            var ladyPalmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_LadyPalm.mat");
            var thatchPalmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_ThatchPalm.mat");

            var brokenTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/brokentree/model.fbx");
            var coconutPalm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/CoconutPalmTree.obj");
            var evergladesPalm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/EvergladesPalmTree.obj");
            var crocPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/animal/American+Alligator.fbx");
            var grassFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/Grass.fbx");
            var flowersFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/Flowers.fbx");

            List<GameObject> grassPrefabs = new List<GameObject>();
            if (grassFbx != null)
            {
                for (int i = 0; i < grassFbx.transform.childCount; i++)
                {
                    var c = grassFbx.transform.GetChild(i).gameObject;
                    if (c.GetComponent<MeshFilter>() != null) grassPrefabs.Add(c);
                }
            }

            List<GameObject> flowerPrefabs = new List<GameObject>();
            if (flowersFbx != null)
            {
                for (int i = 0; i < flowersFbx.transform.childCount; i++)
                {
                    var c = flowersFbx.transform.GetChild(i).gameObject;
                    if (c.GetComponent<MeshFilter>() != null) flowerPrefabs.Add(c);
                }
            }

            // ── 4. Ground Terrains ───────────────────────────────────────────────
            var groundTerrain = GameObject.Find("Ground_Terrain");
            if (groundTerrain != null)
            {
                groundTerrain.transform.position = new Vector3(-85f, 0f, 130f);
                groundTerrain.transform.localScale = new Vector3(22f, 1f, 42f);
                if (groundMat != null) groundTerrain.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            }

            var eastLand = GameObject.Find("Eastern_Riverbank_Land");
            if (eastLand != null && eastSwampMat != null)
            {
                eastLand.transform.position = new Vector3(110f, 0f, 130f);
                eastLand.transform.localScale = new Vector3(12f, 1f, 42f);
                eastLand.GetComponent<MeshRenderer>().sharedMaterial = eastSwampMat;
            }

            var estuarySpit = GameObject.Find("Estuary_Mud_Spit");
            if (estuarySpit != null && groundMat != null)
            {
                estuarySpit.transform.position = new Vector3(-85f, 0f, -84f);
                estuarySpit.transform.localScale = new Vector3(22f, 1f, 1.9f);
                estuarySpit.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            }

            // ── 5. Ocean & Old River Cleanup ─────────────────────────────────────
            var ocean = GameObject.Find("BayOfBengal_Ocean");
            if (ocean != null)
            {
                ocean.transform.position = new Vector3(0f, 0.08f, -690f);
                ocean.transform.localScale = new Vector3(180f, 1f, 122f);
                if (oceanMat != null) ocean.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;
                if (ocean.GetComponent<OceanWaterAnimator>() == null) ocean.AddComponent<OceanWaterAnimator>();
            }

            // Destroy old straight river corridor and segments so the braided delta takes over cleanly
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r != null && (r.name == "River_System" || r.name.StartsWith("River_Segment_") || r.name == "River_Water"))
                {
                    Object.DestroyImmediate(r);
                }
            }

            // Continuous River Bed Underlay bridging Ground_Terrain (ends X=25) and Eastern_Riverbank_Land (starts X=50)
            var riverBed = GameObject.Find("River_Bed_Underlay");
            if (riverBed == null)
            {
                riverBed = GameObject.CreatePrimitive(PrimitiveType.Plane);
                riverBed.name = "River_Bed_Underlay";
            }
            riverBed.transform.position = new Vector3(37.5f, -0.02f, 130f);
            riverBed.transform.localScale = new Vector3(3.5f, 1f, 42f);
            var rbr = riverBed.GetComponent<MeshRenderer>();
            if (rbr != null && riverMat != null) rbr.sharedMaterial = riverMat;

            // Position Tiger at StartZone (-120, 30) facing along trail
            var tigerGO = GameObject.Find("Tiger");
            if (tigerGO != null)
            {
                var cc = tigerGO.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                tigerGO.transform.position = new Vector3(-120f, 0.4f, 30f);
                Vector3 startForward = (new Vector3(-104f, 0f, 34f) - new Vector3(-120f, 0f, 30f)).normalized;
                tigerGO.transform.rotation = Quaternion.LookRotation(startForward, Vector3.up);
                if (cc != null) cc.enabled = true;
            }

            // Universal Backpack attached to all 4 species
            string[] allSpeciesNames = { "Tiger", "Deer", "Dolphin", "Hilsha" };
            foreach (var spName in allSpeciesNames)
            {
                var spGO = GameObject.Find(spName);
                if (spGO == null)
                {
                    foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                    {
                        if (go.scene.isLoaded && go.name == spName) { spGO = go; break; }
                    }
                }
                if (spGO != null)
                {
                    var bp = spGO.GetComponent<Backpack>();
                    if (bp == null) bp = spGO.AddComponent<Backpack>();
                    bp.startingCountPerTool = 3;
                }
            }

            // ── 6. Clean Forest Hierarchy ────────────────────────────────────────
            var forestParent = GameObject.Find("Mangrove_Forest");
            if (forestParent == null) forestParent = new GameObject("Mangrove_Forest");

            string[] groupsToClean = new string[] {
                "Riverbank_Mangroves", "Safe_Dense_Canopy", "Medium_Sparse_Recovery",
                "High_Degraded_Wasteland", "Undergrowth_Flora", "Safe_Sanctuaries",
                "Danger_Mudflats", "Forest_Trails", "Coastal_Coconut_Groves", "Deforestation_Zones",
                "East_Wild_Mangrove_Swamp", "Green_Healing_Sanctuaries", "Fauna_Threats_And_Traps",
                "Braided_River_Delta", "Exploration_Bonus_Pickups", "Tool_Pickups_Exploration"
            };
            foreach (var gName in groupsToClean)
            {
                var tr = forestParent.transform.Find(gName);
                if (tr != null) Object.DestroyImmediate(tr.gameObject);
            }

            Random.InitState(1971);

            // ── 7. PATH & CORRIDOR WAYPOINTS (WINDING ROUTE FROM START TO GOAL) ──
            // StartZone: (-120, 30), GoalZone: (-40, 155)
            Vector3[] mainTrailWaypoints = new Vector3[] {
                new Vector3(-120f, 0f, 30f),   // W0: Level Start Clearing
                new Vector3(-104f, 0f, 34f),   // W1: Exiting start clearing
                new Vector3(-86f, 0f, 42f),    // W2: Mandarbaria Deer Glade
                new Vector3(-72f, 0f, 52f),    // W3: Junction to Kotka Secret Sanctuary
                new Vector3(-58f, 0f, 66f),    // W4: Bottleneck along Deforestation edge (ON-PATH HAZARD!)
                new Vector3(-46f, 0f, 78f),    // W5: Junction to Riverbank Poacher shortcut
                new Vector3(-35f, 0f, 90f),    // W6: Jamtola Forest Haven
                new Vector3(-30f, 0f, 108f),   // W7: Winding bend around mangrove bluff
                new Vector3(-32f, 0f, 126f),   // W8: Riverbank confluence overlook (adjacent to Hypersaline flat)
                new Vector3(-38f, 0f, 142f),   // W9: Valley entrance
                new Vector3(-40f, 0f, 155f)    // W10: Goal - Lush Green Sanctuary
            };

            // Side Branches (Exploration Detours)
            List<Vector3[]> sideBranches = new List<Vector3[]> {
                // Branch 1: Detour to secret Kotka Wildlife Haven
                new Vector3[] {
                    new Vector3(-72f, 0f, 52f),
                    new Vector3(-58f, 0f, 38f),
                    new Vector3(-42f, 0f, 25f) // Ends at Kotka Haven
                },
                // Branch 2: Dangerous Riverbank Shortcut (crosses directly through Poacher Wire Snares!)
                new Vector3[] {
                    new Vector3(-46f, 0f, 78f),
                    new Vector3(-28f, 0f, 68f),
                    new Vector3(-12f, 0f, 52f) // Ends at Riverbank Poacher Overlook
                },
                // Branch 3: Detour into Deep Western Ancient Grove
                new Vector3[] {
                    new Vector3(-30f, 0f, 108f),
                    new Vector3(-56f, 0f, 122f),
                    new Vector3(-86f, 0f, 132f) // Ends at Ancient Sundari Grove
                }
            };

            // Helper: distance from pt to a segment AB
            System.Func<Vector2, Vector2, Vector2, float> DistToSegment = (p, a, b) =>
            {
                Vector2 ab = b - a;
                float l2 = ab.sqrMagnitude;
                if (l2 < 0.0001f) return Vector2.Distance(p, a);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
                Vector2 proj = a + t * ab;
                return Vector2.Distance(p, proj);
            };

            // Helper: minimum distance from pt to ANY path segment
            System.Func<Vector3, float> DistanceToAnyPath = (pt) =>
            {
                Vector2 p2 = new Vector2(pt.x, pt.z);
                float minDist = float.MaxValue;

                // Main trail
                for (int i = 0; i < mainTrailWaypoints.Length - 1; i++)
                {
                    Vector2 a = new Vector2(mainTrailWaypoints[i].x, mainTrailWaypoints[i].z);
                    Vector2 b = new Vector2(mainTrailWaypoints[i + 1].x, mainTrailWaypoints[i + 1].z);
                    float d = DistToSegment(p2, a, b);
                    if (d < minDist) minDist = d;
                }

                // Side branches
                foreach (var branch in sideBranches)
                {
                    for (int i = 0; i < branch.Length - 1; i++)
                    {
                        Vector2 a = new Vector2(branch[i].x, branch[i].z);
                        Vector2 b = new Vector2(branch[i + 1].x, branch[i + 1].z);
                        float d = DistToSegment(p2, a, b);
                        if (d < minDist) minDist = d;
                    }
                }

                return minDist;
            };

            // ── 8. VISIBLE GROUND TRAILS (REASONABLY VISIBLE WALKABLE CORRIDOR) ───
            var trailGroup = new GameObject("Forest_Trails").transform;
            trailGroup.SetParent(forestParent.transform);

            // Create visible dirt/mud trail ribbons along paths
            Material trailMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            trailMat.color = new Color(0.42f, 0.34f, 0.22f, 1.0f); // worn forest mud path
            trailMat.SetFloat("_Smoothness", 0.38f);
            if (richGroundTex != null)
            {
                trailMat.SetTexture("_BaseMap", richGroundTex);
                trailMat.SetTextureScale("_BaseMap", new Vector2(1f, 14f));
            }

            System.Action<Vector3[], float, string> BuildTrailRibbon = (pts, width, ribbonName) =>
            {
                GameObject ribbonGO = new GameObject(ribbonName);
                ribbonGO.transform.SetParent(trailGroup, false);

                Mesh mesh = new Mesh();
                List<Vector3> verts = new List<Vector3>();
                List<int> tris = new List<int>();
                List<Vector2> uvs = new List<Vector2>();

                float halfW = width * 0.5f;
                float distAccum = 0f;

                for (int i = 0; i < pts.Length; i++)
                {
                    Vector3 forward;
                    if (i == 0) forward = (pts[1] - pts[0]).normalized;
                    else if (i == pts.Length - 1) forward = (pts[i] - pts[i - 1]).normalized;
                    else forward = ((pts[i] - pts[i - 1]).normalized + (pts[i + 1] - pts[i]).normalized).normalized;

                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    Vector3 center = new Vector3(pts[i].x, 0.025f, pts[i].z);

                    verts.Add(center - right * halfW);
                    verts.Add(center + right * halfW);

                    if (i > 0) distAccum += Vector3.Distance(pts[i], pts[i - 1]);
                    uvs.Add(new Vector2(0f, distAccum * 0.1f));
                    uvs.Add(new Vector2(1f, distAccum * 0.1f));

                    if (i > 0)
                    {
                        int baseIdx = (i - 1) * 2;
                        tris.Add(baseIdx);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 1);

                        tris.Add(baseIdx + 1);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 3);
                    }
                }

                mesh.SetVertices(verts);
                mesh.SetTriangles(tris, 0);
                mesh.SetUVs(0, uvs);
                mesh.RecalculateNormals();

                var mf = ribbonGO.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = ribbonGO.AddComponent<MeshRenderer>();
                mr.sharedMaterial = trailMat;
            };

            BuildTrailRibbon(mainTrailWaypoints, 5.0f, "Main_Winding_Corridor");
            for (int b = 0; b < sideBranches.Count; b++)
            {
                BuildTrailRibbon(sideBranches[b], 4.0f, $"SideBranch_{b + 1}");
            }

            // ── 9. EXPLORATION BONUS PICKUPS (AT END OF SIDE-BRANCHES) ─────────────
            var pickupGroup = new GameObject("Exploration_Bonus_Pickups").transform;
            pickupGroup.SetParent(forestParent.transform);

            // Bonus 1: Secret Kotka Blue Carbon Haven
            {
                Vector3 bPos = new Vector3(-42f, 0.4f, 25f);
                var bonus1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bonus1.name = "Bonus_Kotka_Blue_Carbon";
                bonus1.transform.SetParent(pickupGroup);
                bonus1.transform.position = bPos;
                bonus1.transform.localScale = new Vector3(2.2f, 0.2f, 2.2f);
                var col = bonus1.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                var trig = bonus1.AddComponent<ExplorationBonusTrigger>();
                trig.bonusTitle = "গোপন কটকা অভয়ারণ্য আবিষ্কার! (Blue Carbon Sanctuary)";
                trig.bonusFactBn = "সুন্দরবনের শ্বাসমূলীয় বনাঞ্চল প্রতি হেক্টরে সাধারণ বনের চেয়ে ৪ গুণ বেশি কার্বন ডাই অক্সাইড মাটির নিচে স্থায়ীভাবে আবদ্ধ (Blue Carbon) রাখতে সক্ষম।";
                trig.citation = "NASA Carbon Monitoring System (CMS) & UNESCO";
                trig.healAmount = 50f;

                var rend = bonus1.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.color = new Color(0.2f, 0.85f, 0.45f, 0.75f);
                    rend.sharedMaterial = m;
                }
            }

            // Bonus 2: Deep Western Ancient Sundari Grove
            {
                Vector3 bPos = new Vector3(-86f, 0.4f, 132f);
                var bonus2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bonus2.name = "Bonus_Ancient_Sundari_Grove";
                bonus2.transform.SetParent(pickupGroup);
                bonus2.transform.position = bPos;
                bonus2.transform.localScale = new Vector3(2.2f, 0.2f, 2.2f);
                var col = bonus2.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                var trig = bonus2.AddComponent<ExplorationBonusTrigger>();
                trig.bonusTitle = "প্রাচীন সুন্দরী কুঞ্জ (Ancient Sundari Grove)";
                trig.bonusFactBn = "সুন্দরবনের নাম হয়েছে 'সুন্দরী' (Heritiera fomes) গাছের নামানুসারে। মিষ্টি পানির প্রবাহ কমে লবণাক্ততা বৃদ্ধি পাওয়ায় এই প্রাচীন সুন্দরী গাছগুলো আজ শীর্ষ-শুকনো (Top-dying) রোগে আক্রান্ত।";
                trig.citation = "Bangladesh Forest Dept & NASA MODIS NDVI Time-Series";
                trig.healAmount = 45f;

                var rend = bonus2.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.color = new Color(0.95f, 0.78f, 0.22f, 0.75f);
                    rend.sharedMaterial = m;
                }
            }

            // ── 10. HAZARD DISCS (ON-PATH RISK/REWARD DECISIONS) ─────────────────
            // Requirement 2: Move hazard zones so at least one overlaps or sits directly on the path!
            Vector3[] dangerCenters = new Vector3[] {
                new Vector3(-58f, 0f, 66f),    // Hazard 1: DIRECTLY ON MAIN PATH (Bottleneck along Deforestation edge)
                new Vector3(-12f, 0f, 52f),    // Hazard 2: DIRECTLY ON RIVER SHORTCUT (Poacher Traps & Wire Snares)
                new Vector3(-18f, 0f, 126f),   // Hazard 3: Hypersaline Mudflat at river confluence
                new Vector3(-105f, 0f, 210f)   // Hazard 4: Northern Deep Degradation Patch
            };
            float[] dangerRadii = new float[] { 9.5f, 13f, 14f, 15f };
            string[] dangerRanks = new string[] { "high", "high", "medium", "medium" };
            string[] dangerNames = new string[] {
                "Deforestation Mud Gap (বন উজাড় ভূমি খানাখন্দ)",
                "Poacher Mudflat (শিকারি তারের ফাঁদ অঞ্চল)",
                "Hypersaline Flat (লবণাক্ত কাদা ভূমি)",
                "Degraded Soil Patch (ক্ষতিগ্রস্ত ভূমি)"
            };

            var dangerGroup = new GameObject("Danger_Mudflats").transform;
            dangerGroup.SetParent(forestParent.transform);

            for (int d = 0; d < dangerCenters.Length; d++)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = $"HazardZone_{d + 1}_{dangerNames[d]}";
                disc.transform.SetParent(dangerGroup);
                disc.transform.position = new Vector3(dangerCenters[d].x, 0.04f, dangerCenters[d].z);
                disc.transform.localScale = new Vector3(dangerRadii[d] * 2f, 0.02f, dangerRadii[d] * 2f);

                var col = disc.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                var trig = disc.AddComponent<HazardZoneTrigger>();
                trig.riskTier = dangerRanks[d];
                trig.rawDataValue = d == 2 ? 28.5f : 0.18f;
                trig.rawDataUnit = d == 2 ? "PSU" : "NDVI degradation";
                trig.ConfigureToolRequirement();

                var rend = disc.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.enabled = true;
                    var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (dangerRanks[d] == "high")
                        m.color = new Color(0.88f, 0.12f, 0.12f, 0.48f); // vivid red
                    else
                        m.color = new Color(0.95f, 0.65f, 0.10f, 0.42f); // amber warning
                    m.SetFloat("_Smoothness", 0.6f);
                    rend.sharedMaterial = m;
                }
            }

            System.Func<Vector3, float, bool> IsInDangerZone = (pt, margin) =>
            {
                for (int i = 0; i < dangerCenters.Length; i++)
                {
                    float dist = Vector2.Distance(new Vector2(pt.x, pt.z), new Vector2(dangerCenters[i].x, dangerCenters[i].z));
                    if (dist < (dangerRadii[i] + margin)) return true;
                }
                return false;
            };

            // Optional debris / red-mangrove prefabs (null = feature disabled gracefully)
            GameObject debrisFbx    = null; // e.g. AssetDatabase.LoadAssetAtPath<GameObject>("Assets/.../Debris.fbx");
            GameObject redMangroveObj = null; // e.g. AssetDatabase.LoadAssetAtPath<GameObject>("Assets/.../RedMangrove.obj");

            // ── 11. DEFORESTATION CLEARING (WITH BROKEN STUMPS & ROOTS) ───────────
            var deforestGroup = new GameObject("Deforestation_Zones").transform;
            deforestGroup.SetParent(forestParent.transform);

            Vector3[] deforestCenters = new Vector3[] {
                new Vector3(-60f, 0f, 75f),    // Main Central Logging Site
                new Vector3(-105f, 0f, 175f),  // Western Deep Forest Logging Clearing
                new Vector3(-50f, 0f, 260f)    // Northern Logging Clearing
            };
            float[] deforestRadii = new float[] { 16f, 16f, 14f };

            for (int s = 0; s < deforestCenters.Length; s++)
            {
                Vector3 site = deforestCenters[s];
                float sRadius = deforestRadii[s];
                int stumpCount = s == 0 ? 30 : 20;

                var clearingDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                clearingDisc.name = $"Deforestation_Clearing_Bed_{s + 1}";
                clearingDisc.transform.SetParent(deforestGroup);
                clearingDisc.transform.position = new Vector3(site.x, 0.02f, site.z);
                clearingDisc.transform.localScale = new Vector3(sRadius * 2f, 0.01f, sRadius * 2f);

                var cCol = clearingDisc.GetComponent<Collider>();
                if (cCol != null) Object.DestroyImmediate(cCol);

                var cRend = clearingDisc.GetComponent<MeshRenderer>();
                if (cRend != null)
                {
                    var clearMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    clearMat.color = new Color(0.28f, 0.18f, 0.12f, 0.70f);
                    cRend.sharedMaterial = clearMat;
                }

                for (int i = 0; i < stumpCount; i++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist = Mathf.Sqrt(Random.Range(0.04f, 0.90f)) * sRadius;
                    Vector3 pos = site + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                    // Skip if directly on path corridor
                    if (DistanceToAnyPath(pos) < 3.2f) continue;

                    if (brokenTreePrefab != null)
                    {
                        var bt = Object.Instantiate(brokenTreePrefab, pos, Quaternion.identity, deforestGroup);
                        bt.name = $"BrokenTree_Stump_{s}_{i}";

                        int style = i % 3;
                        if (style == 0) // Fallen horizontal log
                        {
                            bt.transform.rotation = Quaternion.Euler(Random.Range(80f, 90f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
                            bt.transform.position = new Vector3(pos.x, 0.12f, pos.z);
                            bt.transform.localScale = Vector3.one * Random.Range(1.4f, 2.2f);
                        }
                        else if (style == 1) // Leaning / snapped trunk
                        {
                            bt.transform.rotation = Quaternion.Euler(Random.Range(35f, 60f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
                            bt.transform.position = new Vector3(pos.x, 0.08f, pos.z);
                            bt.transform.localScale = Vector3.one * Random.Range(1.5f, 2.2f);
                        }
                        else // Upright chopped stump
                        {
                            bt.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f));
                            bt.transform.position = new Vector3(pos.x, 0f, pos.z);
                            bt.transform.localScale = Vector3.one * Random.Range(1.2f, 1.8f);
                        }

                        var mr = bt.GetComponentInChildren<Renderer>();
                        if (mr != null && barkMat != null) mr.sharedMaterial = barkMat;
                    }

                    if (debrisFbx != null && i % 2 == 0)
                    {
                        Vector3 debPos = pos + new Vector3(Random.Range(-1.4f, 1.4f), 0f, Random.Range(-1.4f, 1.4f));
                        if (DistanceToAnyPath(debPos) >= 3.0f)
                        {
                            var deb = Object.Instantiate(debrisFbx, debPos, Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f)), deforestGroup);
                            deb.transform.localScale = Vector3.one * Random.Range(1.2f, 1.9f);
                            var dr = deb.GetComponent<Renderer>();
                            if (dr != null && barkMat != null) dr.sharedMaterial = barkMat;
                        }
                    }
                }
            }

            System.Func<Vector3, float, bool> IsInDeforestSite = (pt, margin) =>
            {
                for (int i = 0; i < deforestCenters.Length; i++)
                {
                    float dist = Vector2.Distance(new Vector2(pt.x, pt.z), new Vector2(deforestCenters[i].x, deforestCenters[i].z));
                    if (dist < (deforestRadii[i] + margin)) return true;
                }
                return false;
            };

            // ── 12. GREEN HEALING GLADES (GRASS & FOOD FOR DEER/TIGER) ───────────
            var sanctuaryGroup = new GameObject("Green_Healing_Sanctuaries").transform;
            sanctuaryGroup.SetParent(forestParent.transform);

            Vector3[] sanctuarySites = new Vector3[] {
                new Vector3(-42f, 0f, 25f),   // Kotka Wildlife Sanctuary
                new Vector3(-86f, 0f, 42f),   // Mandarbaria Deer Glade
                new Vector3(-35f, 0f, 90f),   // Jamtola Forest Glade
                new Vector3(-86f, 0f, 132f),  // Ancient Sundari Grove
                new Vector3(-40f, 0f, 155f)   // Northern Goal Sanctuary
            };
            float[] sanctuaryRadii = new float[] { 18f, 18f, 18f, 18f, 22f };
            string[] sanctuaryNames = new string[] {
                "Kotka Wildlife Sanctuary (কটকা অভয়ারণ্য)",
                "Mandarbaria Deer Glade (মন্দারবাড়িয়া তৃণভূমি)",
                "Jamtola Forest Haven (জামতলা বৃক্ষবন)",
                "Ancient Sundari Grove (প্রাচীন সুন্দরী কুঞ্জ)",
                "Goal: Lush Green Sanctuary (চূড়ান্ত সবুজ অভয়ারণ্য)"
            };

            for (int s = 0; s < sanctuarySites.Length; s++)
            {
                Vector3 site = sanctuarySites[s];
                float radius = sanctuaryRadii[s];

                var healGO = new GameObject($"HealingZone_{s + 1}_{sanctuaryNames[s]}");
                healGO.transform.SetParent(sanctuaryGroup);
                healGO.transform.position = site;

                var sCol = healGO.AddComponent<SphereCollider>();
                sCol.isTrigger = true;
                sCol.radius = radius;
                var hz = healGO.AddComponent<HealingZone>();
                hz.healPerSecond = 8f;
                hz.sanctuaryName = sanctuaryNames[s];

                int grassCount = Random.Range(20, 32);
                for (int g = 0; g < grassCount; g++)
                {
                    float ga = Random.Range(0f, Mathf.PI * 2f);
                    float gd = Mathf.Sqrt(Random.Range(0.04f, 0.88f)) * radius;
                    Vector3 gPos = site + new Vector3(Mathf.Cos(ga) * gd, 0f, Mathf.Sin(ga) * gd);

                    if (grassPrefabs.Count > 0)
                    {
                        var gp = grassPrefabs[Random.Range(0, grassPrefabs.Count)];
                        var gr = Object.Instantiate(gp, gPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), healGO.transform);
                        gr.name = "Glade_GreenGrass";
                        gr.transform.localScale = Vector3.one * Random.Range(1.3f, 2.2f);
                        var grRend = gr.GetComponent<MeshRenderer>();
                        if (grRend != null && grassMat != null) grRend.sharedMaterial = grassMat;

                        if (g % 3 == 0)
                        {
                            var fCol = gr.AddComponent<SphereCollider>();
                            fCol.isTrigger = true;
                            var food = gr.AddComponent<FoodItem>();
                            if (food != null)
                            {
                                food.targetSpecies = "deer";
                                food.healAmount = 30f;
                                food.foodLabel = "Fresh Grazing Grass (কচি ঘাস)";
                            }
                        }
                    }

                    if (flowerPrefabs.Count > 0 && g % 4 == 0)
                    {
                        var fp = flowerPrefabs[Random.Range(0, flowerPrefabs.Count)];
                        var fl = Object.Instantiate(fp, gPos + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), healGO.transform);
                        fl.name = "Glade_WildFlower";
                        fl.transform.localScale = Vector3.one * Random.Range(1.1f, 1.8f);
                        var fRend = fl.GetComponent<MeshRenderer>();
                        if (fRend != null && flowerMat != null) fRend.sharedMaterial = flowerMat;
                    }
                }

                // Prey marker for Tiger
                for (int p = 0; p < 2; p++)
                {
                    float pa = Random.Range(0f, Mathf.PI * 2f);
                    float pd = Random.Range(3f, radius * 0.6f);
                    Vector3 pPos = site + new Vector3(Mathf.Cos(pa) * pd, 0.05f, Mathf.Sin(pa) * pd);

                    var preyGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    preyGO.name = $"Prey_Deer_Marker_{s}_{p}";
                    preyGO.transform.SetParent(healGO.transform);
                    preyGO.transform.position = pPos;
                    preyGO.transform.localScale = new Vector3(1.2f, 0.04f, 1.2f);
                    var pm = preyGO.AddComponent<PreyMarker>();
                    pm.healAmount = 50f;
                    var pmRend = preyGO.GetComponent<MeshRenderer>();
                    if (pmRend != null)
                    {
                        var pMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        pMat.color = new Color(0.95f, 0.82f, 0.25f, 0.85f);
                        pmRend.sharedMaterial = pMat;
                    }
                }
            }

            // ── 13. DENSE PHOTOREALISTIC MANGROVE FOREST (WITH WIDE CLEAR CORRIDOR) ─
            // Requirement 1 & 2:
            // 1. Keep dense vegetation completely OFF the path corridor (width ~9.2m clearance)!
            // 2. Keep surrounding areas dense and natural (spacing 4.4m x 4.2m)
            var inlandCanopy = new GameObject("Safe_Dense_Canopy").transform;
            inlandCanopy.SetParent(forestParent.transform);

            float pathClearanceRadius = 9.2f;

            for (float fz = -28f; fz <= 330f; fz += 10.5f)
            {
                for (float fx = -168f; fx <= 12f; fx += 10.5f)
                {
                    Vector3 pos = new Vector3(fx + Random.Range(-2.2f, 2.2f), 0f, fz + Random.Range(-2.2f, 2.2f));

                    // 1. CLEAR CORRIDOR: Skip trees along the trail network!
                    if (DistanceToAnyPath(pos) < pathClearanceRadius) continue;

                    // 2. CLEAR START & GOAL: Keep immediate spawn and victory glades open
                    if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(-120f, 30f)) < 20.0f) continue;
                    if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(-40f, 155f)) < 24.0f) continue;

                    if (IsInDangerZone(pos, 2.0f)) continue;
                    if (IsInDeforestSite(pos, 2.0f)) continue;

                    // Lush Mangrove Tree in surrounding deep woods
                    if (commonTreePrefab != null)
                    {
                        var prefab = (Random.value > 0.45f) ? commonTreePrefab : twistedTreePrefab;
                        var rot = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));
                        var rm = Object.Instantiate(prefab, pos, rot, inlandCanopy);
                        rm.name = "Mangrove_StandbyTree";
                        rm.transform.localScale = Vector3.one * Random.Range(1.05f, 1.45f);

                        // If tree is within 11m of the path, add a subtle trunk collider so player doesn't clip its trunk
                        if (DistanceToAnyPath(pos) < 11.0f)
                        {
                            var cap = rm.AddComponent<CapsuleCollider>();
                            cap.radius = 0.55f;
                            cap.height = 3.5f;
                            cap.center = new Vector3(0f, 1.75f, 0f);
                        }
                    }

                    // Broken tree stumps & fallen logs scattered naturally on deep forest floor
                    if (brokenTreePrefab != null && Random.value < 0.06f)
                    {
                        Vector3 btPos = pos + new Vector3(Random.Range(1.5f, 2.5f), 0f, Random.Range(1.5f, 2.5f));
                        if (DistanceToAnyPath(btPos) >= 7.0f)
                        {
                            var bt = Object.Instantiate(brokenTreePrefab, btPos, Quaternion.identity, inlandCanopy);
                            bt.name = "Forest_BrokenStump";
                            if (Random.value < 0.40f)
                            {
                                bt.transform.rotation = Quaternion.Euler(Random.Range(78f, 90f), Random.Range(0f, 360f), 0f);
                                bt.transform.position = new Vector3(btPos.x, 0.12f, btPos.z);
                            }
                            else
                            {
                                bt.transform.rotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 360f), Random.Range(-6f, 6f));
                            }
                            bt.transform.localScale = Vector3.one * Random.Range(1.2f, 1.9f);
                            var mr = bt.GetComponentInChildren<Renderer>();
                            if (mr != null && barkMat != null) mr.sharedMaterial = barkMat;
                        }
                    }

                    // Stilt roots debris clusters
                    if (debrisFbx != null && Random.value < 0.18f)
                    {
                        Vector3 debPos = pos + new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f));
                        if (DistanceToAnyPath(debPos) >= 7.0f)
                        {
                            var deb = Object.Instantiate(debrisFbx, debPos, Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f)), inlandCanopy);
                            deb.transform.localScale = Vector3.one * Random.Range(1.3f, 2.0f);
                            var dr = deb.GetComponent<Renderer>();
                            if (dr != null && barkMat != null) dr.sharedMaterial = barkMat;
                        }
                    }
                }
            }

            // ── 14. COASTAL COCONUT GROVES (HORIZON WALL AT OCEAN EDGE) ──────────
            var coastalGroup = new GameObject("Coastal_Coconut_Groves").transform;
            coastalGroup.SetParent(forestParent.transform);

            // Layer 1: Tall leaning coconut palms at the sea front (Z = -38 to -88)
            for (float cz = -38f; cz >= -88f; cz -= 9.5f)
            {
                for (float cx = -170f; cx <= 18f; cx += 9.5f)
                {
                    float px = cx + Random.Range(-2.2f, 2.2f);
                    float pz = cz + Random.Range(-2.2f, 2.2f);
                    Vector3 pos = new Vector3(px, 0f, pz);

                    if (DistanceToAnyPath(pos) < pathClearanceRadius) continue;

                    if (coconutPalm != null)
                    {
                        var rot = Quaternion.Euler(Random.Range(-15f, -5f), Random.Range(0f, 360f), Random.Range(-6f, 6f));
                        var cp = Object.Instantiate(coconutPalm, pos, rot, coastalGroup);
                        cp.name = "Coastal_Coconut_Tree";
                        cp.transform.localScale = Vector3.one * Random.Range(1.15f, 1.55f);
                    }
                }
            }

            // Layer 2: Bright green understory bushes/palms directly in front of the coconut palms
            for (float cz = -34f; cz >= -42f; cz -= 7.0f)
            {
                for (float cx = -168f; cx <= 16f; cx += 8.5f)
                {
                    float px = cx + Random.Range(-1.8f, 1.8f);
                    Vector3 pos = new Vector3(px, 0f, cz + Random.Range(-1.5f, 1.5f));

                    if (DistanceToAnyPath(pos) < pathClearanceRadius) continue;

                    if (evergladesPalm != null)
                    {
                        var rot = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 360f), Random.Range(-6f, 6f));
                        var ep = Object.Instantiate(evergladesPalm, pos, rot, coastalGroup);
                        ep.name = "Coastal_Green_Fringe";
                        ep.transform.localScale = Vector3.one * Random.Range(0.75f, 1.15f);
                    }
                }
            }

            // ── 15. BRAIDED RIVER DELTA & CENTRAL MANGROVE ISLAND ────────────────
            // Redesigning river to match satellite image (winding branching channels)
            var deltaGroup = new GameObject("Braided_River_Delta").transform;
            deltaGroup.SetParent(forestParent.transform);

            // Helper: procedural water ribbon
            System.Action<Vector3[], float, string> BuildWaterRibbon = (pts, width, channelName) =>
            {
                GameObject waterGO = new GameObject(channelName);
                waterGO.transform.SetParent(deltaGroup, false);

                Mesh mesh = new Mesh();
                List<Vector3> verts = new List<Vector3>();
                List<int> tris = new List<int>();
                List<Vector2> uvs = new List<Vector2>();

                float halfW = width * 0.5f;
                float distAccum = 0f;

                for (int i = 0; i < pts.Length; i++)
                {
                    Vector3 forward;
                    if (i == 0) forward = (pts[1] - pts[0]).normalized;
                    else if (i == pts.Length - 1) forward = (pts[i] - pts[i - 1]).normalized;
                    else forward = ((pts[i] - pts[i - 1]).normalized + (pts[i + 1] - pts[i]).normalized).normalized;

                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    Vector3 center = new Vector3(pts[i].x, 0.04f, pts[i].z);

                    verts.Add(center - right * halfW);
                    verts.Add(center + right * halfW);

                    if (i > 0) distAccum += Vector3.Distance(pts[i], pts[i - 1]);
                    uvs.Add(new Vector2(0f, distAccum * 0.05f));
                    uvs.Add(new Vector2(1f, distAccum * 0.05f));

                    if (i > 0)
                    {
                        int baseIdx = (i - 1) * 2;
                        tris.Add(baseIdx);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 1);

                        tris.Add(baseIdx + 1);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 3);
                    }
                }

                mesh.SetVertices(verts);
                mesh.SetTriangles(tris, 0);
                mesh.SetUVs(0, uvs);
                mesh.RecalculateNormals();

                var mf = waterGO.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = waterGO.AddComponent<MeshRenderer>();
                mr.sharedMaterial = riverMat;

                var col = waterGO.AddComponent<MeshCollider>();
                col.sharedMesh = mesh;
            };

            // Main Estuary (South)
            Vector3[] southEstuary = new Vector3[] {
                new Vector3(32f, 0f, -80f),
                new Vector3(30f, 0f, -45f),
                new Vector3(28f, 0f, -15f),
                new Vector3(25f, 0f, 15f)  // Fork point
            };
            BuildWaterRibbon(southEstuary, 30f, "South_Estuary_Mouth");

            // Western Channel (curves left around Central Island)
            Vector3[] westChannel = new Vector3[] {
                new Vector3(25f, 0f, 15f),
                new Vector3(18f, 0f, 35f),
                new Vector3(12f, 0f, 60f),
                new Vector3(16f, 0f, 88f),
                new Vector3(24f, 0f, 115f) // Rejoin point
            };
            BuildWaterRibbon(westChannel, 18f, "Western_Braided_Channel");

            // Eastern Channel (curves right around Central Island)
            Vector3[] eastChannel = new Vector3[] {
                new Vector3(25f, 0f, 15f),
                new Vector3(36f, 0f, 38f),
                new Vector3(54f, 0f, 65f),
                new Vector3(44f, 0f, 92f),
                new Vector3(24f, 0f, 115f) // Rejoin point
            };
            BuildWaterRibbon(eastChannel, 20f, "Eastern_Braided_Channel");

            // North Merged River
            Vector3[] northRiver = new Vector3[] {
                new Vector3(24f, 0f, 115f),
                new Vector3(26f, 0f, 155f),
                new Vector3(35f, 0f, 195f),
                new Vector3(38f, 0f, 240f),
                new Vector3(42f, 0f, 290f)
            };
            BuildWaterRibbon(northRiver, 24f, "North_Merged_River");

            // Side Tidal Khal 1 (winding into western forest matching satellite image)
            Vector3[] tidalKhal1 = new Vector3[] {
                new Vector3(18f, 0f, 35f),
                new Vector3(6f, 0f, 31f),
                new Vector3(-6f, 0f, 34f)
            };
            BuildWaterRibbon(tidalKhal1, 8f, "Tidal_Khal_SouthWest");

            // Side Tidal Khal 2 (winding into western forest matching satellite image)
            Vector3[] tidalKhal2 = new Vector3[] {
                new Vector3(16f, 0f, 88f),
                new Vector3(4f, 0f, 94f),
                new Vector3(-8f, 0f, 102f)
            };
            BuildWaterRibbon(tidalKhal2, 8f, "Tidal_Khal_MidWest");

            // Central Mangrove Island (sitting between the two braided channels)
            var islandGroup = new GameObject("Central_Mangrove_Island").transform;
            islandGroup.SetParent(deltaGroup);

            // Ground base for Central Island
            var islandBed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            islandBed.name = "Central_Island_Ground";
            islandBed.transform.SetParent(islandGroup);
            islandBed.transform.position = new Vector3(32f, 0.02f, 65f);
            islandBed.transform.localScale = new Vector3(18f, 0.05f, 75f);
            var ibRend = islandBed.GetComponent<MeshRenderer>();
            if (ibRend != null && groundMat != null) ibRend.sharedMaterial = groundMat;

            // Mangroves on Central Island
            for (float iz = 32f; iz <= 98f; iz += 5.5f)
            {
                for (float ix = 24f; ix <= 40f; ix += 5.5f)
                {
                    Vector3 pos = new Vector3(ix + Random.Range(-1.2f, 1.2f), 0f, iz + Random.Range(-1.4f, 1.4f));
                    if (redMangroveObj != null)
                    {
                        var rot = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));
                        var rm = Object.Instantiate(redMangroveObj, pos, rot, islandGroup);
                        rm.name = "Island_Mangrove";
                        rm.transform.localScale = Vector3.one * Random.Range(0.0011f, 0.0016f);
                    }

                    if (debrisFbx != null && Random.value < 0.45f)
                    {
                        Vector3 rPos = pos + new Vector3(Random.Range(-1.4f, 1.4f), -0.05f, Random.Range(-1.4f, 1.4f));
                        var root = Object.Instantiate(debrisFbx, rPos, Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f)), islandGroup);
                        root.name = "Island_WaterRoots";
                        root.transform.localScale = Vector3.one * Random.Range(1.4f, 2.2f);
                        var dr = root.GetComponent<Renderer>();
                        if (dr != null && barkMat != null) dr.sharedMaterial = barkMat;
                    }
                }
            }

            // ── 16. RIVERBANK MANGROVES (LINING THE BRAIDED CHANNELS) ────────────
            var riverbankGroup = new GameObject("Riverbank_Mangroves").transform;
            riverbankGroup.SetParent(forestParent.transform);

            // Lining the West Channel bank
            for (int i = 0; i < westChannel.Length; i++)
            {
                Vector3 pt = westChannel[i];
                for (int m = 0; m < 3; m++)
                {
                    Vector3 pos = new Vector3(pt.x - 10.5f - m * 2.5f, 0f, pt.z + Random.Range(-6f, 6f));
                    if (DistanceToAnyPath(pos) < pathClearanceRadius) continue;

                    if (redMangroveObj != null)
                    {
                        var rot = Quaternion.Euler(Random.Range(-3f, 3f), Random.Range(0f, 360f), Random.Range(6f, 16f));
                        var rm = Object.Instantiate(redMangroveObj, pos, rot, riverbankGroup);
                        rm.name = "River_OverhangingMangrove";
                        rm.transform.localScale = Vector3.one * Random.Range(0.0012f, 0.0017f);
                    }
                    if (debrisFbx != null && m == 0)
                    {
                        Vector3 rPos = new Vector3(pos.x + 1.8f, -0.06f, pos.z);
                        var root = Object.Instantiate(debrisFbx, rPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), riverbankGroup);
                        root.name = "River_WaterRoots";
                        root.transform.localScale = Vector3.one * Random.Range(1.4f, 2.2f);
                        var dr = root.GetComponent<Renderer>();
                        if (dr != null && barkMat != null) dr.sharedMaterial = barkMat;
                    }
                }
            }

            // ── 17. EAST WILD MANGROVE SWAMP (ACROSS EAST CHANNEL) ───────────────
            var eastSwampGroup = new GameObject("East_Wild_Mangrove_Swamp").transform;
            eastSwampGroup.SetParent(forestParent.transform);

            for (float ez = -30f; ez <= 300f; ez += 12.0f)
            {
                for (float ex = 68f; ex <= 165f; ex += 12.0f)
                {
                    Vector3 pos = new Vector3(ex + Random.Range(-2.2f, 2.2f), 0f, ez + Random.Range(-2.2f, 2.2f));
                    if (redMangroveObj != null)
                    {
                        var rot = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-4f, 4f));
                        var rm = Object.Instantiate(redMangroveObj, pos, rot, eastSwampGroup);
                        rm.name = "EastSwamp_DeepMangrove";
                        rm.transform.localScale = Vector3.one * Random.Range(0.0013f, 0.0018f);
                    }
                }
            }

            // ── 18. FAUNA THREATS, CROCODILE, BOATS & TRAPS ──────────────────────
            var threatsGroup = new GameObject("Fauna_Threats_And_Traps").transform;
            threatsGroup.SetParent(forestParent.transform);

            // Saltwater Crocodile in East braided channel
            if (crocPrefab != null)
            {
                var croc = Object.Instantiate(crocPrefab, new Vector3(48f, -0.1f, 62f), Quaternion.Euler(0f, 140f, 0f), threatsGroup);
                croc.name = "River_Saltwater_Crocodile";
                croc.transform.localScale = Vector3.one * 1.0f;
                var cCol = croc.AddComponent<BoxCollider>();
                cCol.isTrigger = true;
                var ct = croc.AddComponent<CrocodileThreat>();
                ct.biteDamage = 40f;
                ct.patrolOffset = new Vector3(0f, 0f, 22f);
                var matSetup = croc.AddComponent<CrocodileMaterialSetup>();
                matSetup.ApplyMaterials();
            }

            // Human Poacher Wire Snares near Poacher River Shortcut
            for (int t = 0; t < 5; t++)
            {
                float ta = Random.Range(0f, Mathf.PI * 2f);
                float td = Random.Range(3f, 10f);
                Vector3 tPos = dangerCenters[1] + new Vector3(Mathf.Cos(ta) * td, 0.05f, Mathf.Sin(ta) * td);

                var trap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trap.name = $"Human_Poacher_Trap_{t + 1}";
                trap.transform.SetParent(threatsGroup);
                trap.transform.position = tPos;
                trap.transform.localScale = new Vector3(1.2f, 0.08f, 1.2f);
                var tCol = trap.GetComponent<Collider>();
                if (tCol != null) tCol.isTrigger = true;
                var trapObs = trap.AddComponent<TrapObstacle>();
                trapObs.trapDamage = 35f;
                var tRend = trap.GetComponent<MeshRenderer>();
                if (tRend != null)
                {
                    var tm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    tm.color = new Color(0.20f, 0.20f, 0.22f, 1.0f);
                    tm.SetFloat("_Metallic", 0.85f);
                    tm.SetFloat("_Smoothness", 0.65f);
                    tRend.sharedMaterial = tm;
                }
            }

            // Illegal Monofilament Fishing Nets in river channels (clearable with Net-cutter knife)
            Vector3[] netPositions = new Vector3[] {
                new Vector3(18f, 0.2f, 40f),  // Western channel net
                new Vector3(28f, 0.2f, -25f)  // Southern estuary mouth net
            };
            for (int n = 0; n < netPositions.Length; n++)
            {
                var netGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                netGO.name = $"Illegal_Fishing_Net_{n + 1}";
                netGO.transform.SetParent(threatsGroup);
                netGO.transform.position = netPositions[n];
                netGO.transform.localScale = new Vector3(16f, 1.8f, 0.35f);
                var nCol = netGO.GetComponent<Collider>();
                if (nCol != null) nCol.isTrigger = true;

                var netObs = netGO.AddComponent<IllegalNetObstacle>();
                netObs.hitDamage = 25f;

                var nRend = netGO.GetComponent<MeshRenderer>();
                if (nRend != null)
                {
                    var nm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    nm.color = new Color(0.85f, 0.90f, 0.95f, 0.65f);
                    nm.SetFloat("_Smoothness", 0.8f);
                    nRend.sharedMaterial = nm;
                }
            }

            // Motorized Fishing Trawler in River
            var trawlerPos = new Vector3(26f, 0.05f, 145f);
            var boat = new GameObject("River_Motorized_Trawler");
            boat.transform.SetParent(threatsGroup);
            boat.transform.position = trawlerPos;

            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Trawler_Hull";
            hull.transform.SetParent(boat.transform, false);
            hull.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            hull.transform.localScale = new Vector3(3.2f, 1.2f, 8.5f);
            var hr = hull.GetComponent<MeshRenderer>();
            if (hr != null && barkMat != null) hr.sharedMaterial = barkMat;

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Trawler_Cabin";
            cabin.transform.SetParent(boat.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 1.4f, -1.2f);
            cabin.transform.localScale = new Vector3(2.4f, 1.5f, 2.8f);
            var cr = cabin.GetComponent<MeshRenderer>();
            if (cr != null && barkMat != null) cr.sharedMaterial = barkMat;

            var bCol = boat.AddComponent<BoxCollider>();
            bCol.isTrigger = true;
            bCol.size = new Vector3(3.6f, 2.0f, 9.0f);
            bCol.center = new Vector3(0f, 0.6f, 0f);

            var bt2 = boat.AddComponent<BoatThreat>();
            bt2.patrolSpeed = 3.5f;
            bt2.patrolDistance = 35f;
            bt2.directHitDamage = 30f;

            // ── 19. UNIVERSAL TOOL PICKUPS (COLLECTIBLES FOR BACKPACK) ────────────
            var toolPickupGroup = new GameObject("Tool_Pickups_Exploration").transform;
            toolPickupGroup.SetParent(forestParent.transform);

            var pickupsToSpawn = new (ToolType type, Vector3 pos)[] {
                // Seeds (Green) - near start, deforestation, and ancient grove
                (ToolType.Seed, new Vector3(-100f, 0.5f, 34f)),
                (ToolType.Seed, new Vector3(-68f, 0.5f, 54f)),
                (ToolType.Seed, new Vector3(-50f, 0.5f, 68f)),
                (ToolType.Seed, new Vector3(-84f, 0.5f, 130f)),

                // Trap Destroyers (Amber) - along poacher path and river shortcut
                (ToolType.TrapDestroyer, new Vector3(-74f, 0.5f, 52f)),
                (ToolType.TrapDestroyer, new Vector3(-40f, 0.5f, 74f)),
                (ToolType.TrapDestroyer, new Vector3(-24f, 0.5f, 64f)),
                (ToolType.TrapDestroyer, new Vector3(-34f, 0.5f, 92f)),

                // Cleaning Liquids (Cyan) - near Kotka, confluence, and hypersaline flat
                (ToolType.CleaningLiquid, new Vector3(-56f, 0.5f, 36f)),
                (ToolType.CleaningLiquid, new Vector3(-32f, 0.5f, 110f)),
                (ToolType.CleaningLiquid, new Vector3(-22f, 0.5f, 124f)),
                (ToolType.CleaningLiquid, new Vector3(-36f, 0.5f, 140f)),

                // Net Cutters (Steel Blue) - along river banks, mouth, and mudflats
                (ToolType.NetCutter, new Vector3(-14f, 0.5f, 54f)),
                (ToolType.NetCutter, new Vector3(12f, 0.5f, 58f)),
                (ToolType.NetCutter, new Vector3(22f, 0.5f, 16f)),
                (ToolType.NetCutter, new Vector3(38f, 0.5f, 25f))
            };

            for (int p = 0; p < pickupsToSpawn.Length; p++)
            {
                var item = pickupsToSpawn[p];
                var pGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pGO.name = $"ToolPickup_{item.type}_{p + 1}";
                pGO.transform.SetParent(toolPickupGroup);
                pGO.transform.position = item.pos;
                pGO.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

                var pCol = pGO.GetComponent<Collider>();
                if (pCol != null) pCol.isTrigger = true;

                var tp = pGO.AddComponent<ToolPickup>();
                tp.toolType = item.type;
                tp.refillAmount = 1;

                var mr = pGO.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.color = ToolInfo.GetThemeColor(item.type);
                    m.SetFloat("_Smoothness", 0.85f);
                    m.SetFloat("_Metallic", 0.4f);
                    mr.sharedMaterial = m;
                }

                // Decorative floating aura ring
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Aura_Ring";
                ring.transform.SetParent(pGO.transform, false);
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localScale = new Vector3(1.6f, 0.04f, 1.6f);
                var rCol = ring.GetComponent<Collider>();
                if (rCol != null) Object.DestroyImmediate(rCol);
                var rRend = ring.GetComponent<MeshRenderer>();
                if (rRend != null && mr != null) rRend.sharedMaterial = mr.sharedMaterial;
            }

            // ── 18.5 BAKE TRANSPARENT HUD ───────────────────────────────────────
            SundarbanGuardian.Core.HudController.BakeHUDToSceneMenu();

            // ── 19. SAVE SCENE & ASSETS ──────────────────────────────────────────
            EditorUtility.SetDirty(groundMat);
            EditorUtility.SetDirty(riverMat);
            EditorUtility.SetDirty(oceanMat);
            AssetDatabase.SaveAssets();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[BuildSundarbanWorldCorrected] Successfully rebuilt world with clear path corridor, satellite river delta, on-path hazards, exploration side-branches, and baked transparent HUD!");

            // Take top-down and game view screenshots
            CaptureScreenshots();
        }

        public static void CaptureScreenshots()
        {
            try
            {
                string dir = "ProjectScreenshots";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 1. Top-Down Satellite Comparison View
                GameObject topCamGO = new GameObject("Screenshot_TopDownCamera");
                Camera topCam = topCamGO.AddComponent<Camera>();
                topCam.orthographic = true;
                topCam.orthographicSize = 145f;
                topCam.nearClipPlane = 0.5f;
                topCam.farClipPlane = 800f;
                topCam.transform.position = new Vector3(-10f, 300f, 85f);
                topCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                RenderTexture rtTop = new RenderTexture(1920, 1080, 24);
                topCam.targetTexture = rtTop;
                topCam.Render();

                RenderTexture.active = rtTop;
                Texture2D texTop = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                texTop.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texTop.Apply();

                byte[] bytesTop = texTop.EncodeToPNG();
                string pathTop = Path.Combine(dir, "terrain_vs_satellite_topdown.png");
                File.WriteAllBytes(pathTop, bytesTop);

                RenderTexture.active = null;
                topCam.targetTexture = null;
                Object.DestroyImmediate(rtTop);
                Object.DestroyImmediate(texTop);
                Object.DestroyImmediate(topCamGO);

                Debug.Log($"[BuildSundarbanWorldCorrected] Saved top-down screenshot: {pathTop}");

                // 2. Player Perspective Game View
                // Positioned directly behind the Tiger at StartZone (-120, 30), framing the third-person view down the cleared visible corridor
                GameObject gameCamGO = new GameObject("Screenshot_GameViewCamera");
                Camera gameCam = gameCamGO.AddComponent<Camera>();
                gameCam.fieldOfView = 60f;
                gameCam.nearClipPlane = 0.3f;
                gameCam.farClipPlane = 1200f;

                var t = GameObject.Find("Tiger");
                if (t != null)
                {
                    Vector3 fwd = t.transform.forward;
                    gameCam.transform.position = t.transform.position - fwd * 3.8f + Vector3.up * 1.65f;
                    gameCam.transform.LookAt(t.transform.position + Vector3.up * 0.85f + fwd * 1.2f);
                }
                else
                {
                    gameCam.transform.position = new Vector3(-124f, 2.0f, 28f);
                    gameCam.transform.LookAt(new Vector3(-104f, 1.0f, 34f));
                }

                RenderTexture rtGame = new RenderTexture(1920, 1080, 24);
                gameCam.targetTexture = rtGame;
                gameCam.Render();

                RenderTexture.active = rtGame;
                Texture2D texGame = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                texGame.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texGame.Apply();

                byte[] bytesGame = texGame.EncodeToPNG();
                string pathGame = Path.Combine(dir, "game_view_player_perspective.png");
                File.WriteAllBytes(pathGame, bytesGame);

                RenderTexture.active = null;
                gameCam.targetTexture = null;
                Object.DestroyImmediate(rtGame);
                Object.DestroyImmediate(texGame);
                Object.DestroyImmediate(gameCamGO);

                Debug.Log($"[BuildSundarbanWorldCorrected] Saved game view screenshot: {pathGame}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BuildSundarbanWorldCorrected] Error capturing screenshots: {ex}");
            }
        }
    }
}
