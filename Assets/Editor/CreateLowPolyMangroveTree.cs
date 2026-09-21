using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SundarbanGuardian.Editor
{
    public static class CreateLowPolyMangroveTree
    {
        [MenuItem("Sundarban/Generate Low-Poly Mangrove Assets")]
        public static void GenerateAssets()
        {
            EnsureAssetFolders();
            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Bark.mat");
            var leavesMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Leaves_Lush.mat");

            if (barkMat == null)
            {
                barkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                barkMat.SetColor("_BaseColor", new Color(0.24f, 0.18f, 0.13f));
                barkMat.SetFloat("_Smoothness", 0.25f);
                AssetDatabase.CreateAsset(barkMat, "Assets/Materials/Mangrove_Bark.mat");
            }
            if (leavesMat == null)
            {
                leavesMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                leavesMat.SetColor("_BaseColor", new Color(0.18f, 0.38f, 0.14f));
                leavesMat.SetFloat("_Smoothness", 0.45f);
                AssetDatabase.CreateAsset(leavesMat, "Assets/Materials/Mangrove_Leaves_Lush.mat");
            }

            // 1. Generate Upright Stilt Root Mangrove
            string mesh1Path = "Assets/Meshes/Mesh_Mangrove_StiltRoot.asset";
            var meshUpright = BuildMangroveMesh("Mesh_Mangrove_StiltRoot", leanAngle: 8f, spreadRadius: 1.4f, canopyRadius: 1.8f);
            AssetDatabase.CreateAsset(meshUpright, mesh1Path);
            AssetDatabase.SaveAssets();
            var loadedMesh1 = AssetDatabase.LoadAssetAtPath<Mesh>(mesh1Path);
            CreatePrefab("Assets/Prefabs/Trees/Tree_LowPoly_Mangrove_Stilt.prefab", loadedMesh1, barkMat, leavesMat, 1.4f);

            // 2. Generate Arching Overhanging Mangrove (leans over river water)
            string mesh2Path = "Assets/Meshes/Mesh_Mangrove_Overhanging.asset";
            var meshLeaning = BuildMangroveMesh("Mesh_Mangrove_Overhanging", leanAngle: 24f, spreadRadius: 1.8f, canopyRadius: 2.2f);
            AssetDatabase.CreateAsset(meshLeaning, mesh2Path);
            AssetDatabase.SaveAssets();
            var loadedMesh2 = AssetDatabase.LoadAssetAtPath<Mesh>(mesh2Path);
            CreatePrefab("Assets/Prefabs/Trees/Tree_LowPoly_Mangrove_Overhanging.prefab", loadedMesh2, barkMat, leavesMat, 1.8f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateLowPolyMangroveTree] Successfully generated ultra-low-poly mangrove trees (<500 tris) with authentic stilt roots and overhanging canopy!");
        }

        private static void EnsureAssetFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
                AssetDatabase.CreateFolder("Assets", "Meshes");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Trees"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Trees");
        }

        private static void CreatePrefab(string prefabPath, Mesh mesh, Material barkMat, Material leavesMat, float colRadius)
        {
            var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { barkMat, leavesMat };

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.6f, 0f);
            col.radius = 0.5f;
            col.height = 3.2f;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        public static Mesh BuildMangroveMesh(string name, float leanAngle, float spreadRadius, float canopyRadius)
        {
            var mesh = new Mesh { name = name };
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var trunkTris = new List<int>();
            var leafTris = new List<int>();

            System.Action<Vector3, Vector3, float, float, int, List<int>> addTube = (start, end, rStart, rEnd, radialSides, triList) =>
            {
                int baseIdx = verts.Count;
                Vector3 dir = (end - start).normalized;
                Vector3 up = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.9f) up = Vector3.forward;
                Vector3 right = Vector3.Cross(dir, up).normalized;
                Vector3 forward = Vector3.Cross(right, dir).normalized;

                for (int side = 0; side <= radialSides; side++)
                {
                    float angle = (float)side / radialSides * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    Vector3 offset = (right * cos + forward * sin);

                    verts.Add(start + offset * rStart);
                    norms.Add(offset);
                    uvs.Add(new Vector2((float)side / radialSides, 0f));

                    verts.Add(end + offset * rEnd);
                    norms.Add(offset);
                    uvs.Add(new Vector2((float)side / radialSides, 1f));
                }

                for (int side = 0; side < radialSides; side++)
                {
                    int i0 = baseIdx + side * 2;
                    int i1 = i0 + 1;
                    int i2 = i0 + 2;
                    int i3 = i0 + 3;

                    triList.Add(i0); triList.Add(i1); triList.Add(i2);
                    triList.Add(i2); triList.Add(i1); triList.Add(i3);
                }
            };

            // 1. Arching Stilt Roots (curving down from y=0.9m into intertidal mud)
            int rootCount = 7;
            for (int i = 0; i < rootCount; i++)
            {
                float angle = (float)i / rootCount * Mathf.PI * 2f + (i % 2 == 0 ? 0.1f : -0.1f);
                float spreadR = spreadRadius * (0.85f + (i % 3) * 0.15f);
                Vector3 p0 = new Vector3(Mathf.Cos(angle) * 0.22f, 0.95f, Mathf.Sin(angle) * 0.22f);
                Vector3 pMid = new Vector3(Mathf.Cos(angle) * (spreadR * 0.72f), 0.50f, Mathf.Sin(angle) * (spreadR * 0.72f));
                Vector3 pEnd = new Vector3(Mathf.Cos(angle) * spreadR, -0.38f, Mathf.Sin(angle) * spreadR);

                addTube(p0, pMid, 0.13f, 0.10f, 5, trunkTris);
                addTube(pMid, pEnd, 0.10f, 0.08f, 5, trunkTris);
            }

            // 2. Leaning Gnarled Central Trunk
            float leanRad = leanAngle * Mathf.Deg2Rad;
            Vector3 leanDir = new Vector3(Mathf.Sin(leanRad), 0f, Mathf.Cos(leanRad) * 0.3f);

            Vector3 t0 = new Vector3(0f, 0.85f, 0f);
            Vector3 t1 = t0 + new Vector3(leanDir.x * 0.8f, 1.1f, leanDir.z * 0.8f);
            Vector3 t2 = t1 + new Vector3(leanDir.x * 1.2f, 1.2f, leanDir.z * 1.2f);
            addTube(t0, t1, 0.26f, 0.21f, 6, trunkTris);
            addTube(t1, t2, 0.21f, 0.15f, 6, trunkTris);

            // 3. Spreading Boughs
            Vector3[] boughs = new Vector3[] {
                t2 + new Vector3( 1.1f + leanDir.x * 0.5f, 0.9f,  0.4f),
                t2 + new Vector3(-0.9f + leanDir.x * 0.3f, 0.7f,  0.7f),
                t2 + new Vector3( 0.2f + leanDir.x * 0.4f, 1.1f, -1.0f),
                t2 + new Vector3( 1.3f + leanDir.x * 0.8f, 0.6f, -0.3f)
            };
            foreach (var b in boughs)
            {
                addTube(t2, b, 0.14f, 0.07f, 5, trunkTris);
            }

            // 4. Foliage Clusters (Multi-tiered canopy)
            System.Action<Vector3, float, float> addCanopyCluster = (center, rx, ry) =>
            {
                int latRings = 4;
                int lonSegs = 7;
                int baseIdx = verts.Count;

                for (int lat = 0; lat <= latRings; lat++)
                {
                    float v = (float)lat / latRings;
                    float phi = v * Mathf.PI;
                    for (int lon = 0; lon <= lonSegs; lon++)
                    {
                        float u = (float)lon / lonSegs;
                        float theta = u * Mathf.PI * 2f;

                        float rNoise = 1f + Mathf.Sin(theta * 3f) * 0.12f + Mathf.Cos(phi * 2f) * 0.08f;
                        float x = Mathf.Sin(phi) * Mathf.Cos(theta) * rx * rNoise;
                        float y = Mathf.Cos(phi) * ry * rNoise;
                        float z = Mathf.Sin(phi) * Mathf.Sin(theta) * rx * rNoise;

                        Vector3 pos = center + new Vector3(x, y, z);
                        Vector3 n = (pos - center).normalized;
                        verts.Add(pos);
                        norms.Add(n);
                        uvs.Add(new Vector2(u, v));
                    }
                }

                for (int lat = 0; lat < latRings; lat++)
                {
                    for (int lon = 0; lon < lonSegs; lon++)
                    {
                        int i0 = baseIdx + lat * (lonSegs + 1) + lon;
                        int i1 = i0 + (lonSegs + 1);
                        int i2 = i0 + 1;
                        int i3 = i1 + 1;

                        leafTris.Add(i0); leafTris.Add(i1); leafTris.Add(i2);
                        leafTris.Add(i2); leafTris.Add(i1); leafTris.Add(i3);
                    }
                }
            };

            // Canopy puffs centered on boughs (larger, fuller tropical mangrove canopy)
            addCanopyCluster(t2 + new Vector3(0f, 1.2f, 0f), canopyRadius * 1.1f, 1.4f);
            addCanopyCluster(boughs[0], canopyRadius * 0.95f, 1.2f);
            addCanopyCluster(boughs[1], canopyRadius * 0.90f, 1.1f);
            addCanopyCluster(boughs[2], canopyRadius * 0.95f, 1.2f);
            addCanopyCluster(boughs[3], canopyRadius * 0.85f, 1.1f);

            mesh.subMeshCount = 2;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(trunkTris, 0);
            mesh.SetTriangles(leafTris, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
