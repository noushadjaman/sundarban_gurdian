using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Editor
{
    public static class BakeAuthenticSundarbanDecorations
    {
        [MenuItem("Sundarban/Bake Authentic Scale & High-Distance Decorations")]
        public static void BakeAllDecorations()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/SampleScene.unity")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            Debug.Log("<color=cyan>[BakeDecorations] Starting authentic decoration scaling and high-distance distribution...</color>");

            // 1. Process Mangrove Environment Trees (Clear paths, space out at high distance)
            BakeTreesScaleAndDistribution();

            // 2. Process Mangrove Environment Undergrowth (Enlarge to character match, space widely)
            BakeUndergrowthScaleAndDistribution();

            // 3. Process Coastal Beach Decorations (Clear river channel, enlarge dune grasses)
            BakeCoastalBeachDecorations();

            // 4. Process Ambient Wildlife Decorations (Character-proportional authentic scales)
            BakeWildlifeScaleAndDistribution();

            // 5. Setup Initial Player Start Positions at North facing South (Ocean)
            SetupNorthToOceanAnimalStarts();

            // 6. Save Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[BakeDecorations] COMPLETED! All decorations are naturally sized matching characters, widely spaced at high distance, and completely cleared from player paths!</color>");
        }

        public static void BakeTreesScaleAndDistribution()
        {
            var env = GameObject.Find("Mangrove_Environment_Decorations");
            if (env == null)
            {
                Debug.LogWarning("[BakeDecorations] Mangrove_Environment_Decorations not found in scene!");
                return;
            }

            var trees = env.transform.Find("Trees");
            if (trees == null) return;

            // Player central migration corridors to keep 100% CLEAR (North to South, Z: 265 -> -110):
            // West trail (Tiger & Deer main route): X: -43.0 to -27.0
            // River channel (Dolphin & Hilsha migration): X: -7.0 to 15.0
            // East trail: X: 22.0 to 38.0
            const float TrailClearanceWestMin = -43.0f;
            const float TrailClearanceWestMax = -27.0f;
            const float TrailClearanceRiverMin = -7.0f;
            const float TrailClearanceRiverMax = 15.0f;
            const float TrailClearanceEastMin =  22.0f;
            const float TrailClearanceEastMax =  38.0f;

            // Playable bounds
            const float PlayMinZ = -115.0f;
            const float PlayMaxZ =  270.0f;
            const float PlayMaxAbsX = 85.0f;

            // High-distance tree spacing: minimum 16 meters between active tree clusters
            const float MinTreeDistance = 16.0f;
            List<Vector2> keptPositions = new List<Vector2>();

            int deactivatedInPath = 0;
            int deactivatedOutOfMap = 0;
            int deactivatedForSpacing = 0;
            int keptTrees = 0;

            for (int i = 0; i < trees.childCount; i++)
            {
                var treeChunk = trees.GetChild(i);
                if (treeChunk.childCount == 0) continue;

                var rends = treeChunk.GetComponentsInChildren<MeshRenderer>();
                if (rends.Length == 0) continue;

                Vector3 center = rends[0].bounds.center;

                // 1. Check if outside playable boundaries
                if (center.z < PlayMinZ || center.z > PlayMaxZ || Mathf.Abs(center.x) > PlayMaxAbsX)
                {
                    treeChunk.gameObject.SetActive(false);
                    deactivatedOutOfMap++;
                    continue;
                }

                // 2. Clear corridors: NEVER block player paths or river
                bool inWestTrail  = (center.x >= TrailClearanceWestMin  && center.x <= TrailClearanceWestMax);
                bool inRiverTrail = (center.x >= TrailClearanceRiverMin && center.x <= TrailClearanceRiverMax);
                bool inEastTrail  = (center.x >= TrailClearanceEastMin  && center.x <= TrailClearanceEastMax);

                if (inWestTrail || inRiverTrail || inEastTrail)
                {
                    treeChunk.gameObject.SetActive(false);
                    deactivatedInPath++;
                    continue;
                }

                // 3. Enforce high-distance distribution
                Vector2 c2D = new Vector2(center.x, center.z);
                bool tooClose = false;
                foreach (var kp in keptPositions)
                {
                    if (Vector2.Distance(c2D, kp) < MinTreeDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    treeChunk.gameObject.SetActive(false);
                    deactivatedForSpacing++;
                    continue;
                }

                // Keep this majestic mangrove tree
                keptPositions.Add(c2D);
                treeChunk.gameObject.SetActive(true);

                // Ensure authentic grand mangrove scale (~12m - 15m canopy height)
                for (int j = 0; j < treeChunk.childCount; j++)
                {
                    var sub = treeChunk.GetChild(j);
                    sub.localScale = Vector3.one * 1.10f;
                }

                keptTrees++;
            }

            Debug.Log($"<color=orange>[BakeDecorations] Trees: Kept {keptTrees} scenic trees at high distance (>16m apart). Cleared {deactivatedInPath} path-blocking trees, {deactivatedForSpacing} dense trees, and {deactivatedOutOfMap} out-of-bounds trees.</color>");
        }

        public static void BakeUndergrowthScaleAndDistribution()
        {
            var env = GameObject.Find("Mangrove_Environment_Decorations");
            if (env == null)
            {
                Debug.LogWarning("[BakeDecorations] Mangrove_Environment_Decorations not found in scene!");
                return;
            }

            var under = env.transform.Find("Undergrowth");
            if (under == null) return;

            // Player central migration corridors to keep CLEAR (North to South, Z: 265 -> -110):
            const float TrailClearanceWestMin = -43.0f;
            const float TrailClearanceWestMax = -27.0f;
            const float TrailClearanceRiverMin = -7.0f;
            const float TrailClearanceRiverMax =  15.0f;
            const float TrailClearanceEastMin =   22.0f;
            const float TrailClearanceEastMax =   38.0f;

            // Playable bounds
            const float PlayMinZ = -115.0f;
            const float PlayMaxZ =  270.0f;
            const float PlayMaxAbsX = 85.0f;

            // High-distance spacing between active undergrowth clumps: minimum 22 meters
            const float MinClusterDistance = 22.0f;
            List<Vector2> keptPositions = new List<Vector2>();

            int deactivatedInPath = 0;
            int deactivatedOutOfMap = 0;
            int deactivatedForSpacing = 0;
            int keptAndScaled = 0;

            for (int i = 0; i < under.childCount; i++)
            {
                var chunk = under.GetChild(i);
                if (chunk.childCount == 0) continue;

                var mrs = chunk.GetComponentsInChildren<MeshRenderer>();
                if (mrs.Length == 0) continue;

                Vector3 center = mrs[0].bounds.center;

                // 1. Check if outside playable boundaries
                if (center.z < PlayMinZ || center.z > PlayMaxZ || Mathf.Abs(center.x) > PlayMaxAbsX)
                {
                    chunk.gameObject.SetActive(false);
                    deactivatedOutOfMap++;
                    continue;
                }

                // 2. Check if within character walking corridor (NEVER come directly in front of character)
                bool inWestTrail  = (center.x >= TrailClearanceWestMin  && center.x <= TrailClearanceWestMax);
                bool inRiverTrail = (center.x >= TrailClearanceRiverMin && center.x <= TrailClearanceRiverMax);
                bool inEastTrail  = (center.x >= TrailClearanceEastMin  && center.x <= TrailClearanceEastMax);

                if (inWestTrail || inRiverTrail || inEastTrail)
                {
                    chunk.gameObject.SetActive(false);
                    deactivatedInPath++;
                    continue;
                }

                // 3. Enforce high-distance distribution
                Vector2 c2D = new Vector2(center.x, center.z);
                bool tooClose = false;
                foreach (var kp in keptPositions)
                {
                    if (Vector2.Distance(c2D, kp) < MinClusterDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    chunk.gameObject.SetActive(false);
                    deactivatedForSpacing++;
                    continue;
                }

                // Keep this chunk!
                keptPositions.Add(c2D);
                chunk.gameObject.SetActive(true);

                // 4. Scale up subchildren to authentic, prominent sizes matching characters
                for (int j = 0; j < chunk.childCount; j++)
                {
                    var sub = chunk.GetChild(j);
                    string sName = sub.name.ToLower();

                    if (sName.Contains("fern"))
                    {
                        // Mangrove Acrostichum ferns: ~1.6m tall, lush, reaching deer chest height
                        sub.localScale = new Vector3(4.5f, 4.5f, 4.5f);
                    }
                    else if (sName.Contains("pneu"))
                    {
                        // Pneumatophore breathing root clusters: ~0.9m tall rising from mud
                        sub.localScale = new Vector3(6.5f, 6.5f, 6.5f);
                    }
                    else if (sName.Contains("log"))
                    {
                        // Fallen mangrove logs: ~1.2m diameter, substantial ancient timber
                        sub.localScale = new Vector3(4.5f, 4.5f, 4.5f);
                    }
                    else if (sName.Contains("seed") || sName.Contains("sap"))
                    {
                        // Mangrove seedlings: ~1.4m tall
                        sub.localScale = new Vector3(3.8f, 3.8f, 3.8f);
                    }
                    else if (sName.Contains("grass"))
                    {
                        // Tidal marsh grass: ~1.2m tall
                        sub.localScale = new Vector3(3.5f, 3.5f, 3.5f);
                    }
                    else
                    {
                        sub.localScale = new Vector3(3.8f, 3.8f, 3.8f);
                    }
                }

                keptAndScaled++;
            }

            Debug.Log($"<color=orange>[BakeDecorations] Undergrowth: Kept {keptAndScaled} scenic clusters at high distance (>22m apart). Cleared {deactivatedInPath} path-blocking chunks, {deactivatedForSpacing} dense chunks, and {deactivatedOutOfMap} out-of-map chunks.</color>");
        }

        public static void BakeCoastalBeachDecorations()
        {
            var beach = GameObject.Find("Coastal_Beach_Decorations");
            if (beach == null)
            {
                var parent = GameObject.Find("Sea_Beach_Shorelines");
                if (parent != null)
                {
                    var t = parent.transform.Find("Coastal_Beach_Decorations");
                    if (t != null) beach = t.gameObject;
                }
            }

            if (beach == null) return;

            const float RiverMinX = -7.0f;
            const float RiverMaxX = 15.0f;
            const float TrailClearanceWestMin = -43.0f;
            const float TrailClearanceWestMax = -27.0f;
            const float MinBeachItemDistance = 22.0f;

            List<Vector2> keptPositions = new List<Vector2>();
            int removedFromRiverOrTrail = 0;
            int removedForSpacing = 0;
            int keptCount = 0;

            for (int i = 0; i < beach.transform.childCount; i++)
            {
                var item = beach.transform.GetChild(i);
                Vector3 pos = item.position;

                // 1. Remove if planted inside river channel or main player trail
                if ((pos.x >= RiverMinX && pos.x <= RiverMaxX) ||
                    (pos.x >= TrailClearanceWestMin && pos.x <= TrailClearanceWestMax))
                {
                    item.gameObject.SetActive(false);
                    removedFromRiverOrTrail++;
                    continue;
                }

                // 2. High-distance distribution along coastal dunes
                Vector2 p2D = new Vector2(pos.x, pos.z);
                bool tooClose = false;
                foreach (var kp in keptPositions)
                {
                    if (Vector2.Distance(p2D, kp) < MinBeachItemDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    item.gameObject.SetActive(false);
                    removedForSpacing++;
                    continue;
                }

                // 3. Keep item and fix sizes matching characters
                keptPositions.Add(p2D);
                item.gameObject.SetActive(true);

                string iName = item.name.ToLower();
                if (iName.Contains("grass"))
                {
                    // Scale dune grass up from puny 0.45m to natural 1.2m height
                    item.localScale = Vector3.one * 1.85f;
                }
                else if (iName.Contains("log") || iName.Contains("driftwood"))
                {
                    item.localScale = Vector3.one * 1.60f;
                }
                else if (iName.Contains("palm"))
                {
                    item.localScale = Vector3.one * 1.15f;
                }

                keptCount++;
            }

            Debug.Log($"<color=cyan>[BakeDecorations] Beach: Kept {keptCount} coastal decorations at high distance (>22m apart). Cleared {removedFromRiverOrTrail} river/trail blockers and {removedForSpacing} dense items.</color>");
        }

        public static void BakeWildlifeScaleAndDistribution()
        {
            const string WildlifeRootName = "[Sundarban_Wildlife_Decorations]";
            var oldRoot = GameObject.Find(WildlifeRootName);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            var rootGo = new GameObject(WildlifeRootName);
            rootGo.transform.position = Vector3.zero;

            // Load Materials
            var birdBodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_Bird_Body.mat");
            var cobraMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_Cobra.mat");
            var crocMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wildlife_Crocodile.mat");
            var deerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Authentic_Deer.mat");

            // Load Models
            var birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Bird by Poly by Google - 8Ph79kHbt9s/Bird_01(1).obj");
            var cobraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Cobra by Poly by Google - 40_5Xq467-U/cobra.obj");
            var crocPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Crocodile by Poly by Google - 2an6E2WjW3z/Mesh_Crocodile.obj");
            var deerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/animal/Deer by Poly by Google - fUo4AIcd8XR/Mesh_Deer.obj");

            // Authentic character-proportional scales:
            // Tiger is ~2.5m length; Player Deer is ~2.3m height; Dolphin is ~2.0m length.
            const float BirdScale  = 0.0095f; // ~2.1m wingspan Brahminy kite / White-bellied sea eagle (clearly visible in trees!)
            const float CobraScale = 0.080f;  // ~1.3m coiled width king cobra (realistic, not oversized)
            const float CrocScale  = 0.019f;  // ~4.8m apex saltwater crocodile (perfect match for 2.5m tiger & 2.0m dolphin)
            const float DeerScale  = 0.015f;  // ~2.0m height (perfectly matches player Chitra spotted deer)

            // ── 1. MANGROVE BIRDS (Perched high on canopy branches, spaced 45-55m apart) ──
            var birdsGroup = new GameObject("Birds_Mangrove_Perches").transform;
            birdsGroup.SetParent(rootGo.transform, false);
            var birdSpawns = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-55f, 6.2f,  235f),  135f),
                (new Vector3( 48f, 6.5f,  190f),  -60f),
                (new Vector3(-54f, 6.0f,  145f),   45f),
                (new Vector3( 46f, 6.4f,  100f),  190f),
                (new Vector3(-52f, 6.1f,   55f),  -40f),
                (new Vector3( 45f, 6.3f,   10f),  120f),
                (new Vector3(-55f, 5.8f,  -40f),  -85f),
                (new Vector3( 42f, 6.2f,  -80f),   30f)
            };
            for (int i = 0; i < birdSpawns.Length; i++)
            {
                var bp = birdSpawns[i];
                var birdGo = CreateWildlifeInstance(birdPrefab, $"Mangrove_Bird_{i + 1:D2}", birdsGroup, bp.pos, bp.yaw, BirdScale);
                ApplyMaterial(birdGo, birdBodyMat);
                var idle = birdGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.Bird;
                idle.animSpeed = Random.Range(0.85f, 1.25f);
            }

            // ── 2. SALTWATER CROCODILES (Placed on side mudbanks, leaving river channel open) ──
            var riverCrocsGroup = new GameObject("River_Crocodiles_Swimming").transform;
            riverCrocsGroup.SetParent(rootGo.transform, false);
            var crocSpawns = new (Vector3 pos, float yaw, bool swimming)[]
            {
                (new Vector3( 17.5f, 0.25f,  215f), -80f, false), // East mudbank
                (new Vector3(-16.0f, 0.22f,  130f),  80f, false), // West mudbank
                (new Vector3( 18.5f, 0.20f,   25f), -85f, false), // East mudbank
                (new Vector3(-16.5f, 0.18f,  -65f),  70f, false)  // West mudbank
            };
            for (int i = 0; i < crocSpawns.Length; i++)
            {
                var cp = crocSpawns[i];
                Vector3 p = SnapGround(cp.pos, 0.05f);
                var crocGo = CreateWildlifeInstance(crocPrefab, $"Saltwater_Crocodile_{i + 1:D2}", riverCrocsGroup, p, cp.yaw, CrocScale);
                ApplyMaterial(crocGo, crocMat);
                var idle = crocGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.Crocodile;
                idle.isSwimmingInRiver = false;
                idle.animSpeed = Random.Range(0.75f, 1.15f);

                var col = crocGo.GetComponent<SphereCollider>();
                if (col == null) col = crocGo.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 60f;
            }

            // ── 3. WILD SPOTTED DEER (Grazing in distant meadows off the main trail, spaced 50m apart) ──
            var deerGroup = new GameObject("WildDeer_Meadows_Grazing").transform;
            deerGroup.SetParent(rootGo.transform, false);
            var deerSpawns = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-60f, 0f,  225f),   70f),
                (new Vector3( 58f, 0f,  165f),  -80f),
                (new Vector3(-58f, 0f,  105f),   50f),
                (new Vector3( 55f, 0f,   45f),  -70f),
                (new Vector3(-56f, 0f,  -35f),   60f),
                (new Vector3( 52f, 0f,  -70f),  -90f)
            };
            for (int i = 0; i < deerSpawns.Length; i++)
            {
                var dp = deerSpawns[i];
                Vector3 p = SnapGround(dp.pos, 0.05f);
                var wildDeerGo = CreateWildlifeInstance(deerPrefab, $"Wild_SpottedDeer_{i + 1:D2}", deerGroup, p, dp.yaw, DeerScale);
                ApplyMaterial(wildDeerGo, deerMat);
                var idle = wildDeerGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.WildDeer;
                idle.animSpeed = Random.Range(0.85f, 1.15f);
            }

            // ── 4. KING COBRAS (Coiled on shaded periphery mounds, 50-60m apart) ──
            var cobrasGroup = new GameObject("Cobras_Mangrove_Thickets").transform;
            cobrasGroup.SetParent(rootGo.transform, false);
            var cobraSpawns = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-52f, 0f,  205f),   50f),
                (new Vector3( 50f, 0f,  140f), -110f),
                (new Vector3(-50f, 0f,   80f),  130f),
                (new Vector3( 48f, 0f,   20f),  -60f),
                (new Vector3(-52f, 0f,  -60f),   75f)
            };
            for (int i = 0; i < cobraSpawns.Length; i++)
            {
                var cp = cobraSpawns[i];
                Vector3 p = SnapGround(cp.pos, 0.04f);
                var cobraGo = CreateWildlifeInstance(cobraPrefab, $"King_Cobra_{i + 1:D2}", cobrasGroup, p, cp.yaw, CobraScale);
                ApplyMaterial(cobraGo, cobraMat);
                var idle = cobraGo.AddComponent<AmbientWildlifeIdle>();
                idle.wildlifeType = WildlifeType.Cobra;
                idle.animSpeed = Random.Range(0.8f, 1.2f);
            }

            Debug.Log($"<color=green>[BakeDecorations] Ambient Wildlife: Built {birdSpawns.Length} Birds, {crocSpawns.Length} Crocodiles, {deerSpawns.Length} Deer, and {cobraSpawns.Length} Cobras at authentic character-matching scales along forest margins!</color>");
        }

        public static void SetupNorthToOceanAnimalStarts()
        {
            var tiger = GameObject.Find("Tiger");
            var deer = GameObject.Find("Deer");
            var dolphin = GameObject.Find("Dolphin");
            var hilsha = GameObject.Find("Hilsha");

            if (tiger != null)
            {
                Vector3 p = SnapGround(new Vector3(-45f, 25f, 250f), 0.05f);
                tiger.transform.position = p;
                tiger.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                var cc = tiger.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
                var h = tiger.GetComponent<SpeciesHealth>();
                if (h != null) h.ResetHealth();
                EditorUtility.SetDirty(tiger);
            }

            if (deer != null)
            {
                Vector3 p = SnapGround(new Vector3(-35f, 25f, 245f), 0.05f);
                deer.transform.position = p;
                deer.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                var cc = deer.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
                var h = deer.GetComponent<SpeciesHealth>();
                if (h != null) h.ResetHealth();
                EditorUtility.SetDirty(deer);
            }

            if (dolphin != null)
            {
                dolphin.transform.position = new Vector3(8f, -0.40f, 255f);
                dolphin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EditorUtility.SetDirty(dolphin);
            }

            if (hilsha != null)
            {
                hilsha.transform.position = new Vector3(8f, -0.50f, 250f);
                hilsha.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EditorUtility.SetDirty(hilsha);
            }

            var gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                string sp = !string.IsNullOrEmpty(GameManager.selectedSpecies) ? GameManager.selectedSpecies : gm.currentSpecies;
                GameObject activePlayer = tiger != null ? tiger : deer; // Tiger default for Level 1
                if (sp.ToLower().Contains("deer")) activePlayer = deer;
                else if (sp.ToLower().Contains("dolphin")) activePlayer = dolphin;
                else if (sp.ToLower().Contains("hilsha")) activePlayer = hilsha;

                gm.playerObject = activePlayer;
                gm.activeHealth = activePlayer != null ? activePlayer.GetComponent<SpeciesHealth>() : null;

                if (tiger != null) tiger.SetActive(tiger == activePlayer);
                if (deer != null) deer.SetActive(deer == activePlayer);
                if (dolphin != null) dolphin.SetActive(dolphin == activePlayer);
                if (hilsha != null) hilsha.SetActive(hilsha == activePlayer);

                var cam = Camera.main;
                if (cam != null && activePlayer != null)
                {
                    var cf = cam.GetComponent<SundarbanGuardian.Player.CameraFollow>();
                    if (cf != null)
                    {
                        cf.target = activePlayer.transform;
                        cf.currentYaw = 180f;
                        cf.currentPitch = 12f;
                        cf.defaultDistance = 4.2f;
                        EditorUtility.SetDirty(cf);
                    }
                    cam.transform.position = activePlayer.transform.position + new Vector3(0f, 2.2f, 4.2f);
                    cam.transform.rotation = Quaternion.Euler(14f, 180f, 0f);
                    EditorUtility.SetDirty(cam);
                }

                EditorUtility.SetDirty(gm);
            }

            Debug.Log("<color=cyan>[BakeDecorations] Player start positions set at North Ridge facing South towards Ocean!</color>");
        }

        private static GameObject CreateWildlifeInstance(GameObject prefab, string name, Transform parent, Vector3 pos, float yaw, float scale)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }

            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
            }
        }

        private static Vector3 SnapGround(Vector3 pos, float yOffset)
        {
            Ray ray = new Ray(new Vector3(pos.x, 35f, pos.z), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * yOffset;
            }
            return new Vector3(pos.x, 0.45f + yOffset, pos.z);
        }
    }
}
