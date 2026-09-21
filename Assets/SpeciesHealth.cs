using System;
using UnityEngine;

namespace SundarbanGuardian.Core
{
    public class SpeciesHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 200f;
        public float currentHealth { get; private set; }

        [Header("Travel & Regen")]
        [Tooltip("HP recovered per second while moving in safe environments")]
        public float travelHealRate = 1.0f;

        [Header("Starvation (Tiger Food Chain)")]
        [Tooltip("Passive HP loss rate if starving (e.g. Tiger in deforested zones)")]
        public float starvationDrainRate = 0f;

        public event Action<float> HealthChanged;
        public event Action Died;

        private Vector3 lastPosition;
        private float lastDamageTime = -10f;

        private void Awake()
        {
            currentHealth = maxHealth;
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (currentHealth <= 0f) return;

            // Passive starvation drain if enabled (e.g., Tiger without food)
            if (starvationDrainRate > 0f)
            {
                TakeDamage(starvationDrainRate * Time.deltaTime);
            }

            // Travel healing: when moving and safe (not taking damage in the last 1.5s)
            float movedDist = Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;

            if (movedDist > 0.05f * Time.deltaTime && (Time.time - lastDamageTime) > 1.5f)
            {
                if (currentHealth < maxHealth)
                {
                    Heal(travelHealRate * Time.deltaTime);
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f) return;

            lastDamageTime = Time.time;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth / maxHealth);

            if (currentHealth <= 0f)
            {
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth / maxHealth);
        }

        public void EatFood(float healAmount)
        {
            Heal(healAmount);
        }
    }
}
