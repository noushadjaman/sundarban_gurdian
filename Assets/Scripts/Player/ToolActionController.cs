using System.Collections;
using UnityEngine;
using SundarbanGuardian.Inventory;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Free Fire style Tool Hit / Action Controller.
    /// Handles the "HIT" button action, executing procedural lunge animations,
    /// dynamic tool FX bursts (Seed spray, Blade slash, Cleanser mist, Trap smash),
    /// and triggers Backpack tool clearance and +10 HP healing.
    /// </summary>
    public class ToolActionController : MonoBehaviour
    {
        [Header("Components")]
        public Backpack backpack;
        public Animator animator;
        public Transform visualTransform;

        [Header("Animation Tuning")]
        public float lungeDistance = 0.45f;
        public float lungeDuration = 0.12f;
        public float recoverDuration = 0.22f;

        private Vector3 initialVisualLocalPos;
        private Quaternion initialVisualLocalRot;
        private Coroutine activeActionCoroutine;
        private bool isPerformingAction = false;

        private void Awake()
        {
            if (backpack == null) backpack = GetComponent<Backpack>() ?? GetComponentInParent<Backpack>();
            if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

            if (visualTransform == null)
            {
                visualTransform = transform.Find("VisualModel");
                if (visualTransform == null && transform.childCount > 0)
                {
                    visualTransform = transform.GetChild(0);
                }
            }

            if (visualTransform != null)
            {
                initialVisualLocalPos = visualTransform.localPosition;
                initialVisualLocalRot = visualTransform.localRotation;
            }
        }

        private void OnEnable()
        {
            MobileInputManager.OnHitActionTriggered += OnHitPressed;
        }

        private void OnDisable()
        {
            MobileInputManager.OnHitActionTriggered -= OnHitPressed;
        }

        public void OnHitPressed()
        {
            if (isPerformingAction) return;

            ToolType currentTool = (backpack != null) ? backpack.activeTool : ToolType.Seed;

            // Trigger clearance if near obstacle
            bool clearedAny = false;
            if (backpack != null)
            {
                clearedAny = backpack.TryUseTool();
            }

            // Always execute energetic hit animation & tool FX
            if (activeActionCoroutine != null) StopCoroutine(activeActionCoroutine);
            activeActionCoroutine = StartCoroutine(PerformToolActionRoutine(currentTool, clearedAny));
        }

        private IEnumerator PerformToolActionRoutine(ToolType tool, bool clearedObstacle)
        {
            isPerformingAction = true;

            // 1. Check for Animator attack triggers
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (HasParameter(animator, "Attack")) animator.SetTrigger("Attack");
                else if (HasParameter(animator, "Strike")) animator.SetTrigger("Strike");
                else if (HasParameter(animator, "Action")) animator.SetTrigger("Action");
            }

            // 2. Spawn 3D Visual Tool Slash/Burst FX at front of character
            SpawnToolVisualFx(tool, clearedObstacle);

            // 3. Procedural dynamic strike lunge & recoil
            if (visualTransform != null)
            {
                Vector3 startPos = initialVisualLocalPos;
                Quaternion startRot = initialVisualLocalRot;
                Vector3 forwardLunge = startPos + new Vector3(0f, 0.12f, lungeDistance);
                Quaternion strikeRot = startRot * Quaternion.Euler(-12f, 0f, 0f);

                // Quick forward strike thrust
                float elapsed = 0f;
                while (elapsed < lungeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / lungeDuration);
                    float curve = Mathf.Sin(t * Mathf.PI * 0.5f);
                    visualTransform.localPosition = Vector3.Lerp(startPos, forwardLunge, curve);
                    visualTransform.localRotation = Quaternion.Slerp(startRot, strikeRot, curve);
                    yield return null;
                }

                // Elastic recovery back to stance
                elapsed = 0f;
                while (elapsed < recoverDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / recoverDuration);
                    float smooth = Mathf.SmoothStep(0f, 1f, t);
                    visualTransform.localPosition = Vector3.Lerp(forwardLunge, startPos, smooth);
                    visualTransform.localRotation = Quaternion.Slerp(strikeRot, startRot, smooth);
                    yield return null;
                }

                visualTransform.localPosition = startPos;
                visualTransform.localRotation = startRot;
            }
            else
            {
                yield return new WaitForSeconds(lungeDuration + recoverDuration);
            }

            isPerformingAction = false;
        }

        private void SpawnToolVisualFx(ToolType tool, bool hitTarget)
        {
            Vector3 spawnOrigin = transform.position + transform.forward * 1.5f + Vector3.up * 0.8f;
            var fxRoot = new GameObject($"FX_{tool}_Strike");
            fxRoot.transform.position = spawnOrigin;
            fxRoot.transform.rotation = transform.rotation;

            switch (tool)
            {
                case ToolType.Seed:
                    // Emerald green seed scatter & blooming burst
                    CreateSeedBurst(fxRoot.transform);
                    break;

                case ToolType.NetCutter:
                    // Cyan glowing crescent slash arc
                    CreateSlashArc(fxRoot.transform);
                    break;

                case ToolType.CleaningLiquid:
                    // Aquamarine cleansing splash spray
                    CreateCleaningSplash(fxRoot.transform);
                    break;

                case ToolType.TrapDestroyer:
                    // Heavy golden shockwave ground slam
                    CreateTrapSmash(fxRoot.transform);
                    break;
            }

            Destroy(fxRoot, 1.2f);
        }

        private void CreateSeedBurst(Transform parent)
        {
            for (int i = 0; i < 8; i++)
            {
                var seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                seed.transform.SetParent(parent);
                seed.transform.localScale = Vector3.one * 0.22f;
                seed.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.5f), Random.Range(0f, 0.6f));

                var col = seed.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var rend = seed.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.2f, 0.95f, 0.35f, 1.0f);
                    mat.SetFloat("_Smoothness", 0.8f);
                    rend.sharedMaterial = mat;
                }
            }
        }

        private void CreateSlashArc(Transform parent)
        {
            var slash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slash.transform.SetParent(parent);
            slash.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            slash.transform.localRotation = Quaternion.Euler(0f, 90f, 80f);
            slash.transform.localScale = new Vector3(1.8f, 0.04f, 0.55f);

            var col = slash.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = slash.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.25f, 0.85f, 1.0f, 0.85f); // glowing cyan blade arc
                mat.SetFloat("_Smoothness", 0.9f);
                rend.sharedMaterial = mat;
            }
        }

        private void CreateCleaningSplash(Transform parent)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.transform.SetParent(parent);
            disc.transform.localPosition = Vector3.down * 0.4f;
            disc.transform.localScale = new Vector3(2.2f, 0.05f, 2.2f);

            var col = disc.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = disc.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.15f, 0.92f, 0.85f, 0.65f); // aquamarine cleansing wave
                mat.SetFloat("_Smoothness", 0.7f);
                rend.sharedMaterial = mat;
            }
        }

        private void CreateTrapSmash(Transform parent)
        {
            var slam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slam.transform.SetParent(parent);
            slam.transform.localPosition = Vector3.down * 0.5f;
            slam.transform.localScale = new Vector3(2.5f, 0.08f, 2.5f);

            var col = slam.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = slam.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.95f, 0.70f, 0.15f, 0.85f); // golden impact wave
                mat.SetFloat("_Smoothness", 0.8f);
                rend.sharedMaterial = mat;
            }
        }

        private static bool HasParameter(Animator anim, string paramName)
        {
            if (anim == null) return false;
            foreach (var p in anim.parameters)
            {
                if (p.name == paramName) return true;
            }
            return false;
        }
    }
}
