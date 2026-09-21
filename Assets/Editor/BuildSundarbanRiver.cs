using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Editor
{
    /// <summary>
    /// Sculpt and build an authentic, contoured Sundarban river following the NASA Landsat map,
    /// complete with flowing reflective water, sloped riverbed, colored saltwater crocodile,
    /// deep multi-tier mangrove backdrop (no floating sky trees), and river props.
    /// </summary>
    // NOTE: Auto-build disabled; use BuildSundarbanFullMap instead.
    // [InitializeOnLoad]
    public static class BuildSundarbanRiver
    {
        // Static constructor disabled to prevent conflict with BuildSundarbanFullMap
        // static BuildSundarbanRiver() { }

        [MenuItem("Sundarban/Build Natural River & Deep Forest")]
        public static void ConstructRiverAndForest()
        {
            Debug.Log("[BuildSundarbanRiver] Constructing natural contoured river matching Landsat radar map...");

            // 1. Materials
            var riverWaterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
            var riverbankGroundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Riverbank_Ground.mat");
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Ground.mat");
            var eastSwampMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_EastSwamp.mat");
            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Bark.mat");
            var leavesLushMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mangrove_Leaves_Lush.mat");
            var crocBodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Body.mat");
            var crocTeethMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Teeth.mat");
            var crocEyesMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Eyes.mat");
            var ladyPalmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_LadyPalm.mat");
            var thatchPalmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_ThatchPalm.mat");

            // Prefabs
            var twistedTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Twisted Tree by Quaternius - 7PDBpElkQr/TwistedTree_4.fbx");
            var commonTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Tree by Quaternius - QVOop92WmG/CommonTree_3.fbx");
            var ladyPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Lady palm tree by Poly by Google - e0AbYA90ux5/LadyPalmTree.obj");
            var thatchPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/Thatch palm tree by Poly by Google - 74GpKCkUqcn/KeyThatchPalmTree.obj");

            // 2. Cleanup old flat river underlay and old river meshes
            var oldRiverBed = GameObject.Find("River_Bed_Underlay");
            if (oldRiverBed != null) Object.DestroyImmediate(oldRiverBed);

            var oldRiverSys = GameObject.Find("River_System");
            if (oldRiverSys != null) Object.DestroyImmediate(oldRiverSys);

            var riverParent = new GameObject("River_System");

            // 3. Build Contoured River Mesh (Water Surface + Sloped Riverbed)
            BuildRiverMeshes(riverParent.transform, riverWaterMat, riverbankGroundMat != null ? riverbankGroundMat : groundMat);

            // 4. Extend and Align Eastern Riverbank Ground
            var eastLand = GameObject.Find("Eastern_Riverbank_Land");
            if (eastLand == null)
            {
                eastLand = GameObject.CreatePrimitive(PrimitiveType.Plane);
                eastLand.name = "Eastern_Riverbank_Land";
            }
            // Positioned to cover from X: 45 to 185, Z: -90 to 330
            eastLand.transform.position = new Vector3(115f, 0f, 120f);
            eastLand.transform.localScale = new Vector3(14f, 1f, 44f);
            if (eastSwampMat != null) eastLand.GetComponent<MeshRenderer>().sharedMaterial = eastSwampMat;

            // 5. Build Deep Multi-Tier East Forest (Eliminating Sky Trees)
            var forestParent = GameObject.Find("Mangrove_Forest");
            if (forestParent == null) forestParent = new GameObject("Mangrove_Forest");

            // Remove previous canopy arches and riverbank mangroves to rebuild with proper depth & sink
            string[] toClear = { "Decorator_Canopy_Arches", "Riverbank_Mangroves", "Deep_East_Forest_Wall", "Mudflat_Palms_And_Fringes" };
            foreach (var n in toClear)
            {
                var tr = forestParent.transform.Find(n);
                if (tr != null) Object.DestroyImmediate(tr.gameObject);
            }

            var archesGroup = new GameObject("Decorator_Canopy_Arches").transform;
            archesGroup.SetParent(forestParent.transform);

            var riverbankGroup = new GameObject("Riverbank_Mangroves").transform;
            riverbankGroup.SetParent(forestParent.transform);

            var eastWallGroup = new GameObject("Deep_East_Forest_Wall").transform;
            eastWallGroup.SetParent(forestParent.transform);

            var palmsGroup = new GameObject("Mudflat_Palms_And_Fringes").transform;
            palmsGroup.SetParent(forestParent.transform);

            Random.InitState(1971);

            // Plant Arching & Riverbank Mangroves along the curved river spline
            for (float z = -80f; z <= 240f; z += 4.5f)
            {
                GetRiverBounds(z, out float xLeft, out float xRight, out _);

                // --- West Bank Arching Tree (leans over water) ---
                float xWest = xLeft - Random.Range(0.4f, 2.5f);
                // Sunk by -0.35m so roots are firmly buried in mud with ZERO daylight beneath
                Vector3 posWest = new Vector3(xWest, -0.35f, z + Random.Range(-1.2f, 1.2f));
                float leanWest = Random.Range(14f, 24f);
                float rotYWest = Random.Range(-25f, 25f);
                var rotWest = Quaternion.Euler(Random.Range(-4f, 4f), rotYWest, -leanWest);

                var archW = (GameObject)Object.Instantiate(twistedTreePrefab, posWest, rotWest, archesGroup);
                archW.name = $"Arch_West_{z:F0}";
                archW.transform.localScale = Vector3.one * Random.Range(1.1f, 1.45f);
                EnsureTreeMaterials(archW, barkMat, leavesLushMat);

                // West Bank Second Row (CommonTree)
                if (Random.value > 0.3f && commonTreePrefab != null)
                {
                    Vector3 posW2 = posWest + new Vector3(Random.Range(-3.5f, -1.8f), 0.15f, Random.Range(-1.5f, 1.5f));
                    var tW2 = (GameObject)Object.Instantiate(commonTreePrefab, posW2, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), riverbankGroup);
                    tW2.name = $"Canopy_West_{z:F0}";
                    tW2.transform.localScale = Vector3.one * Random.Range(1.15f, 1.55f);
                    EnsureTreeMaterials(tW2, barkMat, leavesLushMat);
                }

                // --- East Bank Arching Tree (leans over water) ---
                float xEast = xRight + Random.Range(0.4f, 2.5f);
                // Sunk by -0.35m so roots are firmly buried in mud
                Vector3 posEast = new Vector3(xEast, -0.35f, z + Random.Range(-1.2f, 1.2f));
                float leanEast = Random.Range(14f, 24f);
                float rotYEast = Random.Range(155f, 205f);
                var rotEast = Quaternion.Euler(Random.Range(-4f, 4f), rotYEast, leanEast);

                var archE = (GameObject)Object.Instantiate(twistedTreePrefab, posEast, rotEast, archesGroup);
                archE.name = $"Arch_East_{z:F0}";
                archE.transform.localScale = Vector3.one * Random.Range(1.1f, 1.45f);
                EnsureTreeMaterials(archE, barkMat, leavesLushMat);

                // East Bank Row 2 (Immediate bank canopy)
                Vector3 posE2 = posEast + new Vector3(Random.Range(2.5f, 5.0f), 0.15f, Random.Range(-1.5f, 1.5f));
                var tE2 = (GameObject)Object.Instantiate(commonTreePrefab, posE2, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), riverbankGroup);
                tE2.name = $"Canopy_East_{z:F0}";
                tE2.transform.localScale = Vector3.one * Random.Range(1.2f, 1.6f);
                EnsureTreeMaterials(tE2, barkMat, leavesLushMat);

                // Riverbank Palms (Golpata / Thatch Palm)
                if (Random.value > 0.45f && thatchPalmPrefab != null)
                {
                    Vector3 pPos = new Vector3(xLeft - Random.Range(2.0f, 4.0f), 0f, z + Random.Range(-1f, 1f));
                    var palm = (GameObject)Object.Instantiate(thatchPalmPrefab, pPos, Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(4f, 10f)), palmsGroup);
                    palm.name = $"Palm_West_{z:F0}";
                    palm.transform.localScale = Vector3.one * Random.Range(0.7f, 0.95f);
                    ApplyMat(palm, thatchPalmMat);
                }
                if (Random.value > 0.45f && ladyPalmPrefab != null)
                {
                    Vector3 pPos = new Vector3(xRight + Random.Range(2.0f, 4.0f), 0f, z + Random.Range(-1f, 1f));
                    var palm = (GameObject)Object.Instantiate(ladyPalmPrefab, pPos, Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(4f, 10f)), palmsGroup);
                    palm.name = $"Palm_East_{z:F0}";
                    palm.transform.localScale = Vector3.one * Random.Range(0.55f, 0.80f);
                    ApplyMat(palm, ladyPalmMat);
                }
            }

            // --- DEEP EAST FOREST WALL (Layers 3 & 4 extending to X: 165, Z: -80..260) ---
            // This creates a dense, impenetrable wall of lush green foliage so that
            // when the player looks across the river, the sky is completely blocked by forest.
            for (float fz = -75f; fz <= 250f; fz += 8.0f)
            {
                GetRiverBounds(fz, out _, out float xRight, out _);
                float startX = xRight + 7.0f;

                for (float fx = startX; fx <= 165f; fx += 8.5f)
                {
                    Vector3 fPos = new Vector3(fx + Random.Range(-2.5f, 2.5f), -0.15f, fz + Random.Range(-2.5f, 2.5f));
                    GameObject prefab = (Random.value > 0.35f) ? commonTreePrefab : twistedTreePrefab;
                    if (prefab != null)
                    {
                        var tree = (GameObject)Object.Instantiate(prefab, fPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), eastWallGroup);
                        tree.name = $"EastForest_{fx:F0}_{fz:F0}";
                        // Farther trees are slightly taller to create a majestic tiered canopy ridge
                        float depthScale = Mathf.Lerp(1.25f, 2.0f, (fx - startX) / 100f);
                        tree.transform.localScale = Vector3.one * (depthScale * Random.Range(0.9f, 1.15f));
                        EnsureTreeMaterials(tree, barkMat, leavesLushMat);
                    }
                }
            }

            // 6. Crocodile Positioning & Material Setup
            PositionAndColorCrocodile(crocBodyMat, crocTeethMat, crocEyesMat);

            // 7. Traditional River Country Boat (নৌকা) prop on sandbank
            BuildCountryBoat(riverParent.transform, barkMat);

            Debug.Log("[BuildSundarbanRiver] Natural river, deep forest wall, and crocodile coloring completed successfully!");
        }

        // River Spline Calibration matching NASA Landsat map
        public static void GetRiverBounds(float z, out float xLeft, out float xRight, out float xCenter)
        {
            // Centerline meandering curve
            if (z < -40f)
            {
                // Southern Estuary widening towards Bay of Bengal
                float t = Mathf.InverseLerp(-80f, -40f, z);
                xCenter = Mathf.Lerp(42f, 39f, t);
                float width = Mathf.Lerp(44f, 30f, t);
                xLeft = xCenter - width * 0.5f;
                xRight = xCenter + width * 0.5f;
            }
            else if (z < 60f)
            {
                // Central lower reach
                float t = Mathf.InverseLerp(-40f, 60f, z);
                xCenter = Mathf.Lerp(39f, 36.5f, t);
                float width = Mathf.Lerp(30f, 24f, t);
                xLeft = xCenter - width * 0.5f;
                xRight = xCenter + width * 0.5f;
            }
            else if (z < 150f)
            {
                // Central meander past crocodile crossing
                float t = Mathf.InverseLerp(60f, 150f, z);
                xCenter = Mathf.Lerp(36.5f, 39f, t) + Mathf.Sin(t * Mathf.PI) * 2.5f;
                float width = 23f;
                xLeft = xCenter - width * 0.5f;
                xRight = xCenter + width * 0.5f;
            }
            else
            {
                // Northern river reach
                float t = Mathf.InverseLerp(150f, 250f, z);
                xCenter = Mathf.Lerp(39f, 41f, t);
                float width = Mathf.Lerp(23f, 26f, t);
                xLeft = xCenter - width * 0.5f;
                xRight = xCenter + width * 0.5f;
            }
        }

        private static void BuildRiverMeshes(Transform parent, Material waterMat, Material bedMat)
        {
            // Generate segmented procedural river grid:
            // 80 segments along Z (step 4m), 10 vertices across X
            float zStart = -85f;
            float zEnd = 250f;
            float zStep = 4.0f;
            int zCount = Mathf.CeilToInt((zEnd - zStart) / zStep) + 1;
            int xCount = 10;

            // 1. Water Surface Mesh (at Y = 0.05f)
            GameObject waterGO = new GameObject("River_Water_Surface");
            waterGO.transform.SetParent(parent, false);
            var waterMf = waterGO.AddComponent<MeshFilter>();
            var waterMr = waterGO.AddComponent<MeshRenderer>();
            waterMr.sharedMaterial = waterMat;
            waterMf.sharedMesh = CreateRiverSurfaceMesh(zStart, zStep, zCount, xCount, 0.05f);

            // 2. Contoured Riverbed Mesh (underwater mud shelves & deep channel)
            GameObject bedGO = new GameObject("River_Bed_Contoured");
            bedGO.transform.SetParent(parent, false);
            var bedMf = bedGO.AddComponent<MeshFilter>();
            var bedMr = bedGO.AddComponent<MeshRenderer>();
            bedMr.sharedMaterial = bedMat;
            bedMf.sharedMesh = CreateRiverBedMesh(zStart, zStep, zCount, xCount);
        }

        private static Mesh CreateRiverSurfaceMesh(float zStart, float zStep, int zCount, int xCount, float waterY)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RiverWaterSurfaceMesh";

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector4> tangents = new List<Vector4>();
            List<int> tris = new List<int>();

            for (int zi = 0; zi < zCount; zi++)
            {
                float z = zStart + zi * zStep;
                GetRiverBounds(z, out float xLeft, out float xRight, out _);

                for (int xi = 0; xi < xCount; xi++)
                {
                    float u = (float)xi / (xCount - 1);
                    float x = Mathf.Lerp(xLeft - 1.2f, xRight + 1.2f, u); // slightly overlap mudbank
                    verts.Add(new Vector3(x, waterY, z));
                    uvs.Add(new Vector2(u, z * 0.04f));
                    norms.Add(Vector3.up);
                    tangents.Add(new Vector4(1f, 0f, 0f, 1f));
                }
            }

            for (int zi = 0; zi < zCount - 1; zi++)
            {
                for (int xi = 0; xi < xCount - 1; xi++)
                {
                    int i0 = zi * xCount + xi;
                    int i1 = i0 + 1;
                    int i2 = (zi + 1) * xCount + xi;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(norms);
            mesh.SetTangents(tangents);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateRiverBedMesh(float zStart, float zStep, int zCount, int xCount)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RiverBedMesh";

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> norms = new List<Vector3>();
            List<int> tris = new List<int>();

            for (int zi = 0; zi < zCount; zi++)
            {
                float z = zStart + zi * zStep;
                GetRiverBounds(z, out float xLeft, out float xRight, out float xCenter);

                for (int xi = 0; xi < xCount; xi++)
                {
                    float u = (float)xi / (xCount - 1);
                    float x = Mathf.Lerp(xLeft - 2.5f, xRight + 2.5f, u);

                    // Parabolic riverbed depth: edge at 0m, center reaches -1.6m deep
                    float distFromCenterNorm = Mathf.Abs(u - 0.5f) * 2.0f; // 0 at center, 1 at edge
                    float depth = Mathf.Lerp(-1.6f, -0.05f, Mathf.Pow(distFromCenterNorm, 1.8f));

                    verts.Add(new Vector3(x, depth, z));
                    uvs.Add(new Vector2(x * 0.15f, z * 0.15f));
                    norms.Add(Vector3.up);
                }
            }

            for (int zi = 0; zi < zCount - 1; zi++)
            {
                for (int xi = 0; xi < xCount - 1; xi++)
                {
                    int i0 = zi * xCount + xi;
                    int i1 = i0 + 1;
                    int i2 = (zi + 1) * xCount + xi;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void PositionAndColorCrocodile(Material bodyMat, Material teethMat, Material eyesMat)
        {
            var croc = GameObject.Find("River_Saltwater_Crocodile");
            if (croc == null)
            {
                var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                foreach (var g in all)
                {
                    if (g != null && (g.name.Contains("Crocodile") || g.name.Contains("Alligator")))
                    {
                        croc = g;
                        break;
                    }
                }
            }

            if (croc != null)
            {
                // Position basking on eastern sunlit mud shelf right at the water's edge
                GetRiverBounds(62f, out _, out float xRight, out _);
                croc.transform.position = new Vector3(xRight - 0.8f, -0.02f, 62f);
                croc.transform.rotation = Quaternion.Euler(0f, 130f, 0f);

                var setup = croc.GetComponent<CrocodileMaterialSetup>();
                if (setup == null) setup = croc.AddComponent<CrocodileMaterialSetup>();
                setup.bodyMaterial = bodyMat;
                setup.teethMaterial = teethMat;
                setup.eyesMaterial = eyesMat;
                setup.ApplyMaterials();

                Debug.Log($"[BuildSundarbanRiver] Positioned & colored crocodile at {croc.transform.position}");
            }
        }

        private static void BuildCountryBoat(Transform parent, Material woodMat)
        {
            var existingBoat = GameObject.Find("Sundarban_Country_Boat");
            if (existingBoat != null) Object.DestroyImmediate(existingBoat);

            var boatGO = new GameObject("Sundarban_Country_Boat");
            boatGO.transform.SetParent(parent);
            // Position moored near west riverbank at Z = 28
            GetRiverBounds(28f, out float xLeft, out _, out _);
            boatGO.transform.position = new Vector3(xLeft + 2.2f, 0.08f, 28f);
            boatGO.transform.rotation = Quaternion.Euler(-2f, 32f, 3f);

            // Wooden Hull
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Boat_Hull";
            hull.transform.SetParent(boatGO.transform, false);
            hull.transform.localPosition = Vector3.zero;
            hull.transform.localScale = new Vector3(1.6f, 0.65f, 5.2f);
            var hr = hull.GetComponent<MeshRenderer>();
            if (hr != null && woodMat != null) hr.sharedMaterial = woodMat;

            // Bamboo Canopy / Roof (Chhoi / ছই)
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.name = "Boat_Bamboo_Roof";
            roof.transform.SetParent(boatGO.transform, false);
            roof.transform.localPosition = new Vector3(0f, 0.60f, -0.4f);
            roof.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            roof.transform.localScale = new Vector3(1.55f, 1.4f, 0.75f);
            var rr = roof.GetComponent<MeshRenderer>();
            if (rr != null && woodMat != null) rr.sharedMaterial = woodMat;
        }

        private static void EnsureTreeMaterials(GameObject treeGO, Material barkMat, Material leavesMat)
        {
            if (treeGO == null) return;
            var renderers = treeGO.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                string goName = r.gameObject.name.ToLower();
                if (goName.Contains("leaves") || goName.Contains("foliage") || goName.Contains("crown"))
                {
                    if (leavesMat != null) r.sharedMaterial = leavesMat;
                }
                else
                {
                    if (barkMat != null) r.sharedMaterial = barkMat;
                }
            }
        }

        private static void ApplyMat(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null) r.sharedMaterial = mat;
            }
        }
    }
}
