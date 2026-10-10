using System.Collections;
using UnityEngine;
using SundarbanGuardian.Inventory;
using SundarbanGuardian.Environment;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Free Fire / RPG style Tool Hit & Action Controller.
    /// Manages 3D tool visualization mounted in front of the animal (Knife, Seed, Cleanser, Crowbar),
    /// and executes animated tool actions including throwing a spinning knife projectile to cut illegal nets!
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

        [Header("Tool Socket & 3D Models")]
        public Transform toolSocket;
        private GameObject[] toolVisualModels = new GameObject[4];
        private ToolType lastTool = (ToolType)(-1);

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

            BuildToolSocketAndModels();
        }

        private void BuildToolSocketAndModels()
        {
            if (toolSocket == null)
            {
                var sGo = new GameObject("Front_Tool_Socket");
                sGo.transform.SetParent(transform, false);
                sGo.transform.localPosition = new Vector3(0.35f, 0.48f, 0.95f);
                sGo.transform.localRotation = Quaternion.Euler(15f, 18f, -10f);
                toolSocket = sGo.transform;
            }

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 0. Seed Propagule Model
            var seedGo = new GameObject("Visual_Seed");
            seedGo.transform.SetParent(toolSocket, false);
            var seedBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seedBody.transform.SetParent(seedGo.transform, false);
            seedBody.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
            Destroy(seedBody.GetComponent<Collider>());
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.transform.SetParent(seedGo.transform, false);
            leaf.transform.localPosition = new Vector3(0f, 0.38f, 0.05f);
            leaf.transform.localScale = new Vector3(0.18f, 0.06f, 0.22f);
            Destroy(leaf.GetComponent<Collider>());
            var seedMat = new Material(litShader);
            seedMat.SetColor("_BaseColor", new Color(0.20f, 0.85f, 0.35f, 1f));
            seedBody.GetComponent<MeshRenderer>().sharedMaterial = seedMat;
            leaf.GetComponent<MeshRenderer>().sharedMaterial = seedMat;
            toolVisualModels[0] = seedGo;

            // 1. Knife / Net Cutter Model (Front of character)
            var knifeGo = new GameObject("Visual_Knife");
            knifeGo.transform.SetParent(toolSocket, false);
            // Handle
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.transform.SetParent(knifeGo.transform, false);
            handle.transform.localScale = new Vector3(0.07f, 0.22f, 0.07f);
            Destroy(handle.GetComponent<Collider>());
            var handleMat = new Material(litShader);
            handleMat.SetColor("_BaseColor", new Color(0.36f, 0.22f, 0.12f, 1f));
            handle.GetComponent<MeshRenderer>().sharedMaterial = handleMat;
            // Blade
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(knifeGo.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            blade.transform.localScale = new Vector3(0.035f, 0.45f, 0.14f);
            Destroy(blade.GetComponent<Collider>());
            var bladeMat = new Material(litShader);
            bladeMat.SetColor("_BaseColor", new Color(0.92f, 0.95f, 0.98f, 1f));
            bladeMat.SetFloat("_Metallic", 0.92f);
            bladeMat.SetFloat("_Smoothness", 0.88f);
            blade.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;
            toolVisualModels[1] = knifeGo;

            // 2. Cleaning Liquid Flask
            var flaskGo = new GameObject("Visual_Flask");
            flaskGo.transform.SetParent(toolSocket, false);
            var flaskBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flaskBody.transform.SetParent(flaskGo.transform, false);
            flaskBody.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
            Destroy(flaskBody.GetComponent<Collider>());
            var flaskMat = new Material(litShader);
            flaskMat.SetColor("_BaseColor", new Color(0.15f, 0.80f, 0.95f, 1f));
            flaskBody.GetComponent<MeshRenderer>().sharedMaterial = flaskMat;
            toolVisualModels[2] = flaskGo;

            // 3. Trap Destroyer Crowbar
            var barGo = new GameObject("Visual_TrapDestroyer");
            barGo.transform.SetParent(toolSocket, false);
            var barBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barBody.transform.SetParent(barGo.transform, false);
            barBody.transform.localScale = new Vector3(0.08f, 0.55f, 0.08f);
            Destroy(barBody.GetComponent<Collider>());
            var claw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            claw.transform.SetParent(barGo.transform, false);
            claw.transform.localPosition = new Vector3(0.08f, 0.28f, 0f);
            claw.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            claw.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
            Destroy(claw.GetComponent<Collider>());
            var barMat = new Material(litShader);
            barMat.SetColor("_BaseColor", new Color(0.85f, 0.70f, 0.20f, 1f));
            barMat.SetFloat("_Metallic", 0.80f);
            barBody.GetComponent<MeshRenderer>().sharedMaterial = barMat;
            claw.GetComponent<MeshRenderer>().sharedMaterial = barMat;
            toolVisualModels[3] = barGo;

            UpdateVisibleTool();
        }

        private void Update()
        {
            if (backpack != null && backpack.activeTool != lastTool)
            {
                UpdateVisibleTool();
            }

            // Gentle tool bobbing in idle
            if (toolSocket != null && !isPerformingAction)
            {
                float bob = Mathf.Sin(Time.time * 2.5f) * 0.03f;
                toolSocket.localPosition = new Vector3(0.35f, 0.48f + bob, 0.95f);
            }

            // Direct input check for PC / Editor (F key, Enter key, Left Mouse Click)
            bool triggerAction = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (kb != null && (kb.fKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) triggerAction = true;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es == null || !es.IsPointerOverGameObject()) triggerAction = true;
                }
            }
            catch (System.Exception) { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return)) triggerAction = true;
                if (Input.GetMouseButtonDown(0))
                {
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es == null || !es.IsPointerOverGameObject()) triggerAction = true;
                }
            }
            catch (System.Exception) { }
#endif
            if (triggerAction)
            {
                OnHitPressed();
            }
        }

        private void UpdateVisibleTool()
        {
            if (backpack == null) return;
            lastTool = backpack.activeTool;

            int activeIdx = (int)lastTool;
            for (int i = 0; i < toolVisualModels.Length; i++)
            {
                if (toolVisualModels[i] != null)
                {
                    toolVisualModels[i].SetActive(isPerformingAction && i == activeIdx);
                }
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

            // Trigger clearance check
            bool clearedAny = false;

            // 1. Tiger trap swipe check
            bool isTiger = gameObject.name.Contains("Tiger") || (transform.parent != null && transform.parent.name.Contains("Tiger"));
            if (isTiger)
            {
                var traps = Object.FindObjectsByType<HumanTrap>(FindObjectsSortMode.None);
                foreach (var trap in traps)
                {
                    if (trap != null && !trap.isDestroyed && Vector3.Distance(transform.position, trap.transform.position) <= 4.5f)
                    {
                        trap.DestroyTrap(gameObject);
                        clearedAny = true;
                        break;
                    }
                }
            }

            // 2. Deforestation zone seed planting check
            if (backpack != null && backpack.GetCount(ToolType.Seed) > 0)
            {
                var worldZones = Object.FindObjectsByType<WorldDangerZone>(FindObjectsSortMode.None);
                foreach (var wz in worldZones)
                {
                    if (wz != null && !wz.isFinished && Vector3.Distance(transform.position, wz.transform.position) <= wz.interactionRadius)
                    {
                        if (wz.TryFinishWithSeeds(gameObject))
                        {
                            clearedAny = true;
                            break;
                        }
                    }
                }

                var hazardTriggers = Object.FindObjectsByType<SundarbanGuardian.Core.HazardZoneTrigger>(FindObjectsSortMode.None);
                foreach (var ht in hazardTriggers)
                {
                    if (ht != null && !ht.isCleared && Vector3.Distance(transform.position, ht.transform.position) <= ht.interactionRadius)
                    {
                        if (ht.TryInteractWithSeeds(backpack))
                        {
                            clearedAny = true;
                            break;
                        }
                    }
                }
            }

            // 3. Standard backpack tool clearance
            if (!clearedAny && backpack != null)
            {
                clearedAny = backpack.TryUseTool();
            }

            if (activeActionCoroutine != null) StopCoroutine(activeActionCoroutine);
            activeActionCoroutine = StartCoroutine(PerformToolActionRoutine(currentTool, clearedAny));
        }

        private IEnumerator PerformToolActionRoutine(ToolType tool, bool clearedObstacle)
        {
            isPerformingAction = true;
            UpdateVisibleTool();

            // 1. Animator attack triggers if present
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (HasParameter(animator, "Attack")) animator.SetTrigger("Attack");
                else if (HasParameter(animator, "Strike")) animator.SetTrigger("Strike");
                else if (HasParameter(animator, "Action")) animator.SetTrigger("Action");
            }

            // 2. Specific animated tool behavior
            if (tool == ToolType.NetCutter)
            {
                // Animated Knife Throwing Action to cut the net!
                yield return StartCoroutine(ThrowKnifeAtNetRoutine());
            }
            else
            {
                // Procedural tool strike lunge
                SpawnToolVisualFx(tool, clearedObstacle);

                if (visualTransform != null)
                {
                    Vector3 startPos = initialVisualLocalPos;
                    Quaternion startRot = initialVisualLocalRot;
                    Vector3 forwardLunge = startPos + new Vector3(0f, 0.12f, lungeDistance);
                    Quaternion strikeRot = startRot * Quaternion.Euler(-12f, 0f, 0f);

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
            }

            isPerformingAction = false;
            UpdateVisibleTool();
        }

        private IEnumerator ThrowKnifeAtNetRoutine()
        {
            // Temporarily hide the mounted front knife
            if (toolVisualModels[1] != null) toolVisualModels[1].SetActive(false);

            // Find nearest Illegal Net within range
            IllegalNetObstacle targetNet = null;
            var nets = Object.FindObjectsByType<IllegalNetObstacle>(FindObjectsSortMode.None);
            float nearestDist = 18f;
            foreach (var n in nets)
            {
                if (n == null || !n.gameObject.activeInHierarchy || n.isCleared) continue;
                float d = Vector3.Distance(transform.position, n.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    targetNet = n;
                }
            }

            // Spawn 3D Knife Projectile
            Vector3 startPos = (toolSocket != null) ? toolSocket.position : transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
            Vector3 targetPos = (targetNet != null) ? targetNet.transform.position + Vector3.up * 0.4f : startPos + transform.forward * 8f;

            var projGo = new GameObject("Knife_Projectile");
            projGo.transform.position = startPos;

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.transform.SetParent(projGo.transform, false);
            handle.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            Destroy(handle.GetComponent<Collider>());
            var handleMat = new Material(litShader);
            handleMat.SetColor("_BaseColor", new Color(0.36f, 0.22f, 0.12f, 1f));
            handle.GetComponent<MeshRenderer>().sharedMaterial = handleMat;

            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(projGo.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            blade.transform.localScale = new Vector3(0.04f, 0.55f, 0.16f);
            Destroy(blade.GetComponent<Collider>());
            var bladeMat = new Material(litShader);
            bladeMat.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f, 1f));
            bladeMat.SetFloat("_Metallic", 0.95f);
            bladeMat.SetFloat("_Smoothness", 0.95f);
            blade.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;

            // Trail
            var trail = projGo.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.18f;
            trail.endWidth = 0.02f;
            var tMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? litShader);
            tMat.SetColor("_BaseColor", new Color(0.3f, 0.9f, 1.0f, 0.75f));
            trail.sharedMaterial = tMat;

            // Animate flight & spinning
            float flightTime = Mathf.Clamp(Vector3.Distance(startPos, targetPos) / 16f, 0.22f, 0.45f);
            float elapsed = 0f;

            while (elapsed < flightTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flightTime;

                // Parabolic arc trajectory
                Vector3 currentP = Vector3.Lerp(startPos, targetPos, t);
                currentP.y += Mathf.Sin(t * Mathf.PI) * 0.75f;
                projGo.transform.position = currentP;

                // High-speed throw spin
                projGo.transform.Rotate(Vector3.right * 1440f * Time.deltaTime, Space.Self);
                yield return null;
            }

            // Impact cut effect
            SpawnNetCutSlashFx(targetPos);

            if (targetNet != null)
            {
                targetNet.DestroyWithKnife(backpack);
            }

            Destroy(projGo, 0.1f);
            yield return new WaitForSeconds(0.15f);

            // Re-show mounted knife
            if (toolVisualModels[1] != null && backpack != null && backpack.activeTool == ToolType.NetCutter)
            {
                toolVisualModels[1].SetActive(true);
            }
        }

        private void SpawnNetCutSlashFx(Vector3 pos)
        {
            var fxGo = new GameObject("Net_Cut_Slash_FX");
            fxGo.transform.position = pos;

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Cyan slash crescent
            var slash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slash.transform.SetParent(fxGo.transform, false);
            slash.transform.localScale = new Vector3(2.2f, 0.04f, 0.7f);
            slash.transform.localRotation = Quaternion.Euler(25f, Random.Range(0f, 180f), 70f);
            Destroy(slash.GetComponent<Collider>());
            var sMat = new Material(litShader);
            sMat.SetColor("_BaseColor", new Color(0.2f, 0.95f, 1.0f, 0.9f));
            sMat.SetFloat("_Smoothness", 0.95f);
            slash.GetComponent<MeshRenderer>().sharedMaterial = sMat;

            Destroy(fxGo, 0.45f);
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
                    CreateSeedBurst(fxRoot.transform);
                    break;
                case ToolType.NetCutter:
                    CreateSlashArc(fxRoot.transform);
                    break;
                case ToolType.CleaningLiquid:
                    CreateCleaningSplash(fxRoot.transform);
                    break;
                case ToolType.TrapDestroyer:
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
                Destroy(seed.GetComponent<Collider>());
                var rend = seed.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
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
            Destroy(slash.GetComponent<Collider>());
            var rend = slash.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.85f, 1.0f, 0.85f);
                mat.SetFloat("_Smoothness", 0.9f);
                rend.sharedMaterial = mat;
            }
        }

        private void CreateCleaningSplash(Transform parent)
        {
            for (int i = 0; i < 12; i++)
            {
                var drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                drop.transform.SetParent(parent);
                drop.transform.localScale = Vector3.one * Random.Range(0.12f, 0.28f);
                drop.transform.localPosition = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.2f, 0.5f), Random.Range(0.2f, 1.1f));
                Destroy(drop.GetComponent<Collider>());
                var rend = drop.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(0.15f, 0.85f, 0.95f, 0.85f);
                    mat.SetFloat("_Smoothness", 0.95f);
                    rend.sharedMaterial = mat;
                }
            }
        }

        private void CreateTrapSmash(Transform parent)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(parent);
            ring.transform.localPosition = new Vector3(0f, -0.6f, 0.4f);
            ring.transform.localScale = new Vector3(2.2f, 0.05f, 2.2f);
            Destroy(ring.GetComponent<Collider>());
            var rend = ring.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.95f, 0.80f, 0.20f, 0.75f);
                mat.SetFloat("_Smoothness", 0.7f);
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
