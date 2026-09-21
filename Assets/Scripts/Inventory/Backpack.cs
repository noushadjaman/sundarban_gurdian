using System;
using System.Collections.Generic;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Inventory
{
    /// <summary>
    /// Universal backpack and environmental restoration tool system.
    /// Attached to all four species (Tiger, Deer, Dolphin, Hilsha), tracking tool inventory,
    /// proximity to clearable obstacles, and tool usage with +10 HP healing rewards.
    /// </summary>
    public class Backpack : MonoBehaviour
    {
        [Header("Starting Tool Inventory")]
        [Tooltip("Initial count of each tool at level start (default 3)")]
        public int startingCountPerTool = 3;

        [Header("Interaction Settings")]
        public float interactionRadius = 5.0f;

        [Header("Active Selection")]
        public ToolType activeTool = ToolType.Seed;

        public static Backpack LocalPlayerBackpack { get; private set; }

        // Inventory counts
        private readonly int[] toolCounts = new int[4];

        // Nearby clearable obstacle
        public UsableObstacle nearbyObstacle { get; private set; }

        public event Action<ToolType, int> ToolCountChanged;
        public event Action<ToolType> ActiveToolChanged;
        public event Action<UsableObstacle> NearbyObstacleChanged;
        public event Action<UsableObstacle, ToolType> ObstacleRestored;

        private SpeciesHealth speciesHealth;

        private bool isInitialized = false;

        public void EnsureInitialized()
        {
            if (isInitialized) return;
            isInitialized = true;
            for (int i = 0; i < 4; i++)
            {
                if (toolCounts[i] == 0)
                    toolCounts[i] = startingCountPerTool;
            }
            if (speciesHealth == null)
            {
                speciesHealth = GetComponent<SpeciesHealth>();
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            LocalPlayerBackpack = this;
        }

        private void Start()
        {
            EnsureInitialized();

            // Notify UI of initial counts
            for (int i = 0; i < 4; i++)
            {
                ToolCountChanged?.Invoke((ToolType)i, toolCounts[i]);
            }
            ActiveToolChanged?.Invoke(activeTool);
        }

        private void Update()
        {
            // Keep LocalPlayerBackpack updated for active GameObject
            if (gameObject.activeInHierarchy && LocalPlayerBackpack != this)
            {
                LocalPlayerBackpack = this;
            }

            UpdateNearbyObstacle();
        }

        public void UpdateNearbyObstacle()
        {
            UsableObstacle closest = null;
            float closestDist = float.MaxValue;
            Vector3 playerPos = transform.position;

            var list = UsableObstacle.GetAllObstacles();
            for (int i = 0; i < list.Count; i++)
            {
                var obs = list[i];
                if (obs == null || obs.isCleared || !obs.gameObject.activeInHierarchy) continue;

                float maxDist = Mathf.Max(interactionRadius, obs.interactionRadius);
                float dist = Vector3.Distance(playerPos, obs.transform.position);

                if (dist <= maxDist && dist < closestDist)
                {
                    closestDist = dist;
                    closest = obs;
                }
            }

            if (closest != nearbyObstacle)
            {
                nearbyObstacle = closest;
                if (nearbyObstacle != null)
                {
                    // Auto-select matching tool if available
                    if (HasTool(nearbyObstacle.requiredTool))
                    {
                        SelectTool(nearbyObstacle.requiredTool);
                    }
                }
                NearbyObstacleChanged?.Invoke(nearbyObstacle);
            }
        }

        public int GetCount(ToolType tool)
        {
            EnsureInitialized();
            int idx = (int)tool;
            if (idx >= 0 && idx < toolCounts.Length) return toolCounts[idx];
            return 0;
        }

        public bool HasTool(ToolType tool)
        {
            return GetCount(tool) > 0;
        }

        public void AddTool(ToolType tool, int amount = 1)
        {
            if (amount <= 0) return;
            int idx = (int)tool;
            if (idx >= 0 && idx < toolCounts.Length)
            {
                toolCounts[idx] += amount;
                ToolCountChanged?.Invoke(tool, toolCounts[idx]);
                Debug.Log($"[Backpack] Added {amount}x {tool} (Total: {toolCounts[idx]})");
            }
        }

        public bool ConsumeTool(ToolType tool, int amount = 1)
        {
            if (amount <= 0) return false;
            int idx = (int)tool;
            if (idx >= 0 && idx < toolCounts.Length && toolCounts[idx] >= amount)
            {
                toolCounts[idx] -= amount;
                ToolCountChanged?.Invoke(tool, toolCounts[idx]);
                Debug.Log($"[Backpack] Consumed {amount}x {tool} (Remaining: {toolCounts[idx]})");
                return true;
            }
            return false;
        }

        public void SelectTool(ToolType tool)
        {
            if (activeTool != tool)
            {
                activeTool = tool;
                ActiveToolChanged?.Invoke(activeTool);
            }
        }

        /// <summary>
        /// Main interaction entry point: Uses the active tool (or required matching tool)
        /// to dismantle/restore a nearby obstacle, granting +10 HP heal and raising celebration feedback.
        /// </summary>
        public bool TryUseTool()
        {
            if (nearbyObstacle == null || nearbyObstacle.isCleared)
            {
                Debug.LogWarning("[Backpack] No clearable obstacle within interaction range!");
                return false;
            }

            ToolType required = nearbyObstacle.requiredTool;
            if (!HasTool(required))
            {
                Debug.LogWarning($"[Backpack] Missing required tool: {required} to clear {nearbyObstacle.obstacleNameBn}!");
                return false;
            }

            // Consume 1 tool
            if (!ConsumeTool(required, 1)) return false;

            // Clear obstacle
            bool cleared = nearbyObstacle.Clear(this);
            if (cleared)
            {
                // Reward: +10 HP heal
                if (speciesHealth != null)
                {
                    speciesHealth.Heal(10f);
                }

                // Raise restoration feedback event
                string rewardMsg = $"+10 HP — পরিবেশ পুনরুদ্ধার! ({ToolInfo.GetNameBn(required)})";
                HudEvents.RaiseEnvironmentalRestoration(rewardMsg);
                ObstacleRestored?.Invoke(nearbyObstacle, required);
                Debug.Log($"[Backpack] Successfully cleared {nearbyObstacle.name}! {rewardMsg}");

                // Re-check nearby obstacles
                UpdateNearbyObstacle();
                return true;
            }

            return false;
        }
    }
}
