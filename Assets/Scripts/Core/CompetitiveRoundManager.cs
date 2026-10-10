using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SundarbanGuardian.Data;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Core
{
    [Serializable]
    public class RoundConfig
    {
        public int roundIndex;
        public string roundNumberText;
        public string speciesName;
        public string speciesKey;
        public string missionTitle;
        public string environmentName;
        public string levelFileName;
        public float parTime;
        public int targetScore;
        public Color themeColor;
        [TextArea(2, 4)]
        public string missionSummary;
    }

    [Serializable]
    public class RoundScoreResult
    {
        public int roundIndex;
        public RoundConfig config;
        public float timeTaken;
        public float parTime;
        public int baseScore;
        public int timeBonus;
        public int vitalityBonus;
        public int cleanRunBonus;
        public int totalScore;
        public int starsEarned;
        public string rankGrade;
        public string rankTitle;
        public bool isNewBestTime;
        public bool isNewHighScore;
        public float previousBestTime;
        public int previousHighScore;
    }

    [Serializable]
    public class TournamentResult
    {
        public float totalTournamentTime;
        public int totalTournamentScore;
        public int totalStars;
        public string overallGrade;
        public List<RoundRecord> roundRecords;
    }

    [Serializable]
    public class RoundRecord
    {
        public int roundIndex;
        public float bestTime;
        public int highScore;
        public int stars;
        public bool isUnlocked;
        public bool isCompleted;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string rangerName;
        public float timeTaken;
        public int score;
        public bool isPlayer;
    }

    /// <summary>
    /// Core Competitive Round & Tournament Manager:
    /// Manages speedrun timers, par time targets, dynamic scoring, star ratings,
    /// persistent high scores, simulated live rival leaderboards, and seamless
    /// navigation between rounds (Tiger -> Deer -> Dolphin -> Hilsha -> Grand Finale).
    /// </summary>
    public class CompetitiveRoundManager : MonoBehaviour
    {
        public static CompetitiveRoundManager Instance { get; private set; }

        public static int ActiveRoundIndex { get; set; } = 0;

        [Header("Round Configurations")]
        public RoundConfig[] rounds = new RoundConfig[]
        {
            new RoundConfig
            {
                roundIndex = 0,
                roundNumberText = "ROUND 1 / 4",
                speciesName = "Royal Bengal Tiger",
                speciesKey = "tiger",
                missionTitle = "Destroy 10 Human Traps & Ocean Migration",
                environmentName = "Kotka Tiger Reserve",
                levelFileName = "level_tiger_01",
                parTime = 105f,
                targetScore = 2000,
                themeColor = new Color(0.96f, 0.58f, 0.12f),
                missionSummary = "Pounce on poachers, destroy at least 10 human-made wire snares, and reach the Kotka Ocean Sanctuary to complete Level 1."
            },
            new RoundConfig
            {
                roundIndex = 1,
                roundNumberText = "ROUND 2 / 4",
                speciesName = "Chitra Spotted Deer",
                speciesKey = "deer",
                missionTitle = "Collect Seeds & Reforest 3 Deforestation Zones",
                environmentName = "Wildlife Sanctuary Mudflats",
                levelFileName = "level_deer_01",
                parTime = 85f,
                targetScore = 2200,
                themeColor = new Color(0.22f, 0.85f, 0.45f),
                missionSummary = "Collect seeds from the ground, plant 5 seeds into 3 deforestation zones to restore them into lush forest (red turns green on radar), and reach the goal."
            },
            new RoundConfig
            {
                roundIndex = 2,
                roundNumberText = "ROUND 3 / 4",
                speciesName = "Gangetic River Dolphin",
                speciesKey = "dolphin",
                missionTitle = "River to Ocean Migration & Net-Cutter Run",
                environmentName = "Passur-Shibsa Estuary",
                levelFileName = "level_dolphin_01",
                parTime = 90f,
                targetScore = 2500,
                themeColor = new Color(0.20f, 0.75f, 0.95f),
                missionSummary = "Use 3 default Net-Cutter knives to slice illegal river nets. Leaping over nets costs 50 HP. Endure deep purple polluted waters (5 HP/sec loss) and reach the ocean."
            },
            new RoundConfig
            {
                roundIndex = 3,
                roundNumberText = "ROUND 4 / 4 [GRAND FINALE]",
                speciesName = "Mother Hilsha Shad",
                speciesKey = "hilsha",
                missionTitle = "The 130km Great Spawning Run vs Illegal Gillnets",
                environmentName = "Bay of Bengal to Meghna/Padma Confluence",
                levelFileName = "level_hilsha_bongoposagor_to_meghna",
                parTime = 120f,
                targetScore = 3000,
                themeColor = new Color(0.35f, 0.95f, 0.82f),
                missionSummary = "Championship Migration: Swim 130km upstream through salinity changes, evading illegal gillnets during the 22-day October spawning ban."
            }
        };

        // Live Timing
        private float roundTimer = 0f;
        private bool isTimerRunning = false;

        // Events
        public event Action<RoundConfig> RoundStarted;
        public event Action<float, float> RoundTimerTick; // elapsed, parTime
        public event Action<RoundScoreResult> RoundCompleted;
        public event Action<TournamentResult> TournamentCompleted;
        public event Action<RoundConfig, float> RoundFailed;

        public float CurrentRoundTime => roundTimer;
        public bool IsTimerRunning => isTimerRunning;
        public RoundConfig CurrentRoundConfig => (ActiveRoundIndex >= 0 && ActiveRoundIndex < rounds.Length) ? rounds[ActiveRoundIndex] : rounds[0];

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Ensure Round 1 is always unlocked
            if (PlayerPrefs.GetInt("Round_0_Unlocked", 1) == 0)
            {
                PlayerPrefs.SetInt("Round_0_Unlocked", 1);
            }
        }

        private void Start()
        {
            // Sync active round with GameManager's current species if already set
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                string sp = !string.IsNullOrEmpty(GameManager.selectedSpecies) ? GameManager.selectedSpecies : gm.currentSpecies;
                ActiveRoundIndex = GetRoundIndexForSpecies(sp);
            }

            StartRoundTimer();
        }

        private void Update()
        {
            if (isTimerRunning)
            {
                roundTimer += Time.deltaTime;
                RoundTimerTick?.Invoke(roundTimer, CurrentRoundConfig.parTime);
            }
        }

        public void StartRoundTimer()
        {
            roundTimer = 0f;
            isTimerRunning = true;
            RoundStarted?.Invoke(CurrentRoundConfig);
        }

        public void PauseRoundTimer()
        {
            isTimerRunning = false;
        }

        public void ResumeRoundTimer()
        {
            isTimerRunning = true;
        }

        public int GetRoundIndexForSpecies(string species)
        {
            string s = (species ?? "").ToLowerInvariant();
            if (s.Contains("deer")) return 1;
            if (s.Contains("dolphin")) return 2;
            if (s.Contains("hilsha")) return 3;
            return 0; // Default Tiger
        }

        public RoundConfig GetConfig(int roundIndex)
        {
            if (roundIndex >= 0 && roundIndex < rounds.Length)
                return rounds[roundIndex];
            return rounds[0];
        }

        public RoundRecord GetRecord(int roundIndex)
        {
            return new RoundRecord
            {
                roundIndex = roundIndex,
                bestTime = PlayerPrefs.GetFloat($"Round_{roundIndex}_BestTime", 0f),
                highScore = PlayerPrefs.GetInt($"Round_{roundIndex}_HighScore", 0),
                stars = PlayerPrefs.GetInt($"Round_{roundIndex}_Stars", 0),
                isUnlocked = PlayerPrefs.GetInt($"Round_{roundIndex}_Unlocked", roundIndex == 0 ? 1 : 0) == 1,
                isCompleted = PlayerPrefs.GetInt($"Round_{roundIndex}_Completed", 0) == 1
            };
        }

        /// <summary>
        /// Complete the current round, calculate competitive score, update high scores, and fire events.
        /// </summary>
        public RoundScoreResult CompleteCurrentRound(SpeciesHealth health)
        {
            isTimerRunning = false;

            RoundConfig cfg = CurrentRoundConfig;
            float finalTime = roundTimer;
            float healthPct = 1f;
            if (health != null && health.maxHealth > 0f)
            {
                healthPct = Mathf.Clamp01(health.currentHealth / health.maxHealth);
            }

            // Scoring Engine
            int baseScore = 1000;
            int timeBonus = Mathf.Max(0, Mathf.RoundToInt((cfg.parTime - finalTime) * 25f));
            int vitalityBonus = Mathf.RoundToInt(healthPct * 500f);
            int cleanRunBonus = healthPct >= 0.85f ? 350 : (healthPct >= 0.50f ? 150 : 0);
            int totalScore = baseScore + timeBonus + vitalityBonus + cleanRunBonus;

            // Star Calculation (★★★)
            int stars = 1;
            if (finalTime <= cfg.parTime && healthPct >= 0.50f)
                stars = 3;
            else if (finalTime <= cfg.parTime * 1.35f || totalScore >= cfg.targetScore * 0.75f)
                stars = 2;

            // Rank Grade
            string grade = "B";
            string title = "FOREST RANGER";
            if (stars == 3)
            {
                grade = "S";
                title = "APEX GUARDIAN";
            }
            else if (stars == 2)
            {
                grade = "A";
                title = "ECOSYSTEM DEFENDER";
            }
            else if (finalTime > cfg.parTime * 1.5f)
            {
                grade = "C";
                title = "NOVICE SCOUT";
            }

            // Save Records
            float prevBestTime = PlayerPrefs.GetFloat($"Round_{cfg.roundIndex}_BestTime", 0f);
            int prevHighScore = PlayerPrefs.GetInt($"Round_{cfg.roundIndex}_HighScore", 0);

            bool isNewBestTime = (prevBestTime <= 0f || finalTime < prevBestTime);
            bool isNewHighScore = (totalScore > prevHighScore);

            if (isNewBestTime)
            {
                PlayerPrefs.SetFloat($"Round_{cfg.roundIndex}_BestTime", finalTime);
            }
            if (isNewHighScore)
            {
                PlayerPrefs.SetInt($"Round_{cfg.roundIndex}_HighScore", totalScore);
            }

            int prevStars = PlayerPrefs.GetInt($"Round_{cfg.roundIndex}_Stars", 0);
            if (stars > prevStars)
            {
                PlayerPrefs.SetInt($"Round_{cfg.roundIndex}_Stars", stars);
            }

            PlayerPrefs.SetInt($"Round_{cfg.roundIndex}_Completed", 1);

            // Unlock next round
            if (cfg.roundIndex + 1 < rounds.Length)
            {
                PlayerPrefs.SetInt($"Round_{cfg.roundIndex + 1}_Unlocked", 1);
            }
            PlayerPrefs.Save();

            RoundScoreResult result = new RoundScoreResult
            {
                roundIndex = cfg.roundIndex,
                config = cfg,
                timeTaken = finalTime,
                parTime = cfg.parTime,
                baseScore = baseScore,
                timeBonus = timeBonus,
                vitalityBonus = vitalityBonus,
                cleanRunBonus = cleanRunBonus,
                totalScore = totalScore,
                starsEarned = stars,
                rankGrade = grade,
                rankTitle = title,
                isNewBestTime = isNewBestTime,
                isNewHighScore = isNewHighScore,
                previousBestTime = prevBestTime,
                previousHighScore = prevHighScore
            };

            RoundCompleted?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Launch a specific round seamlessly with dynamic in-scene transition.
        /// </summary>
        public void LaunchRound(int roundIndex, bool forceSceneReload = false)
        {
            if (roundIndex < 0 || roundIndex >= rounds.Length) roundIndex = 0;
            ActiveRoundIndex = roundIndex;
            RoundConfig targetConfig = rounds[roundIndex];

            GameManager.selectedSpecies = targetConfig.speciesKey;

            if (forceSceneReload)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            // In-Scene Seamless Transition
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.currentSpecies = targetConfig.speciesKey;
                gm.SwitchToSpeciesSeamlessly(targetConfig.speciesKey);
            }

            StartRoundTimer();
        }

        /// <summary>
        /// Advance to the next round, or trigger Grand Tournament Victory if round 4 was completed.
        /// </summary>
        public void AdvanceToNextRound()
        {
            if (ActiveRoundIndex < rounds.Length - 1)
            {
                LaunchRound(ActiveRoundIndex + 1);
            }
            else
            {
                // Grand Tournament Championship Complete!
                TriggerTournamentCompletion();
            }
        }

        /// <summary>
        /// Restart the current active round.
        /// </summary>
        public void RestartCurrentRound()
        {
            LaunchRound(ActiveRoundIndex);
        }

        public void FailCurrentRound()
        {
            isTimerRunning = false;
            RoundFailed?.Invoke(CurrentRoundConfig, roundTimer);
        }

        public void TriggerTournamentCompletion()
        {
            isTimerRunning = false;

            float totalTime = 0f;
            int totalScore = 0;
            int totalStars = 0;
            List<RoundRecord> recs = new List<RoundRecord>();

            for (int i = 0; i < rounds.Length; i++)
            {
                var r = GetRecord(i);
                recs.Add(r);
                totalTime += r.bestTime;
                totalScore += r.highScore;
                totalStars += r.stars;
            }

            string overallGrade = "APEX GUARDIAN (S-TIER)";
            if (totalStars >= 12) overallGrade = "CHAMPION OF THE SUNDARBANS (SSS-TIER)";
            else if (totalStars >= 10) overallGrade = "ROYAL ECOSYSTEM GUARDIAN (SS-TIER)";
            else if (totalStars >= 8) overallGrade = "DELTA DEFENDER (A-TIER)";
            else overallGrade = "WILDLIFE RANGER (B-TIER)";

            TournamentResult tourneyResult = new TournamentResult
            {
                totalTournamentTime = totalTime,
                totalTournamentScore = totalScore,
                totalStars = totalStars,
                overallGrade = overallGrade,
                roundRecords = recs
            };

            TournamentCompleted?.Invoke(tourneyResult);
        }

        /// <summary>
        /// Generate a simulated live leaderboard with authentic Sundarban ranger names
        /// and dynamic placement of the player.
        /// </summary>
        public List<LeaderboardEntry> GetLeaderboardForRound(int roundIndex, int playerScore, float playerTime)
        {
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

            // Baseline rival rangers per round
            string[][] rivalNames = new string[][]
            {
                new string[] { "SundarbanApex_99", "KotkaPatrol_Rafi", "DeltaRanger_Tanvir", "ShunderbanScout" },
                new string[] { "ChitraSpeedrunner", "MangroveWarden_Joy", "EstuaryRanger_09", "ForestScout_Babu" },
                new string[] { "SonarPilot_Mehedi", "ShibsaNavigator", "DolphinGuardian_7", "PassurRiverPatrol" },
                new string[] { "HilsaChampion_Fahim", "PadmaMeghnaHero", "FisheriesGuardian", "BayBreaker_2025" }
            };

            int[] baseRivalScores = new int[][]
            {
                new int[] { 2350, 2080, 1820, 1590 },
                new int[] { 2510, 2240, 1980, 1670 },
                new int[] { 2720, 2390, 2050, 1780 },
                new int[] { 3420, 3050, 2710, 2350 }
            }[Mathf.Clamp(roundIndex, 0, 3)];

            float[] baseRivalTimes = new float[][]
            {
                new float[] { 74.2f, 86.5f, 98.9f, 112.1f },
                new float[] { 54.8f, 64.1f, 72.7f, 81.4f },
                new float[] { 66.3f, 78.4f, 87.5f, 96.2f },
                new float[] { 88.6f, 102.1f, 116.8f, 128.4f }
            }[Mathf.Clamp(roundIndex, 0, 3)];

            string[] names = rivalNames[Mathf.Clamp(roundIndex, 0, 3)];

            for (int i = 0; i < 4; i++)
            {
                entries.Add(new LeaderboardEntry
                {
                    rangerName = names[i],
                    score = baseRivalScores[i],
                    timeTaken = baseRivalTimes[i],
                    isPlayer = false
                });
            }

            // Insert player
            entries.Add(new LeaderboardEntry
            {
                rangerName = "YOU (GUARDIAN)",
                score = playerScore,
                timeTaken = playerTime,
                isPlayer = true
            });

            // Sort by Score descending, then by Time ascending
            entries.Sort((a, b) =>
            {
                if (b.score != a.score) return b.score.CompareTo(a.score);
                return a.timeTaken.CompareTo(b.timeTaken);
            });

            // Assign ranks 1 to 5
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].rank = i + 1;
            }

            return entries;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int mins = Mathf.FloorToInt(seconds / 60f);
            float secs = seconds % 60f;
            return string.Format("{0:00}:{1:00.00}", mins, secs);
        }
    }
}
