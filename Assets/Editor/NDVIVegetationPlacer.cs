using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Editor
{
    public static class NDVIVegetationPlacer
    {
        class ZoneRecord
        {
            public Vector3 center;
            public float radius;
            public string risk;
        }

        [MenuItem("Sundarban/Apply NDVI-Reflective Vegetation")]
        public static void ApplyNDVIVegetation()
        {
            Debug.Log("[NDVIVegetationPlacer] Rebuilding vegetation based on real zone risk tiers...");

            // ── Assets ───────────────────────────────────────────────────────
            var normalTreesFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/Normal Trees.fbx");
            var redMangrove = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/RedMangrove/RedMangrove_Tree.obj");
            var coconutPalm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/CoconutPalmTree.obj");
            var evergladesPalm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/EvergladesPalmTree.obj");
            var grassFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/Grass.fbx");
            var debrisFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/MangroveRoots/source/230501_Beach Debris 4.fbx");

            var trees = new List<GameObject>();
            if (normalTreesFbx != null)
                for (int i = 0; i < normalTreesFbx.transform.childCount; i++)
                {
                    var c = normalTreesFbx.transform.GetChild(i).gameObject;
                    if (c.GetComponent<MeshFilter>() != null) trees.Add(c);
                }

            var grasses = new List<GameObject>();
            if (grassFbx != null)
                for (int i = 0; i < grassFbx.transform.childCount; i++)
                {
                    var c = grassFbx.transform.GetChild(i).gameObject;
                    if (c.GetComponent<MeshFilter>() != null) grasses.Add(c);
                }

            // ── Hazard zones ──────────────────────────────────────────────────
            var zones = new List<ZoneRecord>();
            foreach (var t in Object.FindObjectsByType<HazardZoneTrigger>())
            {
                zones.Add(new ZoneRecord
                {
                    center = t.transform.position,
                    radius = t.transform.localScale.x * 0.5f,
                    risk   = t.riskTier
                });
            }

            // ── Forest parent ─────────────────────────────────────────────────
            var forestParent = GameObject.Find("Mangrove_Forest");
            if (forestParent == null)
                forestParent = new GameObject("Mangrove_Forest");

            string[] toClear = { "Safe_Dense_Canopy", "Medium_Sparse_Recovery",
                                  "High_Degraded_Wasteland", "Undergrowth_Flora",
                                  "Safe_Sanctuaries", "Forest_Trails",
                                  "Danger_Mudflats", "Undergrowth_Flora" };
            foreach (var n in toClear)
            {
                var t = forestParent.transform.Find(n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            var safeGrp   = new GameObject("Safe_Dense_Canopy").transform;
            var medGrp    = new GameObject("Medium_Sparse_Recovery").transform;
            var highGrp   = new GameObject("High_Degraded_Wasteland").transform;
            var floraGrp  = new GameObject("Undergrowth_Flora").transform;
            safeGrp.SetParent(forestParent.transform);
            medGrp.SetParent(forestParent.transform);
            highGrp.SetParent(forestParent.transform);
            floraGrp.SetParent(forestParent.transform);

            // ── Helpers ───────────────────────────────────────────────────────
            Random.InitState(42);
            const float minSpacingLow  = 2.2f;
            const float minSpacingMed  = 4.0f;
            const float minSpacingHigh = 6.0f;
            var placed = new List<Vector3>();

            bool TooClose(Vector3 p, float minDist)
            {
                foreach (var q in placed)
                    if (new Vector2(p.x - q.x, p.z - q.z).magnitude < minDist)
                        return true;
                return false;
            }

            string GetRisk(Vector3 p)
            {
                string worst = "low";
                foreach (var z in zones)
                {
                    float d = new Vector2(p.x - z.center.x, p.z - z.center.z).magnitude;
                    if (d <= z.radius)
                    {
                        if (z.risk == "high") return "high";
                        if (z.risk == "medium") worst = "medium";
                    }
                }
                return worst;
            }

            // ── Scatter ───────────────────────────────────────────────────────
            int safeN = 0, medN = 0, highN = 0;

            for (float gx = -102f; gx <= 16f; gx += 6f)
            {
                for (float gz = -68f; gz <= 168f; gz += 6f)
                {
                    bool planted = false;
                    for (int attempt = 0; attempt < 5 && !planted; attempt++)
                    {
                        float px = gx + Random.Range(-2.8f, 2.8f);
                        float pz = gz + Random.Range(-2.8f, 2.8f);
                        var pos3 = new Vector3(px, 0f, pz);

                        if (pos3.magnitude < 9f) continue;
                        if (px > 17.5f) continue;

                        string risk = GetRisk(pos3);

                        float densityChance = (risk == "low") ? 0.80f
                                            : (risk == "medium") ? 0.32f
                                            : 0.06f;
                        if (Random.value > densityChance) continue;

                        float spacing = (risk == "low") ? minSpacingLow
                                      : (risk == "medium") ? minSpacingMed
                                      : minSpacingHigh;
                        if (TooClose(pos3, spacing)) continue;

                        placed.Add(pos3);
                        planted = true;

                        float ry = Random.Range(0f, 360f);

                        if (risk == "low")
                        {
                            // Zone 1: Full-height dense canopy
                            if (trees.Count > 0)
                            {
                                var prefab = trees[Random.Range(0, trees.Count)];
                                var go = (GameObject)Object.Instantiate(prefab, pos3, Quaternion.Euler(0, ry, 0), safeGrp);
                                float sc = Random.Range(1.05f, 1.35f);
                                go.transform.localScale = Vector3.one * sc;
                                safeN++;
                            }
                            // Rich grass
                            if (grasses.Count > 0 && Random.value > 0.28f)
                            {
                                int clumps = Random.Range(1, 3);
                                for (int ci = 0; ci < clumps; ci++)
                                {
                                    var gp = grasses[Random.Range(0, grasses.Count)];
                                    var gpos = pos3 + new Vector3(Random.Range(-1.8f, 1.8f), 0, Random.Range(-1.8f, 1.8f));
                                    var gr = (GameObject)Object.Instantiate(gp, gpos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), floraGrp);
                                    float gs = Random.Range(1.6f, 2.8f);
                                    gr.transform.localScale = Vector3.one * gs;
                                }
                            }
                        }
                        else if (risk == "medium")
                        {
                            // Zone 2: Mixed sparse – struggling vs healthy
                            if (trees.Count > 0)
                            {
                                var prefab = trees[Random.Range(0, trees.Count)];
                                var go = (GameObject)Object.Instantiate(prefab, pos3, Quaternion.Euler(0, ry, 0), medGrp);
                                bool struggling = Random.value > 0.48f;
                                float sc = struggling ? Random.Range(0.50f, 0.70f) : Random.Range(0.80f, 1.0f);
                                go.transform.localScale = Vector3.one * sc;
                                medN++;
                            }
                            // Patchy sparse grass
                            if (grasses.Count > 0 && Random.value > 0.58f)
                            {
                                var gp = grasses[Random.Range(0, grasses.Count)];
                                var gr = (GameObject)Object.Instantiate(gp,
                                    pos3 + new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f)),
                                    Quaternion.Euler(0, Random.Range(0f, 360f), 0), floraGrp);
                                float gs = Random.Range(0.8f, 1.4f);
                                gr.transform.localScale = Vector3.one * gs;
                            }
                            // Fallen/decaying debris
                            if (debrisFbx != null && Random.value > 0.78f)
                            {
                                var deb = (GameObject)Object.Instantiate(debrisFbx,
                                    pos3 + new Vector3(Random.Range(-0.5f, 0.5f), 0.05f, Random.Range(-0.5f, 0.5f)),
                                    Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 360f), Random.Range(-5f, 5f)),
                                    medGrp);
                                float ds = Random.Range(1.1f, 1.9f);
                                deb.transform.localScale = Vector3.one * ds;
                            }
                        }
                        else // high
                        {
                            // Zone 3: Nearly bare – rare stunted tree (12% chance)
                            if (trees.Count > 0 && Random.value > 0.88f)
                            {
                                var prefab = trees[Random.Range(0, trees.Count)];
                                var go = (GameObject)Object.Instantiate(prefab, pos3,
                                    Quaternion.Euler(0, ry, Random.Range(-10f, 10f)), highGrp);
                                float sc = Random.Range(0.25f, 0.42f); // stunted
                                go.transform.localScale = Vector3.one * sc;

                                // Desaturate material – drought-stressed brown tint
                                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                                {
                                    var mats = mr.materials;
                                    for (int mi = 0; mi < mats.Length; mi++)
                                    {
                                        var c = mats[mi].color;
                                        mats[mi].color = new Color(c.r * 0.85f, c.g * 0.68f, c.b * 0.52f, c.a);
                                    }
                                    mr.materials = mats;
                                }
                                highN++;
                            }
                            // Exposed dead root debris (open mudflat scatter)
                            if (debrisFbx != null && Random.value > 0.62f)
                            {
                                var deb = (GameObject)Object.Instantiate(debrisFbx, pos3,
                                    Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f)),
                                    highGrp);
                                float ds = Random.Range(0.9f, 1.6f);
                                deb.transform.localScale = Vector3.one * ds;
                            }
                        }
                    }
                }
            }

            // ── Save ──────────────────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[NDVIVegetationPlacer] Done! Safe(dense):{safeN}  Medium(sparse):{medN}  High(degraded):{highN}  Zones used:{zones.Count}");
        }
    }
}
