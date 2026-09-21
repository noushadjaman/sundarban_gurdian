using System.Collections;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Represents banned monofilament current nets (কারেন্ট জাল) illegally set across the river channels.
    /// Can be sliced and destroyed using the Net-cutter Knife (জাল কাটার ছুরি) tool.
    /// </summary>
    public class IllegalNetObstacle : UsableObstacle
    {
        [Header("Damage & Entanglement")]
        public float hitDamage = 30f;
        public float knockbackDistance = 4f;

        [Header("Net Animation")]
        public bool bobbingInCurrent = true;
        public float bobFrequency = 1.5f;
        public float bobAmplitude = 0.15f;

        private Vector3 initialPos;
        private Collider netCollider;

        public IllegalNetObstacle()
        {
            requiredTool = ToolType.NetCutter;
            obstacleNameEn = "Illegal Fishing Net";
            obstacleDescriptionEn = "Cut and remove the illegal net with Net Cutter.";
            obstacleNameBn = "নিষিদ্ধ কারেন্ট জাল (Illegal Current Net)";
            obstacleDescription = "জাল কাটার ছুরি দিয়ে জাল কেটে জলপথ উন্মুক্ত করুন।";
            interactionRadius = 5.5f;
        }

        private void Awake()
        {
            requiredTool = ToolType.NetCutter;
            obstacleNameEn = "Illegal Fishing Net";
            obstacleDescriptionEn = "Cut and remove the illegal net with Net Cutter.";
            obstacleNameBn = "নিষিদ্ধ কারেন্ট জাল (Illegal Current Net)";
            obstacleDescription = "জাল কাটার ছুরি দিয়ে জাল কেটে জলপথ উন্মুক্ত করুন।";
            interactionRadius = 5.5f;

            netCollider = GetComponent<Collider>();
            if (netCollider != null) netCollider.isTrigger = true;
        }

        private void Start()
        {
            initialPos = transform.position;
        }

        private void Update()
        {
            if (isCleared) return;

            if (bobbingInCurrent)
            {
                float offset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                transform.position = initialPos + new Vector3(0f, offset, 0f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCleared) return;

            var health = other.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                health.TakeDamage(hitDamage);
                HudEvents.RaiseEnteredHazardZone("high", 100f, "% নিষিদ্ধ কারেন্ট জাল (Illegal Current Net)");

                // Knock player back slightly downstream
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.position -= other.transform.forward * knockbackDistance;
                }
                else
                {
                    other.transform.position -= other.transform.forward * knockbackDistance;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SpeciesHealth>() != null)
            {
                HudEvents.RaiseExitedHazardZone();
            }
        }

        protected override void OnClearedVisuals()
        {
            if (netCollider != null) netCollider.enabled = false;
            HudEvents.RaiseExitedHazardZone();
            StartCoroutine(SinkAndDestroyCoroutine());
        }

        private IEnumerator SinkAndDestroyCoroutine()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos - new Vector3(0f, 3.5f, 0f);
            Vector3 startScale = transform.localScale;

            while (elapsed < 1.2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1.2f;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, new Vector3(startScale.x, 0.1f, startScale.z), t);
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
