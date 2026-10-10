using System;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Collectible 3D food item in the Sundarban ecosystem (Keora mangrove fruit, prey rations, fish).
    /// Regains HP when character is hurt, or stores in backpack as emergency food rations.
    /// Features floating 3D miniature assets, warm glowing beacon, and minimap radar integration.
    /// </summary>
    public class FoodItem : MonoBehaviour
    {
        [Header("Food Configuration")]
        public string targetSpecies = "all"; // "all", "tiger", "deer", "dolphin", "hilsha"
        public float healAmount = 35f;
        public string foodLabelEn = "Fresh Mangrove Fruit & Prey (পুষ্টিকর খাবার)";
        public string foodLabel { get => foodLabelEn; set => foodLabelEn = value; }

        [Header("Floating Animation")]
        public float bobSpeed = 2.4f;
        public float bobHeight = 0.14f;
        public float rotateSpeedDeg = 60f;

        [Header("Collection Settings")]
        public float collectionRadius = 1.8f;

        private Vector3 initialBasePos;
        private Transform visualChild;
        private bool isConsumed = false;

        public static event Action<FoodItem> FoodSpawned;
        public static event Action<FoodItem> FoodEaten;

        private void Awake()
        {
            initialBasePos = transform.position;
            Ensure3DVisualAsset();
            EnsureTriggerCollider();
        }

        private void Start()
        {
            FoodSpawned?.Invoke(this);
        }

        private void OnDestroy()
        {
            FoodEaten?.Invoke(this);
        }

        private void Update()
        {
            if (isConsumed) return;

            // 1. Floating bobbing & rotation
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = initialBasePos + new Vector3(0f, yOffset, 0f);
            transform.Rotate(Vector3.up, rotateSpeedDeg * Time.deltaTime, Space.World);

            // 2. Proximity check fallback for player CharacterController
            var backpack = Backpack.LocalPlayerBackpack;
            if (backpack != null && backpack.gameObject.activeInHierarchy)
            {
                float dist = Vector3.Distance(transform.position, backpack.transform.position);
                if (dist <= collectionRadius)
                {
                    EatOrCollect(backpack);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed) return;

            var backpack = other.GetComponent<Backpack>() ?? other.GetComponentInParent<Backpack>();
            if (backpack != null)
            {
                EatOrCollect(backpack);
            }
        }

        public void EatOrCollect(Backpack backpack)
        {
            if (isConsumed) return;
            isConsumed = true;

            var health = backpack.GetComponent<SpeciesHealth>();
            if (health != null && health.currentHealth < health.maxHealth)
            {
                // Instantly eat food to regain HP
                health.EatFood(healAmount);
                HudEvents.RaiseEnvironmentalRestoration($"🍗 +{healAmount:F0} HP — Ate Food (পুষ্টিকর খাবার)! Current HP: {health.currentHealth:F0}/{health.maxHealth:F0}");
            }
            else
            {
                // Add to carried backpack food inventory
                backpack.AddFood(1);
                HudEvents.RaiseEnvironmentalRestoration($"🍗 Collected Food Ration (+1 Food in Backpack)!");
            }

            FoodEaten?.Invoke(this);
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

            var visGo = new GameObject("3D_Visual");
            visGo.transform.SetParent(transform, false);
            visualChild = visGo.transform;

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Golden Keora Fruit / Meat body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(visGo.transform, false);
            body.transform.localScale = new Vector3(0.32f, 0.36f, 0.32f);
            body.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            Destroy(body.GetComponent<Collider>());
            var bMat = new Material(litShader);
            bMat.color = new Color(1.0f, 0.72f, 0.20f); // warm golden amber
            bMat.SetFloat("_Smoothness", 0.75f);
            body.GetComponent<MeshRenderer>().sharedMaterial = bMat;

            // 2. Leaf sprout / bone handle
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(visGo.transform, false);
            stem.transform.localScale = new Vector3(0.04f, 0.10f, 0.04f);
            stem.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            Destroy(stem.GetComponent<Collider>());
            var sMat = new Material(litShader);
            sMat.color = new Color(0.38f, 0.25f, 0.14f);
            stem.GetComponent<MeshRenderer>().sharedMaterial = sMat;

            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.transform.SetParent(visGo.transform, false);
            leaf.transform.localScale = new Vector3(0.18f, 0.04f, 0.12f);
            leaf.transform.localPosition = new Vector3(0.08f, 0.40f, 0f);
            leaf.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            Destroy(leaf.GetComponent<Collider>());
            var lMat = new Material(litShader);
            lMat.color = new Color(0.25f, 0.92f, 0.35f);
            leaf.GetComponent<MeshRenderer>().sharedMaterial = lMat;

            // 3. Glowing warm point light
            var lightGo = new GameObject("FoodGlow");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3.2f;
            light.intensity = 1.3f;
            light.color = new Color(1.0f, 0.80f, 0.30f);
            light.shadows = LightShadows.None;
        }
    }
}
