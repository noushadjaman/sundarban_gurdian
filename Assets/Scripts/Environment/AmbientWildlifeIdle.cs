using System.Collections;
using UnityEngine;

namespace SundarbanGuardian.Environment
{
    public enum WildlifeType
    {
        Bird,
        Cobra,
        Crocodile,
        WildDeer,
        WildDog,
        FishSchool
    }

    /// <summary>
    /// Lightweight procedural ambient idle animation for decorated Sundarban wildlife.
    /// Brings the mangrove forest to life with biological micro-movements:
    /// - Birds twitching their heads and scanning the canopy
    /// - Cobras swaying gently in shade
    /// - Saltwater crocodiles breathing slowly on sunlit mudflats
    /// - Wild spotted deer grazing and staying vigilant
    /// - Dholes (wild dogs) sniffing the breeze along trails
    /// - River fish undulating smoothly in tidal river currents
    /// </summary>
    [SelectionBase]
    public class AmbientWildlifeIdle : MonoBehaviour
    {
        public WildlifeType wildlifeType = WildlifeType.WildDeer;

        [Header("Movement & Animation")]
        public float animSpeed = 1.0f;
        public float animMagnitude = 1.0f;

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private float seed;
        private float nextTwitchTime;
        private Quaternion targetTwitchRot;

        private void Awake()
        {
            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
            seed = Random.Range(0f, 100f);
            nextTwitchTime = Time.time + Random.Range(1f, 3f);
            targetTwitchRot = initialLocalRot;
        }

        private void Update()
        {
            float t = Time.time * animSpeed + seed;

            switch (wildlifeType)
            {
                case WildlifeType.Bird:
                    UpdateBirdIdle(t);
                    break;

                case WildlifeType.Cobra:
                    UpdateCobraIdle(t);
                    break;

                case WildlifeType.Crocodile:
                    UpdateCrocodileIdle(t);
                    break;

                case WildlifeType.WildDeer:
                    UpdateDeerIdle(t);
                    break;

                case WildlifeType.WildDog:
                    UpdateDogIdle(t);
                    break;

                case WildlifeType.FishSchool:
                    UpdateFishIdle(t);
                    break;
            }
        }

        private void UpdateBirdIdle(float t)
        {
            // Rapid discrete head/body twitches typical of wild kingfishers/songbirds
            if (Time.time > nextTwitchTime)
            {
                float randomYaw = Random.Range(-35f, 35f);
                float randomPitch = Random.Range(-10f, 8f);
                targetTwitchRot = initialLocalRot * Quaternion.Euler(randomPitch, randomYaw, 0f);
                nextTwitchTime = Time.time + Random.Range(1.8f, 4.5f);
            }

            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetTwitchRot, 220f * Time.deltaTime);

            // Subtle breathing and perching bob
            float bob = Mathf.Sin(t * 3.5f) * 0.008f * animMagnitude;
            transform.localPosition = initialLocalPos + Vector3.up * bob;
        }

        private void UpdateCobraIdle(float t)
        {
            // Hypnotic gentle sinusoidal sway of the reared hood
            float swayYaw = Mathf.Sin(t * 1.2f) * 12f * animMagnitude;
            float swayRoll = Mathf.Cos(t * 0.8f) * 4f * animMagnitude;
            float breathing = Mathf.Sin(t * 2.2f) * 0.006f;

            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, swayYaw, swayRoll);
            transform.localPosition = initialLocalPos + Vector3.up * breathing;
        }

        public bool isSwimmingInRiver = false;

        private void UpdateCrocodileIdle(float t)
        {
            if (isSwimmingInRiver)
            {
                // Fluid river swimming motion: lateral tail sway and water current bobbing
                float swimTail = Mathf.Sin(t * 1.6f) * 4.5f * animMagnitude;
                float waterBob = Mathf.Sin(t * 2.0f) * 0.015f * animMagnitude;
                transform.localPosition = initialLocalPos + Vector3.up * waterBob;
                transform.localRotation = initialLocalRot * Quaternion.Euler(0f, swimTail, 0f);
            }
            else
            {
                // Very slow, heavy breathing expansion on mudbank
                float breathCycle = Mathf.Sin(t * 0.65f) * 0.015f * animMagnitude;
                float tailSway = Mathf.Sin(t * 0.35f) * 1.5f;

                transform.localPosition = initialLocalPos + Vector3.up * breathCycle;
                transform.localRotation = initialLocalRot * Quaternion.Euler(0f, tailSway, 0f);
            }
        }

        private void UpdateDeerIdle(float t)
        {
            // Grazing dip and vigilance head raise
            float grazeAngle = (Mathf.Sin(t * 0.8f) > 0.3f) ? -8f : 3f;
            float breathe = Mathf.Sin(t * 2.0f) * 0.01f * animMagnitude;
            float tailFlick = (Mathf.Sin(t * 4f) > 0.85f) ? 3f : 0f;

            transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRot * Quaternion.Euler(grazeAngle, tailFlick, 0f), Time.deltaTime * 2.5f);
            transform.localPosition = initialLocalPos + Vector3.up * breathe;
        }

        private void UpdateDogIdle(float t)
        {
            // Alert standing posture with attentive scanning
            if (Time.time > nextTwitchTime)
            {
                float randomYaw = Random.Range(-25f, 25f);
                targetTwitchRot = initialLocalRot * Quaternion.Euler(0f, randomYaw, 0f);
                nextTwitchTime = Time.time + Random.Range(2.5f, 5.0f);
            }

            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetTwitchRot, 80f * Time.deltaTime);

            float panting = Mathf.Sin(t * 4.0f) * 0.008f * animMagnitude;
            transform.localPosition = initialLocalPos + Vector3.up * panting;
        }

        private void UpdateFishIdle(float t)
        {
            // Fluid sinusoidal swimming undulation and current drift
            float swimYaw = Mathf.Sin(t * 3.2f) * 14f * animMagnitude;
            float swimRoll = Mathf.Cos(t * 2.0f) * 6f * animMagnitude;
            float currentDriftX = Mathf.Sin(t * 0.8f) * 0.25f;
            float currentDriftY = Mathf.Cos(t * 1.1f) * 0.08f;

            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, swimYaw, swimRoll);
            transform.localPosition = initialLocalPos + new Vector3(currentDriftX, currentDriftY, 0f);
        }
    }
}
