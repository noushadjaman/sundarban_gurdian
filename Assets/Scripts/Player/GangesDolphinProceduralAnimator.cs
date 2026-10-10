using UnityEngine;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Authentic Ganges River Dolphin (Platanista gangetica / শুশুক) Locomotion & Body Movement.
    /// Simulates biological cetacean swimming:
    /// 1. Dorso-ventral spine undulation (Spine1 -> Spine2 -> Spine3 -> Spine4 -> Tail)
    /// 2. Tail fluking with speed-adaptive oscillation frequency
    /// 3. Echolocation head-scanning (unfused cervical neck allows active side-to-side sweeping)
    /// 4. Pectoral flipper banking and stabilization (TopFlipper.L/R, MidFlipper.L/R)
    /// 5. Works seamlessly in both Edit Mode ([ExecuteAlways]) and Play Mode.
    /// </summary>
    [ExecuteAlways]
    public class GangesDolphinProceduralAnimator : MonoBehaviour
    {
        [Header("Editor Preview")]
        public bool editorPreview = true;
        [Range(0f, 3f)] public float previewSpeed = 1.0f;

        [Header("Spine Undulation Wave")]
        [Tooltip("Base swimming cycle frequency (Hz)")]
        public float swimFrequency = 1.6f;
        [Tooltip("Maximum vertical spine pitch flexion in degrees")]
        public float spineFlexionAngle = 14f;
        [Tooltip("Tail fluke vertical pitch stroke angle")]
        public float tailStrokeAngle = 26f;
        [Tooltip("Phase delay between consecutive spine vertebrae")]
        public float phaseDelay = 0.45f;

        [Header("Echolocation Head Scan (Unfused Neck)")]
        [Tooltip("Head side-to-side sweeping frequency")]
        public float headScanFrequency = 0.85f;
        [Tooltip("Head yaw angle for acoustic scanning")]
        public float headScanYaw = 9f;
        [Tooltip("Head vertical pitch pitch for benthic scanning")]
        public float headScanPitch = 5f;

        [Header("Pectoral Flipper Stabilization")]
        [Tooltip("Paddle flipper bank roll angle during turns")]
        public float flipperBankAngle = 18f;
        [Tooltip("Gentle flipper flutter angle")]
        public float flipperFlutterAngle = 6f;

        [Header("Bones Reference (Auto-detected if null)")]
        public Transform rootBone;
        public Transform spine1;
        public Transform spine2;
        public Transform spine3;
        public Transform spine4;
        public Transform tail;
        public Transform head;
        public Transform topFlipperL;
        public Transform midFlipperL;
        public Transform bottomFlipperL;
        public Transform topFlipperR;
        public Transform midFlipperR;
        public Transform bottomFlipperR;

        // Cached base local rotations
        private Quaternion baseSpine1Rot;
        private Quaternion baseSpine2Rot;
        private Quaternion baseSpine3Rot;
        private Quaternion baseSpine4Rot;
        private Quaternion baseTailRot;
        private Quaternion baseHeadRot;
        private Quaternion baseTopFlipperLRot;
        private Quaternion baseMidFlipperLRot;
        private Quaternion baseBottomFlipperLRot;
        private Quaternion baseTopFlipperRRot;
        private Quaternion baseMidFlipperRRot;
        private Quaternion baseBottomFlipperRRot;

        private float animTime;
        private Rigidbody rb;
        private DolphinSwimController swimController;
        private bool initialized;

        private void OnEnable()
        {
            InitializeBones();
        }

        private void Start()
        {
            InitializeBones();
            rb = GetComponent<Rigidbody>();
            swimController = GetComponent<DolphinSwimController>();
        }

        public void InitializeBones()
        {
            if (initialized) return;

            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.bones != null)
            {
                foreach (var b in smr.bones)
                {
                    if (b == null) continue;
                    switch (b.name)
                    {
                        case "Root": rootBone = b; break;
                        case "Spine1": spine1 = b; break;
                        case "Spine2": spine2 = b; break;
                        case "Spine3": spine3 = b; break;
                        case "Spine4": spine4 = b; break;
                        case "Tail": tail = b; break;
                        case "Head": head = b; break;
                        case "TopFlipper.L": topFlipperL = b; break;
                        case "MidFlipper.L": midFlipperL = b; break;
                        case "BottomFlipper.L": bottomFlipperL = b; break;
                        case "TopFlipper.R": topFlipperR = b; break;
                        case "MidFlipper.R": midFlipperR = b; break;
                        case "BottomFlipper.R": bottomFlipperR = b; break;
                    }
                }
            }

            // Fallback hierarchy search if bones array wasn't wired
            if (spine1 == null) spine1 = transform.Find("Armature/Root/Spine1");
            if (spine2 == null && spine1 != null) spine2 = spine1.Find("Spine2");
            if (spine3 == null && spine2 != null) spine3 = spine2.Find("Spine3");
            if (spine4 == null && spine3 != null) spine4 = spine3.Find("Spine4");
            if (tail == null && spine4 != null) tail = spine4.Find("Tail");
            if (head == null && spine2 != null) head = spine2.Find("Head");

            CacheBaseRotations();
            initialized = true;
        }

        private void CacheBaseRotations()
        {
            if (spine1 != null) baseSpine1Rot = spine1.localRotation;
            if (spine2 != null) baseSpine2Rot = spine2.localRotation;
            if (spine3 != null) baseSpine3Rot = spine3.localRotation;
            if (spine4 != null) baseSpine4Rot = spine4.localRotation;
            if (tail != null) baseTailRot = tail.localRotation;
            if (head != null) baseHeadRot = head.localRotation;

            if (topFlipperL != null) baseTopFlipperLRot = topFlipperL.localRotation;
            if (midFlipperL != null) baseMidFlipperLRot = midFlipperL.localRotation;
            if (bottomFlipperL != null) baseBottomFlipperLRot = bottomFlipperL.localRotation;

            if (topFlipperR != null) baseTopFlipperRRot = topFlipperR.localRotation;
            if (midFlipperR != null) baseMidFlipperRRot = midFlipperR.localRotation;
            if (bottomFlipperR != null) baseBottomFlipperRRot = bottomFlipperR.localRotation;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !editorPreview) return;

            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            float speedFactor = 1.0f;

            if (Application.isPlaying)
            {
                if (rb != null)
                {
                    float speed = rb.linearVelocity.magnitude;
                    speedFactor = Mathf.Clamp(speed / 6f, 0.45f, 2.0f);
                }
            }
            else
            {
                speedFactor = previewSpeed;
            }

            animTime += dt * swimFrequency * speedFactor;

            AnimateCetaceanLocomotion(speedFactor);
        }

        private void AnimateCetaceanLocomotion(float speedFactor)
        {
            // 1. Dorso-Ventral Undulation Wave across Vertebral Column
            // The wave travels backwards from Spine1 to Tail:
            // Local X axis of bones corresponds to vertical pitch flexion in standard cetacean rigs
            float t = animTime * Mathf.PI * 2f;

            float spine1Pitch = Mathf.Sin(t) * (spineFlexionAngle * 0.35f);
            float spine2Pitch = Mathf.Sin(t - phaseDelay) * (spineFlexionAngle * 0.65f);
            float spine3Pitch = Mathf.Sin(t - phaseDelay * 2f) * (spineFlexionAngle * 0.90f);
            float spine4Pitch = Mathf.Sin(t - phaseDelay * 3f) * spineFlexionAngle;
            float tailPitch   = Mathf.Sin(t - phaseDelay * 4f) * tailStrokeAngle;

            // Subtle lateral yaw weave (sinuous river body flex)
            float spineYaw = Mathf.Cos(t * 0.5f) * 3.5f;

            if (spine1 != null) spine1.localRotation = baseSpine1Rot * Quaternion.Euler(spine1Pitch, spineYaw * 0.3f, 0f);
            if (spine2 != null) spine2.localRotation = baseSpine2Rot * Quaternion.Euler(spine2Pitch, spineYaw * 0.6f, 0f);
            if (spine3 != null) spine3.localRotation = baseSpine3Rot * Quaternion.Euler(spine3Pitch, spineYaw * 0.8f, 0f);
            if (spine4 != null) spine4.localRotation = baseSpine4Rot * Quaternion.Euler(spine4Pitch, spineYaw, 0f);
            if (tail != null)   tail.localRotation   = baseTailRot   * Quaternion.Euler(tailPitch, spineYaw * 1.2f, 0f);

            // 2. Active Echolocation Scanning on Head (Unfused cervical vertebrae)
            // Gangetic river dolphin continuously sweeps its long rostrum laterally to acoustic-map benthic silt
            float headT = animTime * headScanFrequency * Mathf.PI * 2f;
            float headYaw = Mathf.Sin(headT) * headScanYaw;
            float headPitch = Mathf.Cos(headT * 1.5f) * headScanPitch;
            if (head != null)
            {
                head.localRotation = baseHeadRot * Quaternion.Euler(headPitch, headYaw, 0f);
            }

            // 3. Pectoral Flipper Stabilization & Water Banking
            // Flippers adjust angle with swimming wave to generate hydrodynamic lift
            float flipperFlutter = Mathf.Sin(t - phaseDelay) * flipperFlutterAngle;
            float flipperRoll = Mathf.Cos(t * 0.5f) * (flipperBankAngle * 0.4f);

            if (topFlipperL != null)
                topFlipperL.localRotation = baseTopFlipperLRot * Quaternion.Euler(flipperFlutter, flipperRoll, 0f);
            if (midFlipperL != null)
                midFlipperL.localRotation = baseMidFlipperLRot * Quaternion.Euler(flipperFlutter * 0.5f, 0f, 0f);
            if (bottomFlipperL != null)
                bottomFlipperL.localRotation = baseBottomFlipperLRot * Quaternion.Euler(flipperFlutter * 0.3f, 0f, 0f);

            if (topFlipperR != null)
                topFlipperR.localRotation = baseTopFlipperRRot * Quaternion.Euler(-flipperFlutter, -flipperRoll, 0f);
            if (midFlipperR != null)
                midFlipperR.localRotation = baseMidFlipperRRot * Quaternion.Euler(-flipperFlutter * 0.5f, 0f, 0f);
            if (bottomFlipperR != null)
                bottomFlipperR.localRotation = baseBottomFlipperRRot * Quaternion.Euler(-flipperFlutter * 0.3f, 0f, 0f);
        }
    }
}
