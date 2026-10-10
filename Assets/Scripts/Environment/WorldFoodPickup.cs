using System;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Self-contained 3D collectible food pickup in the Sundarbans.
    /// Features floating bobbing, warm golden beacon light, and restores +35 HP
    /// when player is hurt or stores as carried food rations if player has full health.
    /// </summary>
    public class WorldFoodPickup : MonoBehaviour
    {
        public float healAmount = 35f;
        public float collectionRadius = 2.0f;
        public float bobSpeed = 2.5f;
        public float bobHeight = 0.12f;
        public float rotateSpeed = 65f;

        private Vector3 startPos;
        private bool collected = false;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            if (collected) return;

            // 1. Floating animation
            float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + new Vector3(0f, y, 0f);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // 2. Proximity check fallback for player
            var player = PlayerNutrition.Instance != null ? PlayerNutrition.Instance.gameObject : GameObject.Find("Tiger");
            if (player != null && player.activeInHierarchy)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= collectionRadius)
                {
                    Collect(player);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (other.name.Contains("Tiger") || other.name.Contains("Deer") || other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }

        public void Collect(GameObject player)
        {
            if (collected) return;
            collected = true;

            var health = player.GetComponent<SpeciesHealth>() ?? player.GetComponentInParent<SpeciesHealth>();
            var nutrition = player.GetComponent<PlayerNutrition>() ?? player.GetComponentInParent<PlayerNutrition>();

            if (health != null && health.currentHealth < health.maxHealth)
            {
                // Eat immediately to regain HP
                health.EatFood(healAmount);
                HudEvents.RaiseEnvironmentalRestoration($"🍗 +{healAmount:F0} HP — Ate Fresh Keora Fruit! Health: {health.currentHealth:F0}/{health.maxHealth:F0}");
            }
            else if (nutrition != null)
            {
                // Add to emergency food inventory
                nutrition.AddFood(1);
                HudEvents.RaiseEnvironmentalRestoration("🍗 Stored Keora Fruit (+1 Food Ration in Backpack)!");
            }

            Destroy(gameObject, 0.05f);
        }
    }
}
