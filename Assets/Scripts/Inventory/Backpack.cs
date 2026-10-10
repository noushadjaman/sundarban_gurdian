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
        [Tooltip("Initial count of each tool at level start (0 = must collect from world)")]
        public int startingCountPerTool = 0;

        [Header("Interaction Settings")]
        public float interactionRadius = 5.0f;

        [Header("Active Selection")]
        public ToolType activeTool = ToolType.Seed;

        public static Backpack LocalPlayerBackpack { get; private set; }

        // Inventory counts
        private readonly int[] toolCounts = new int[4];

        // Nearby clearable obstacle
        public UsableObstacle nearbyObstacle { get; private set; }

        [Header("Food & Sustenance")]
        public int foodCount = 1; // Starts with 1 emergency food ration
        private float nextAutoEatTime = 0f;

        public event Action<ToolType, int> ToolCountChanged;
        public event Action<ToolType> ActiveToolChanged;
        public event Action<UsableObstacle> NearbyObstacleChanged;
        public event Action<UsableObstacle, ToolType> ObstacleRestored;
        public event Action<int> FoodCountChanged;

        private SpeciesHealth speciesHealth;

        private bool isInitialized = false;

        public void EnsureInitialized()
        {
            if (isInitialized) return;
            isInitialized = true;
            for (int i = 0; i < 4; i++)
            {
                toolCounts[i] = startingCountPerTool; // 0 tools by default - must explore to find!
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
            FoodCountChanged?.Invoke(foodCount);
        }

        private void Update()
        {
            // Keep LocalPlayerBackpack updated for active GameObject
            if (gameObject.activeInHierarchy && LocalPlayerBackpack != this)
            {
                LocalPlayerBackpack = this;
            }

            // Keyboard shortcuts (New Input System with safe legacy fallback)
            bool dropPressed = false;
            bool eatPressed = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.gKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) dropPressed = true;
                    if (kb.eKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame) eatPressed = true;
                }
            }
            catch (System.Exception) { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Q)) dropPressed = true;
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.H)) eatPressed = true;
            }
            catch (System.Exception) { }
#endif

            if (dropPressed)
            {
                DropActiveTool();
            }

            if (eatPressed)
            {
                TryEatFood();
            }

            // Auto-eat emergency nutrition if character HP is dangerously low (< 25% of max)
            if (speciesHealth != null && speciesHealth.currentHealth < (speciesHealth.maxHealth * 0.25f) && foodCount > 0)
            {
                if (Time.time >= nextAutoEatTime)
                {
                    nextAutoEatTime = Time.time + 3.0f; // 3 sec cooldown between auto-eating
                    TryEatFood();
                    Debug.Log("[Backpack] Character HP low — automatically ate food to regain HP!");
                }
            }

            UpdateNearbyObstacle();
        }

        public void AddFood(int amount = 1)
        {
            if (amount <= 0) return;
            foodCount += amount;
            FoodCountChanged?.Invoke(foodCount);
            Debug.Log($"[Backpack] Added {amount}x Food (Total: {foodCount})");
        }

        public bool TryEatFood()
        {
            if (foodCount <= 0)
            {
                Debug.LogWarning("[Backpack] No food in inventory!");
                return false;
            }

            if (speciesHealth == null)
            {
                speciesHealth = GetComponent<SpeciesHealth>();
            }

            if (speciesHealth == null) return false;

            foodCount--;
            speciesHealth.EatFood(25f);
            FoodCountChanged?.Invoke(foodCount);
            HudEvents.RaiseEnvironmentalRestoration($"🍗 +25 HP — Ate Food Ration! Restored Health: {speciesHealth.currentHealth:F0}/{speciesHealth.maxHealth:F0}");
            Debug.Log($"[Backpack] Ate food! Remaining food: {foodCount}, Current HP: {speciesHealth.currentHealth}");
            return true;
        }

        /// <summary>
        /// Drops 1 instance of the specified tool into the 3D world in front of player.
        /// Spawns a ToolPickupItem with 3D miniature visual asset and minimap radar icon.
        /// </summary>
        public bool DropTool(ToolType tool, int amount = 1)
        {
            if (amount <= 0) return false;
            int idx = (int)tool;
            if (idx < 0 || idx >= toolCounts.Length || toolCounts[idx] < amount)
            {
                Debug.LogWarning($"[Backpack] Cannot drop {tool}: not enough items in inventory!");
                return false;
            }

            // Consume from inventory
            toolCounts[idx] -= amount;
            ToolCountChanged?.Invoke(tool, toolCounts[idx]);

            // Calculate drop position in front of player
            Vector3 dropPos = transform.position + transform.forward * 2.2f + Vector3.up * 0.4f;

            // Snap to ground/water surface if possible
            if (Physics.Raycast(dropPos + Vector3.up * 3.0f, Vector3.down, out RaycastHit hit, 15f, ~0, QueryTriggerInteraction.Ignore))
            {
                dropPos.y = Mathf.Max(hit.point.y + 0.35f, 0.25f);
            }

            // Create dropped tool GameObject with 3D visual asset
            GameObject dropGo = new GameObject($"Dropped_{tool}");
            dropGo.transform.position = dropPos;
            var pickup = dropGo.AddComponent<ToolPickupItem>();
            pickup.toolType = tool;
            pickup.amount = amount;
            pickup.SetDropCooldown(2.5f);
            pickup.Ensure3DVisualAsset();
            pickup.EnsureTriggerCollider();

            string tName = ToolInfo.GetNameEn(tool);
            HudEvents.RaiseEnvironmentalRestoration($"Dropped {tName} at location");
            Debug.Log($"[Backpack] Dropped {amount}x {tool} at {dropPos}");

            return true;
        }

        public bool DropActiveTool()
        {
            return DropTool(activeTool, 1);
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
                string rewardMsg = $"+10 HP — Habitat Restored! ({ToolInfo.GetNameEn(required)})";
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
