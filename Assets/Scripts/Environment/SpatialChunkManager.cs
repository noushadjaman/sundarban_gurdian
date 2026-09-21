using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Lightweight spatial chunking manager for mobile performance.
    /// Partitions world vegetation/props into a 60x60m grid and toggles chunks
    /// within a 1-2 cell radius around the player every 0.5s via coroutine.
    /// </summary>
    public class SpatialChunkManager : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [Tooltip("Size of each square chunk in world units")]
        public float chunkSize = 60f;

        [Tooltip("Active radius in chunk units (1 = 3x3 grid around player, 180m span)")]
        public int activeRadius = 1;

        [Tooltip("Interval in seconds between chunk visibility updates")]
        public float updateInterval = 0.5f;

        [Header("Target Tracking")]
        public Transform playerTransform;

        [System.Serializable]
        public class Chunk
        {
            public int gridX;
            public int gridZ;
            public GameObject rootObject;
            public bool isCurrentlyActive = true;
        }

        [SerializeField]
        private List<Chunk> registeredChunks = new List<Chunk>();

        private int lastPlayerChunkX = int.MinValue;
        private int lastPlayerChunkZ = int.MinValue;

        private void Start()
        {
            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform == null)
            {
                var gm = Object.FindAnyObjectByType<SundarbanGuardian.Core.GameManager>();
                if (gm != null && gm.playerObject != null) playerTransform = gm.playerObject.transform;
            }

            if (playerTransform == null && Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }

            StartCoroutine(UpdateChunksRoutine());
        }

        private IEnumerator UpdateChunksRoutine()
        {
            var wait = new WaitForSeconds(updateInterval);

            while (true)
            {
                if (playerTransform == null)
                {
                    var p = GameObject.FindWithTag("Player");
                    if (p != null) playerTransform = p.transform;
                }

                if (playerTransform != null)
                {
                    Vector3 pos = playerTransform.position;
                    int curChunkX = Mathf.FloorToInt(pos.x / chunkSize);
                    int curChunkZ = Mathf.FloorToInt(pos.z / chunkSize);

                    if (curChunkX != lastPlayerChunkX || curChunkZ != lastPlayerChunkZ)
                    {
                        lastPlayerChunkX = curChunkX;
                        lastPlayerChunkZ = curChunkZ;
                        UpdateChunkVisibility(curChunkX, curChunkZ);
                    }
                }

                yield return wait;
            }
        }

        public void UpdateChunkVisibility(int centerChunkX, int centerChunkZ)
        {
            for (int i = 0; i < registeredChunks.Count; i++)
            {
                var chunk = registeredChunks[i];
                if (chunk.rootObject == null) continue;

                int dx = Mathf.Abs(chunk.gridX - centerChunkX);
                int dz = Mathf.Abs(chunk.gridZ - centerChunkZ);

                bool shouldBeActive = (dx <= activeRadius && dz <= activeRadius);

                if (chunk.isCurrentlyActive != shouldBeActive)
                {
                    chunk.rootObject.SetActive(shouldBeActive);
                    chunk.isCurrentlyActive = shouldBeActive;
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Build Spatial Chunks From Children")]
        public void BuildSpatialChunksFromChildren()
        {
            registeredChunks.Clear();

            // Find all vegetation objects in Mangrove_Forest
            var forest = GameObject.Find("Mangrove_Forest");
            if (forest == null)
            {
                Debug.LogWarning("[SpatialChunkManager] Mangrove_Forest not found!");
                return;
            }

            // Categories to partition
            string[] chunkableCategories = new string[] {
                "Safe_Dense_Canopy",
                "Coastal_Coconut_Groves",
                "Riverbank_Mangroves",
                "East_Wild_Mangrove_Swamp",
                "Deforestation_Zones"
            };

            var chunkMap = new Dictionary<Vector2Int, GameObject>();

            // Root container for chunks
            Transform chunkRoot = forest.transform.Find("Spatial_Vegetation_Chunks");
            if (chunkRoot == null)
            {
                var go = new GameObject("Spatial_Vegetation_Chunks");
                go.transform.SetParent(forest.transform, false);
                chunkRoot = go.transform;
            }

            int movedObjects = 0;

            foreach (var catName in chunkableCategories)
            {
                var catTr = forest.transform.Find(catName);
                if (catTr == null) continue;

                var children = new List<Transform>();
                for (int i = 0; i < catTr.childCount; i++)
                {
                    children.Add(catTr.GetChild(i));
                }

                foreach (var child in children)
                {
                    // Don't chunk triggers or pickups
                    if (child.GetComponent<Collider>() != null && child.GetComponent<Collider>().isTrigger) continue;
                    if (child.name.Contains("Pickup") || child.name.Contains("Goal") || child.name.Contains("Trail")) continue;

                    Vector3 pos = child.position;
                    int gx = Mathf.FloorToInt(pos.x / chunkSize);
                    int gz = Mathf.FloorToInt(pos.z / chunkSize);
                    var key = new Vector2Int(gx, gz);

                    if (!chunkMap.TryGetValue(key, out GameObject chunkObj))
                    {
                        string cName = $"Chunk_{gx}_{gz}";
                        var existing = chunkRoot.Find(cName);
                        if (existing != null)
                        {
                            chunkObj = existing.gameObject;
                        }
                        else
                        {
                            chunkObj = new GameObject(cName);
                            chunkObj.transform.SetParent(chunkRoot, false);
                        }
                        chunkMap[key] = chunkObj;
                    }

                    child.SetParent(chunkObj.transform, true);
                    movedObjects++;
                }
            }

            // Register chunks
            foreach (var kvp in chunkMap)
            {
                registeredChunks.Add(new Chunk
                {
                    gridX = kvp.Key.x,
                    gridZ = kvp.Key.y,
                    rootObject = kvp.Value,
                    isCurrentlyActive = true
                });
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(forest);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log($"[SpatialChunkManager] Successfully created {registeredChunks.Count} chunks and organized {movedObjects} vegetation objects!");
        }
#endif
    }
}
