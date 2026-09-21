using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Mechanized trawler/boat obstacle in the river that threatens aquatic life (Dolphin/Hilsha).
    /// Represents human motorized vessel traffic causing acoustic disruption and propeller strikes.
    /// </summary>
    public class BoatThreat : MonoBehaviour
    {
        [Header("Damage Settings")]
        public float damagePerSecond = 20f;
        public float directHitDamage = 35f;

        [Header("Patrol Movement")]
        public bool patrolRiver = true;
        public Vector3 patrolDirection = new Vector3(0f, 0f, 1f);
        public float patrolDistance = 25f;
        public float patrolSpeed = 2.5f;

        private Vector3 startPosition;
        private bool movingForward = true;

        private void Start()
        {
            startPosition = transform.position;
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (!patrolRiver) return;

            float currentDist = Vector3.Dot(transform.position - startPosition, patrolDirection.normalized);
            if (movingForward)
            {
                transform.position += patrolDirection.normalized * patrolSpeed * Time.deltaTime;
                if (currentDist >= patrolDistance) movingForward = false;
            }
            else
            {
                transform.position -= patrolDirection.normalized * patrolSpeed * Time.deltaTime;
                if (currentDist <= -patrolDistance * 0.2f) movingForward = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                health.TakeDamage(directHitDamage);
                HudEvents.RaiseEnteredHazardZone("high", 45.0f, "dB propeller noise");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var health = other.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                health.TakeDamage(damagePerSecond * Time.deltaTime);
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
