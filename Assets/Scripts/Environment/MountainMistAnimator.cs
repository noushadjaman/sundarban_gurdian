using UnityEngine;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Animates drifting atmospheric mist, rolling cloud layers, and horizon fog
    /// across the perimeter mountains (West, East, North) to bring the horizon to life
    /// and eliminate static, floating jagged polygons.
    /// </summary>
    public class MountainMistAnimator : MonoBehaviour
    {
        [Header("UV Scrolling / Mist Drift")]
        public Vector2 mistScrollSpeed = new Vector2(0.012f, 0.003f);
        public Vector2 secondaryMistSpeed = new Vector2(-0.007f, 0.005f);

        [Header("Atmospheric Horizon Fog Tuning")]
        public bool configureGlobalFog = true;
        public Color swampFogColor = new Color(0.13f, 0.19f, 0.17f, 1f); // Deep mangrove swamp mist
        public float swampFogDensity = 0.0095f;

        [Header("Mist Planes (Procedural Horizon Clouds)")]
        public bool spawnDriftingMistBands = true;
        public int mistBandCount = 6;

        private Renderer targetRenderer;
        private MaterialPropertyBlock propBlock;
        private Vector2 currentOffset;
        private Transform[] mistBands;
        private Vector3[] mistDriftSpeeds;

        private void Start()
        {
            targetRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();

            if (configureGlobalFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = swampFogColor;
                RenderSettings.fogDensity = swampFogDensity;
            }

            if (spawnDriftingMistBands && transform.name.Contains("Mountain"))
            {
                CreateDriftingMistBands();
            }
        }

        private void Update()
        {
            // 1. Scroll texture offset for rolling mist effect
            currentOffset += mistScrollSpeed * Time.deltaTime;
            if (currentOffset.x > 10f) currentOffset.x -= 10f;
            if (currentOffset.y > 10f) currentOffset.y -= 10f;

            if (targetRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(propBlock);
                propBlock.SetVector("_BaseMap_ST", new Vector4(8f, 8f, currentOffset.x, currentOffset.y));
                targetRenderer.SetPropertyBlock(propBlock);
            }

            // 2. Animate procedural drifting mist cloud bands
            if (mistBands != null)
            {
                for (int i = 0; i < mistBands.Length; i++)
                {
                    if (mistBands[i] == null) continue;

                    mistBands[i].position += mistDriftSpeeds[i] * Time.deltaTime;

                    // Subtle breathing pulse
                    float pulse = 1f + Mathf.Sin(Time.time * 0.4f + i) * 0.08f;
                    mistBands[i].localScale = new Vector3(mistBands[i].localScale.x, mistBands[i].localScale.y * pulse, mistBands[i].localScale.z);

                    // Loop back when drifting too far
                    float distFromOrigin = Vector3.Distance(mistBands[i].position, transform.position);
                    if (distFromOrigin > 180f)
                    {
                        mistBands[i].position = transform.position - mistDriftSpeeds[i].normalized * 120f + Vector3.up * Random.Range(15f, 35f);
                    }
                }
            }
        }

        private void CreateDriftingMistBands()
        {
            mistBands = new Transform[mistBandCount];
            mistDriftSpeeds = new Vector3[mistBandCount];

            var mistRoot = new GameObject("Drifting_Mist_Layer");
            mistRoot.transform.SetParent(transform, false);

            var mistShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            for (int i = 0; i < mistBandCount; i++)
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Quad);
                band.name = $"Mist_Band_{i}";
                band.transform.SetParent(mistRoot.transform, false);

                // Remove collider
                var col = band.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Position along the mountain ridge with varied elevations
                float t = (float)i / mistBandCount;
                band.transform.localPosition = new Vector3(
                    Random.Range(-50f, 50f),
                    Random.Range(18f, 38f),
                    Random.Range(-60f, 60f)
                );
                band.transform.localRotation = Quaternion.Euler(Random.Range(75f, 105f), Random.Range(0f, 360f), 0f);
                band.transform.localScale = new Vector3(Random.Range(45f, 75f), Random.Range(18f, 30f), 1f);

                var rend = band.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    var mat = new Material(mistShader);
                    mat.SetFloat("_Surface", 1f); // Transparent
                    mat.SetFloat("_Blend", 0f);
                    mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.renderQueue = 3050;
                    mat.SetColor("_BaseColor", new Color(0.72f, 0.78f, 0.76f, Random.Range(0.20f, 0.35f)));
                    mat.SetFloat("_Smoothness", 0.05f);
                    rend.sharedMaterial = mat;
                }

                mistBands[i] = band.transform;
                mistDriftSpeeds[i] = new Vector3(Random.Range(0.8f, 1.8f), Random.Range(-0.1f, 0.1f), Random.Range(0.4f, 1.2f));
            }
        }
    }
}
