using System;
using System.IO;
using UnityEngine;
using SundarbanGuardian.Data;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Species Configuration")]
        [Tooltip("Active species: tiger, deer, gangetic_dolphin, or hilsha")]
        public string currentSpecies = "tiger";

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
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // Mobile thermal/battery optimization: Cap frame rate to 60 FPS (prevents thermal throttling and battery drain)
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
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
                GameObject go = GameObject.Find(sName);
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
            }

            if (activeHealth == null)
            {
                activeHealth = UnityEngine.Object.FindAnyObjectByType<SpeciesHealth>();
            }

            if (activeHealth != null)
            {
                activeHealth.Died += HandlePlayerDied;
            }

            if (hilshaController == null)
            {
                hilshaController = UnityEngine.Object.FindAnyObjectByType<HilshaMigrationController>();
            }
        }

        private GameObject FindPlayerForSpecies(string species)
        {
            string sp = species?.ToLowerInvariant().Trim() ?? "";

            if (sp.Contains("tiger"))
                return GameObject.Find("Tiger");
            if (sp.Contains("deer"))
                return GameObject.Find("Deer");
            if (sp.Contains("dolphin"))
                return GameObject.Find("Dolphin");
            if (sp.Contains("hilsha"))
                return GameObject.Find("Hilsha");

            // Fallback by tag or name
            GameObject byTag = GameObject.FindWithTag("Player");
            if (byTag != null) return byTag;

            return GameObject.Find(species);
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
                Vector3 spawnPos = new Vector3(currentLevelData.startZone.centerX, 1f, currentLevelData.startZone.centerZ);
                var cc = playerObject.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerObject.transform.position = spawnPos;
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
                    OnLevelComplete();
                }
            }
        }

        private void HandlePlayerDied()
        {
            if (isGameOver || isLevelComplete) return;

            isGameOver = true;
            Debug.Log("Game Over");
            GameOver?.Invoke();
        }

        private void OnLevelComplete()
        {
            if (isGameOver || isLevelComplete) return;

            isLevelComplete = true;
            Debug.Log("Level Complete!");
            LevelCompleted?.Invoke();
        }

        public static string selectedSpecies;

        public void AdvanceToNextSpecies()
        {
            string cur = currentSpecies?.ToLowerInvariant().Trim() ?? "deer";
            string next;
            if (cur.Contains("deer")) next = "gangetic_dolphin";
            else if (cur.Contains("dolphin")) next = "tiger";
            else if (cur.Contains("tiger")) next = "hilsha";
            else next = "deer";

            selectedSpecies = next;
            currentSpecies = next;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public void RestartLevel()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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
