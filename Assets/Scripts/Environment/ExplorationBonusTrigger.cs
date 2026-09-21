using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Trigger placed at the end of exploration side-branches (khals).
    /// Grants bonus educational fact popups and health/research rewards.
    /// </summary>
    public class ExplorationBonusTrigger : MonoBehaviour
    {
        [Header("Bonus Content")]
        public string bonusTitle = "গোপন খাল আবিষ্কার! (Secret Khal Discovered)";
        [TextArea(3, 6)]
        public string bonusFactBn = "সুন্দরবনের শ্বাসমূলীয় বনাঞ্চল পৃথিবীর অন্যতম বৃহৎ প্রাকৃতিক কার্বন ভাণ্ডার (Blue Carbon)। প্রতি হেক্টর ম্যানগ্রোভ বন সাধারণ বনের তুলনায় ৩-৫ গুণ বেশি কার্বন ধরে রাখতে পারে।";
        public string citation = "UNESCO World Heritage & Forest Dept Bangladesh";
        public float healAmount = 40f;
        public string pickupItemName = "গবেষণা সরঞ্জাম (Research Tool Kit)";

        private bool hasTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            // Check if player
            var health = other.GetComponent<SpeciesHealth>() ?? other.GetComponentInParent<SpeciesHealth>();
            if (health != null || other.CompareTag("Player") || other.name.Contains("Tiger") || other.name.Contains("Deer") || other.name.Contains("Dolphin") || other.name.Contains("Hilsha"))
            {
                hasTriggered = true;

                if (health != null && healAmount > 0f)
                {
                    health.Heal(healAmount);
                }

                // Show notification via HudEvents if available
                HudEvents.RaiseEnteredHazardZone("low", 100f, bonusFactBn);

                // Play subtle visual feedback - disable mesh or change color
                var rend = GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.2f, 0.8f, 0.4f, 0.4f);
                }

                Debug.Log($"[ExplorationBonusTrigger] Discovered: {bonusTitle} - Healed {healAmount} HP");
            }
        }
    }
}
