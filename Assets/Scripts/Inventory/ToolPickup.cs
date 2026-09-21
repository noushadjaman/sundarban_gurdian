using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Inventory
{
    /// <summary>
    /// Collectible 3D world pickup item that replenishes the player's Backpack tool count
    /// for one of the four universal environmental restoration tools.
    /// </summary>
    public class ToolPickup : MonoBehaviour
    {
        [Header("Tool Configuration")]
        public ToolType toolType = ToolType.Seed;
        public int refillAmount = 1;

        [Header("Animation")]
        public float rotationSpeed = 65f;
        public float bobSpeed = 2.4f;
        public float bobHeight = 0.18f;

        private Vector3 basePos;
        private bool isCollected = false;

        private void Start()
        {
            basePos = transform.position;
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            ApplyVisualColor();
        }

        private void Update()
        {
            if (isCollected) return;

            // Rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Gentle floating bob
            float yOffset = Mathf.Sin((Time.time + transform.position.x) * bobSpeed) * bobHeight;
            transform.position = basePos + new Vector3(0f, yOffset, 0f);
        }

        private void ApplyVisualColor()
        {
            var rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = ToolInfo.GetThemeColor(toolType);
                mat.SetFloat("_Smoothness", 0.75f);
                mat.SetFloat("_Metallic", 0.3f);
                rend.sharedMaterial = mat;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            var backpack = other.GetComponent<Backpack>() ?? other.GetComponentInParent<Backpack>();
            if (backpack == null)
            {
                // Check if other is player
                if (other.CompareTag("Player") || other.name.Contains("Tiger") || other.name.Contains("Deer") || other.name.Contains("Dolphin") || other.name.Contains("Hilsha"))
                {
                    backpack = Backpack.LocalPlayerBackpack;
                }
            }

            if (backpack != null)
            {
                isCollected = true;
                backpack.AddTool(toolType, refillAmount);

                string toast = $"{ToolInfo.GetSymbol(toolType)} {ToolInfo.GetNameBn(toolType)} সংগৃহীত! (+{refillAmount})";
                HudEvents.RaiseEnvironmentalRestoration(toast);
                Debug.Log($"[ToolPickup] Collected {toolType} by {other.name}");

                gameObject.SetActive(false);
            }
        }
    }
}
