using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Illegal poacher steel-jaw trap set in the forest.
    /// Represents illegal wildlife poaching and tiger/deer traps in the Sundarbans.
    /// Skilled players can escape the trap by pressing the JUMP button to leap safely over it.
    /// </summary>
    public class HumanTrap : MonoBehaviour
    {
        [Header("Trap Settings")]
        public float trapDamage = 35f;
        public string trapName = "Poacher Wire Snare (শিকারির তারের ফাঁদ)";
        public bool isAlmostInvisible = false;
        public bool isDestroyed = false;

        private bool triggered = false;
        private Transform leftJaw;
        private Transform rightJaw;
        private Collider col;

        private void Start()
        {
            col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1.2f, 0.4f, 1.2f);
                box.isTrigger = true;
                col = box;
            }
            else
            {
                col.isTrigger = true;
            }

            BuildTrapVisuals();
        }

        private void BuildTrapVisuals()
        {
            leftJaw = transform.Find("Jaw_Left");
            rightJaw = transform.Find("Jaw_Right");
            if (leftJaw != null && rightJaw != null) return;

            // Base mounting plate
            var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlate.name = "Trap_Base_Plate";
            basePlate.transform.SetParent(transform, false);
            basePlate.transform.localScale = new Vector3(1.1f, 0.03f, 1.1f);
            basePlate.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            Destroy(basePlate.GetComponent<Collider>());

            // Left serrated jaw
            var jawL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jawL.name = "Jaw_Left";
            jawL.transform.SetParent(transform, false);
            jawL.transform.localPosition = new Vector3(-0.40f, 0.12f, 0f);
            jawL.transform.localRotation = Quaternion.Euler(0f, 0f, 42f);
            jawL.transform.localScale = new Vector3(0.10f, 0.26f, 0.95f);
            Destroy(jawL.GetComponent<Collider>());
            leftJaw = jawL.transform;

            // Right serrated jaw
            var jawR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jawR.name = "Jaw_Right";
            jawR.transform.SetParent(transform, false);
            jawR.transform.localPosition = new Vector3(0.40f, 0.12f, 0f);
            jawR.transform.localRotation = Quaternion.Euler(0f, 0f, -42f);
            jawR.transform.localScale = new Vector3(0.10f, 0.26f, 0.95f);
            Destroy(jawR.GetComponent<Collider>());
            rightJaw = jawR.transform;

            // Center trip pan / trigger
            var tripPan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tripPan.name = "Trip_Pan";
            tripPan.transform.SetParent(transform, false);
            tripPan.transform.localScale = new Vector3(0.42f, 0.025f, 0.42f);
            tripPan.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            Destroy(tripPan.GetComponent<Collider>());

            // Rusty iron / camouflaged mud material
            var rustShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var rustMat = new Material(rustShader);
            Color rustColor = isAlmostInvisible ? new Color(0.23f, 0.19f, 0.15f, 1f) : new Color(0.38f, 0.29f, 0.22f, 1f);
            rustMat.SetColor("_BaseColor", rustColor);
            rustMat.SetFloat("_Metallic", 0.70f);
            rustMat.SetFloat("_Smoothness", 0.22f);

            basePlate.GetComponent<MeshRenderer>().sharedMaterial = rustMat;
            jawL.GetComponent<MeshRenderer>().sharedMaterial = rustMat;
            jawR.GetComponent<MeshRenderer>().sharedMaterial = rustMat;
            tripPan.GetComponent<MeshRenderer>().sharedMaterial = rustMat;

            // Semi-concealed / almost invisible in intertidal mud
            if (isAlmostInvisible)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, -0.04f, transform.localPosition.z);
            }
        }

        public void DestroyTrap(GameObject attacker = null)
        {
            if (isDestroyed) return;
            isDestroyed = true;
            triggered = true;

            if (col != null) col.enabled = false;
            HudEvents.RaiseExitedHazardZone();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.trapsDestroyedCount++;
                int count = GameManager.Instance.trapsDestroyedCount;
                if (count < 10)
                {
                    HudEvents.RaiseEnvironmentalRestoration($"🪤 Poacher Trap Destroyed! ({count}/10)");
                }
                else if (count == 10)
                {
                    HudEvents.RaiseEnvironmentalRestoration("🎉 10/10 Traps Destroyed! Reach the Ocean Sanctuary to complete Level 1!");
                }
                else
                {
                    HudEvents.RaiseEnvironmentalRestoration($"🪤 Poacher Trap Destroyed! ({count} total)");
                }
            }
            else
            {
                HudEvents.RaiseEnvironmentalRestoration("🪤 Poacher Trap Destroyed!");
            }

            if (attacker != null)
            {
                var h = attacker.GetComponent<SpeciesHealth>() ?? attacker.GetComponentInParent<SpeciesHealth>();
                if (h != null) h.Heal(10f);
            }

            StartCoroutine(DestroyFxRoutine());
        }

        private System.Collections.IEnumerator DestroyFxRoutine()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            while (elapsed < 0.22f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.22f;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered || isDestroyed) return;

            // 1. Check if the player is jumping to safely clear the trap
            var lac = other.GetComponent<LandAnimalController>() ?? other.GetComponentInParent<LandAnimalController>();
            if (lac != null && lac.isJumping)
            {
                Debug.Log("<color=green>[HumanTrap] Player leapt safely over poacher trap!</color>");
                return;
            }

            // 2. Vertical elevation check: airborne characters above trigger height don't snap trap
            if (other.transform.position.y > transform.position.y + 0.38f)
            {
                return;
            }

            // 3. Tool clearance check (Trap Destroyer Crowbar in Backpack)
            var bp = other.GetComponent<SundarbanGuardian.Inventory.Backpack>() ?? other.GetComponentInParent<SundarbanGuardian.Inventory.Backpack>();
            if (bp != null && bp.HasTool(SundarbanGuardian.Inventory.ToolType.TrapDestroyer))
            {
                bp.ConsumeTool(SundarbanGuardian.Inventory.ToolType.TrapDestroyer, 1);
                DestroyTrap(other.gameObject);
                return;
            }

            var health = other.GetComponent<SpeciesHealth>() ?? other.GetComponentInParent<SpeciesHealth>();
            if (health != null)
            {
                triggered = true;
                health.TakeDamage(trapDamage);
                HudEvents.RaiseEnteredHazardZone("high", 100f, "% শিকারির অবৈধ ফাঁদ (Poacher Trap)");

                // Snap jaws shut
                if (leftJaw != null) leftJaw.localRotation = Quaternion.identity;
                if (rightJaw != null) rightJaw.localRotation = Quaternion.identity;
                transform.localScale = new Vector3(transform.localScale.x * 0.9f, 0.6f, transform.localScale.z * 0.9f);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SpeciesHealth>() != null || other.GetComponentInParent<SpeciesHealth>() != null)
            {
                HudEvents.RaiseExitedHazardZone();
            }
        }
    }
}
