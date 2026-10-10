using System.Collections;
using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// 3D Gunman / Poacher threat in the Sundarbans forest hunting Deer and Tigers.
    /// Aims a scoped rifle with a red laser sight at any deer or tiger entering detection range.
    /// The player must flee outside the range within 5.0 seconds, otherwise the gunman fires dealing 50 HP damage!
    /// A Bengal Tiger can also sneak close and pounce on the poacher to disarm and eliminate him.
    /// </summary>
    public class GunmanThreat : MonoBehaviour
    {
        [Header("Targeting & Threat Settings")]
        public float detectionRadius = 24.0f;
        public float shotCooldown = 5.0f;
        public float shotDamage = 50.0f;
        public float aimAcquisitionTime = 1.2f;
        public float turnSpeedDeg = 120.0f;

        [Header("Tiger Counter-Attack")]
        public bool canBeEliminatedByTiger = true;
        public float tigerTakedownRadius = 3.6f;

        [Header("Visual Components")]
        public Transform rifleTransform;
        public Transform muzzleTransform;
        public LineRenderer laserLine;
        public Light muzzleLight;

        [Header("Patrol & Idle Scanning")]
        public float scanAngleRange = 55.0f;
        public float scanSpeed = 0.8f;

        private float shotTimer = 0f;
        private float inRangeDuration = 0f;
        private bool isPlayerInRange = false;
        private bool isNeutralized = false;
        private Quaternion initialRotation;
        private AudioSource audioSource;
        private AudioClip gunshotClip;
        private AudioClip lockWarningClip;
        private int lastAnnouncedSecond = -1;
        private GameObject laserDotGo;

        private void Awake()
        {
            initialRotation = transform.rotation;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
                audioSource.minDistance = 5f;
                audioSource.maxDistance = 50f;
            }

            gunshotClip = GenerateGunshotAudioClip();
            lockWarningClip = GenerateLockWarningAudioClip();

            CreateLaserImpactDot();
        }

        private void CreateLaserImpactDot()
        {
            laserDotGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            laserDotGo.name = "Laser_Impact_Dot";
            laserDotGo.transform.SetParent(transform, true);
            laserDotGo.transform.localScale = Vector3.one * 0.12f;
            Destroy(laserDotGo.GetComponent<Collider>());

            var dotRen = laserDotGo.GetComponent<MeshRenderer>();
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var dotMat = new Material(urpUnlit);
            dotMat.SetColor("_BaseColor", Color.red);
            dotRen.sharedMaterial = dotMat;

            laserDotGo.SetActive(false);
        }

        private void Start()
        {
            shotTimer = shotCooldown;
            if (laserLine != null)
            {
                laserLine.enabled = false;
            }
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }
        }

        private void Update()
        {
            if (isNeutralized) return;

            GameObject target = FindActiveTarget();
            if (target == null)
            {
                IdlePatrolScan();
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float dist = toTarget.magnitude;

            // Check if animal is within hunting range
            if (dist <= detectionRadius)
            {
                // Tiger counter-takedown check
                if (canBeEliminatedByTiger && dist <= tigerTakedownRadius)
                {
                    bool isTiger = target.name.ToLower().Contains("tiger");
                    if (isTiger)
                    {
                        EliminateByTiger(target);
                        return;
                    }
                }

                HandleTargetLocked(target, toTarget, dist);
            }
            else
            {
                if (isPlayerInRange)
                {
                    HandlePlayerEscapedRange();
                }
                IdlePatrolScan();
            }
        }

        private void HandleTargetLocked(GameObject target, Vector3 toTarget, float dist)
        {
            if (!isPlayerInRange)
            {
                isPlayerInRange = true;
                inRangeDuration = 0f;
                shotTimer = shotCooldown; // Give player initial 5.0 seconds to react and flee!
                lastAnnouncedSecond = Mathf.CeilToInt(shotCooldown);
                HudEvents.RaiseEnteredHazardZone("high", 100f, "⚠️ POACHER IN RANGE! FLEE IMMEDIATELY! (শিকারি বন্দুকধারী)");

                if (audioSource != null && lockWarningClip != null)
                {
                    audioSource.PlayOneShot(lockWarningClip, 0.75f);
                }
            }

            inRangeDuration += Time.deltaTime;

            // Aim rifle and body towards target
            Vector3 aimDir = toTarget.normalized;
            Vector3 horizontalAim = new Vector3(aimDir.x, 0f, aimDir.z).normalized;
            if (horizontalAim.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(horizontalAim, Vector3.up), turnSpeedDeg * Time.deltaTime);
            }

            Vector3 targetCenter = target.transform.position + Vector3.up * 0.75f;

            if (rifleTransform != null)
            {
                rifleTransform.LookAt(targetCenter);
            }

            // Laser Sight Targeting Beam & Red Dot on target
            if (laserLine != null && muzzleTransform != null)
            {
                laserLine.enabled = true;
                Vector3 muzzlePos = muzzleTransform.position;

                laserLine.SetPosition(0, muzzlePos);
                laserLine.SetPosition(1, targetCenter);

                // Laser color pulses faster as shot time approaches!
                float timeRemaining = shotTimer;
                float pulse = Mathf.PingPong(Time.time * 7f, 1f);
                Color laserCol = timeRemaining < 1.6f ? Color.red : Color.Lerp(new Color(1f, 0.45f, 0f), Color.red, pulse);
                laserLine.startColor = laserCol;
                laserLine.endColor = laserCol;

                if (laserDotGo != null)
                {
                    laserDotGo.SetActive(true);
                    laserDotGo.transform.position = targetCenter;
                    float dotScale = (timeRemaining < 1.6f) ? 0.16f + 0.05f * pulse : 0.11f;
                    laserDotGo.transform.localScale = Vector3.one * dotScale;
                }
            }

            // Second-by-second countdown warning on HUD
            int currentSecond = Mathf.CeilToInt(shotTimer);
            if (currentSecond != lastAnnouncedSecond && currentSecond > 0 && currentSecond <= 4)
            {
                lastAnnouncedSecond = currentSecond;
                HudEvents.RaiseEnteredHazardZone("high", 100f, $"⚠️ POACHER LOCKING ON! FLEE WITHIN {currentSecond}s OR LOSE 50 HP!");
                if (audioSource != null && lockWarningClip != null)
                {
                    audioSource.pitch = 1.0f + (5 - currentSecond) * 0.15f;
                    audioSource.PlayOneShot(lockWarningClip, 0.6f);
                }
            }

            // Countdown to gunshot
            shotTimer -= Time.deltaTime;
            if (shotTimer <= 0f)
            {
                FireGunshot(target);
                shotTimer = shotCooldown; // Reset for next 5 seconds
                lastAnnouncedSecond = Mathf.CeilToInt(shotCooldown);
            }
        }

        private void HandlePlayerEscapedRange()
        {
            isPlayerInRange = false;
            shotTimer = shotCooldown;
            lastAnnouncedSecond = -1;

            if (laserLine != null) laserLine.enabled = false;
            if (laserDotGo != null) laserDotGo.SetActive(false);

            HudEvents.RaiseExitedHazardZone();

            if (inRangeDuration >= 0.8f)
            {
                HudEvents.RaiseEnvironmentalRestoration("✓ ESCAPED POACHER'S RIFLE RANGE! (+100 SPEEDRUN PTS)");
            }
            inRangeDuration = 0f;
        }

        private void FireGunshot(GameObject target)
        {
            // Play Audio
            if (audioSource != null && gunshotClip != null)
            {
                audioSource.pitch = Random.Range(0.92f, 1.08f);
                audioSource.PlayOneShot(gunshotClip, 1.0f);
            }

            // Visual Muzzle Flash
            StartCoroutine(MuzzleFlashRoutine());

            // Deal 50 HP Damage
            var health = target.GetComponent<SpeciesHealth>() ?? target.GetComponentInParent<SpeciesHealth>();
            if (health != null)
            {
                health.TakeDamage(shotDamage);
                HudEvents.RaiseEnteredHazardZone("high", 100f, $"⚠️ HIT BY GUNSHOT! -{shotDamage:F0} HP! FLEE RANGE!");
            }

            // Push target back from bullet impact
            var cc = target.GetComponent<CharacterController>();
            if (cc != null)
            {
                Vector3 impactDir = (target.transform.position - transform.position).normalized;
                impactDir.y = 0.2f;
                // Move player backwards slightly
                cc.Move(impactDir * 2.2f);
            }
            else
            {
                var rb = target.GetComponent<Rigidbody>() ?? target.GetComponentInParent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 impactDir = (target.transform.position - transform.position).normalized;
                    rb.AddForce(impactDir * 7f, ForceMode.Impulse);
                }
            }
        }

        private IEnumerator MuzzleFlashRoutine()
        {
            if (muzzleLight != null) muzzleLight.enabled = true;
            yield return new WaitForSeconds(0.09f);
            if (muzzleLight != null) muzzleLight.enabled = false;
        }

        private void IdlePatrolScan()
        {
            if (laserLine != null) laserLine.enabled = false;
            if (laserDotGo != null) laserDotGo.SetActive(false);

            // Oscillate yaw slowly scanning the forest
            float scanOffset = Mathf.Sin(Time.time * scanSpeed) * scanAngleRange;
            transform.rotation = initialRotation * Quaternion.Euler(0f, scanOffset, 0f);

            if (rifleTransform != null)
            {
                rifleTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        public void EliminateByTiger(GameObject tiger)
        {
            if (isNeutralized) return;
            isNeutralized = true;
            isPlayerInRange = false;

            if (laserLine != null) laserLine.enabled = false;
            if (laserDotGo != null) laserDotGo.SetActive(false);
            if (muzzleLight != null) muzzleLight.enabled = false;

            HudEvents.RaiseExitedHazardZone();
            HudEvents.RaiseEnvironmentalRestoration("★ POACHER DISARMED & ELIMINATED BY TIGER! (+350 PTS)");

            StartCoroutine(KnockdownAndDisarmRoutine());
        }

        private IEnumerator KnockdownAndDisarmRoutine()
        {
            // Drop rifle and fall backward
            if (rifleTransform != null)
            {
                rifleTransform.SetParent(null);
                var rRb = rifleTransform.gameObject.GetComponent<Rigidbody>() ?? rifleTransform.gameObject.AddComponent<Rigidbody>();
                rRb.AddForce(Vector3.up * 2f + transform.forward * -2f, ForceMode.Impulse);
            }

            float elapsed = 0f;
            Quaternion startRot = transform.rotation;
            Quaternion fallRot = startRot * Quaternion.Euler(75f, 0f, 0f); // Fall backward on mud
            Vector3 startPos = transform.position;

            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.6f;
                transform.rotation = Quaternion.Slerp(startRot, fallRot, t);
                transform.position = Vector3.Lerp(startPos, startPos - Vector3.up * 0.4f, t);
                yield return null;
            }

            yield return new WaitForSeconds(4f);

            // Fade out / disable after defeat
            float fadeElapsed = 0f;
            Vector3 endPos = transform.position;
            while (fadeElapsed < 1.5f)
            {
                fadeElapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(endPos, endPos - Vector3.up * 2f, fadeElapsed / 1.5f);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private GameObject FindActiveTarget()
        {
            // 1. Check nearby colliders within detection radius for Tiger or Deer
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius * 1.25f);
            GameObject closestAnimal = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.transform == transform || h.transform.IsChildOf(transform)) continue;

                string n = h.gameObject.name.ToLower();
                if (n.Contains("tiger") || n.Contains("deer") || h.CompareTag("Player"))
                {
                    // Check if it has SpeciesHealth or LandAnimalController
                    var health = h.GetComponent<SpeciesHealth>() ?? h.GetComponentInParent<SpeciesHealth>();
                    if (health != null && health.currentHealth > 0)
                    {
                        float d = Vector3.Distance(transform.position, h.transform.position);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            closestAnimal = health.gameObject;
                        }
                    }
                }
            }
            if (closestAnimal != null) return closestAnimal;

            // 2. Check active GameManager playerObject
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null && gm.playerObject != null && gm.playerObject.activeInHierarchy)
            {
                string name = gm.playerObject.name.ToLower();
                if (name.Contains("tiger") || name.Contains("deer"))
                {
                    return gm.playerObject;
                }
            }

            // 3. Fallback search by standard hierarchy names
            var tiger = GameObject.Find("Tiger");
            if (tiger != null && tiger.activeInHierarchy) return tiger;

            var deer = GameObject.Find("Deer");
            if (deer != null && deer.activeInHierarchy) return deer;

            return null;
        }

        private AudioClip GenerateGunshotAudioClip()
        {
            int sampleRate = 22050;
            float duration = 0.45f;
            int totalSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // 1. Initial sharp muzzle crack (white noise transient)
                float initialCrack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 85f);

                // 2. Low-frequency barrel resonance thump (120 Hz down to 40 Hz)
                float freq = Mathf.Lerp(120f, 40f, t / duration);
                float thump = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 14f);

                // 3. Forest echo reflection (delayed noise tail)
                float echo = 0f;
                if (t > 0.06f)
                {
                    float echoT = t - 0.06f;
                    echo = (Random.value * 2f - 1f) * 0.35f * Mathf.Exp(-echoT * 8f);
                }

                samples[i] = Mathf.Clamp(initialCrack * 0.85f + thump * 0.70f + echo, -1f, 1f);
            }

            var clip = AudioClip.Create("Gunshot_Crack_FX", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateLockWarningAudioClip()
        {
            int sampleRate = 22050;
            float duration = 0.08f;
            int totalSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // High-pitch alert pip / sonar chirp (1800 Hz)
                float chirp = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 35f);
                samples[i] = Mathf.Clamp(chirp * 0.7f, -1f, 1f);
            }

            var clip = AudioClip.Create("Poacher_Lock_Beep", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

