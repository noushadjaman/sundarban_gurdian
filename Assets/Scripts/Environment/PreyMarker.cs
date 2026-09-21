using System;
using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Represents prey (Spotted Deer) in the Sundarban food chain for Level 3 (Tiger).
    /// Highlights that deforestation destroys prey populations, driving tigers to starve or venture into human settlements.
    /// </summary>
    public class PreyMarker : MonoBehaviour
    {
        [Header("Food Value")]
        public float healAmount = 50f;
        public string preyName = "Spotted Deer (চিত্রা হরিণ)";

        public static event Action<PreyMarker> PreyConsumed;

        private bool isConsumed = false;

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed) return;

            // Only Tiger consumes prey in Level 3
            var health = other.GetComponent<SpeciesHealth>();
            if (health != null && other.name.ToLower().Contains("tiger"))
            {
                isConsumed = true;
                health.EatFood(healAmount);
                PreyConsumed?.Invoke(this);

                // Small celebratory pulse / pop
                gameObject.SetActive(false);
            }
        }
    }
}
