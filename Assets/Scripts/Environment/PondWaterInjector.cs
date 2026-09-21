using UnityEngine;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Automatically ensures any pond in the scene has an authentic, reflective,
    /// glistening water surface plane fitted cleanly inside the rock basin.
    /// Works in both Edit Mode and Play Mode.
    /// </summary>
    [ExecuteAlways]
    public class PondWaterInjector : MonoBehaviour
    {
        [Header("Water Settings")]
        public Material waterMaterial;
        public float waterHeight = 0.08f;
        public Vector2 waterScaleXZ = new Vector2(1.95f, 1.55f);
        public Vector2 waterOffsetXZ = new Vector2(0.05f, 0.03f);

        private void Awake()
        {
            EnsureWaterSurface();
        }

        private void OnEnable()
        {
            EnsureWaterSurface();
        }

        private void Start()
        {
            EnsureWaterSurface();
        }

        private void Update()
        {
            // Keep water attached if in editor or play mode
            if (transform.Find("Pond_Water_Surface") == null)
            {
                EnsureWaterSurface();
            }
        }

        public void EnsureWaterSurface()
        {
            Transform existing = transform.Find("Pond_Water_Surface");
            if (existing != null)
            {
                if (waterMaterial != null)
                {
                    var rend = existing.GetComponent<MeshRenderer>();
                    if (rend != null && rend.sharedMaterial != waterMaterial)
                    {
                        rend.sharedMaterial = waterMaterial;
                    }
                }
                return;
            }

            // Create circular / cylindrical water sheet
            GameObject waterGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterGO.name = "Pond_Water_Surface";
            waterGO.transform.SetParent(transform, false);

            waterGO.transform.localPosition = new Vector3(waterOffsetXZ.x, waterHeight, waterOffsetXZ.y);
            waterGO.transform.localRotation = Quaternion.identity;
            waterGO.transform.localScale = new Vector3(waterScaleXZ.x, 0.012f, waterScaleXZ.y);

            // Remove cylinder collider so animals/player can walk through or drink
            var col = waterGO.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            if (waterMaterial == null)
            {
#if UNITY_EDITOR
                waterMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_PondWater.mat");
                if (waterMaterial == null)
                {
                    waterMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sundarban_RiverWater.mat");
                }
#endif
            }

            var mr = waterGO.GetComponent<MeshRenderer>();
            if (mr != null && waterMaterial != null)
            {
                mr.sharedMaterial = waterMaterial;
            }
        }
    }
}
