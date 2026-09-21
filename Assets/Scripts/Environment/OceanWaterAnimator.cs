using UnityEngine;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Animates ocean surface waves by continuously scrolling normal and base texture UV offsets.
    /// Creates realistic flowing marine currents for the Bay of Bengal ocean horizon.
    /// </summary>
    public class OceanWaterAnimator : MonoBehaviour
    {
        [Header("Wave Scroll Speeds")]
        public Vector2 bumpSpeed = new Vector2(0.015f, 0.025f);
        public Vector2 baseMapSpeed = new Vector2(0.008f, 0.012f);

        private Material instanceMat;
        private Vector2 currentBumpOffset;
        private Vector2 currentBaseOffset;

        private void Start()
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                instanceMat = rend.material;
            }
        }

        private void Update()
        {
            if (instanceMat == null) return;

            currentBumpOffset += bumpSpeed * Time.deltaTime;
            currentBaseOffset += baseMapSpeed * Time.deltaTime;

            if (instanceMat.HasProperty("_BumpMap"))
            {
                instanceMat.SetTextureOffset("_BumpMap", currentBumpOffset);
            }
            if (instanceMat.HasProperty("_BaseMap"))
            {
                instanceMat.SetTextureOffset("_BaseMap", currentBaseOffset);
            }
        }
    }
}
