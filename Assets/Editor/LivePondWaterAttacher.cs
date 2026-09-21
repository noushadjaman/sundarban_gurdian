using UnityEditor;
using UnityEngine;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Editor
{
    // [InitializeOnLoad] DISABLED
    public static class LivePondWaterAttacher
    {
        static LivePondWaterAttacher()
        {
            EditorApplication.update += Tick;
            EditorApplication.delayCall += AttachWaterToAllPonds;
        }

        private static double lastCheckTime = 0;

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - lastCheckTime < 1.0) return;
            lastCheckTime = EditorApplication.timeSinceStartup;

            AttachWaterToAllPonds();
        }

        [MenuItem("Sundarban/Attach Water To All Ponds Now")]
        public static void AttachWaterToAllPonds()
        {
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_PondWater.mat");
            if (waterMat == null)
            {
                waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
            }

            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int count = 0;
            foreach (var go in allGOs)
            {
                if (go == null) continue;
                string name = go.name.ToLower();
                // Match Pond, pond, Freshwater_Pond, etc. but not water or sanctuary parent
                if ((name.Contains("pond") || name.Contains("5rf3yu") || name.Contains("watering_hole")) && 
                    !name.Contains("water") && !name.Contains("sanctuary") && !name.Contains("mangrove"))
                {
                    // Attach PondWaterInjector if not present
                    var injector = go.GetComponent<PondWaterInjector>();
                    if (injector == null)
                    {
                        injector = go.AddComponent<PondWaterInjector>();
                        injector.waterMaterial = waterMat;
                        injector.waterHeight = 0.08f;
                        injector.waterScaleXZ = new Vector2(1.95f, 1.55f);
                        injector.waterOffsetXZ = new Vector2(0.05f, 0.03f);
                    }
                    injector.EnsureWaterSurface();

                    Transform existing = go.transform.Find("Pond_Water_Surface");
                    if (existing == null)
                    {
                        GameObject waterGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        waterGO.name = "Pond_Water_Surface";
                        waterGO.transform.SetParent(go.transform, false);

                        waterGO.transform.localPosition = new Vector3(0.05f, 0.08f, 0.03f);
                        waterGO.transform.localRotation = Quaternion.identity;
                        waterGO.transform.localScale = new Vector3(1.95f, 0.015f, 1.55f);

                        var col = waterGO.GetComponent<Collider>();
                        if (col != null)
                        {
                            if (Application.isPlaying) Object.Destroy(col);
                            else Object.DestroyImmediate(col);
                        }

                        var mr = waterGO.GetComponent<MeshRenderer>();
                        if (mr != null && waterMat != null)
                        {
                            mr.sharedMaterial = waterMat;
                        }
                        count++;
                        Debug.Log($"[LivePondWaterAttacher] Attached water to pond: '{go.name}' at {go.transform.position}");
                    }
                    else
                    {
                        var mr = existing.GetComponent<MeshRenderer>();
                        if (mr != null && waterMat != null && mr.sharedMaterial != waterMat)
                        {
                            mr.sharedMaterial = waterMat;
                        }
                    }
                }
            }

            // Also ensure Saltwater Crocodile is colored with authentic reptile skin
            var crocs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in crocs)
            {
                if (go == null) continue;
                if (go.name.Contains("Crocodile") || go.name.Contains("Alligator"))
                {
                    var setup = go.GetComponent<CrocodileMaterialSetup>();
                    if (setup == null) setup = go.AddComponent<CrocodileMaterialSetup>();
                    setup.ApplyMaterials();
                }
            }
        }
    }
}
