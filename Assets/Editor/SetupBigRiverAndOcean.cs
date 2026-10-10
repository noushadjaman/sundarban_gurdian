using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace SundarbanGuardian.Editor
{
    public static class SetupBigRiverAndOcean
    {
        public const float WaterY = 0.04f;
        public const float RiverBedDepth = -2.10f;

        [MenuItem("Sundarban/Setup Big River and Ocean Waterway")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log("[SetupBigRiverAndOcean] Beginning big river & ocean waterway integration...");

            // 1. Activate BayOfBengal_Ocean_System
            ActivateOceanSystem();

            // 2. Ensure Main_River_Shibsa is active and positioned properly
            EnsureMainRiverShibsa();

            // 3. Adjust coastal chars to open a wide deep estuary channel
            OpenEstuaryShippingChannel();

            // 4. Carve river & estuary channel through Mangrove_Environment_Decorations terrain (Z: 285 down to -220)
            CarveRiverThroughGLBTerrain();

            // 5. Remove / disable any trees and undergrowth in the river channel
            ClearRiverFlora();

            // 6. Move any collectible tools or stray colliders out of the river channel onto the riverbank
            ClearStrayRiverColliders();

            // 7. Connect River Water with Ocean Water seamlessly
            EnsureWaterConnection();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[SetupBigRiverAndOcean] Big river and ocean successfully connected and sculpted!");
        }

        private static void ActivateOceanSystem()
        {
            GameObject oceanSys = null;
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r.name == "BayOfBengal_Ocean_System")
                {
                    oceanSys = r;
                    break;
                }
            }

            if (oceanSys != null)
            {
                oceanSys.SetActive(true);
                for (int i = 0; i < oceanSys.transform.childCount; i++)
                {
                    oceanSys.transform.GetChild(i).gameObject.SetActive(true);
                }
                Debug.Log("[SetupBigRiverAndOcean] BayOfBengal_Ocean_System activated!");
            }
            else
            {
                Debug.LogWarning("[SetupBigRiverAndOcean] BayOfBengal_Ocean_System not found!");
            }
        }

        private static void EnsureMainRiverShibsa()
        {
            GameObject river = null;
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r.name == "Main_River_Shibsa")
                {
                    river = r;
                    break;
                }
            }

            if (river != null)
            {
                river.SetActive(true);
                for (int i = 0; i < river.transform.childCount; i++)
                {
                    river.transform.GetChild(i).gameObject.SetActive(true);
                }
                Debug.Log("[SetupBigRiverAndOcean] Main_River_Shibsa activated!");
            }
            else
            {
                Debug.LogWarning("[SetupBigRiverAndOcean] Main_River_Shibsa not found!");
            }
        }

        private static void OpenEstuaryShippingChannel()
        {
            // Position coastal barrier islands (chars) to frame the river mouth on East and West sides,
            // leaving a massive 60m-wide deep central waterway from river into the Bay of Bengal.
            var charsGroup = GameObject.Find("Coastal_Sandbar_Chars");
            if (charsGroup != null)
            {
                var char2 = charsGroup.transform.Find("Coastal_Char_2");
                if (char2 != null)
                {
                    // Shift East barrier char further East (X = 48)
                    char2.position = new Vector3(38f, 0f, 0f);
                }

                var char4 = charsGroup.transform.Find("Coastal_Char_4");
                if (char4 != null)
                {
                    // Shift West barrier char further West (X = -45)
                    char4.position = new Vector3(-35f, 0f, 0f);
                }
                Debug.Log("[SetupBigRiverAndOcean] Adjusted coastal sandbars to open 60m central estuary channel!");
            }
        }

        public static float GetEstuaryCenterX(float z)
        {
            if (z >= -105f)
            {
                return BuildSundarbanFullMap.RiverCenterX(z);
            }
            else
            {
                // Smoothly blend from river mouth at Z=-105 (rx=6.2) to ocean center (X=10)
                float t = Mathf.Clamp01((-105f - z) / 60f);
                return Mathf.Lerp(BuildSundarbanFullMap.RiverCenterX(-105f), 10f, t);
            }
        }

        public static float GetEstuaryHalfWidth(float z)
        {
            if (z >= -40f)
            {
                return BuildSundarbanFullMap.RiverHalfWidth(z);
            }
            else if (z >= -105f)
            {
                // Estuary flare approaching the sea
                float t = Mathf.InverseLerp(-40f, -105f, z);
                return Mathf.Lerp(BuildSundarbanFullMap.RiverHalfWidth(-40f), 26f, t);
            }
            else
            {
                // Grand funnel opening into Bay of Bengal
                float t = Mathf.Clamp01((-105f - z) / 80f);
                return Mathf.Lerp(26f, 65f, t);
            }
        }

        private static void CarveRiverThroughGLBTerrain()
        {
            var env = GameObject.Find("Mangrove_Environment_Decorations");
            if (env == null)
            {
                Debug.LogWarning("[SetupBigRiverAndOcean] Mangrove_Environment_Decorations not found!");
                return;
            }

            var terrainGroup = env.transform.Find("Terrain");
            if (terrainGroup == null) return;

            int carvedTiles = 0;
            int carvedVertsTotal = 0;

            for (int i = 0; i < terrainGroup.childCount; i++)
            {
                var tile = terrainGroup.GetChild(i);
                var mf = tile.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                var mr = tile.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                var b = mr.bounds;
                // Check if tile bounding box overlaps the river X range [-90, 90] and Z range [-220, 290]
                if (b.max.x < -90f || b.min.x > 90f || b.max.z < -220f || b.min.z > 290f) continue;

                Mesh mesh = Object.Instantiate(mf.sharedMesh);
                mesh.name = mf.sharedMesh.name + "_RiverCarved";
                Vector3[] verts = mesh.vertices;
                bool modified = false;

                for (int vi = 0; vi < verts.Length; vi++)
                {
                    Vector3 worldPos = tile.TransformPoint(verts[vi]);
                    float z = worldPos.z;

                    if (z < -220f || z > 290f) continue;

                    // Compute river/estuary center and width at this Z
                    float rx = GetEstuaryCenterX(z);
                    float rhw = GetEstuaryHalfWidth(z);
                    float dist = Mathf.Abs(worldPos.x - rx);

                    // Deep water channel
                    if (dist <= rhw)
                    {
                        float u = dist / Mathf.Max(rhw, 1f);
                        // Parabolic riverbed trough: deep center (-2.2m), rising to -1.5m at edge
                        float bedY = Mathf.Lerp(RiverBedDepth - 0.35f, -1.50f, u * u);
                        worldPos.y = bedY;
                        verts[vi] = tile.InverseTransformPoint(worldPos);
                        modified = true;
                        carvedVertsTotal++;
                    }
                    // Sloping mud riverbank
                    else if (dist <= rhw + 9.0f)
                    {
                        float tBank = (dist - rhw) / 9.0f;
                        float smoothT = Mathf.SmoothStep(0f, 1f, tBank);
                        float bankTargetY = Mathf.Max(0.20f, worldPos.y);
                        worldPos.y = Mathf.Lerp(-1.50f, bankTargetY, smoothT);
                        verts[vi] = tile.InverseTransformPoint(worldPos);
                        modified = true;
                        carvedVertsTotal++;
                    }
                }

                if (modified)
                {
                    mesh.vertices = verts;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    mf.sharedMesh = mesh;

                    var col = tile.GetComponent<MeshCollider>();
                    if (col != null)
                    {
                        col.sharedMesh = null;
                        col.sharedMesh = mesh;
                    }
                    carvedTiles++;
                }
            }

            Debug.Log($"[SetupBigRiverAndOcean] Carved {carvedTiles} terrain tiles ({carvedVertsTotal} vertices) along big river & estuary channel!");
        }

        private static void ClearRiverFlora()
        {
            var env = GameObject.Find("Mangrove_Environment_Decorations");
            if (env == null) return;

            // Clear trees
            var trees = env.transform.Find("Trees");
            int disabledTrees = 0;
            if (trees != null)
            {
                for (int i = 0; i < trees.childCount; i++)
                {
                    var t = trees.GetChild(i);
                    var rend = t.GetComponentInChildren<MeshRenderer>();
                    if (rend == null) continue;

                    Vector3 center = rend.bounds.center;
                    if (center.z < -220f || center.z > 290f) continue;

                    float rx = GetEstuaryCenterX(center.z);
                    float rhw = GetEstuaryHalfWidth(center.z);

                    if (Mathf.Abs(center.x - rx) < rhw + 1.5f)
                    {
                        t.gameObject.SetActive(false);
                        disabledTrees++;
                    }
                }
            }

            // Clear undergrowth
            var undergrowth = env.transform.Find("Undergrowth");
            int disabledUndergrowth = 0;
            if (undergrowth != null)
            {
                for (int i = 0; i < undergrowth.childCount; i++)
                {
                    var u = undergrowth.GetChild(i);
                    var rend = u.GetComponentInChildren<MeshRenderer>();
                    if (rend == null) continue;

                    Vector3 center = rend.bounds.center;
                    if (center.z < -220f || center.z > 290f) continue;

                    float rx = GetEstuaryCenterX(center.z);
                    float rhw = GetEstuaryHalfWidth(center.z);

                    if (Mathf.Abs(center.x - rx) < rhw + 1.0f)
                    {
                        u.gameObject.SetActive(false);
                        disabledUndergrowth++;
                    }
                }
            }

            Debug.Log($"[SetupBigRiverAndOcean] Cleared {disabledTrees} trees and {disabledUndergrowth} undergrowth items from river channel!");
        }

        private static void ClearStrayRiverColliders()
        {
            // Move Collectible_Tools items out of water onto dry riverbank
            var tools = GameObject.Find("[Collectible_Tools]");
            if (tools != null)
            {
                int movedTools = 0;
                for (int i = 0; i < tools.transform.childCount; i++)
                {
                    var child = tools.transform.GetChild(i);
                    Vector3 pos = child.position;
                    float rx = GetEstuaryCenterX(pos.z);
                    float rhw = GetEstuaryHalfWidth(pos.z);

                    if (Mathf.Abs(pos.x - rx) < rhw + 3.0f)
                    {
                        // Shift to east or west riverbank
                        float sign = pos.x >= rx ? 1f : -1f;
                        pos.x = rx + sign * (rhw + 6.0f);
                        pos.y = 0.45f;
                        child.position = pos;
                        movedTools++;
                    }
                }
                Debug.Log($"[SetupBigRiverAndOcean] Moved {movedTools} collectible tools from water channel to riverbank!");
            }
        }

        private static void EnsureWaterConnection()
        {
            var riverGO = GameObject.Find("Main_River_Shibsa");
            var oceanGO = GameObject.Find("BayOfBengal_Ocean");

            if (riverGO != null && oceanGO != null)
            {
                // Create or update Estuary water transition plane at the river-ocean junction (Z: -95 to -180)
                var estuaryGO = GameObject.Find("Estuary_Water_Connection");
                if (estuaryGO == null)
                {
                    estuaryGO = new GameObject("Estuary_Water_Connection");
                    estuaryGO.transform.SetParent(riverGO.transform, false);
                }

                var mf = estuaryGO.GetComponent<MeshFilter>();
                if (mf == null) mf = estuaryGO.AddComponent<MeshFilter>();

                var mr = estuaryGO.GetComponent<MeshRenderer>();
                if (mr == null) mr = estuaryGO.AddComponent<MeshRenderer>();

                // Build a seamless trapezoid water mesh expanding from river mouth into ocean
                int segsZ = 20;
                int segsX = 20;
                float startZ = -95f;
                float endZ = -190f;

                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();

                for (int zi = 0; zi <= segsZ; zi++)
                {
                    float tz = (float)zi / segsZ;
                    float z = Mathf.Lerp(startZ, endZ, tz);

                    float halfW = Mathf.Lerp(28f, 100f, tz);
                    float cx = GetEstuaryCenterX(z);

                    for (int xi = 0; xi <= segsX; xi++)
                    {
                        float tx = (float)xi / segsX;
                        float x = Mathf.Lerp(cx - halfW, cx + halfW, tx);

                        verts.Add(new Vector3(x, WaterY, z));
                        uvs.Add(new Vector2(tx * 4f, tz * 4f));
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

                var mesh = new Mesh { name = "Estuary_Water_Mesh" };
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                mf.sharedMesh = mesh;

                var oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_OceanWater.mat");
                var riverMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
                mr.sharedMaterial = oceanMat != null ? oceanMat : riverMat;

                Debug.Log("[SetupBigRiverAndOcean] Estuary water connection created spanning Z=-95 to -190!");
            }
        }
    }
}
