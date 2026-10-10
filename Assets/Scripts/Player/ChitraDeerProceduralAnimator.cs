using UnityEngine;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Specialized procedural ungulate locomotion animator for the Spotted Deer (চিত্রা হরিণ / Axis axis).
    /// Simulates authentic, biological deer locomotion distinct from predatory felines:
    /// - Alert, proud upright neck carriage and vigilance
    /// - High-stepping knee action (elevated carpus lift over mangrove roots and marsh)
    /// - Elastic double-bounce trot with springy hoof suspension
    /// - Energetic, rhythmic head nodding synchronized with forehoof strikes
    /// - High-speed bounding (stotting / pronk) with cocked alarm tail
    /// - Hyper-vigilant idle behaviors: periodic head scanning, ear twitches, weight shifting, and tail flicks
    /// - Graceful steeplechase arching leap when jumping over canals and obstacles
    /// </summary>
    [ExecuteAlways]
    public class ChitraDeerProceduralAnimator : MonoBehaviour
    {
        [Header("Live Editor Preview")]
        [Tooltip("Test ungulate trot in edit mode without starting play mode (0 = idle, 0.5 = trot, 1.0 = sprint)")]
        [Range(0f, 1f)]
        public float editorPreviewSpeed = 0f;

        [Header("Bone Hierarchy References")]
        public Transform rootBone;
        public Transform pelvisBone;
        public Transform spineBone;
        public Transform chestBone;
        public Transform neckBone;
        public Transform headBone;

        [Header("Tail Bones")]
        public Transform tailBaseBone;
        public Transform tailMidBone;
        public Transform tailTipBone;

        [Header("Forelegs (Front Knees & Hooves)")]
        public Transform legFLUpper;
        public Transform legFLLower;
        public Transform hoofFL;

        public Transform legFRUpper;
        public Transform legFRLower;
        public Transform hoofFR;

        [Header("Hindlegs (Hocks & Hooves)")]
        public Transform legBLUpper;
        public Transform legBLLower;
        public Transform hoofBL;

        public Transform legBRUpper;
        public Transform legBRLower;
        public Transform hoofBR;

        [Header("Ungulate Trot Tuning")]
        [Tooltip("Stride cycle frequency multiplier for nimble ungulate steps")]
        public float strideFrequency = 4.8f;
        [Tooltip("Forward/backward upper leg swing arc (degrees)")]
        public float legSwingAngle = 32f;
        [Tooltip("High-knee carpus lift angle during forward swing (degrees)")]
        public float kneeLiftAngle = 50f;
        [Tooltip("Back hock flexion angle during push-off (degrees)")]
        public float hockBendAngle = 44f;
        [Tooltip("Delicate hoof flexion during ground contact (degrees)")]
        public float hoofFlexAngle = 22f;

        [Header("Elastic Body Spring & Bounce")]
        [Tooltip("Vertical body bounce per step (ungulate suspension)")]
        public float bodyBounceHeight = 0.055f;
        [Tooltip("Subtle pelvic roll transfer")]
        public float pelvisRollAngle = 2.2f;

        [Header("Alert Head & Neck Carriage")]
        [Tooltip("Proud upright resting angle offset for neck (degrees)")]
        public float alertNeckElevation = 14f;
        [Tooltip("Rhythmic vertical head nod angle during stride (degrees)")]
        public float headNodAngle = 5.2f;

        [Header("Cocked Deer Tail")]
        [Tooltip("Base cocked upward angle of short deer tail")]
        public float tailCockAngle = 20f;
        [Tooltip("Vertical tail flick during locomotion")]
        public float tailFlickAngle = 16f;

        [Header("Hyper-Vigilant Idle Behaviors")]
        public float idleBreathSpeed = 2.4f;
        public float idleBreathAmplitude = 1.0f;
        [Tooltip("How far the deer turns its head to scan for predators while stationary (degrees)")]
        public float headScanAngle = 18f;

        // Initial default transforms for additive posing
        private Quaternion initPelvisRot, initSpineRot, initChestRot, initNeckRot, initHeadRot;
        private Quaternion initTailBaseRot, initTailMidRot, initTailTipRot;
        private Quaternion initFLUpperRot, initFLLowerRot, initFLHoofRot;
        private Quaternion initFRUpperRot, initFRLowerRot, initFRHoofRot;
        private Quaternion initBLUpperRot, initBLLowerRot, initBLHoofRot;
        private Quaternion initBRUpperRot, initBRLowerRot, initBRHoofRot;
        private Vector3 initChestPos, initPelvisPos;

        private float walkPhase = 0f;
        private float currentSpeedMag = 0f;
        private float smoothedSpeed = 0f;
        private bool isInitialized = false;

        // Idle scan timers
        private float nextScanTime = 0f;
        private float currentScanYaw = 0f;
        private float targetScanYaw = 0f;
        private float earTwitchTimer = 0f;
        private float currentEarTwitch = 0f;

        // Jump blend
        private LandAnimalController landController;
        private float jumpBlend = 0f;

        private void Awake()
        {
            landController = GetComponent<LandAnimalController>();
            CacheInitialTransforms(false);
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                CacheInitialTransforms(false);
            }
        }

        public void CacheInitialTransforms(bool force = false)
        {
            if (isInitialized && !force) return;
            if (pelvisBone == null) return;

            initPelvisRot = pelvisBone.localRotation;
            initPelvisPos = pelvisBone.localPosition;

            if (spineBone != null) initSpineRot = spineBone.localRotation;
            if (chestBone != null)
            {
                initChestRot = chestBone.localRotation;
                initChestPos = chestBone.localPosition;
            }
            if (neckBone != null) initNeckRot = neckBone.localRotation;
            if (headBone != null) initHeadRot = headBone.localRotation;

            if (tailBaseBone != null) initTailBaseRot = tailBaseBone.localRotation;
            if (tailMidBone != null) initTailMidRot = tailMidBone.localRotation;
            if (tailTipBone != null) initTailTipRot = tailTipBone.localRotation;

            if (legFLUpper != null) initFLUpperRot = legFLUpper.localRotation;
            if (legFLLower != null) initFLLowerRot = legFLLower.localRotation;
            if (hoofFL != null) initFLHoofRot = hoofFL.localRotation;

            if (legFRUpper != null) initFRUpperRot = legFRUpper.localRotation;
            if (legFRLower != null) initFRLowerRot = legFRLower.localRotation;
            if (hoofFR != null) initFRHoofRot = hoofFR.localRotation;

            if (legBLUpper != null) initBLUpperRot = legBLUpper.localRotation;
            if (legBLLower != null) initBLLowerRot = legBLLower.localRotation;
            if (hoofBL != null) initBLHoofRot = hoofBL.localRotation;

            if (legBRUpper != null) initBRUpperRot = legBRUpper.localRotation;
            if (legBRLower != null) initBRLowerRot = legBRLower.localRotation;
            if (hoofBR != null) initBRHoofRot = hoofBR.localRotation;

            isInitialized = true;
        }

        public void SetSpeed(float speed)
        {
            currentSpeedMag = Mathf.Clamp01(speed);
        }

        private void LateUpdate()
        {
            if (!isInitialized)
            {
                CacheInitialTransforms(false);
                if (!isInitialized) return;
            }

            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            if (dt <= 0f) dt = 0.016f;

            if (!Application.isPlaying)
            {
                currentSpeedMag = editorPreviewSpeed;
            }

            // Smooth speed transitions
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeedMag, dt * 10f);

            // Handle jumping blend
            bool jumping = landController != null && landController.isJumping;
            jumpBlend = Mathf.MoveTowards(jumpBlend, jumping ? 1f : 0f, dt * 6f);

            // Advance gait phase proportional to movement speed
            float cycleFreq = strideFrequency * (0.9f + smoothedSpeed * 0.45f);
            if (smoothedSpeed > 0.04f)
            {
                walkPhase += dt * cycleFreq * (smoothedSpeed * 6.283185f);
            }

            // Update hyper-vigilant idle scan
            UpdateIdleScan(dt);

            // Evaluate procedural ungulate motion
            AnimateDeerGait(walkPhase, smoothedSpeed);
        }

        private void UpdateIdleScan(float dt)
        {
            if (smoothedSpeed > 0.15f)
            {
                targetScanYaw = 0f;
                currentScanYaw = Mathf.MoveTowards(currentScanYaw, 0f, dt * 60f);
                return;
            }

            float curTime = Time.realtimeSinceStartup;

            if (curTime >= nextScanTime)
            {
                nextScanTime = curTime + Random.Range(3.5f, 6.5f);
                // Randomly look left, center, or right
                float roll = Random.value;
                if (roll < 0.35f) targetScanYaw = -headScanAngle;
                else if (roll < 0.70f) targetScanYaw = headScanAngle;
                else targetScanYaw = 0f;
            }

            currentScanYaw = Mathf.Lerp(currentScanYaw, targetScanYaw, dt * 3.5f);

            // Ear twitch bursts
            if (curTime >= earTwitchTimer)
            {
                earTwitchTimer = curTime + Random.Range(2.0f, 4.5f);
                currentEarTwitch = Random.Range(-12f, 12f);
            }
            else
            {
                currentEarTwitch = Mathf.Lerp(currentEarTwitch, 0f, dt * 8f);
            }
        }

        private void AnimateDeerGait(float phase, float speed)
        {
            float idleBlend = 1f - speed;

            // ── 1. Diagonal Ungulate Trot Phases ──────────────────────────────────
            // Pair A: Front-Left (FL) and Back-Right (BR)
            // Pair B: Front-Right (FR) and Back-Left (BL) - 180 deg out of phase
            float phaseFL = phase;
            float phaseBR = phase;
            float phaseFR = phase + Mathf.PI;
            float phaseBL = phase + Mathf.PI;

            // High-speed bounding sprint blend
            bool isBounding = speed > 0.75f;
            float boundFactor = isBounding ? 1.35f : 1.0f;

            // Foreleg swings: high athletic reaching forward & back
            float swingFL = Mathf.Sin(phaseFL) * legSwingAngle * speed * boundFactor;
            float swingFR = Mathf.Sin(phaseFR) * legSwingAngle * speed * boundFactor;

            // Hindleg swings: strong athletic extension
            float swingBL = Mathf.Sin(phaseBL) * (legSwingAngle * 1.05f) * speed * boundFactor;
            float swingBR = Mathf.Sin(phaseBR) * (legSwingAngle * 1.05f) * speed * boundFactor;

            // ── 2. High-Knee Action (Elevated Carpus Lift) ────────────────────────
            // Front knees bend sharply upwards only during the forward swing (Sin > 0)
            float kneeFL = Mathf.Max(0f, Mathf.Sin(phaseFL)) * kneeLiftAngle * speed;
            float kneeFR = Mathf.Max(0f, Mathf.Sin(phaseFR)) * kneeLiftAngle * speed;

            // Back hocks flex tightly during swing phase
            float hockBL = Mathf.Max(0f, -Mathf.Sin(phaseBL)) * hockBendAngle * speed;
            float hockBR = Mathf.Max(0f, -Mathf.Sin(phaseBR)) * hockBendAngle * speed;

            // Delicate hoof flick (unguligrade toe flex)
            float hoofFLAngle = -Mathf.Sin(phaseFL) * hoofFlexAngle * speed;
            float hoofFRAngle = -Mathf.Sin(phaseFR) * hoofFlexAngle * speed;
            float hoofBLAngle = -Mathf.Sin(phaseBL) * hoofFlexAngle * speed;
            float hoofBRAngle = -Mathf.Sin(phaseBR) * hoofFlexAngle * speed;

            // ── 2.5 Natural Ungulate Alert Standing Stance (Idle Offsets) ─────────
            // When resting, deer do not stand like rigid mannequins: one front foot is forward,
            // rear hocks are flexed and ready to spring.
            float stanceBlend = Mathf.Clamp01(1f - speed * 3f);
            float restSwingFL = 6.0f * stanceBlend;
            float restKneeFL  = 8.0f * stanceBlend;
            float restSwingFR = -4.0f * stanceBlend;
            float restKneeFR  = 3.0f * stanceBlend;
            float restSwingBL = -6.0f * stanceBlend;
            float restHockBL  = 14.0f * stanceBlend;
            float restSwingBR = 4.0f * stanceBlend;
            float restHockBR  = 7.0f * stanceBlend;

            swingFL += restSwingFL;
            kneeFL  += restKneeFL;
            swingFR += restSwingFR;
            kneeFR  += restKneeFR;
            swingBL += restSwingBL;
            hockBL  += restHockBL;
            swingBR += restSwingBR;
            hockBR  += restHockBR;

            // ── 3. Apply to Limbs (with Jump Morph) ───────────────────────────────
            if (jumpBlend > 0.01f)
            {
                // In mid-air hurdle leap: forelegs fold gracefully under chest, hindlegs stretch rearward
                float jSwingFront = Mathf.Lerp(0f, -22f, jumpBlend);
                float jKneeFront  = Mathf.Lerp(0f, 62f, jumpBlend);
                float jSwingHind  = Mathf.Lerp(0f, 32f, jumpBlend);
                float jHockHind   = Mathf.Lerp(0f, -18f, jumpBlend);

                swingFL = Mathf.Lerp(swingFL, jSwingFront, jumpBlend);
                swingFR = Mathf.Lerp(swingFR, jSwingFront, jumpBlend);
                kneeFL  = Mathf.Lerp(kneeFL,  jKneeFront, jumpBlend);
                kneeFR  = Mathf.Lerp(kneeFR,  jKneeFront, jumpBlend);

                swingBL = Mathf.Lerp(swingBL, jSwingHind, jumpBlend);
                swingBR = Mathf.Lerp(swingBR, jSwingHind, jumpBlend);
                hockBL  = Mathf.Lerp(hockBL,  jHockHind, jumpBlend);
                hockBR  = Mathf.Lerp(hockBR,  jHockHind, jumpBlend);
            }

            // Apply Front-Left Leg
            if (legFLUpper != null) legFLUpper.localRotation = initFLUpperRot * Quaternion.Euler(swingFL, 0f, 0f);
            if (legFLLower != null) legFLLower.localRotation = initFLLowerRot * Quaternion.Euler(-kneeFL, 0f, 0f);
            if (hoofFL != null)     hoofFL.localRotation     = initFLHoofRot  * Quaternion.Euler(hoofFLAngle, 0f, 0f);

            // Apply Front-Right Leg
            if (legFRUpper != null) legFRUpper.localRotation = initFRUpperRot * Quaternion.Euler(swingFR, 0f, 0f);
            if (legFRLower != null) legFRLower.localRotation = initFRLowerRot * Quaternion.Euler(-kneeFR, 0f, 0f);
            if (hoofFR != null)     hoofFR.localRotation     = initFRHoofRot  * Quaternion.Euler(hoofFRAngle, 0f, 0f);

            // Apply Back-Left Leg
            if (legBLUpper != null) legBLUpper.localRotation = initBLUpperRot * Quaternion.Euler(swingBL, 0f, 0f);
            if (legBLLower != null) legBLLower.localRotation = initBLLowerRot * Quaternion.Euler(hockBL, 0f, 0f);
            if (hoofBL != null)     hoofBL.localRotation     = initBLHoofRot  * Quaternion.Euler(hoofBLAngle, 0f, 0f);

            // Apply Back-Right Leg
            if (legBRUpper != null) legBRUpper.localRotation = initBRUpperRot * Quaternion.Euler(swingBR, 0f, 0f);
            if (legBRLower != null) legBRLower.localRotation = initBRLowerRot * Quaternion.Euler(hockBR, 0f, 0f);
            if (hoofBR != null)     hoofBR.localRotation     = initBRHoofRot  * Quaternion.Euler(hoofBRAngle, 0f, 0f);

            // ── 4. Elastic Double-Bounce Ungulate Suspension ──────────────────────
            // Two vertical bounces per complete stride cycle (one for each diagonal strike)
            float bounceCycle = Mathf.Abs(Mathf.Sin(phase)) * bodyBounceHeight * speed * boundFactor;
            float pelvisRoll = Mathf.Cos(phase) * pelvisRollAngle * speed;

            // Idle breathing
            float breath = Mathf.Sin(Time.time * idleBreathSpeed) * idleBreathAmplitude * idleBlend;

            if (chestBone != null)
            {
                chestBone.localPosition = initChestPos + new Vector3(0f, bounceCycle, 0f);
                chestBone.localRotation = initChestRot * Quaternion.Euler(-breath * 0.4f, 0f, -pelvisRoll * 0.4f);
            }

            if (pelvisBone != null)
            {
                pelvisBone.localPosition = initPelvisPos + new Vector3(0f, bounceCycle * 0.85f, 0f);
                pelvisBone.localRotation = initPelvisRot * Quaternion.Euler(breath * 0.3f, 0f, pelvisRoll);
            }

            if (spineBone != null)
            {
                // Ungulates have a rigid thoracic spine: minimal lateral yaw, gentle spring
                spineBone.localRotation = initSpineRot * Quaternion.Euler(breath * 0.2f, 0f, 0f);
            }

            // ── 5. Alert Proud Neck & Rhythmic Head Nod ───────────────────────────
            // Real deer carry their necks high and nod rhythmically with every step
            float nod = Mathf.Sin(phase * 2f) * headNodAngle * speed;
            float jumpHeadLift = Mathf.Lerp(0f, 15f, jumpBlend);

            if (neckBone != null)
            {
                // Base upright elevation + gait nod + idle scanning yaw
                Quaternion alertTilt = Quaternion.Euler(-alertNeckElevation + nod * 0.6f + jumpHeadLift, currentScanYaw * 0.45f, 0f);
                neckBone.localRotation = initNeckRot * alertTilt;
            }

            if (headBone != null)
            {
                // Counter-nod to keep eyes scanning ahead + ear twitches
                Quaternion headTilt = Quaternion.Euler(-nod * 0.4f, currentScanYaw * 0.55f, currentEarTwitch);
                headBone.localRotation = initHeadRot * headTilt;
            }

            // ── 6. Cocked Short Deer Tail ─────────────────────────────────────────
            // Real spotted deer have short bushy tails that cock up alertly and flick
            float tailMotion = Mathf.Sin(phase * 2f) * tailFlickAngle * speed;
            float alarmCock = isBounding ? 25f : 0f; // Tail raises high when sprinting/alarmed
            float idleFlick = Mathf.Sin(Time.time * 1.5f) * 4f * idleBlend;

            if (tailBaseBone != null)
            {
                Quaternion tailRot = Quaternion.Euler(-tailCockAngle - alarmCock - tailMotion - idleFlick, 0f, 0f);
                tailBaseBone.localRotation = initTailBaseRot * tailRot;
            }

            if (tailMidBone != null)
            {
                tailMidBone.localRotation = initTailMidRot * Quaternion.Euler(-tailMotion * 0.5f, 0f, 0f);
            }
        }
    }
}
