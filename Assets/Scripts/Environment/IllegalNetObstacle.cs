using System.Collections;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Environment
{
    public enum RiverNetType
    {
        MonofilamentGillnet, // কারেন্ট জাল (Curtain net strung across channel with buoys)
        AccordionBoxTrap     // চাই / বেহুন্দি জাল (Collapsible rectangular tunnel trap on riverbed)
    }

    /// <summary>
    /// Represents banned monofilament gillnets (কারেন্ট জাল) and accordion tunnel traps (চাই / বেহুন্দি জাল)
    /// illegally set across river migration channels.
    /// Can be sliced with the Net-Cutter Knife tool, jumped over by breaching dolphins, or dodged through boat gaps!
    /// </summary>
    public class IllegalNetObstacle : UsableObstacle
    {
        [Header("Net Configuration")]
        public RiverNetType netType = RiverNetType.MonofilamentGillnet;

        [Header("Damage & Entanglement")]
        public float hitDamage = 25f;
        public float knockbackDistance = 3.5f;

        [Header("Net Animation")]
        public bool bobbingInCurrent = true;
        public float bobFrequency = 1.35f;
        public float bobAmplitude = 0.08f;

        private Vector3 initialPos;
        private Collider netCollider;
        private float lastBreachTime = -10f;

        public IllegalNetObstacle()
        {
            requiredTool = ToolType.NetCutter;
            obstacleNameEn = "Illegal Fishing Net";
            obstacleDescriptionEn = "Cut and remove the illegal net with Net Cutter Knife.";
            obstacleNameBn = "নিষিদ্ধ কারেন্ট জাল (Illegal Current Net)";
            obstacleDescription = "জাল কাটার ছুরি দিয়ে জাল কেটে জলপথ উন্মুক্ত করুন।";
            interactionRadius = 6.0f;
        }

        private void Awake()
        {
            requiredTool = ToolType.NetCutter;
            if (netType == RiverNetType.AccordionBoxTrap)
            {
                obstacleNameEn = "Banned China Box Net";
                obstacleDescriptionEn = "Destroy the illegal riverbed tunnel trap.";
                obstacleNameBn = "নিষিদ্ধ চাই / চায়না জাল (Box Trap Net)";
                obstacleDescription = "জাল কাটার ছুরি দিয়ে নদী তলদেশের ফাঁদ ধ্বংস করুন।";
            }
            else
            {
                obstacleNameEn = "Illegal Monofilament Gillnet";
                obstacleDescriptionEn = "Cut the illegal monofilament curtain with Net Cutter.";
                obstacleNameBn = "নিষিদ্ধ কারেন্ট জাল (Current Gillnet)";
                obstacleDescription = "জাল কাটার ছুরি দিয়ে জাল কেটে জলপথ উন্মুক্ত করুন।";
            }
            interactionRadius = 6.0f;

            netCollider = GetComponent<Collider>();
            if (netCollider != null) netCollider.isTrigger = true;
        }

        private void Start()
        {
            initialPos = transform.position;
        }

        private void Update()
        {
            if (isCleared) return;

            if (bobbingInCurrent)
            {
                float offset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                transform.position = initialPos + new Vector3(0f, offset, 0f);
            }
        }

        private float lastDamageTime = -10f;
        private float lastBlockToastTime = -10f;

        private void OnTriggerEnter(Collider other)
        {
            HandleNetCollision(other);
        }

        private void OnTriggerStay(Collider other)
        {
            HandleNetCollision(other);
        }

        private void HandleNetCollision(Collider other)
        {
            if (isCleared) return;

            var health = other.GetComponent<SpeciesHealth>() ?? other.GetComponentInParent<SpeciesHealth>();
            if (health == null) return;

            var dsc = other.GetComponent<SundarbanGuardian.Player.DolphinSwimController>() ?? other.GetComponentInParent<SundarbanGuardian.Player.DolphinSwimController>();
            bool isJumping = (dsc != null && dsc.isLeaping) || other.transform.position.y > 0.08f;

            if (isJumping)
            {
                // Jumping allows moving in front, but causes 50 HP damage!
                if (Time.time - lastDamageTime > 2.0f)
                {
                    lastDamageTime = Time.time;
                    health.TakeDamage(50f);
                    HudEvents.RaiseEnvironmentalRestoration("⚠️ Tangled in Fishing Net while leaping! (-50 HP)");
                }
            }
            else
            {
                // Touching net blocks dolphin from moving in front
                var rb = other.GetComponent<Rigidbody>() ?? other.GetComponentInParent<Rigidbody>();
                if (rb != null)
                {
                    // River migration heads south (-Z), so block moving in front past net's Z
                    float safeZ = transform.position.z + 1.25f;
                    if (rb.position.z < safeZ)
                    {
                        rb.position = new Vector3(rb.position.x, rb.position.y, safeZ);
                        if (rb.linearVelocity.z < 0f)
                        {
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
                        }
                    }
                }
                else
                {
                    float safeZ = transform.position.z + 1.25f;
                    if (other.transform.position.z < safeZ)
                    {
                        other.transform.position = new Vector3(other.transform.position.x, other.transform.position.y, safeZ);
                    }
                }

                if (Time.time - lastBlockToastTime > 2.5f)
                {
                    lastBlockToastTime = Time.time;
                    HudEvents.RaiseEnvironmentalRestoration("⚠️ Fishing Net blocking channel! Cut with Knife (Hit) or Leap over!");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SpeciesHealth>() != null ||
                (other.transform.parent != null && other.transform.parent.GetComponent<SpeciesHealth>() != null))
            {
                HudEvents.RaiseExitedHazardZone();
            }
        }

        public bool TryInteract(GameObject interactor = null)
        {
            Backpack bp = interactor != null ? interactor.GetComponent<Backpack>() : null;
            return Clear(bp);
        }

        public void DestroyWithKnife(Backpack bp)
        {
            if (isCleared) return;

            if (bp != null)
            {
                if (bp.GetCount(ToolType.NetCutter) > 0)
                {
                    bp.ConsumeTool(ToolType.NetCutter, 1);
                    Clear(bp);
                }
                else
                {
                    HudEvents.RaiseEnvironmentalRestoration("⚠️ Need a Knife to cut the fishing net!");
                }
            }
            else
            {
                Clear(null);
            }
        }

        protected override void OnClearedVisuals()
        {
            if (netCollider != null) netCollider.enabled = false;
            HudEvents.RaiseExitedHazardZone();

            // V2 exact popup requirement
            HudEvents.RaiseEnvironmentalRestoration("Fishing Net Destroyed!");

            StartCoroutine(SinkAndDestroyCoroutine());
        }

        private IEnumerator SinkAndDestroyCoroutine()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos - new Vector3(0f, 3.5f, 0f);
            Vector3 startScale = transform.localScale;

            while (elapsed < 1.4f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1.4f;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, new Vector3(startScale.x, 0.05f, startScale.z), t);
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
