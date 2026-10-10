using UnityEngine;

namespace SundarbanGuardian.Player
{
    /// One controller, two species. Drag this onto both the Tiger and Deer
    /// prefabs and just change moveSpeed/turnSpeed per prefab in the
    /// Inspector — don't fork the script.
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class LandAnimalController : MonoBehaviour
    {
        [Header("Movement tuning (differs per species prefab)")]
        public float moveSpeed = 7f;       // Tiger: 7 (powerful lunges), Deer: 9 (skittish sprinter)
        public float sprintMultiplier = 1.8f; // Hold Shift / second joystick button to sprint
        public float turnSpeedDeg = 480f;  // Snappier turns for challenging terrain navigation
        public float gravity = -18f;       // Heavier gravity = more committed jumps
        public float jumpSpeed = 8.5f;     // Higher jump to clear mangrove roots & obstacles

        public bool isJumping { get; private set; }

        [Header("Input")]
        public bool useVirtualJoystick = true; // mobile build
        public FixedJoystick joystick;         // assign a UI joystick asset, or null for keyboard testing

        private CharacterController cc;
        private Animator animator;
        private QuadrupedProceduralAnimator proceduralAnimator;
        private ChitraDeerProceduralAnimator deerAnimator;
        private ChitraDeerLegLocomotion legLocomotion;
        private Vector3 velocity;
        private Transform visualTransform;
        private Vector3 initialVisualPos;
        private Quaternion initialVisualRot;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            legLocomotion = GetComponent<ChitraDeerLegLocomotion>() ?? GetComponentInChildren<ChitraDeerLegLocomotion>();
            deerAnimator = GetComponent<ChitraDeerProceduralAnimator>() ?? GetComponentInChildren<ChitraDeerProceduralAnimator>();
            proceduralAnimator = GetComponent<QuadrupedProceduralAnimator>() ?? GetComponentInChildren<QuadrupedProceduralAnimator>();

            // Auto-tune nimble physics if this is the Spotted Deer
            if (legLocomotion != null || deerAnimator != null || gameObject.name.ToLower().Contains("deer"))
            {
                if (moveSpeed < 8.0f) moveSpeed = 8.2f;
                sprintMultiplier = 1.9f;
                jumpSpeed = 10.5f; // high graceful steeplechase leap
                turnSpeedDeg = 520f; // fast skittish turns
            }

            visualTransform = transform.Find("VisualModel");
            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }
            if (visualTransform != null)
            {
                initialVisualPos = visualTransform.localPosition;
                initialVisualRot = visualTransform.localRotation;
            }

            if (GetComponent<ToolActionController>() == null)
            {
                gameObject.AddComponent<ToolActionController>();
            }
        }

        private void OnEnable()
        {
            MobileInputManager.OnJumpTriggered += HandleJump;
        }

        private void OnDisable()
        {
            MobileInputManager.OnJumpTriggered -= HandleJump;
        }

        private float lastGroundedTime = -10f;
        private const float CoyoteTime = 0.25f;

        public void HandleJump()
        {
            bool canJump = (Time.time - lastGroundedTime <= CoyoteTime) || (cc != null && cc.isGrounded);
            if (!canJump && cc != null)
            {
                // Fallback raycast to detect terrain right under hooves/paws
                canJump = Physics.Raycast(transform.position + Vector3.up * 0.25f, Vector3.down, 0.55f, ~0, QueryTriggerInteraction.Ignore);
            }

            if (canJump && !isJumping)
            {
                velocity.y = jumpSpeed;
                isJumping = true;
                lastGroundedTime = -10f; // consume

                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetTrigger("Jump");
                }

                if (legLocomotion != null)
                {
                    legLocomotion.SetJumping(true);
                }
            }
        }

        private void Update()
        {
            MobileInputManager.PollKeyboardShortcuts();
            Vector2 input = ReadInput();

            // Sprint detection: Shift key (keyboard) or secondary joystick button (mobile)
            bool isSprinting = false;
#if ENABLE_INPUT_SYSTEM
            try { isSprinting = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed; } catch {}
#else
            isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

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
            float speedMag = isSprinting ? Mathf.Min(moveDir.magnitude * sprintMultiplier, 1f) : moveDir.magnitude;

            Vector3 horizontalMove = Vector3.zero;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();

                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedDeg * Time.deltaTime);

                horizontalMove = transform.forward * currentSpeed * Time.deltaTime;
            }

            // Ground detection and landing reset
            if (cc.isGrounded)
            {
                lastGroundedTime = Time.time;
                if (velocity.y < 0f)
                {
                    velocity.y = -2f;
                    isJumping = false;
                    if (legLocomotion != null) legLocomotion.SetJumping(false);
                }
            }
            else
            {
                // Extra ground detection in case cc.isGrounded is false due to micro-gaps on bumpy mesh
                if (velocity.y < 0f && Physics.Raycast(transform.position + Vector3.up * 0.25f, Vector3.down, 0.45f, ~0, QueryTriggerInteraction.Ignore))
                {
                    lastGroundedTime = Time.time;
                    velocity.y = -2f;
                    isJumping = false;
                    if (legLocomotion != null) legLocomotion.SetJumping(false);
                }
            }

            velocity.y += gravity * Time.deltaTime;
            Vector3 verticalMove = velocity * Time.deltaTime;

            // Single unified CharacterController move per frame to preserve physics contact flags
            cc.Move(horizontalMove + verticalMove);

            // Fall-off-map safeguard: clamp to GLB world bounds (960x960m centered at origin)
            if (transform.position.y < -5.0f)
            {
                cc.enabled = false;
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, -480f, 480f), 0.5f, Mathf.Clamp(transform.position.z, -480f, 480f));
                velocity = Vector3.zero;
                cc.enabled = true;
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(SpeedHash, speedMag);
            }

            // Procedural quadruped/deer leg locomotion & gait
            if (legLocomotion != null)
            {
                legLocomotion.SetSpeed(speedMag);
                legLocomotion.SetJumping(isJumping);
            }
            else if (deerAnimator != null)
            {
                deerAnimator.SetSpeed(speedMag);
            }
            else if (proceduralAnimator != null)
            {
                proceduralAnimator.SetSpeed(speedMag);
            }
            else
            {
                AnimateVisualModel(speedMag);
            }
        }

        private void AnimateVisualModel(float speedMag)
        {
            if (visualTransform == null) return;

            if (speedMag > 0.05f)
            {
                // Walking stride gait: vertical bobbing + subtle roll and pitch
                float bobFreq = moveSpeed * 2.8f;
                float bobY = Mathf.Sin(Time.time * bobFreq) * 0.045f;
                float rollZ = Mathf.Sin(Time.time * (bobFreq * 0.5f)) * 2.2f;
                float pitchX = Mathf.Cos(Time.time * bobFreq) * 1.5f;

                visualTransform.localPosition = initialVisualPos + new Vector3(0f, Mathf.Abs(bobY), 0f);
                visualTransform.localRotation = initialVisualRot * Quaternion.Euler(pitchX, 0f, rollZ);
            }
            else
            {
                // Gentle idle breathing
                float breathY = Mathf.Sin(Time.time * 2.2f) * 0.008f;
                visualTransform.localPosition = initialVisualPos + new Vector3(0f, breathY, 0f);
                visualTransform.localRotation = Quaternion.Slerp(visualTransform.localRotation, initialVisualRot, Time.deltaTime * 6f);
            }
        }

        private Vector2 ReadInput()
        {
            if (useVirtualJoystick && joystick != null)
            {
                var joy = new Vector2(joystick.Horizontal, joystick.Vertical);
                if (joy.sqrMagnitude > 0.01f) return joy;
            }

            return MobileInputManager.GetMoveInput();
        }
    }

    /// Minimal stand-in interface so this file compiles standalone.
    /// Replace with your actual mobile joystick asset (e.g. "Joystick Pack" from Asset Store).
    public class FixedJoystick : MonoBehaviour
    {
        public float Horizontal;
        public float Vertical;
    }
}
