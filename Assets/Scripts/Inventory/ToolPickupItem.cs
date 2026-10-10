using System;
using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Inventory
{
    /// <summary>
    /// Collectible 3D tool pickup scattered across the Sundarbans.
    /// Features floating 3D miniature assets, gentle bobbing, glowing beacon,
    /// and auto-registers a small icon on the Landsat Minimap Radar.
    /// </summary>
    public class ToolPickupItem : MonoBehaviour
    {
        [Header("Tool Configuration")]
        public ToolType toolType = ToolType.Seed;
        public int amount = 1;

        [Header("Floating Animation")]
        public float bobSpeed = 2.2f;
        public float bobHeight = 0.12f;
        public float rotateSpeedDeg = 65f;

        [Header("Pickup Detection")]
        public float collectionRadius = 1.8f;

        private Vector3 initialBasePos;
        private Transform visualChild;
        private bool isCollected = false;
        private float canCollectTime = 0f;

        public static event Action<ToolPickupItem> PickupSpawned;
        public static event Action<ToolPickupItem> PickupCollected;

        public void SetDropCooldown(float duration = 2.0f)
        {
            canCollectTime = Time.time + duration;
        }

        public bool CanBeCollected()
        {
            return !isCollected && Time.time >= canCollectTime;
        }

        private void Awake()
        {
            // Limit excessive world duplicates to ensure scarce, balanced survival gameplay
            if (gameObject.name.StartsWith("Seed_Pickup_V2_"))
            {
                if (int.TryParse(gameObject.name.Substring("Seed_Pickup_V2_".Length), out int idx) && idx > 3)
                {
                    Destroy(gameObject);
                    return;
                }
                amount = Mathf.Max(amount, 2); // Each retained seed pouch contains 2 seeds
            }

            initialBasePos = transform.position;
            Ensure3DVisualAsset();
            EnsureTriggerCollider();
        }

        private void Start()
        {
            PickupSpawned?.Invoke(this);
        }

        private void OnDestroy()
        {
            PickupCollected?.Invoke(this);
        }

        private void Update()
        {
            if (isCollected) return;

            // 1. Floating bobbing & rotation
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = initialBasePos + new Vector3(0f, yOffset, 0f);
            transform.Rotate(Vector3.up, rotateSpeedDeg * Time.deltaTime, Space.World);

            // 2. Proximity check fallback for player CharacterController
            if (!CanBeCollected()) return;

            var player = Backpack.LocalPlayerBackpack;
            if (player != null && player.gameObject.activeInHierarchy)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= collectionRadius)
                {
                    Collect(player);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanBeCollected()) return;

            var backpack = other.GetComponent<Backpack>() ?? other.GetComponentInParent<Backpack>();
            if (backpack != null)
            {
                Collect(backpack);
            }
        }

        public void Collect(Backpack backpack)
        {
            if (isCollected) return;
            isCollected = true;

            backpack.AddTool(toolType, amount);

            // Show HUD notification
            string toolName = ToolInfo.GetNameEn(toolType);
            HudEvents.RaiseEnvironmentalRestoration($"🎉 Collected: +{amount}x {toolName}!");

            PickupCollected?.Invoke(this);

            Destroy(gameObject, 0.05f);
        }

        public void EnsureTriggerCollider()
        {
            var col = GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = collectionRadius;
        }

        public void Ensure3DVisualAsset()
        {
            if (transform.childCount > 0)
            {
                visualChild = transform.GetChild(0);
                return;
            }

            // Procedurally create small 3D tool asset
            var visGo = new GameObject("3D_Visual");
            visGo.transform.SetParent(transform, false);
            visGo.transform.localPosition = Vector3.zero;
            visualChild = visGo.transform;

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            switch (toolType)
            {
                case ToolType.Seed:
                    BuildSeedModel(visGo, litShader);
                    break;
                case ToolType.NetCutter:
                    BuildKnifeModel(visGo, litShader);
                    break;
                case ToolType.CleaningLiquid:
                    BuildFlaskModel(visGo, litShader);
                    break;
                case ToolType.TrapDestroyer:
                    BuildCrowbarModel(visGo, litShader);
                    break;
            }

            // Add subtle point light glow
            var lightGo = new GameObject("GlowLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.5f;
            light.intensity = 1.2f;
            light.color = ToolInfo.GetThemeColor(toolType);
            light.shadows = LightShadows.None;
        }

        private void BuildSeedModel(GameObject parent, Shader shader)
        {
            // Slender stem root
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(parent.transform, false);
            stem.transform.localScale = new Vector3(0.06f, 0.28f, 0.06f);
            stem.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            Destroy(stem.GetComponent<Collider>());
            var stemMat = new Material(shader);
            stemMat.color = new Color(0.42f, 0.28f, 0.16f);
            stem.GetComponent<MeshRenderer>().sharedMaterial = stemMat;

            // Arching mangrove stilt roots
            for (int r = 0; r < 2; r++)
            {
                var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.transform.SetParent(parent.transform, false);
                root.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
                root.transform.localPosition = new Vector3((r == 0 ? -0.06f : 0.06f), 0.04f, 0f);
                root.transform.localRotation = Quaternion.Euler(0f, 0f, (r == 0 ? 28f : -28f));
                Destroy(root.GetComponent<Collider>());
                root.GetComponent<MeshRenderer>().sharedMaterial = stemMat;
            }

            // Sprouting green leaves
            var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(parent.transform, false);
            leaves.transform.localScale = new Vector3(0.24f, 0.08f, 0.28f);
            leaves.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            Destroy(leaves.GetComponent<Collider>());
            var leafMat = new Material(shader);
            leafMat.color = new Color(0.22f, 0.90f, 0.32f);
            leafMat.SetFloat("_Smoothness", 0.65f);
            leaves.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
        }

        private void BuildKnifeModel(GameObject parent, Shader shader)
        {
            parent.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Handle
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.transform.SetParent(parent.transform, false);
            handle.transform.localScale = new Vector3(0.05f, 0.16f, 0.05f);
            handle.transform.localPosition = new Vector3(0f, -0.10f, 0f);
            Destroy(handle.GetComponent<Collider>());
            var hMat = new Material(shader);
            hMat.color = new Color(0.15f, 0.15f, 0.18f);
            handle.GetComponent<MeshRenderer>().sharedMaterial = hMat;

            // Steel blade with glint
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(parent.transform, false);
            blade.transform.localScale = new Vector3(0.02f, 0.32f, 0.10f);
            blade.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            Destroy(blade.GetComponent<Collider>());
            var bMat = new Material(shader);
            bMat.color = new Color(0.88f, 0.92f, 0.96f);
            bMat.SetFloat("_Metallic", 0.95f);
            bMat.SetFloat("_Smoothness", 0.90f);
            blade.GetComponent<MeshRenderer>().sharedMaterial = bMat;
        }

        private void BuildFlaskModel(GameObject parent, Shader shader)
        {
            // Glass body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(parent.transform, false);
            body.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Destroy(body.GetComponent<Collider>());
            var fMat = new Material(shader);
            fMat.color = new Color(0.15f, 0.92f, 0.98f);
            fMat.SetFloat("_Smoothness", 0.95f);
            body.GetComponent<MeshRenderer>().sharedMaterial = fMat;

            // Cork stopper
            var cork = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cork.transform.SetParent(parent.transform, false);
            cork.transform.localScale = new Vector3(0.09f, 0.06f, 0.09f);
            cork.transform.localPosition = new Vector3(0f, 0.27f, 0f);
            Destroy(cork.GetComponent<Collider>());
            var cMat = new Material(shader);
            cMat.color = new Color(0.55f, 0.38f, 0.22f);
            cork.GetComponent<MeshRenderer>().sharedMaterial = cMat;
        }

        private void BuildCrowbarModel(GameObject parent, Shader shader)
        {
            parent.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.transform.SetParent(parent.transform, false);
            shaft.transform.localScale = new Vector3(0.05f, 0.45f, 0.05f);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0f);
            Destroy(shaft.GetComponent<Collider>());
            var cMat = new Material(shader);
            cMat.color = new Color(0.95f, 0.65f, 0.15f);
            cMat.SetFloat("_Metallic", 0.85f);
            cMat.SetFloat("_Smoothness", 0.75f);
            shaft.GetComponent<MeshRenderer>().sharedMaterial = cMat;

            var claw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            claw.transform.SetParent(parent.transform, false);
            claw.transform.localScale = new Vector3(0.04f, 0.14f, 0.09f);
            claw.transform.localPosition = new Vector3(0.08f, 0.24f, 0f);
            claw.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            Destroy(claw.GetComponent<Collider>());
            var tipMat = new Material(shader);
            tipMat.color = new Color(0.85f, 0.88f, 0.92f);
            tipMat.SetFloat("_Metallic", 0.95f);
            claw.GetComponent<MeshRenderer>().sharedMaterial = tipMat;
        }
    }
}
