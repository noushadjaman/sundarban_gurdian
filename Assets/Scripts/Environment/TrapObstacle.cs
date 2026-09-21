using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Illegal poacher snare / jaw trap placed across Sundarban wildlife corridors.
    /// Can be dismantled and neutralized using the Trap Destroyer (ফাঁদ ধ্বংসকারী) tool.
    /// </summary>
    public class TrapObstacle : UsableObstacle
    {
        [Header("Trap Settings")]
        public float trapDamage = 35f;

        private Collider trapCollider;

        public TrapObstacle()
        {
            requiredTool = ToolType.TrapDestroyer;
            obstacleNameEn = "Poacher Wire Snare";
            obstacleDescriptionEn = "Dismantle the poacher trap with Trap Tool.";
            obstacleNameBn = "শিকারির অবৈধ ফাঁদ (Poacher Wire Snare)";
            obstacleDescription = "ফাঁদ ধ্বংসকারী যন্ত্র দিয়ে তারের ফাঁদ নিষ্ক্রিয় করুন।";
            interactionRadius = 4.5f;
        }

        private void Awake()
        {
            requiredTool = ToolType.TrapDestroyer;
            obstacleNameEn = "Poacher Wire Snare";
            obstacleDescriptionEn = "Dismantle the poacher trap with Trap Tool.";
            obstacleNameBn = "শিকারির অবৈধ ফাঁদ (Poacher Wire Snare)";
            obstacleDescription = "ফাঁদ ধ্বংসকারী যন্ত্র দিয়ে তারের ফাঁদ নিষ্ক্রিয় করুন।";
            interactionRadius = 4.5f;

            trapCollider = GetComponent<Collider>();
            if (trapCollider != null) trapCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCleared) return;

            var health = other.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                health.TakeDamage(trapDamage);
                HudEvents.RaiseEnteredHazardZone("high", 100f, "% শিকারির অবৈধ ফাঁদ (Poacher Trap)");

                // Trap sprung visual
                transform.localScale = new Vector3(transform.localScale.x * 0.85f, transform.localScale.y * 0.25f, transform.localScale.z * 0.85f);
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
            // Dismantle trap: disable collider, flatten, change color to neutralized grey/rust
            if (trapCollider != null) trapCollider.enabled = false;

            transform.localScale = new Vector3(transform.localScale.x * 1.3f, 0.02f, transform.localScale.z * 1.3f);
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.45f, 0.45f, 0.45f, 0.65f); // dismantled grey metal
                mat.SetFloat("_Metallic", 0.1f);
                rend.sharedMaterial = mat;
            }

            HudEvents.RaiseExitedHazardZone();
        }
    }
}
