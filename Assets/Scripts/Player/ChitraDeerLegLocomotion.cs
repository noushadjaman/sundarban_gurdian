using UnityEngine;

namespace SundarbanGuardian.Player
{
    /// <summary>
    /// Biological ungulate leg locomotion and gait synthesizer for the Spotted Deer (Axis axis / চিত্রা হরিণ).
    /// Generates authentic, smooth leg articulation, high-knee carpus lift, hock push-off, and elastic suspension
    /// without any skeletal rig errors, neck twisting, or mesh distortion.
    /// </summary>
    [ExecuteAlways]
    public class ChitraDeerLegLocomotion : MonoBehaviour
    {
        [Header("Editor Live Preview")]
        [Tooltip("Test gait in Edit Mode (0 = Alert Idle, 0.5 = Nimble Trot, 1.0 = Bounding Sprint)")]
        [Range(0f, 1f)]
        public float editorPreviewSpeed = 0f;

        [Header("Gait Kinematics")]
        [Tooltip("Stride cycle frequency multiplier")]
        public float strideFrequency = 3.8f;
        [Tooltip("Forward/backward upper leg swing arc (degrees)")]
        public float legSwingAngle = 28.0f;
        [Tooltip("High-knee carpus lift angle during forward swing (degrees)")]
        public float kneeLiftAngle = 44.0f;
        [Tooltip("Back hock flexion angle during push-off (degrees)")]
        public float hockBendAngle = 38.0f;
        [Tooltip("Delicate hoof flexion during ground contact (degrees)")]
        public float hoofFlexAngle = 18.0f;

        [Header("Elastic Body Suspension")]
        [Tooltip("Vertical body bounce per step")]
        public float bodyBounceHeight = 0.038f;
        [Tooltip("Pelvic roll transfer")]
        public float pelvisRollAngle = 1.8f;
        [Tooltip("Spine pitch nodding during locomotion")]
        public float spinePitchAngle = 1.4f;

        [Header("Idle Vigilance")]
        public float idleBreathSpeed = 2.2f;
        public float idleBreathAmount = 0.008f;

        // Internal references
        private MeshFilter targetMeshFilter;
        private Mesh baseMesh;
        private Mesh animatedMesh;
        private Vector3[] baseVertices;
        private Vector3[] deformedVertices;
        private Vector3[] baseNormals;

        // Vertex assignment cache
        private struct VertexBinding
        {
            public int legIndex;      // -1 = Body/Torso, 0 = FL, 1 = FR, 2 = BL, 3 = BR
            public float weightBody;  // 1 = fully body, 0 = fully leg
            public float weightKnee;  // 0 = upper leg, 1 = lower leg
            public float weightHoof;  // 0 = lower leg, 1 = hoof
        }
        private VertexBinding[] vertexBindings;

        // Leg pivot coordinates (in local model space: centimeters)
        // FL, FR, BL, BR
        private static readonly Vector3 pivotFL_Hip   = new Vector3(-11.8f, -20.0f,  26.0f);
        private static readonly Vector3 pivotFL_Knee  = new Vector3(-11.8f, -44.0f,  26.0f);
        private static readonly Vector3 pivotFL_Hoof  = new Vector3(-11.8f, -59.0f,  26.0f);

        private static readonly Vector3 pivotFR_Hip   = new Vector3( 11.8f, -20.0f,  26.0f);
        private static readonly Vector3 pivotFR_Knee  = new Vector3( 11.8f, -44.0f,  26.0f);
        private static readonly Vector3 pivotFR_Hoof  = new Vector3( 11.8f, -59.0f,  26.0f);

        private static readonly Vector3 pivotBL_Hip   = new Vector3(-11.8f, -22.0f, -41.0f);
        private static readonly Vector3 pivotBL_Hock  = new Vector3(-11.8f, -45.0f, -41.0f);
        private static readonly Vector3 pivotBL_Hoof  = new Vector3(-11.8f, -60.0f, -41.0f);

        private static readonly Vector3 pivotBR_Hip   = new Vector3( 11.8f, -22.0f, -41.0f);
        private static readonly Vector3 pivotBR_Hock  = new Vector3( 11.8f, -45.0f, -41.0f);
        private static readonly Vector3 pivotBR_Hoof  = new Vector3( 11.8f, -60.0f, -41.0f);

        // State
        private float walkPhase = 0f;
        private float currentSpeed = 0f;
        private float smoothedSpeed = 0f;
        private bool isJumping = false;
        private float jumpBlend = 0f;
        private Vector3 initialLocalPos = Vector3.zero;
        private Quaternion initialLocalRot = Quaternion.identity;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            ResetMeshToRest();
        }

        public void Initialize()
        {
            if (isInitialized && animatedMesh != null) return;

            targetMeshFilter = GetComponent<MeshFilter>();
            if (targetMeshFilter == null)
            {
                targetMeshFilter = GetComponentInChildren<MeshFilter>();
            }

            if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null)
            {
                return;
            }

            baseMesh = targetMeshFilter.sharedMesh;
#if UNITY_EDITOR
            if (baseMesh != null && baseMesh.name.Contains("_LegLocomotionInstance"))
            {
                var orig = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Deer/Mesh_ChitraDeer_Authentic.asset");
                if (orig != null) baseMesh = orig;
            }
#endif
            if (baseMesh == null || !baseMesh.isReadable)
            {
                return;
            }

            // Create a runtime instance to avoid modifying the asset on disk
            animatedMesh = Instantiate(baseMesh);
            animatedMesh.name = baseMesh.name + "_LegLocomotionInstance";
            animatedMesh.MarkDynamic();
            targetMeshFilter.sharedMesh = animatedMesh;

            baseVertices = baseMesh.vertices;
            deformedVertices = new Vector3[baseVertices.Length];
            System.Array.Copy(baseVertices, deformedVertices, baseVertices.Length);

            baseNormals = baseMesh.normals;

            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;

            PrecomputeVertexBindings();
            isInitialized = true;
        }

        private void PrecomputeVertexBindings()
        {
            vertexBindings = new VertexBinding[baseVertices.Length];

            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector3 v = baseVertices[i];
                var b = new VertexBinding();

                // Check if vertex belongs to a leg
                // Leg criteria: Y < -16.0cm and |X| > 3.0cm
                if (v.y < -15.5f && Mathf.Abs(v.x) > 3.0f)
                {
                    bool isFront = v.z > 0f;
                    bool isLeft  = v.x < 0f;

                    if (isFront)
                    {
                        b.legIndex = isLeft ? 0 : 1; // 0 = FL, 1 = FR
                    }
                    else
                    {
                        b.legIndex = isLeft ? 2 : 3; // 2 = BL, 3 = BR
                    }

                    // Shoulder / Hip blend zone: Y between -16.0 and -24.0
                    if (v.y >= -16.0f)
                    {
                        b.weightBody = 1.0f;
                    }
                    else if (v.y <= -24.0f)
                    {
                        b.weightBody = 0.0f;
                    }
                    else
                    {
                        float t = (v.y - (-24.0f)) / 8.0f; // 0 at bottom (-24), 1 at top (-16)
                        b.weightBody = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI); // Smooth S-curve
                    }

                    // Knee / Hock blend zone: Y between -40.0 and -47.0
                    if (v.y >= -40.0f)
                    {
                        b.weightKnee = 0.0f;
                    }
                    else if (v.y <= -47.0f)
                    {
                        b.weightKnee = 1.0f;
                    }
                    else
                    {
                        float t = (-40.0f - v.y) / 7.0f;
                        b.weightKnee = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
                    }

                    // Hoof blend zone: Y below -58.0
                    if (v.y >= -58.0f)
                    {
                        b.weightHoof = 0.0f;
                    }
                    else if (v.y <= -63.0f)
                    {
                        b.weightHoof = 1.0f;
                    }
                    else
                    {
                        float t = (-58.0f - v.y) / 5.0f;
                        b.weightHoof = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
                    }
                }
                else
                {
                    b.legIndex = -1; // Torso / Head / Neck / Antlers
                    b.weightBody = 1.0f;
                }

                vertexBindings[i] = b;
            }
        }

        public void SetSpeed(float speed)
        {
            currentSpeed = Mathf.Clamp01(speed);
        }

        public void SetJumping(bool jumping)
        {
            isJumping = jumping;
        }

        private void LateUpdate()
        {
            if (!isInitialized)
            {
                Initialize();
                if (!isInitialized) return;
            }

            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            if (dt <= 0f) dt = 0.016f;

            if (!Application.isPlaying)
            {
                currentSpeed = editorPreviewSpeed;
            }

            smoothedSpeed = Mathf.Lerp(smoothedSpeed, currentSpeed, dt * 10f);
            jumpBlend = Mathf.MoveTowards(jumpBlend, isJumping ? 1f : 0f, dt * 6f);

            // Stride cycle advances proportionally to speed
            float freq = strideFrequency * (0.9f + smoothedSpeed * 0.45f);
            if (smoothedSpeed > 0.02f)
            {
                walkPhase += dt * freq * (smoothedSpeed * 6.283185f);
            }

            AnimateLegsAndBody(walkPhase, smoothedSpeed, dt);
        }

        private void AnimateLegsAndBody(float phase, float speed, float dt)
        {
            // ── 1. Diagonal Pair Gait Phase Angles ──
            // Diagonal Pair A: Front-Left (FL) and Back-Right (BR)
            // Diagonal Pair B: Front-Right (FR) and Back-Left (BL) - 180° offset
            float phaseFL = phase;
            float phaseBR = phase;
            float phaseFR = phase + Mathf.PI;
            float phaseBL = phase + Mathf.PI;

            // Sprint bound factor
            float bound = speed > 0.75f ? 1.3f : 1.0f;

            // ── 2. Upper Leg Swings (Pitch around local X) ──
            float swingFL = Mathf.Sin(phaseFL) * legSwingAngle * speed * bound;
            float swingFR = Mathf.Sin(phaseFR) * legSwingAngle * speed * bound;
            float swingBL = Mathf.Sin(phaseBL) * (legSwingAngle * 1.05f) * speed * bound;
            float swingBR = Mathf.Sin(phaseBR) * (legSwingAngle * 1.05f) * speed * bound;

            // ── 3. High-Knee Carpus Lift & Hock Push-Off ──
            // Front knees bend backward (negative pitch) only when swinging forward (sin > 0)
            float kneeFL = -Mathf.Max(0f, Mathf.Sin(phaseFL)) * kneeLiftAngle * speed;
            float kneeFR = -Mathf.Max(0f, Mathf.Sin(phaseFR)) * kneeLiftAngle * speed;

            // Back hocks flex forward (positive pitch) when swinging forward
            float hockBL = Mathf.Max(0f, Mathf.Sin(phaseBL)) * hockBendAngle * speed;
            float hockBR = Mathf.Max(0f, Mathf.Sin(phaseBR)) * hockBendAngle * speed;

            // Hoof flexion
            float hoofFL = -Mathf.Sin(phaseFL) * hoofFlexAngle * speed;
            float hoofFR = -Mathf.Sin(phaseFR) * hoofFlexAngle * speed;
            float hoofBL = -Mathf.Sin(phaseBL) * hoofFlexAngle * speed;
            float hoofBR = -Mathf.Sin(phaseBR) * hoofFlexAngle * speed;

            // ── 4. Natural Alert Idle Offsets (Standing Stance) ──
            float idleFactor = Mathf.Clamp01(1f - speed * 3f);
            swingFL +=  4.0f * idleFactor;
            kneeFL  += -6.0f * idleFactor;
            swingFR += -3.0f * idleFactor;
            kneeFR  += -2.0f * idleFactor;
            swingBL += -5.0f * idleFactor;
            hockBL  +=  9.0f * idleFactor;
            swingBR +=  3.0f * idleFactor;
            hockBR  +=  4.0f * idleFactor;

            // ── 5. Jump Steeplechase Morph ──
            if (jumpBlend > 0.01f)
            {
                swingFL = Mathf.Lerp(swingFL, -20f, jumpBlend);
                kneeFL  = Mathf.Lerp(kneeFL,  -55f, jumpBlend);
                swingFR = Mathf.Lerp(swingFR, -20f, jumpBlend);
                kneeFR  = Mathf.Lerp(kneeFR,  -55f, jumpBlend);

                swingBL = Mathf.Lerp(swingBL,  30f, jumpBlend);
                hockBL  = Mathf.Lerp(hockBL,  -15f, jumpBlend);
                swingBR = Mathf.Lerp(swingBR,  30f, jumpBlend);
                hockBR  = Mathf.Lerp(hockBR,  -15f, jumpBlend);
            }

            // ── 6. Compute 4 Leg Transform Rotations ──
            // FL
            Quaternion rFL_Hip  = Quaternion.Euler(swingFL, 0f, 0f);
            Quaternion rFL_Knee = rFL_Hip * Quaternion.Euler(kneeFL, 0f, 0f);
            Quaternion rFL_Hoof = rFL_Knee * Quaternion.Euler(hoofFL, 0f, 0f);

            // FR
            Quaternion rFR_Hip  = Quaternion.Euler(swingFR, 0f, 0f);
            Quaternion rFR_Knee = rFR_Hip * Quaternion.Euler(kneeFR, 0f, 0f);
            Quaternion rFR_Hoof = rFR_Knee * Quaternion.Euler(hoofFR, 0f, 0f);

            // BL
            Quaternion rBL_Hip  = Quaternion.Euler(swingBL, 0f, 0f);
            Quaternion rBL_Hock = rBL_Hip * Quaternion.Euler(hockBL, 0f, 0f);
            Quaternion rBL_Hoof = rBL_Hock * Quaternion.Euler(hoofBL, 0f, 0f);

            // BR
            Quaternion rBR_Hip  = Quaternion.Euler(swingBR, 0f, 0f);
            Quaternion rBR_Hock = rBR_Hip * Quaternion.Euler(hockBR, 0f, 0f);
            Quaternion rBR_Hoof = rBR_Hock * Quaternion.Euler(hoofBR, 0f, 0f);

            // ── 7. Transform Leg Vertices ──
            for (int i = 0; i < baseVertices.Length; i++)
            {
                var binding = vertexBindings[i];
                if (binding.legIndex < 0 || binding.weightBody >= 0.999f)
                {
                    deformedVertices[i] = baseVertices[i];
                    continue;
                }

                Vector3 orig = baseVertices[i];
                Vector3 pUpper, pLower, pHoof;
                Vector3 hipPivot, kneePivot, hoofPivot;
                Quaternion rHip, rKnee, rHoof;

                switch (binding.legIndex)
                {
                    case 0: // FL
                        hipPivot = pivotFL_Hip; kneePivot = pivotFL_Knee; hoofPivot = pivotFL_Hoof;
                        rHip = rFL_Hip; rKnee = rFL_Knee; rHoof = rFL_Hoof;
                        break;
                    case 1: // FR
                        hipPivot = pivotFR_Hip; kneePivot = pivotFR_Knee; hoofPivot = pivotFR_Hoof;
                        rHip = rFR_Hip; rKnee = rFR_Knee; rHoof = rFR_Hoof;
                        break;
                    case 2: // BL
                        hipPivot = pivotBL_Hip; kneePivot = pivotBL_Hock; hoofPivot = pivotBL_Hoof;
                        rHip = rBL_Hip; rKnee = rBL_Hock; rHoof = rBL_Hoof;
                        break;
                    default: // BR
                        hipPivot = pivotBR_Hip; kneePivot = pivotBR_Hock; hoofPivot = pivotBR_Hoof;
                        rHip = rBR_Hip; rKnee = rBR_Hock; rHoof = rBR_Hoof;
                        break;
                }

                // Upper leg: rotated around Hip/Shoulder pivot
                pUpper = hipPivot + rHip * (orig - hipPivot);

                // Lower leg: rotated around Knee/Hock pivot, which itself moves with Upper leg
                Vector3 movedKnee = hipPivot + rHip * (kneePivot - hipPivot);
                pLower = movedKnee + rKnee * (orig - kneePivot);

                // Hoof: rotated around Hoof pivot
                Vector3 movedHoof = movedKnee + rKnee * (hoofPivot - kneePivot);
                pHoof = movedHoof + rHoof * (orig - hoofPivot);

                // Blend between Upper and Lower
                Vector3 legP = Vector3.Lerp(pUpper, pLower, binding.weightKnee);
                // Blend in Hoof
                if (binding.weightHoof > 0.001f)
                {
                    legP = Vector3.Lerp(legP, pHoof, binding.weightHoof);
                }

                // Smooth transition at hip/shoulder boundary to prevent seams
                deformedVertices[i] = Vector3.Lerp(legP, orig, binding.weightBody);
            }

            animatedMesh.vertices = deformedVertices;
            animatedMesh.RecalculateNormals();

            // ── 8. Elastic Body Bounce & Suspension ──
            float bounceY = Mathf.Abs(Mathf.Sin(phase)) * bodyBounceHeight * speed * bound;
            float rollZ = Mathf.Cos(phase) * pelvisRollAngle * speed;
            float pitchX = Mathf.Sin(phase * 2f) * spinePitchAngle * speed;

            // Idle breathing
            float breathY = Mathf.Sin(Time.time * idleBreathSpeed) * idleBreathAmount * (1f - speed);

            transform.localPosition = initialLocalPos + new Vector3(0f, bounceY + breathY, 0f);
            transform.localRotation = initialLocalRot * Quaternion.Euler(pitchX, 0f, rollZ);
        }

        private void ResetMeshToRest()
        {
            if (animatedMesh != null && baseVertices != null)
            {
                animatedMesh.vertices = baseVertices;
                animatedMesh.RecalculateNormals();
            }
            transform.localPosition = initialLocalPos;
            transform.localRotation = initialLocalRot;
        }
    }
}
