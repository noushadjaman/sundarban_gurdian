using System;
using UnityEngine;
using UnityEngine.UI;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Player Nutrition and Emergency HP Recovery system.
    /// Monitors character health: when HP is drained in danger zones, the character
    /// can eat food (manual [E] or HUD [EAT 🍗] button) or automatically consume emergency rations
    /// when HP drops below 45% to save the character's life.
    /// </summary>
    public class PlayerNutrition : MonoBehaviour
    {
        [Header("Food Inventory")]
        public int foodCount = 1; // Starts with 1 emergency food ration (Harder survival)
        public float healPerFood = 25f;

        [Header("Emergency Auto-Eat Settings")]
        [Tooltip("HP ratio threshold below which food is automatically consumed")]
        public float autoEatThreshold = 0.25f;
        public float autoEatCooldown = 3.0f;
        private float nextAutoEatTime = 0f;

        private SpeciesHealth health;
        private static PlayerNutrition localInstance;
        public static PlayerNutrition Instance => localInstance;

        public static event Action<int> OnFoodCountChanged;

        private void Awake()
        {
            localInstance = this;
            health = GetComponent<SpeciesHealth>();
        }

        private void OnEnable()
        {
            localInstance = this;
        }

        private void Start()
        {
            if (health == null) health = GetComponent<SpeciesHealth>();
            OnFoodCountChanged?.Invoke(foodCount);
            UpdateHudBadge();
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy && localInstance != this)
            {
                localInstance = this;
            }

            // Keyboard shortcut [E] or [H] to Eat Food (supporting both New Input System & legacy)
            bool eatPressed = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && (kb.eKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame))
                {
                    eatPressed = true;
                }
            }
            catch (Exception) { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.H))
                {
                    eatPressed = true;
                }
            }
            catch (Exception) { }
#endif

            if (eatPressed)
            {
                TryEatFood();
            }

            // Emergency Auto-Eat: if character HP is dangerously low (< 45%) and character has food
            if (health != null && health.currentHealth > 0f && health.currentHealth < (health.maxHealth * autoEatThreshold) && foodCount > 0)
            {
                if (Time.time >= nextAutoEatTime)
                {
                    nextAutoEatTime = Time.time + autoEatCooldown;
                    AutoConsumeEmergencyFood();
                }
            }
        }

        public bool TryEatFood()
        {
            if (health == null) health = GetComponent<SpeciesHealth>();
            if (health == null || health.currentHealth <= 0f) return false;

            if (foodCount <= 0)
            {
                HudEvents.RaiseEnvironmentalRestoration("⚠️ No food in inventory! Collect Keora fruits in the forest.");
                return false;
            }

            if (health.currentHealth >= health.maxHealth)
            {
                HudEvents.RaiseEnvironmentalRestoration("HP is already FULL! (স্বাস্থ্য সম্পূর্ণ)");
                return false;
            }

            foodCount--;
            health.EatFood(healPerFood);
            OnFoodCountChanged?.Invoke(foodCount);
            UpdateHudBadge();

            HudEvents.RaiseEnvironmentalRestoration($"🍗 +{healPerFood:F0} HP — Ate Food Ration! Health: {health.currentHealth:F0}/{health.maxHealth:F0}");
            Debug.Log($"[PlayerNutrition] Ate food! Remaining rations: {foodCount}, Current HP: {health.currentHealth:F0}/{health.maxHealth:F0}");
            return true;
        }

        private void AutoConsumeEmergencyFood()
        {
            foodCount--;
            health.EatFood(healPerFood);
            OnFoodCountChanged?.Invoke(foodCount);
            UpdateHudBadge();

            HudEvents.RaiseEnvironmentalRestoration($"⚠️ CRITICAL HP! Auto-ate Keora Fruit (+{healPerFood:F0} HP)! Health: {health.currentHealth:F0}/{health.maxHealth:F0}");
            Debug.Log($"[PlayerNutrition] Emergency auto-eat triggered! Restored +{healPerFood:F0} HP, Food left: {foodCount}");
        }

        public void AddFood(int amount = 1)
        {
            if (amount <= 0) return;
            foodCount += amount;
            OnFoodCountChanged?.Invoke(foodCount);
            UpdateHudBadge();
            Debug.Log($"[PlayerNutrition] Collected {amount}x Food (Total: {foodCount})");
        }

        public void UpdateHudBadge()
        {
            var badgeObj = GameObject.Find("FoodBadge");
            if (badgeObj != null)
            {
                var txt = badgeObj.GetComponent<Text>();
                if (txt != null) txt.text = foodCount.ToString();
            }
        }
    }
}
