using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Gangetic Dolphin: swims within the wide estuarine river channel of the Sundarbans.
    /// Features fluid 3D swimming, breach leaping (Space key) over illegal gillnets,
    /// diving under acoustic noise, and managing breath stamina.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class DolphinSwimController : MonoBehaviour
    {
        [Header("Movement")]
        public float swimSpeed = 10f;          // Swift river cruising speed
        public float sprintMultiplier = 1.65f; // Shift = burst sprint
        public float turnSpeedDeg = 360f;     // Responsive steering to navigate river bends

        [Header("Breaching & Diving Mechanics")]
        public float cruiseY = -0.55f;         // Normal underwater swim depth
        public float diveY = -1.75f;           // Deep dive to pass under boat trawlers
        public float surfaceBreatheY = 0.25f;  // Snout surfacing to breathe
        public float surfaceY { get => surfaceBreatheY; set => surfaceBreatheY = value; }
        public float leapPeakY = 1.65f;        // Full acrobatic breach leap over floating gillnets
        public float leapDuration = 0.85f;     // Seconds spent in breach leap arc

        [Header("Breathing Stamina")]
        public float breathHoldSeconds = 12f;
        public float breathTimer { get; private set; }
        public bool isSurfaced { get; private set; }
        public bool isLeaping { get; private set; }

        [Header("River Bounds — Matched to Estuary & Shibsa River Channel")]
        public float minX = -220f;
        public float maxX = 120f;
        public float minZ = -295f;
        public float maxZ = -80f;

        private Rigidbody rb;
        private Animator animator;
        private float leapTimer = 0f;
        private float targetPitch = 0f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int SurfacedHash = Animator.StringToHash("Surfaced");

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            animator = GetComponent<Animator>();
            breathTimer = breathHoldSeconds;

            if (GetComponent<ToolActionController>() == null)
            {
                gameObject.AddComponent<ToolActionController>();
            }
        }

        private void OnEnable()
        {
            MobileInputManager.OnJumpTriggered += TriggerLeap;
        }

        private void OnDisable()
        {
            MobileInputManager.OnJumpTriggered -= TriggerLeap;
        }

        public void TriggerLeap()
        {
            if (!isLeaping)
            {
                isLeaping = true;
                leapTimer = leapDuration;
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = MobileInputManager.GetMoveInput();
            Camera cam = Camera.main;
            Vector3 moveDir;

            if (cam != null)
            {
                Vector3 fwd = cam.transform.forward;
                fwd.y = 0f;
                fwd.Normalize();
                Vector3 right = cam.transform.right;
                right.y = 0f;
                right.Normalize();
                moveDir = (fwd * input.y) + (right * input.x);
            }
            else
            {
                moveDir = new Vector3(input.x, 0f, input.y);
            }

            // Sprint detection (Keyboard Shift or Mobile)
            bool isSprinting = false;
#if ENABLE_INPUT_SYSTEM
            try { isSprinting = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed; } catch {}
#else
            isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedDeg * Time.fixedDeltaTime);

                float speed = swimSpeed * (isSprinting ? sprintMultiplier : 1f);
                if (isLeaping) speed *= 1.25f; // Extra momentum carried during leap

                rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
            }

            ClampToRiverBounds();
            HandleBreachAndBreathing();

            animator.SetFloat(SpeedHash, moveDir.magnitude);
        }

        private void ClampToRiverBounds()
        {
            Vector3 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            rb.position = pos;
        }

        private void HandleBreachAndBreathing()
        {
            // Check Space key for Leap / Breach
            bool spacePressed = false;
            bool spaceHeld = false;
            bool diveHeld = false;

#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    spacePressed = kb.spaceKey.wasPressedThisFrame;
                    spaceHeld = kb.spaceKey.isPressed;
                    diveHeld = kb.cKey.isPressed || kb.leftCtrlKey.isPressed;
                }
            }
            catch {}
#else
            spacePressed = Input.GetKeyDown(KeyCode.Space);
            spaceHeld = Input.GetKey(KeyCode.Space);
            diveHeld = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
#endif

            // Trigger Acrobatic Breach Leap over floating nets
            if (spacePressed && !isLeaping)
            {
                isLeaping = true;
                leapTimer = leapDuration;
            }

            Vector3 pos = rb.position;
            float currentY = pos.y;
            float targetY = cruiseY;

            if (isLeaping)
            {
                leapTimer -= Time.fixedDeltaTime;
                float t = 1f - (leapTimer / leapDuration); // 0 -> 1
                // Parabolic Arc: 4 * h * t * (1 - t)
                float arcY = Mathf.Sin(t * Mathf.PI) * (leapPeakY - cruiseY);
                pos.y = cruiseY + arcY;

                // Pitch angle: nose up as it breaches, nose down as it dives back
                targetPitch = (t < 0.45f) ? -28f : 24f;

                if (leapTimer <= 0f)
                {
                    isLeaping = false;
                    pos.y = cruiseY;
                }
            }
            else if (diveHeld)
            {
                targetY = diveY;
                pos.y = Mathf.MoveTowards(currentY, targetY, 3.5f * Time.fixedDeltaTime);
                targetPitch = 18f; // Nose down
            }
            else if (spaceHeld || breathTimer <= 0f)
            {
                targetY = surfaceBreatheY;
                pos.y = Mathf.MoveTowards(currentY, targetY, 3.0f * Time.fixedDeltaTime);
                targetPitch = -12f; // Nose up towards water surface
            }
            else
            {
                targetY = cruiseY;
                pos.y = Mathf.MoveTowards(currentY, targetY, 2.5f * Time.fixedDeltaTime);
                targetPitch = 0f;
            }

            rb.position = pos;

            // Apply smooth pitch tilt to visual rotation without breaking Y yaw
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = Mathf.MoveTowardsAngle(euler.x, targetPitch, 160f * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Euler(euler);

            // Breath accounting
            isSurfaced = pos.y >= 0.0f;
            animator.SetBool(SurfacedHash, isSurfaced);

            if (isSurfaced)
            {
                breathTimer = breathHoldSeconds;
            }
            else
            {
                breathTimer -= Time.fixedDeltaTime;
                if (breathTimer < 0f) breathTimer = 0f;
            }
        }
    }
}
