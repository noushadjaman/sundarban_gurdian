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
        public float moveSpeed = 4f;      // Deer: ~5.5 (faster/skittish), Tiger: ~3.5 (heavier)
        public float turnSpeedDeg = 360f;
        public float gravity = -15f;

        [Header("Input")]
        public bool useVirtualJoystick = true; // mobile build
        public FixedJoystick joystick;         // assign a UI joystick asset, or null for keyboard testing

        private CharacterController cc;
        private Animator animator;
        private QuadrupedProceduralAnimator proceduralAnimator;
        private Vector3 velocity;
        private Transform visualTransform;
        private Vector3 initialVisualPos;
        private Quaternion initialVisualRot;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            proceduralAnimator = GetComponent<QuadrupedProceduralAnimator>();
            if (proceduralAnimator == null) proceduralAnimator = GetComponentInChildren<QuadrupedProceduralAnimator>();

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

        private void Update()
        {
            Vector2 input = ReadInput();
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
            float speedMag = moveDir.magnitude;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();

                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedDeg * Time.deltaTime);

                Vector3 horizontalMove = transform.forward * moveSpeed * Time.deltaTime;
                cc.Move(horizontalMove);
            }

            // gravity so the CharacterController stays grounded on uneven terrain
            if (cc.isGrounded && velocity.y < 0f) velocity.y = -1f;
            velocity.y += gravity * Time.deltaTime;
            cc.Move(velocity * Time.deltaTime);

            // Fall-off-map safeguard: never let character fall into infinity
            if (transform.position.y < -3.0f)
            {
                cc.enabled = false;
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, -120f, 120f), 0.5f, Mathf.Clamp(transform.position.z, -100f, 280f));
                velocity = Vector3.zero;
                cc.enabled = true;
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(SpeedHash, speedMag);
            }

            // Procedural quadruped gait / breathing
            if (proceduralAnimator != null)
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
