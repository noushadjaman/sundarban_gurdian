using System;
using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Collectible food in the Sundarban ecosystem (e.g., fresh grass shoots for Deer, algae for fish).
    /// Restores HP and visually conveys habitat richness.
    /// </summary>
    public class FoodItem : MonoBehaviour
    {
        [Header("Food Configuration")]
        public string targetSpecies = "deer"; // "deer", "hilsha", "all"
        public float healAmount = 30f;
        public string foodLabel = "Fresh Grass Shoots (কচি ঘাস)";

        public static event Action<FoodItem> FoodEaten;

        private bool consumed = false;

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed) return;

            var health = other.GetComponent<SpeciesHealth>();
            if (health == null) return;

            string objName = other.name.ToLower();
            bool matches = targetSpecies == "all" || objName.Contains(targetSpecies.ToLower());

            if (matches)
            {
                consumed = true;
                health.EatFood(healAmount);
                FoodEaten?.Invoke(this);
                gameObject.SetActive(false);
            }
        }
    }
}
