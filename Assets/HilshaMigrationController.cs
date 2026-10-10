using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SundarbanGuardian.Data;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Mother Hilsha Spawning Migration Controller:
    /// Swims upstream against the river current from the deep Bay of Bengal (Bongoposagor)
    /// through the Meghna river to the freshwater spawning grounds.
    /// Features responsive steering through river channels, dodging illegal gillnets & box nets,
    /// sprinting against river currents (Shift key), and real NASA/BFRI salinity tolerance monitoring.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class HilshaMigrationController : MonoBehaviour
    {
        [Header("Movement")]
        public float swimSpeed = 9.5f;          // Upstream migration cruising speed
        public float turnSpeedDeg = 340f;       // Responsive steering to dodge nets
        public float sprintMultiplier = 1.7f;   // Shift = adrenaline burst against the river current
        public float cruiseY = -0.50f;          // Submerged swim depth
        public float diveY = -1.45f;            // Deep dive to slip under floating net lines
        public float corridorWidth = 12f;       // Backwards compatibility for builders
        public float lateralSpeed = 7f;         // Backwards compatibility for builders

        [Header("River Geography")]
        public float riverStartZ = -275f;       // Bay of Bengal mouth
        public float riverGoalZ = -125f;        // Freshwater spawning grounds
        public float minX = -200f;
        public float maxX = 90f;
        public float minZ = -285f;
        public float maxZ = -80f;

        [Header("Level Data & Salinity Tracking")]
        public string levelFileName = "level_hilsha_bongoposagor_to_meghna";
        private HilshaLevelData levelData;
        private int currentSegmentIndex;
        private float distanceTravelledKm;

        [Header("Tuning")]
        public float outOfRangeDamagePerSec = 18f;

        private Rigidbody rb;
        private Animator animator;
        private SpeciesHealth health;
        private float startWorldZ;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int StressedHash = Animator.StringToHash("Stressed");

        public float ProgressKm => distanceTravelledKm;
        public MigrationCorridorPoint CurrentPoint { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            animator = GetComponent<Animator>();
            health = GetComponent<SpeciesHealth>();

            if (GetComponent<ToolActionController>() == null)
            {
                gameObject.AddComponent<ToolActionController>();
            }
        }

        private void Start()
        {
            LoadLevel();
            startWorldZ = transform.position.z;
        }

        private void LoadLevel()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Levels", levelFileName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[HilshaMigrationController] Level file not found: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            levelData = JsonUtility.FromJson<HilshaLevelData>(json);
            currentSegmentIndex = 0;
            distanceTravelledKm = (levelData != null && levelData.corridor != null && levelData.corridor.Count > 0)
                ? levelData.corridor[0].distanceFromMouthKm : 0f;
            CurrentPoint = (levelData != null && levelData.corridor != null && levelData.corridor.Count > 0)
                ? levelData.corridor[0] : null;
        }

        public void ResetMigration()
        {
            if (levelData == null) LoadLevel();
            currentSegmentIndex = 0;
            distanceTravelledKm = 0f;
            CurrentPoint = (levelData != null && levelData.corridor != null && levelData.corridor.Count > 0)
                ? levelData.corridor[0] : null;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            startWorldZ = riverStartZ;
        }

        private void FixedUpdate()
        {
            if (levelData == null || levelData.corridor == null || levelData.corridor.Count == 0) return;

            Vector2 moveInput = MobileInputManager.GetMoveInput();
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
                moveDir = (fwd * moveInput.y) + (right * moveInput.x);
            }
            else
            {
                moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
            }

            // Upstream river current resistance (gentle downstream drift unless swimming)
            float currentResistance = 0.45f;

            // Sprint check
            bool isSprinting = false;
            bool isDiving = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    isSprinting = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                    isDiving = kb.leftCtrlKey.isPressed || kb.cKey.isPressed || kb.spaceKey.isPressed;
                }
            }
            catch {}
#else
            isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            isDiving = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.Space);
#endif

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedDeg * Time.fixedDeltaTime);

                float speed = swimSpeed * (isSprinting ? sprintMultiplier : 1f);
                Vector3 movement = transform.forward * speed * Time.fixedDeltaTime;

                // Subtract slight river current resistance when swimming upstream
                if (transform.forward.z > 0f)
                {
                    movement.z -= currentResistance * Time.fixedDeltaTime;
                }

                rb.MovePosition(rb.position + movement);
            }

            // Depth control (normal cruise depth vs deep dive to pass under net lines)
            Vector3 pos = rb.position;
            float targetY = isDiving ? diveY : cruiseY;
            pos.y = Mathf.MoveTowards(pos.y, targetY, 2.5f * Time.fixedDeltaTime);

            // Clamp to river channel
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            rb.position = pos;

            // Calculate progress km based on world position progress from startZ (-275) to goalZ (-125)
            float totalRiverLength = Mathf.Abs(riverGoalZ - riverStartZ); // 150m
            float currentWorldProgress = Mathf.Clamp(pos.z - riverStartZ, 0f, totalRiverLength);
            float progressRatio = Mathf.Clamp01(currentWorldProgress / totalRiverLength);

            float maxCorridorKm = levelData.corridor[levelData.corridor.Count - 1].distanceFromMouthKm; // 130 km
            distanceTravelledKm = progressRatio * maxCorridorKm;

            UpdateCurrentSegment();
            EvaluateSafety();

            animator.SetFloat(SpeedHash, moveDir.magnitude);
        }

        private void UpdateCurrentSegment()
        {
            while (currentSegmentIndex < levelData.corridor.Count - 1 &&
                   distanceTravelledKm >= levelData.corridor[currentSegmentIndex + 1].distanceFromMouthKm)
            {
                currentSegmentIndex++;
            }
            CurrentPoint = levelData.corridor[currentSegmentIndex];
        }

        private void EvaluateSafety()
        {
            var p = CurrentPoint;
            if (p == null) return;

            bool safe = p.actualSalinityPSU >= p.minSafeSalinityPSU && p.actualSalinityPSU <= p.maxSafeSalinityPSU;

            animator.SetBool(StressedHash, !safe);

            if (!safe && health != null)
            {
                health.TakeDamage(outOfRangeDamagePerSec * Time.fixedDeltaTime);
                // Push fish back downstream slightly when stressed
                Vector3 pos = rb.position;
                pos.z -= 0.6f * Time.fixedDeltaTime;
                rb.position = pos;
            }

            HudEvents.RaiseEnteredHazardZone(
                safe ? "low" : "high",
                p.actualSalinityPSU,
                $"PSU salinity ({p.location})");
        }

        public bool HasReachedSpawningGrounds()
        {
            if (levelData == null || levelData.corridor.Count == 0) return false;
            return transform.position.z >= riverGoalZ || distanceTravelledKm >= 128f;
        }
    }
}
