using System;
using System.Collections.Generic;
using UnityEngine;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Base class for any environmental hazard or obstacle in the Sundarbans that can be
    /// cleared, dismantled, or restored using one of the 4 universal backpack tools.
    /// </summary>
    public abstract class UsableObstacle : MonoBehaviour
    {
        [Header("Tool Requirement")]
        public ToolType requiredTool = ToolType.Seed;
        public string obstacleNameEn = "Environmental Hazard";
        public string obstacleDescriptionEn = "Use matching tool to restore the habitat.";
        public string obstacleNameBn = "পরিবেশগত বাধা";
        public string obstacleDescription = "টুল প্রয়োগ করে পরিবেশ পুনরুদ্ধার করুন।";
        public float interactionRadius = 5.0f;

        [Header("State")]
        public bool isCleared = false;

        public event Action<UsableObstacle> Cleared;

        public static readonly List<UsableObstacle> ActiveObstacles = new List<UsableObstacle>();

        public static List<UsableObstacle> GetAllObstacles()
        {
            ActiveObstacles.RemoveAll(x => x == null);
            if (ActiveObstacles.Count == 0)
            {
                var found = UnityEngine.Object.FindObjectsByType<UsableObstacle>(FindObjectsSortMode.None);
                foreach (var f in found)
                {
                    if (f != null && !ActiveObstacles.Contains(f))
                        ActiveObstacles.Add(f);
                }
            }
            return ActiveObstacles;
        }

        protected virtual void OnEnable()
        {
            if (!ActiveObstacles.Contains(this))
                ActiveObstacles.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActiveObstacles.Remove(this);
        }

        public virtual bool CanClearWith(ToolType tool)
        {
            return !isCleared && tool == requiredTool;
        }

        /// <summary>
        /// Execute clearance and environmental restoration.
        /// </summary>
        public virtual bool Clear(Backpack playerBackpack)
        {
            if (isCleared) return false;

            isCleared = true;
            Cleared?.Invoke(this);
            OnClearedVisuals();
            return true;
        }

        /// <summary>
        /// Override in derived classes for custom particles, animations, mesh changes.
        /// </summary>
        protected abstract void OnClearedVisuals();
    }
}
