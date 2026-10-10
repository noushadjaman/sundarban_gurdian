using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SundarbanGuardian.Data;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Species Configuration")]
        [Tooltip("Active species: tiger, deer, gangetic_dolphin, or hilsha")]
        public string currentSpecies = "tiger";

        public static GameManager Instance { get; private set; }

        [Header("Version 2 Mission Progress")]
        public int trapsDestroyedCount = 0; // Target: 10 for Tiger Level 1
        public int zonesRestoredCount = 0;   // Target: 3 for Deer Level 2
        private float lastGoalWarningTime = -10f;

        [Header("Scene References (Optional - Auto-resolved if null)")]
        public LevelLoader levelLoader;
        public GameObject playerObject;
        public SpeciesHealth activeHealth;

        private LevelData currentLevelData;
        private HilshaMigrationController hilshaController;
        private bool isGameOver = false;
        private bool isLevelComplete = false;

        public bool IsGameOver => isGameOver;
        public bool IsLevelComplete => isLevelComplete;
        public LevelData CurrentLevelData => currentLevelData;

        public event Action LevelCompleted;
        public event Action GameOver;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            EnsureCompetitiveRoundManager();
        }

        private void EnsureCompetitiveRoundManager()
        {
            if (CompetitiveRoundManager.Instance == null)
            {
                var crm = FindAnyObjectByType<CompetitiveRoundManager>();
                if (crm == null)
                {
                    gameObject.AddComponent<CompetitiveRoundManager>();
                }
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;

            if (string.IsNullOrEmpty(selectedSpecies))
            {
                selectedSpecies = PlayerPrefs.GetString("SelectedSpecies", "");
            }

            if (!string.IsNullOrEmpty(selectedSpecies))
            {
                currentSpecies = selectedSpecies;
            }
            else
            {
                selectedSpecies = currentSpecies;
            }

            InitializeLevelLoader();
            InitializePlayer();
            LoadSpeciesLevel();

            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.ActiveRoundIndex = CompetitiveRoundManager.Instance.GetRoundIndexForSpecies(currentSpecies);
                CompetitiveRoundManager.Instance.StartRoundTimer();
            }
        }

        private void InitializeLevelLoader()
        {
            if (levelLoader == null)
            {
                levelLoader = UnityEngine.Object.FindAnyObjectByType<LevelLoader>();
            }

            if (levelLoader == null)
            {
                Debug.LogError("[GameManager] LevelLoader not found in scene!");
            }
        }

        private void InitializePlayer()
        {
            if (playerObject == null)
            {
                playerObject = FindPlayerForSpecies(currentSpecies);
            }

            // Ensure only the selected species is active in the scene
            string[] allSpecies = { "Tiger", "Deer", "Dolphin", "Hilsha" };
            foreach (string sName in allSpecies)
            {
                GameObject go = FindPlayerForSpecies(sName);
                if (go != null)
                {
                    bool isSelected = (playerObject != null && go == playerObject);
                    go.SetActive(isSelected);
                }
            }

            if (playerObject != null)
            {
                playerObject.SetActive(true);

                if (activeHealth == null)
                {
                    activeHealth = playerObject.GetComponent<SpeciesHealth>();
                }

                hilshaController = playerObject.GetComponent<HilshaMigrationController>();

                // Ensure CameraFollow targets the active player on startup
                var cam = Camera.main;
                if (cam != null)
                {
                    var cameraFollow = cam.GetComponent<CameraFollow>();
                    if (cameraFollow != null)
                    {
                        cameraFollow.target = playerObject.transform;
                        cameraFollow.ApplySpeciesPreset(playerObject.name);
                    }
                }
            }

            if (activeHealth == null)
            {
                activeHealth = UnityEngine.Object.FindAnyObjectByType<SpeciesHealth>();
            }

            if (activeHealth != null)
            {
                activeHealth.Died -= HandlePlayerDied;
                activeHealth.Died += HandlePlayerDied;
            }

            if (hilshaController == null)
            {
                hilshaController = UnityEngine.Object.FindAnyObjectByType<HilshaMigrationController>();
            }

            // V2 Species defaults: Gangetic River Dolphin receives 3 default Net-Cutter Knives
            if (currentSpecies.ToLower().Contains("dolphin") && playerObject != null)
            {
                var bp = playerObject.GetComponent<SundarbanGuardian.Inventory.Backpack>();
                if (bp != null)
                {
                    if (bp.GetCount(SundarbanGuardian.Inventory.ToolType.NetCutter) < 3)
                    {
                        bp.AddTool(SundarbanGuardian.Inventory.ToolType.NetCutter, 3 - bp.GetCount(SundarbanGuardian.Inventory.ToolType.NetCutter));
                    }
                    bp.SelectTool(SundarbanGuardian.Inventory.ToolType.NetCutter);
                }
            }
        }

        public GameObject FindPlayerForSpecies(string species)
        {
            string sp = species?.ToLowerInvariant().Trim() ?? "";
            string targetName = "Tiger";
            if (sp.Contains("deer")) targetName = "Deer";
            else if (sp.Contains("dolphin")) targetName = "Dolphin";
            else if (sp.Contains("hilsha")) targetName = "Hilsha";

            // Check active objects in scene
            GameObject go = GameObject.Find(targetName);
            if (go != null) return go;

            // Search all scene objects including inactive
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var candidate in all)
            {
                if (candidate.hideFlags != HideFlags.None || !candidate.scene.isLoaded) continue;
                if (candidate.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            // Fallback by tag
            GameObject byTag = GameObject.FindWithTag("Player");
            if (byTag != null) return byTag;

            return null;
        }

        public void SwitchToSpeciesSeamlessly(string species)
        {
            currentSpecies = species;
            selectedSpecies = species;
            isGameOver = false;
            isLevelComplete = false;

            GameObject newPlayer = FindPlayerForSpecies(species);
            if (newPlayer == null)
            {
                Debug.LogWarning($"[GameManager] Could not find player object for species '{species}'. Reloading scene.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            // Unhook old health
            if (activeHealth != null)
            {
                activeHealth.Died -= HandlePlayerDied;
            }

            // Deactivate all species except the target one
            string[] allSpecies = { "Tiger", "Deer", "Dolphin", "Hilsha" };
            foreach (string sName in allSpecies)
            {
                GameObject obj = FindPlayerForSpecies(sName);
                if (obj != null)
                {
                    obj.SetActive(obj == newPlayer);
                }
            }

            playerObject = newPlayer;
            playerObject.SetActive(true);

            activeHealth = playerObject.GetComponent<SpeciesHealth>();
            if (activeHealth != null)
            {
                activeHealth.ResetHealth();
                activeHealth.Died -= HandlePlayerDied;
                activeHealth.Died += HandlePlayerDied;
            }

            hilshaController = playerObject.GetComponent<HilshaMigrationController>();
            if (hilshaController != null)
            {
                hilshaController.ResetMigration();
            }

            // Update camera target and preset
            var cam = Camera.main;
            if (cam != null)
            {
                var cameraFollow = cam.GetComponent<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.target = playerObject.transform;
                    cameraFollow.ApplySpeciesPreset(playerObject.name);
                }
            }

            // Reload Level Data & Spawn Hazards / Markers
            LoadSpeciesLevel();

            // Reset V2 Mission Progress
            trapsDestroyedCount = 0;
            zonesRestoredCount = 0;

            if (species.ToLower().Contains("dolphin") && playerObject != null)
            {
                var bp = playerObject.GetComponent<SundarbanGuardian.Inventory.Backpack>();
                if (bp != null)
                {
                    if (bp.GetCount(SundarbanGuardian.Inventory.ToolType.NetCutter) < 3)
                    {
                        bp.AddTool(SundarbanGuardian.Inventory.ToolType.NetCutter, 3 - bp.GetCount(SundarbanGuardian.Inventory.ToolType.NetCutter));
                    }
                    bp.SelectTool(SundarbanGuardian.Inventory.ToolType.NetCutter);
                }
            }

            // Sync AnimalSwitcher if present
            var switcher = FindAnyObjectByType<AnimalSwitcher>();
            if (switcher != null)
            {
                switcher.activeSpecies = playerObject.name;
            }

            Debug.Log($"<color=green>[GameManager] Seamlessly switched to round species: {playerObject.name}</color>");
        }

        private void LoadSpeciesLevel()
        {
            if (levelLoader == null) return;

            string levelFileName = GetLevelFileNameForSpecies(currentSpecies);
            currentLevelData = levelLoader.LoadLevel(levelFileName);
            Debug.Log($"[GameManager] Loaded level '{levelFileName}' for species '{currentSpecies}'");

            // Spawn player at startZone if specified
            if (currentLevelData != null && currentLevelData.startZone != null && playerObject != null)
            {
                float spawnY = 0f;
                bool isAquatic = currentSpecies.ToLower().Contains("dolphin") || currentSpecies.ToLower().Contains("hilsha");

                if (!isAquatic)
                {
                    Ray ray = new Ray(new Vector3(currentLevelData.startZone.centerX, 35f, currentLevelData.startZone.centerZ), Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        spawnY = hit.point.y + 0.05f;
                    }
                    else
                    {
                        spawnY = 0.45f;
                    }
                }
                else
                {
                    spawnY = -0.40f; // Submerged swimming depth
                }

                Vector3 spawnPos = new Vector3(currentLevelData.startZone.centerX, spawnY, currentLevelData.startZone.centerZ);
                var cc = playerObject.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                var rb = playerObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = spawnPos;
                }
                playerObject.transform.position = spawnPos;
                playerObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Facing South heading to the Ocean
                if (cc != null) cc.enabled = true;
            }
        }

        public string GetLevelFileNameForSpecies(string species)
        {
            string sp = species?.ToLowerInvariant().Trim() ?? "";

            if (sp.Contains("hilsha"))
                return "level_hilsha_bongoposagor_to_meghna";
            if (sp.Contains("dolphin"))
                return "level_dolphin_01";
            if (sp.Contains("deer"))
                return "level_deer_01";
            if (sp.Contains("tiger"))
                return "level_tiger_01";

            return $"level_{sp}_01";
        }

        private void Update()
        {
            if (isGameOver || isLevelComplete) return;

            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            // For Hilsha: win when HasReachedSpawningGrounds() returns true
            if (hilshaController != null)
            {
                if (hilshaController.HasReachedSpawningGrounds())
                {
                    OnLevelComplete();
                    return;
                }
            }

            // For land animals (and others using LevelData): win when position is within goalZone radius
            if (currentLevelData != null && currentLevelData.goalZone != null && playerObject != null)
            {
                Vector3 playerPos = playerObject.transform.position;
                Vector3 goalPos = new Vector3(currentLevelData.goalZone.centerX, playerPos.y, currentLevelData.goalZone.centerZ);
                float dist = Vector3.Distance(playerPos, goalPos);

                if (dist <= currentLevelData.goalZone.radius)
                {
                    string sp = currentSpecies?.ToLowerInvariant().Trim() ?? "";

                    // Level 1: Tiger requires destroying 10 human-made traps
                    if (sp.Contains("tiger") && trapsDestroyedCount < 10)
                    {
                        if (Time.time - lastGoalWarningTime > 3.0f)
                        {
                            lastGoalWarningTime = Time.time;
                            HudEvents.RaiseEnvironmentalRestoration($"⚠️ Destroy 10 Human-Made Traps first! ({trapsDestroyedCount}/10 Destroyed)");
                        }
                        return;
                    }

                    // Level 2: Deer requires restoring 3 deforestation zones with seeds
                    if (sp.Contains("deer") && zonesRestoredCount < 3)
                    {
                        if (Time.time - lastGoalWarningTime > 3.0f)
                        {
                            lastGoalWarningTime = Time.time;
                            HudEvents.RaiseEnvironmentalRestoration($"⚠️ Restore 3 Deforestation Zones with Seeds first! ({zonesRestoredCount}/3 Restored)");
                        }
                        return;
                    }

                    OnLevelComplete();
                }
            }
        }

        private void HandlePlayerDied()
        {
            if (isGameOver || isLevelComplete) return;

            // Only trigger game over if the active player has actually died (0 HP)
            if (activeHealth != null && activeHealth.currentHealth > 0.01f)
            {
                Debug.LogWarning($"[GameManager] Ignored Died event: active player '{playerObject?.name}' has {activeHealth.currentHealth:F1} HP remaining.");
                return;
            }

            isGameOver = true;
            Debug.Log("Game Over");

            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.FailCurrentRound();
            }

            GameOver?.Invoke();
        }

        private void OnLevelComplete()
        {
            if (isGameOver || isLevelComplete) return;

            isLevelComplete = true;
            Debug.Log("Level Complete!");

            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.CompleteCurrentRound(activeHealth);
            }

            LevelCompleted?.Invoke();
        }

        public static string selectedSpecies;

        public void AdvanceToNextSpecies()
        {
            Time.timeScale = 1f;
            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.AdvanceToNextRound();
            }
            else
            {
                string cur = currentSpecies?.ToLowerInvariant().Trim() ?? "tiger";
                string next;
                if (cur.Contains("tiger")) next = "deer";
                else if (cur.Contains("deer")) next = "gangetic_dolphin";
                else if (cur.Contains("dolphin")) next = "hilsha";
                else next = "tiger";

                selectedSpecies = next;
                currentSpecies = next;
                PlayerPrefs.SetString("SelectedSpecies", next);
                PlayerPrefs.Save();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            if (CompetitiveRoundManager.Instance != null)
            {
                CompetitiveRoundManager.Instance.RestartCurrentRound();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnDestroy()
        {
            if (activeHealth != null)
            {
                activeHealth.Died -= HandlePlayerDied;
            }
        }
    }
}
