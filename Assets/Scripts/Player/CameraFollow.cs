using UnityEngine;
using SundarbanGuardian.Core;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Professional third-person follow camera inspired by Free Fire / PUBG Mobile.
    /// Positions the camera close behind the animal with a subtle over-the-shoulder framing
    /// so the player can see the animal's detailed features up close while enjoying an open,
    /// unobstructed panoramic view to explore the Sundarbans nature.
    /// Features intelligent obstruction handling, touch orbit look, and species auto-tuning.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target to Follow")]
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.10f, 0f); // Upper body / shoulder pivot
        public float defaultDistance = 2.15f;                    // Close Free Fire distance (was 5.5m+)
        public Vector3 shoulderOffset = new Vector3(0.36f, 0.16f, 0f); // Over-the-right-shoulder offset
        public Vector3 defaultOffset = new Vector3(0.36f, 1.26f, -2.15f); // Backwards compatibility

        [Header("Distance & Clearance Tuning")]
        public float minDistance = 1.0f;       // Closest camera can get when obstructed
        public float maxDistance = 4.5f;       // Furthest camera distance
        public float sphereRadius = 0.22f;     // Radius of collision spherecast
        public float collisionMargin = 0.25f;  // Safety cushion from obstacle surface

        [Header("Smoothing")]
        public float positionSmoothSpeed = 12.0f;
        public float zoomInSpeed = 22.0f;      // Snappy zoom in when obstacle enters view
        public float zoomOutSpeed = 8.0f;      // Smooth relaxed zoom out when clear
        public float lookAheadDistance = 15.0f;// Look forward towards nature horizon

        [Header("Collision Layers")]
        public LayerMask obstacleLayers = ~0;  // Everything by default; triggers are ignored

        [Header("Orbit & Touch Look (Free Fire Style)")]
        public float currentYaw = 0f;
        public float currentPitch = 8.5f;      // Ideal Free Fire downward horizon tilt
        public float minPitch = -12f;
        public float maxPitch = 45f;
        public float lookSensitivity = 1.2f;

        [Header("Species Auto-Tuning")]
        public bool autoTuneForSpecies = true;

        private GameManager gameManager;
        private float currentDistance;
        private float baseDistance;
        private Collider targetCollider;
        private bool isInitialized = false;

        private void Start()
        {
            gameManager = Object.FindAnyObjectByType<GameManager>();
            baseDistance = defaultDistance > 0.5f ? defaultDistance : 2.15f;
            currentDistance = baseDistance;
            FindActiveTarget();
            if (target != null)
            {
                currentYaw = target.eulerAngles.y;
                isInitialized = true;
            }
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                FindActiveTarget();
                if (target == null) return;
                if (!isInitialized)
                {
                    currentYaw = target.eulerAngles.y;
                    isInitialized = true;
                }
            }

            // Pivot around animal's upper body/shoulders
            Vector3 pivotPoint = target.position + pivotOffset;

            // Free Fire / PUBG Mobile Touch Look orbit rotation
            MobileInputManager.ConsumeLookDelta(out float deltaX, out float deltaY);
            if (Mathf.Abs(deltaX) > 0.001f || Mathf.Abs(deltaY) > 0.001f)
            {
                currentYaw += deltaX * lookSensitivity;
                currentPitch = Mathf.Clamp(currentPitch - deltaY * lookSensitivity, minPitch, maxPitch);
            }

            // Desktop Mouse Wheel zoom (safe against Input System package setting)
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    float scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
                    if (Mathf.Abs(scroll) > 0.01f)
                    {
                        baseDistance = Mathf.Clamp(baseDistance - Mathf.Sign(scroll) * 0.8f, minDistance, maxDistance);
                    }
                }
            }
            catch (System.Exception) { }
#else
            try
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    baseDistance = Mathf.Clamp(baseDistance - scroll * 2.5f, minDistance, maxDistance);
                }
            }
            catch (System.Exception) { }
#endif

            Quaternion orbitRot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 rightDir = orbitRot * Vector3.right;
            Vector3 upDir = orbitRot * Vector3.up;
            Vector3 backDir = orbitRot * Vector3.back;

            // Free Fire style over-the-shoulder offset
            Vector3 orbitPivot = pivotPoint + (rightDir * shoulderOffset.x) + (upDir * shoulderOffset.y);
            float desiredDist = Mathf.Clamp(baseDistance, minDistance, maxDistance);

            // Obstruction Handling via SphereCast
            float targetDist = desiredDist;
            Ray ray = new Ray(pivotPoint, (orbitPivot + backDir * desiredDist - pivotPoint).normalized);
            float castLength = Vector3.Distance(pivotPoint, orbitPivot + backDir * desiredDist);
            RaycastHit[] hits = Physics.SphereCastAll(ray, sphereRadius, castLength, obstacleLayers, QueryTriggerInteraction.Ignore);

            float closestHitDist = float.MaxValue;
            bool hitObstacle = false;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit h = hits[i];
                if (targetCollider != null && h.collider == targetCollider) continue;
                if (h.transform == target || h.transform.IsChildOf(target)) continue;
                if (h.collider.isTrigger) continue;

                if (h.distance < closestHitDist)
                {
                    closestHitDist = h.distance;
                    hitObstacle = true;
                }
            }

            if (hitObstacle)
            {
                targetDist = Mathf.Clamp(closestHitDist - collisionMargin, minDistance, desiredDist);
            }

            // Fast zoom-in, smooth zoom-out
            float zoomSpeed = (targetDist < currentDistance) ? zoomInSpeed : zoomOutSpeed;
            currentDistance = Mathf.Lerp(currentDistance, targetDist, zoomSpeed * Time.deltaTime);

            Vector3 finalCamPos = orbitPivot + (backDir * currentDistance);

            // Smooth position interpolation
            transform.position = Vector3.Lerp(transform.position, finalCamPos, positionSmoothSpeed * Time.deltaTime);

            // Look forward towards the nature horizon (over the shoulder)
            Vector3 lookTarget = pivotPoint + (orbitRot * Vector3.forward * lookAheadDistance);
            if ((lookTarget - transform.position).sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);
            }
        }

        public void FindActiveTarget()
        {
            if (gameManager != null && gameManager.playerObject != null && gameManager.playerObject.activeInHierarchy)
            {
                target = gameManager.playerObject.transform;
                targetCollider = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
                if (autoTuneForSpecies) ApplySpeciesPreset(target.name);
                return;
            }

            // Check for any active animal
            string[] names = { "Tiger", "Deer", "Dolphin", "Hilsha" };
            foreach (string n in names)
            {
                GameObject go = GameObject.Find(n);
                if (go != null && go.activeInHierarchy)
                {
                    target = go.transform;
                    targetCollider = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
                    if (autoTuneForSpecies) ApplySpeciesPreset(n);
                    return;
                }
            }
        }

        public void ApplySpeciesPreset(string animalName)
        {
            if (string.IsNullOrEmpty(animalName)) return;

            if (animalName.Contains("Tiger"))
            {
                pivotOffset = new Vector3(0f, 1.40f, 0f);
                defaultDistance = 3.5f;
                shoulderOffset = new Vector3(0.50f, 0.25f, 0f);
                lookAheadDistance = 18.0f;
            }
            else if (animalName.Contains("Deer"))
            {
                pivotOffset = new Vector3(0f, 1.10f, 0f);
                defaultDistance = 2.15f;
                shoulderOffset = new Vector3(0.36f, 0.16f, 0f);
                lookAheadDistance = 15.0f;
            }
            else if (animalName.Contains("Dolphin"))
            {
                pivotOffset = new Vector3(0f, 1.20f, 0f);
                defaultDistance = 9.5f;
                shoulderOffset = new Vector3(1.60f, 1.20f, 0f);
                lookAheadDistance = 25.0f;
            }
            else if (animalName.Contains("Hilsha"))
            {
                pivotOffset = new Vector3(0f, 0.35f, 0f);
                defaultDistance = 2.5f;
                shoulderOffset = new Vector3(0.38f, 0.28f, 0f);
                lookAheadDistance = 18.0f;
            }
            baseDistance = defaultDistance;
        }
    }
}
