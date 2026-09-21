using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Safe sanctuary glade in the Sundarban forest where wildlife can rest, graze, and recover HP.
    /// Represents protected, undisturbed forest biomes with healthy NDVI canopy and abundant fresh flora.
    /// </summary>
    public class HealingZone : MonoBehaviour
    {
        [Header("Healing Rate")]
        [Tooltip("HP recovered per second while inside this safe green haven")]
        public float healPerSecond = 8f;
        public string sanctuaryName = "Kotka Wildlife Sanctuary (কটকা অভয়ারণ্য)";

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            var health = other.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                health.Heal(healPerSecond * Time.deltaTime);
            }
        }
    }
}
