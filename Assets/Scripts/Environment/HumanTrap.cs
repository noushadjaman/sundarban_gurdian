using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Illegal poacher snare / jaw trap set in the forest.
    /// Represents illegal wildlife poaching and tiger/deer traps in the Sundarbans.
    /// </summary>
    public class HumanTrap : MonoBehaviour
    {
        [Header("Trap Settings")]
        public float trapDamage = 35f;
        public string trapName = "Poacher Wire Snare (শিকারির তারের ফাঁদ)";

        private bool triggered = false;

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            var health = other.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                triggered = true;
                health.TakeDamage(trapDamage);
                HudEvents.RaiseEnteredHazardZone("high", 100f, "% শিকারির অবৈধ ফাঁদ (Poacher Trap)");

                // Snare snap visual effect: slightly scale down or tilt
                transform.localScale = new Vector3(transform.localScale.x * 0.85f, transform.localScale.y * 0.3f, transform.localScale.z * 0.85f);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SpeciesHealth>() != null)
            {
                HudEvents.RaiseExitedHazardZone();
            }
        }
    }
}
