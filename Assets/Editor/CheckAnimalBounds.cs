using UnityEngine;
using UnityEditor;

namespace SundarbanGuardian.Editor
{
    public static class CheckAnimalBounds
    {
        public static void MeasureAll()
        {
            string[] paths = new string[]
            {
                "Assets/ExternalModels/Animals/animal/Deer by Poly by Google - fUo4AIcd8XR/Mesh_Deer.obj",
                "Assets/ExternalModels/Animals/decorate/Bird by Poly by Google - 8Ph79kHbt9s/Bird_01(1).obj",
                "Assets/ExternalModels/Animals/decorate/Crocodile by Poly by Google - 2an6E2WjW3z/Mesh_Crocodile.obj",
                "Assets/ExternalModels/Animals/decorate/Cobra by Poly by Google - 40_5Xq467-U/cobra.obj",
                "Assets/ExternalModels/Animals/decorate/Dog by madtrollstudio - H2SkxSJCfI/dogFinal (5).fbx",
                "Assets/ExternalModels/Animals/fish/FBX/Fish1.fbx",
                "Assets/ExternalModels/Animals/fish/FBX/Fish2.fbx",
                "Assets/ExternalModels/Animals/fish/FBX/Fish3.fbx",
                "Assets/ExternalModels/Animals/fish/FBX/Manta ray.fbx",
                "Assets/ExternalModels/Animals/fish/FBX/Dolphin.fbx",
                "Assets/ExternalModels/Animals/fish/FBX/Whale.fbx",
                "Assets/ExternalModels/Animals/fish/FBX/Shark.fbx"
            };

            foreach (var path in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null)
                {
                    Debug.LogWarning($"Could not load asset at {path}");
                    continue;
                }

                Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
                bool hasBounds = false;

                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (!hasBounds)
                    {
                        totalBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        totalBounds.Encapsulate(r.bounds);
                    }
                }

                var filters = go.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in filters)
                {
                    if (mf.sharedMesh != null)
                    {
                        if (!hasBounds)
                        {
                            totalBounds = mf.sharedMesh.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            totalBounds.Encapsulate(mf.sharedMesh.bounds);
                        }
                    }
                }

                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    if (smr.sharedMesh != null)
                    {
                        if (!hasBounds)
                        {
                            totalBounds = smr.sharedMesh.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            totalBounds.Encapsulate(smr.sharedMesh.bounds);
                        }
                    }
                }

                Debug.Log($"[AnimalBounds] {go.name} -> Size: {totalBounds.size} (X={totalBounds.size.x:F2}, Y={totalBounds.size.y:F2}, Z={totalBounds.size.z:F2})");
            }
        }
    }
}
