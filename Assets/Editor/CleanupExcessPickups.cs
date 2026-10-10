using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Editor
{
    [InitializeOnLoad]
    public static class CleanupExcessPickups
    {
        static CleanupExcessPickups()
        {
            EditorApplication.delayCall += Cleanup;
        }

        [MenuItem("Sundarban/Limit Tools and Clean Up Minimap")]
        public static void Cleanup()
        {
            bool modified = false;
            var seedRoot = GameObject.Find("[V2_Deer_Seeds]");
            if (seedRoot != null && seedRoot.transform.childCount > 3)
            {
                Debug.Log($"[Cleanup] Found [V2_Deer_Seeds] with {seedRoot.transform.childCount} seeds. Reducing to 3 scarce strategic seeds...");
                for (int i = seedRoot.transform.childCount - 1; i >= 3; i--)
                {
                    var child = seedRoot.transform.GetChild(i).gameObject;
                    Object.DestroyImmediate(child);
                }
                
                // Ensure remaining seeds provide 2 seeds per pickup
                for (int i = 0; i < seedRoot.transform.childCount; i++)
                {
                    var pickup = seedRoot.transform.GetChild(i).GetComponent<ToolPickupItem>();
                    if (pickup != null) pickup.amount = 2;
                }
                modified = true;
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("[Cleanup] Successfully limited in-game tool pickups and saved scene!");
            }
        }
    }
}
