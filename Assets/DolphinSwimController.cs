using UnityEngine;

namespace SundarbanGuardian.Player
{
    /// Gangetic Dolphin: swims within a bounded river channel (X/Z plane),
    /// with a "surface to breathe" mechanic — it's a real behavior (Gangetic
    /// dolphins are nearly blind and surface periodically), so this doubles
    /// as an educational detail, not just decoration.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class DolphinSwimController : MonoBehaviour
    {
        [Header("Movement")]
        public float swimSpeed = 5f;
        public float turnSpeedDeg = 250f;

        [Header("Breathing mechanic")]
        [Tooltip("Seconds the dolphin can stay submerged before needing to surface")]
        public float breathHoldSeconds = 12f;
        public float surfaceY = 0.4f;
        public float diveY = -1.2f;
        private float breathTimer;
        private bool isSurfaced;

        [Header("River bounds (set per level from LevelData if you want it dynamic)")]
        public float minX = -20f, maxX = 20f, minZ = 0f, maxZ = 80f;

        private Rigidbody rb;
        private Animator animator;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int SurfacedHash = Animator.StringToHash("Surfaced");

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            animator = GetComponent<Animator>();
            breathTimer = breathHoldSeconds;
            if (GetComponent<SundarbanGuardian.Player.ToolActionController>() == null)
            {
                gameObject.AddComponent<SundarbanGuardian.Player.ToolActionController>();
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = SundarbanGuardian.Player.MobileInputManager.GetMoveInput();
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

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedDeg * Time.fixedDeltaTime);
                rb.MovePosition(rb.position + transform.forward * swimSpeed * Time.fixedDeltaTime);
            }

            ClampToRiverBounds();
            HandleBreathing();

            animator.SetFloat(SpeedHash, moveDir.magnitude);
        }

        private void ClampToRiverBounds()
        {
            Vector3 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            rb.position = pos;
        }

        private void HandleBreathing()
        {
            bool wantsSurface = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
                wantsSurface = UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;
#endif

            breathTimer -= Time.fixedDeltaTime;
            if (breathTimer <= 0f && !wantsSurface)
            {
                // forced surface — small penalty could be applied via SpeciesHealth here
                wantsSurface = true;
            }

            float targetY = wantsSurface ? surfaceY : diveY;
            Vector3 pos = rb.position;
            pos.y = Mathf.MoveTowards(pos.y, targetY, 2f * Time.fixedDeltaTime);
            rb.position = pos;

            isSurfaced = pos.y >= surfaceY - 0.05f;
            animator.SetBool(SurfacedHash, isSurfaced);

            if (isSurfaced) breathTimer = breathHoldSeconds;
        }
    }
}
