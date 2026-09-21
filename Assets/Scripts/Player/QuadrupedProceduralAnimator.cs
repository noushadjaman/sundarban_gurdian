using UnityEngine;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Professional procedural quadruped locomotion animator for the Royal Bengal Tiger.
    /// Simulates authentic predatory feline locomotion:
    /// - Diagonal quadruped walking/trotting stride (FL+BR vs FR+BL)
    /// - Knee flexion and ground clearance during forward swing
    /// - Paw touchdown and push-off compliance
    /// - Flexible spine lateral yaw and roll transfer
    /// - Predatory neck/head stability and stride dip
    /// - Dynamic multi-joint tail sway with momentum lag
    /// - Gentle natural breathing and idle tail twitching when stationary
    /// </summary>
    public class QuadrupedProceduralAnimator : MonoBehaviour
    {
        [Header("Bone References (Assigned during Rigging)")]
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

        [Header("Front-Left Leg")]
        public Transform legFLUpper;
        public Transform legFLLower;
        public Transform pawFL;

        [Header("Front-Right Leg")]
        public Transform legFRUpper;
        public Transform legFRLower;
        public Transform pawFR;

        [Header("Back-Left Leg")]
        public Transform legBLUpper;
        public Transform legBLLower;
        public Transform pawBL;

        [Header("Back-Right Leg")]
        public Transform legBRUpper;
        public Transform legBRLower;
        public Transform pawBR;

        [Header("Gait Tuning")]
        [Tooltip("Stride cycle frequency multiplier relative to movement speed")]
        public float strideFrequency = 3.2f;
        [Tooltip("Maximum upper leg forward/backward swing angle (degrees)")]
        public float legSwingAngle = 26f;
        [Tooltip("Knee bend angle during swing-forward phase (degrees)")]
        public float kneeBendAngle = 24f;
        [Tooltip("Paw flexion angle during lift-off (degrees)")]
        public float pawFlexAngle = 14f;

        [Header("Spine & Body Dynamics")]
        public float spineLateralYaw = 3.5f;
        public float pelvisRoll = 2.5f;
        public float chestVerticalBob = 0.03f;
        public float headDip = 2.0f;

        [Header("Tail Dynamics")]
        public float tailSwayAngle = 9.0f;
        public float tailVerticalLag = 4.0f;

        [Header("Idle Animation")]
        public float breathSpeed = 1.8f;
        public float breathAmplitude = 1.2f;

        // Initial default local rotations for clean additive poses
        private Quaternion initPelvisRot, initSpineRot, initChestRot, initNeckRot, initHeadRot;
        private Quaternion initTailBaseRot, initTailMidRot, initTailTipRot;
        private Quaternion initFLUpperRot, initFLLowerRot, initFLPawRot;
        private Quaternion initFRUpperRot, initFRLowerRot, initFRPawRot;
        private Quaternion initBLUpperRot, initBLLowerRot, initBLPawRot;
        private Quaternion initBRUpperRot, initBRLowerRot, initBRPawRot;
        private Vector3 initChestPos, initPelvisPos;

        private float walkPhase = 0f;
        private float currentSpeedMag = 0f;
        private float smoothedSpeed = 0f;
        private bool isInitialized = false;

        private void Awake()
        {
            CacheInitialTransforms();
        }

        public void CacheInitialTransforms()
        {
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
            if (pawFL != null) initFLPawRot = pawFL.localRotation;

            if (legFRUpper != null) initFRUpperRot = legFRUpper.localRotation;
            if (legFRLower != null) initFRLowerRot = legFRLower.localRotation;
            if (pawFR != null) initFRPawRot = pawFR.localRotation;

            if (legBLUpper != null) initBLUpperRot = legBLUpper.localRotation;
            if (legBLLower != null) initBLLowerRot = legBLLower.localRotation;
            if (pawBL != null) initBLPawRot = pawBL.localRotation;

            if (legBRUpper != null) initBRUpperRot = legBRUpper.localRotation;
            if (legBRLower != null) initBRLowerRot = legBRLower.localRotation;
            if (pawBR != null) initBRPawRot = pawBR.localRotation;

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
                CacheInitialTransforms();
                if (!isInitialized) return;
            }

            // Smooth speed transitions
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeedMag, Time.deltaTime * 8f);

            // Advance gait phase proportional to movement
            float cycleFreq = strideFrequency * (0.8f + smoothedSpeed * 0.4f);
            if (smoothedSpeed > 0.05f)
            {
                walkPhase += Time.deltaTime * cycleFreq * (smoothedSpeed * 6.283185f);
            }

            // Evaluate procedural gait
            AnimateLocomotion(walkPhase, smoothedSpeed);
        }

        private void AnimateLocomotion(float phase, float speed)
        {
            float idleBlend = 1f - speed;

            // Quadruped diagonal phases:
            // Pair A: Front-Left (FL) and Back-Right (BR)
            // Pair B: Front-Right (FR) and Back-Left (BL) - 180 deg out of phase
            float phaseFL = phase;
            float phaseBR = phase;
            float phaseFR = phase + Mathf.PI;
            float phaseBL = phase + Mathf.PI;

            // Upper leg swings (sinusoidal forward/back)
            float swingFL = Mathf.Sin(phaseFL) * legSwingAngle * speed;
            float swingFR = Mathf.Sin(phaseFR) * legSwingAngle * speed;
            float swingBL = Mathf.Sin(phaseBL) * legSwingAngle * speed;
            float swingBR = Mathf.Sin(phaseBR) * legSwingAngle * speed;

            // Knee bend (only bends during forward swing phase, i.e., when sin > 0)
            float kneeFL = Mathf.Max(0f, Mathf.Sin(phaseFL)) * kneeBendAngle * speed;
            float kneeFR = Mathf.Max(0f, Mathf.Sin(phaseFR)) * kneeBendAngle * speed;
            float kneeBL = Mathf.Max(0f, -Mathf.Sin(phaseBL)) * kneeBendAngle * speed;
            float kneeBR = Mathf.Max(0f, -Mathf.Sin(phaseBR)) * kneeBendAngle * speed;

            // Paw flexion
            float pawFLAngle = -Mathf.Sin(phaseFL) * pawFlexAngle * speed;
            float pawFRAngle = -Mathf.Sin(phaseFR) * pawFlexAngle * speed;
            float pawBLAngle = -Mathf.Sin(phaseBL) * pawFlexAngle * speed;
            float pawBRAngle = -Mathf.Sin(phaseBR) * pawFlexAngle * speed;

            // Apply to Front-Left Leg
            if (legFLUpper != null)
                legFLUpper.localRotation = initFLUpperRot * Quaternion.Euler(swingFL, 0f, 0f);
            if (legFLLower != null)
                legFLLower.localRotation = initFLLowerRot * Quaternion.Euler(-kneeFL, 0f, 0f);
            if (pawFL != null)
                pawFL.localRotation = initFLPawRot * Quaternion.Euler(pawFLAngle, 0f, 0f);

            // Apply to Front-Right Leg
            if (legFRUpper != null)
                legFRUpper.localRotation = initFRUpperRot * Quaternion.Euler(swingFR, 0f, 0f);
            if (legFRLower != null)
                legFRLower.localRotation = initFRLowerRot * Quaternion.Euler(-kneeFR, 0f, 0f);
            if (pawFR != null)
                pawFR.localRotation = initFRPawRot * Quaternion.Euler(pawFRAngle, 0f, 0f);

            // Apply to Back-Left Leg
            if (legBLUpper != null)
                legBLUpper.localRotation = initBLUpperRot * Quaternion.Euler(swingBL, 0f, 0f);
            if (legBLLower != null)
                legBLLower.localRotation = initBLLowerRot * Quaternion.Euler(kneeBL, 0f, 0f);
            if (pawBL != null)
                pawBL.localRotation = initBLPawRot * Quaternion.Euler(pawBLAngle, 0f, 0f);

            // Apply to Back-Right Leg
            if (legBRUpper != null)
                legBRUpper.localRotation = initBRUpperRot * Quaternion.Euler(swingBR, 0f, 0f);
            if (legBRLower != null)
                legBRLower.localRotation = initBRLowerRot * Quaternion.Euler(kneeBR, 0f, 0f);
            if (pawBR != null)
                pawBR.localRotation = initBRPawRot * Quaternion.Euler(pawBRAngle, 0f, 0f);

            // Spine lateral yaw & Hips roll
            float spineYaw = Mathf.Sin(phase * 0.5f) * spineLateralYaw * speed;
            float hipsRoll = Mathf.Cos(phase) * pelvisRoll * speed;
            float chestBob = Mathf.Abs(Mathf.Sin(phase)) * chestVerticalBob * speed;

            // Idle breathing cycle
            float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmplitude * idleBlend;

            if (spineBone != null)
                spineBone.localRotation = initSpineRot * Quaternion.Euler(breath * 0.5f, spineYaw, 0f);

            if (pelvisBone != null)
            {
                pelvisBone.localRotation = initPelvisRot * Quaternion.Euler(breath * 0.3f, -spineYaw * 0.5f, hipsRoll);
                pelvisBone.localPosition = initPelvisPos + new Vector3(0f, -chestBob * 0.5f, 0f);
            }

            if (chestBone != null)
            {
                chestBone.localRotation = initChestRot * Quaternion.Euler(-breath * 0.5f, spineYaw * 0.5f, -hipsRoll * 0.4f);
                chestBone.localPosition = initChestPos + new Vector3(0f, -chestBob, 0f);
            }

            // Head / Neck predatory stability
            if (neckBone != null)
                neckBone.localRotation = initNeckRot * Quaternion.Euler(Mathf.Sin(phase) * headDip * speed, -spineYaw * 0.5f, 0f);
            if (headBone != null)
                headBone.localRotation = initHeadRot * Quaternion.Euler(-Mathf.Sin(phase) * (headDip * 0.6f) * speed, -spineYaw * 0.3f, 0f);

            // Tail multi-joint dynamic sway (lags behind spine)
            float tailSway = -Mathf.Sin(phase * 0.5f - 0.4f) * tailSwayAngle * speed;
            float idleTail = Mathf.Sin(Time.time * 0.8f) * 2.5f * idleBlend;
            if (tailBaseBone != null)
                tailBaseBone.localRotation = initTailBaseRot * Quaternion.Euler(-chestBob * 30f, tailSway * 0.4f + idleTail * 0.3f, 0f);
            if (tailMidBone != null)
                tailMidBone.localRotation = initTailMidRot * Quaternion.Euler(-Mathf.Sin(phase) * tailVerticalLag * speed, tailSway * 0.7f + idleTail * 0.6f, 0f);
            if (tailTipBone != null)
                tailTipBone.localRotation = initTailTipRot * Quaternion.Euler(0f, tailSway * 1.0f + idleTail * 1.0f, 0f);
        }
    }
}
