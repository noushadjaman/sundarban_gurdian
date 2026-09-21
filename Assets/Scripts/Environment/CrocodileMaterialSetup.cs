using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Ensures that the Saltwater Crocodile (লোনা পানির কুমির) is always colored
    /// with authentic Sundarban estuarine scaly hide, ivory teeth, and golden reptile eyes.
    /// Operates in both Edit Mode and Play Mode.
    /// </summary>
    [ExecuteAlways]
    public class CrocodileMaterialSetup : MonoBehaviour
    {
        public Material bodyMaterial;
        public Material teethMaterial;
        public Material eyesMaterial;

        private void OnEnable()
        {
            ApplyMaterials();
        }

        private void Start()
        {
            ApplyMaterials();
        }

        public void ApplyMaterials()
        {
#if UNITY_EDITOR
            if (bodyMaterial == null)
                bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Body.mat");
            if (teethMaterial == null)
                teethMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Teeth.mat");
            if (eyesMaterial == null)
                eyesMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_Crocodile_Eyes.mat");
#endif

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                string goName = r.gameObject.name.ToLower();

                if (goName.Contains("teeth") && teethMaterial != null)
                {
                    r.sharedMaterial = teethMaterial;
                }
                else if ((goName.Contains("eye") || goName.Contains("eyeglass") || goName.Contains("eyememb")) && eyesMaterial != null)
                {
                    r.sharedMaterial = eyesMaterial;
                }
                else if (bodyMaterial != null)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = bodyMaterial;
                    }
                    r.sharedMaterials = mats;
                }
            }
        }
    }
}
