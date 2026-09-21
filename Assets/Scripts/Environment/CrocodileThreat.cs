using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Saltwater Crocodile (লোনা পানির কুমির) threat in river crossings and tidal mudflats.
    /// Represents apex estuarine predators threatening crossing herbivores (Deer) and aquatic life (Dolphin/Hilsha).
    /// </summary>
    public class CrocodileThreat : MonoBehaviour
    {
        [Header("Combat Settings")]
        public float biteDamage = 40f;
        public float attackRadius = 6f;
        public float patrolSpeed = 1.8f;
        public Vector3 patrolOffset = new Vector3(0f, 0f, 15f);

        private Vector3 originPos;
        private bool movingForward = true;

        private void Start()
        {
            originPos = transform.position;
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            // Gentle cruising along riverbank or crossing
            float currentDist = Vector3.Dot(transform.position - originPos, patrolOffset.normalized);
            if (movingForward)
            {
                transform.position += patrolOffset.normalized * patrolSpeed * Time.deltaTime;
                if (currentDist >= patrolOffset.magnitude) movingForward = false;
            }
            else
            {
                transform.position -= patrolOffset.normalized * patrolSpeed * Time.deltaTime;
                if (currentDist <= 0f) movingForward = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponent<SpeciesHealth>();
            if (health != null)
            {
                health.TakeDamage(biteDamage);
                HudEvents.RaiseEnteredHazardZone("high", 80f, "% কুমিরের আক্রমণ (Crocodile Attack)");

                // Lunge slightly toward target
                Vector3 dir = (other.transform.position - transform.position).normalized;
                transform.forward = new Vector3(dir.x, 0f, dir.z);
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
