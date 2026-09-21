using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SundarbanGuardian.Data;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Player
{
    /// This is the differentiator mechanic. The Hilsha doesn't avoid circular
    /// "hazard zones" like the land animals — instead it swims along a fixed
    /// upstream path (Bongoposagor -> Meghna -> Padma), and at every point
    /// along that path there is a REAL salinity + temperature reading. If the
    /// player strays outside the fish's biologically safe range for THAT
    /// point, it takes damage and gets pushed back downstream slightly —
    /// modeling why real Hilsha migration is genuinely constrained by these
    /// two variables.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class HilshaMigrationController : MonoBehaviour
    {
        [Header("Movement")]
        public float swimSpeed = 6f;
        public float lateralSpeed = 4f;   // side-to-side movement within the corridor width
        public float corridorWidth = 6f;  // visual width the player can move laterally, regardless of data

        [Header("Level data")]
        public string levelFileName = "level_hilsha_bongoposagor_to_meghna";
        private HilshaLevelData levelData;
        private int currentSegmentIndex;
        private float distanceTravelledKm;

        [Header("Tuning")]
        public float kmPerSecondAtFullSpeed = 0.4f; // how fast "swimSpeed" input translates to km progress
        public float outOfRangeDamagePerSec = 15f;

        private Rigidbody rb;
        private Animator animator;
        private SpeciesHealth health;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int StressedHash = Animator.StringToHash("Stressed"); // fin-flare/distress anim when out of range

        public float ProgressKm => distanceTravelledKm;
        public MigrationCorridorPoint CurrentPoint { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            animator = GetComponent<Animator>();
            health = GetComponent<SpeciesHealth>();
        }

        private void Start()
        {
            LoadLevel();
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
            distanceTravelledKm = levelData.corridor.Count > 0 ? levelData.corridor[0].distanceFromMouthKm : 0f;
            CurrentPoint = levelData.corridor.Count > 0 ? levelData.corridor[0] : null;
        }

        private void FixedUpdate()
        {
            if (levelData == null || levelData.corridor == null || levelData.corridor.Count == 0) return;

            Vector2 moveInput = MobileInputManager.GetMoveInput();
            float forwardInput = Mathf.Max(0f, moveInput.y); // fish only swims upstream, not backward
            float lateralInput = moveInput.x;

            // Progress upstream (this is what the "km from mouth" number represents)
            distanceTravelledKm += forwardInput * swimSpeed * kmPerSecondAtFullSpeed * Time.fixedDeltaTime;

            // Move visually forward + laterally within the fixed corridor width
            Vector3 lateralMove = transform.right * lateralInput * lateralSpeed * Time.fixedDeltaTime;
            Vector3 forwardMove = transform.forward * forwardInput * swimSpeed * Time.fixedDeltaTime;
            Vector3 nextPos = rb.position + forwardMove + lateralMove;

            // clamp lateral position so the player can't swim off the visual river banks
            float lateralOffset = Vector3.Dot(nextPos - transform.position, transform.right);
            nextPos = Vector3.ClampMagnitude(nextPos - transform.position, corridorWidth * 0.5f) + transform.position;
            rb.MovePosition(nextPos);

            UpdateCurrentSegment();
            EvaluateSafety();

            animator.SetFloat(SpeedHash, forwardInput);
        }

        private void UpdateCurrentSegment()
        {
            // advance through the corridor list as distanceTravelledKm increases
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
            bool safe = p.actualSalinityPSU >= p.minSafeSalinityPSU && p.actualSalinityPSU <= p.maxSafeSalinityPSU;

            animator.SetBool(StressedHash, !safe);

            if (!safe && health != null)
            {
                health.TakeDamage(outOfRangeDamagePerSec * Time.fixedDeltaTime);
                // gentle push back downstream while conditions are unsafe —
                // models the real behavior of fish holding position / retreating
                // when salinity is outside their tolerance
                distanceTravelledKm -= 0.02f;
            }

            HudEvents.RaiseEnteredHazardZone(
                safe ? "low" : "high",
                p.actualSalinityPSU,
                "PSU salinity");
        }

        public bool HasReachedSpawningGrounds()
        {
            if (levelData == null || levelData.corridor.Count == 0) return false;
            float finalPointKm = levelData.corridor[levelData.corridor.Count - 1].distanceFromMouthKm;
            return distanceTravelledKm >= finalPointKm;
        }
    }
}
