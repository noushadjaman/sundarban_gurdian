using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Editor
{
    public static class DeerRigBuilder
    {
        [MenuItem("Sundarban/Rig Deer (Procedural Skeleton & Gait)")]
        public static void BuildRig()
        {
            var deer = GameObject.Find("Deer");
            if (deer == null)
            {
                Debug.LogError("Deer GameObject not found in scene!");
                return;
            }

            // Remove old visual children
            for (int i = deer.transform.childCount - 1; i >= 0; i--)
            {
                var ch = deer.transform.GetChild(i);
                Object.DestroyImmediate(ch.gameObject);
            }

            // Load Deer base mesh
            string objPath = "Assets/Models/Deer/Mesh_Deer.obj";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (prefab == null)
            {
                objPath = "Assets/ExternalModels/Animals/animal/Deer by Poly by Google - fUo4AIcd8XR/Mesh_Deer.obj";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            }
            if (prefab == null)
            {
                Debug.LogError("Failed to load Mesh_Deer.obj at " + objPath);
                return;
            }

            var srcMesh = prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Authentic_Deer.mat");
            if (mat == null)
            {
                mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Deer_Material.mat");
            }

            // Create VisualModel root container
            var visualModel = new GameObject("VisualModel");
            visualModel.transform.SetParent(deer.transform, false);
            visualModel.transform.localPosition = new Vector3(0f, 0.67f, 0f);
            visualModel.transform.localRotation = Quaternion.identity;
            visualModel.transform.localScale = Vector3.one * 0.01f;

            // Create Skeleton Hierarchy under VisualModel
            var rootBone = new GameObject("Deer_Skeleton").transform;
            rootBone.SetParent(visualModel.transform, false);
            rootBone.localPosition = Vector3.zero;
            rootBone.localRotation = Quaternion.identity;

            var vmTransform = visualModel.transform;

            // Coordinate space is centimeters in local model space:
            // Y: [-67.5 (hooves) to 67.8 (antlers)]
            // Z: [-68.2 (tail) to 71.0 (snout)]
            // X: [-20.1 (left) to +20.1 (right)]

            // 1. Pelvis (Hips)
            var pelvis = CreateBone("Bone_Pelvis", rootBone, vmTransform, new Vector3(0f, 9.77f, -32.23f));
            // 2. Spine
            var spine = CreateBone("Bone_Spine", pelvis, vmTransform, new Vector3(0f, 8.80f, -10.83f));
            // 3. Chest (Withers)
            var chest = CreateBone("Bone_Chest", spine, vmTransform, new Vector3(0f, 11.17f, 17.22f));
            // 4. Neck
            var neck = CreateBone("Bone_Neck", chest, vmTransform, new Vector3(0f, 22.01f, 32.47f));
            // 5. Head & Antlers
            var head = CreateBone("Bone_Head", neck, vmTransform, new Vector3(0f, 38.17f, 57.51f));

            // Tail
            var tailBase = CreateBone("Bone_Tail_Base", pelvis, vmTransform, new Vector3(0f, 9.15f, -55.0f));
            var tailMid  = CreateBone("Bone_Tail_Mid",  tailBase, vmTransform, new Vector3(0f, 7.50f, -60.0f));
            var tailTip  = CreateBone("Bone_Tail_Tip",  tailMid,  vmTransform, new Vector3(0f, 5.00f, -65.0f));

            // Front-Left Leg (X < 0)
            var legFLUpper = CreateBone("Bone_Leg_FL_Upper", chest, vmTransform, new Vector3(-11.75f, -36.44f, 24.62f));
            var legFLLower = CreateBone("Bone_Leg_FL_Lower", legFLUpper, vmTransform, new Vector3(-11.71f, -56.83f, 24.25f));
            var hoofFL     = CreateBone("Bone_Hoof_FL",     legFLLower, vmTransform, new Vector3(-11.73f, -66.68f, 27.79f));

            // Front-Right Leg (X > 0)
            var legFRUpper = CreateBone("Bone_Leg_FR_Upper", chest, vmTransform, new Vector3(11.75f, -36.44f, 24.62f));
            var legFRLower = CreateBone("Bone_Leg_FR_Lower", legFRUpper, vmTransform, new Vector3(11.71f, -56.83f, 24.25f));
            var hoofFR     = CreateBone("Bone_Hoof_FR",     legFRLower, vmTransform, new Vector3(11.73f, -66.68f, 27.79f));

            // Back-Left Leg (X < 0)
            var legBLUpper = CreateBone("Bone_Leg_BL_Upper", pelvis, vmTransform, new Vector3(-11.81f, -26.85f, -43.80f));
            var legBLLower = CreateBone("Bone_Leg_BL_Lower", legBLUpper, vmTransform, new Vector3(-11.79f, -56.46f, -41.47f));
            var hoofBL     = CreateBone("Bone_Hoof_BL",     legBLLower, vmTransform, new Vector3(-11.79f, -66.66f, -37.07f));

            // Back-Right Leg (X > 0)
            var legBRUpper = CreateBone("Bone_Leg_BR_Upper", pelvis, vmTransform, new Vector3(11.81f, -26.85f, -43.80f));
            var legBRLower = CreateBone("Bone_Leg_BR_Lower", legBRUpper, vmTransform, new Vector3(11.79f, -56.46f, -41.47f));
            var hoofBR     = CreateBone("Bone_Hoof_BR",     legBRLower, vmTransform, new Vector3(11.79f, -66.66f, -37.07f));

            Transform[] bones = new Transform[]
            {
                pelvis,        // 0
                spine,         // 1
                chest,         // 2
                neck,          // 3
                head,          // 4
                tailBase,      // 5
                tailMid,       // 6
                tailTip,       // 7
                legFLUpper,    // 8
                legFLLower,    // 9
                hoofFL,        // 10
                legFRUpper,    // 11
                legFRLower,    // 12
                hoofFR,        // 13
                legBLUpper,    // 14
                legBLLower,    // 15
                hoofBL,        // 16
                legBRUpper,    // 17
                legBRLower,    // 18
                hoofBR         // 19
            };

            // Clone mesh and compute skinning
            var skinnedMesh = Object.Instantiate(srcMesh);
            skinnedMesh.name = "Skinned_ChitraDeer";

            // Bindposes
            Matrix4x4[] bindposes = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bindposes[i] = bones[i].worldToLocalMatrix * visualModel.transform.localToWorldMatrix;
            }
            skinnedMesh.bindposes = bindposes;

            // Skin Weights
            var verts = skinnedMesh.vertices;
            var boneWeights = new BoneWeight[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                boneWeights[i] = CalculateWeightForVertex(verts[i]);
            }
            skinnedMesh.boneWeights = boneWeights;

            // Create SkinnedMeshRenderer GameObject
            var smrGo = new GameObject("Mesh_SkinnedDeer");
            smrGo.transform.SetParent(visualModel.transform, false);
            smrGo.transform.localPosition = Vector3.zero;
            smrGo.transform.localRotation = Quaternion.identity;
            smrGo.transform.localScale = Vector3.one;

            var smr = smrGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = skinnedMesh;
            smr.bones = bones;
            smr.rootBone = pelvis;
            smr.sharedMaterial = mat;
            smr.updateWhenOffscreen = true;

            // Setup QuadrupedProceduralAnimator on Deer
            var anim = deer.GetComponent<QuadrupedProceduralAnimator>();
            if (anim == null) anim = deer.AddComponent<QuadrupedProceduralAnimator>();

            anim.rootBone = rootBone;
            anim.pelvisBone = pelvis;
            anim.spineBone = spine;
            anim.chestBone = chest;
            anim.neckBone = neck;
            anim.headBone = head;

            anim.tailBaseBone = tailBase;
            anim.tailMidBone = tailMid;
            anim.tailTipBone = tailTip;

            anim.legFLUpper = legFLUpper;
            anim.legFLLower = legFLLower;
            anim.pawFL = hoofFL;

            anim.legFRUpper = legFRUpper;
            anim.legFRLower = legFRLower;
            anim.pawFR = hoofFR;

            anim.legBLUpper = legBLUpper;
            anim.legBLLower = legBLLower;
            anim.pawBL = hoofBL;

            anim.legBRUpper = legBRUpper;
            anim.legBRLower = legBRLower;
            anim.pawBR = hoofBR;

            // Gait tuning for a nimble, graceful Sundarban Chitra Horin
            anim.strideFrequency = 4.0f;
            anim.legSwingAngle = 28.0f;
            anim.kneeBendAngle = 28.0f;
            anim.pawFlexAngle = 18.0f;
            anim.spineLateralYaw = 3.0f;
            anim.pelvisRoll = 2.0f;
            anim.chestVerticalBob = 0.04f;
            anim.headDip = 1.2f;
            anim.tailSwayAngle = 12.0f;
            anim.tailVerticalLag = 5.0f;
            anim.breathSpeed = 2.2f;
            anim.breathAmplitude = 1.4f;

            anim.CacheInitialTransforms();

            // Disable root MeshRenderer / MeshFilter if any
            var rootMf = deer.GetComponent<MeshFilter>();
            var rootMr = deer.GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;

            // Configure CharacterController
            var cc = deer.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 1.35f;
                cc.radius = 0.38f;
                cc.center = new Vector3(0f, 0.68f, 0.05f);
            }

            // Configure LandAnimalController
            var lac = deer.GetComponent<LandAnimalController>();
            if (lac != null)
            {
                lac.moveSpeed = 5.5f;
                lac.turnSpeedDeg = 400f;
            }

            // Position Deer at Kotka Glade
            if (deer.transform.position.sqrMagnitude < 1.0f)
            {
                if (cc != null) cc.enabled = false;
                deer.transform.position = new Vector3(85f, 0.08f, 48f);
                if (cc != null) cc.enabled = true;
            }

            EditorUtility.SetDirty(deer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(deer.scene);

            Debug.Log("<color=green>[DeerRigBuilder] Successfully rigged Chitra Deer with 20 bones, realistic dressup, and QuadrupedProceduralAnimator!</color>");
        }

        private static Transform CreateBone(string name, Transform parent, Transform visualModel, Vector3 localPosInModel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = visualModel.TransformPoint(localPosInModel);
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        private static BoneWeight CalculateWeightForVertex(Vector3 v)
        {
            // Vertex v is in centimeters in model local space
            float x = v.x;
            float y = v.y;
            float z = v.z;

            bool isLeg = (Mathf.Abs(x) > 3.5f) && (y < -15.0f);

            int b0 = 0, b1 = 0;
            float w0 = 1f, w1 = 0f;

            if (isLeg)
            {
                bool isLeft = x < 0f;
                bool isFront = z > 0f;

                int upper = isLeft ? (isFront ? 8 : 14) : (isFront ? 11 : 17);
                int lower = isLeft ? (isFront ? 9 : 15) : (isFront ? 12 : 18);
                int hoof  = isLeft ? (isFront ? 10 : 16) : (isFront ? 13 : 19);

                if (y < -58.0f)
                {
                    // Hoof & ankle
                    b0 = hoof;
                    b1 = lower;
                    float t = Mathf.Clamp01((y + 67.5f) / 9.5f);
                    w0 = 1f - t * 0.35f;
                    w1 = t * 0.35f;
                }
                else if (y < -38.0f)
                {
                    // Lower leg / Knee
                    b0 = lower;
                    if (y < -48.0f)
                    {
                        b1 = hoof;
                        float t = Mathf.Clamp01((-48.0f - y) / 10.0f);
                        w0 = 1f - t * 0.35f;
                        w1 = t * 0.35f;
                    }
                    else
                    {
                        b1 = upper;
                        float t = Mathf.Clamp01((y + 48.0f) / 10.0f);
                        w0 = 1f - t * 0.45f;
                        w1 = t * 0.45f;
                    }
                }
                else
                {
                    // Upper leg / Shoulder or Hip
                    b0 = upper;
                    int torsoBone = isFront ? 2 : 0; // Chest or Pelvis
                    float t = Mathf.Clamp01((y + 38.0f) / 23.0f);
                    b1 = torsoBone;
                    w0 = 1f - t * 0.5f;
                    w1 = t * 0.5f;
                }
            }
            else
            {
                // Torso / Tail / Neck / Head / Antlers
                if (z < -55.0f)
                {
                    // Tail: -55 to -68
                    if (z > -60.0f)
                    {
                        b0 = 5; // TailBase
                        b1 = 6; // TailMid
                        float t = Mathf.Clamp01((-55.0f - z) / 5.0f);
                        w0 = 1f - t;
                        w1 = t;
                    }
                    else
                    {
                        b0 = 6; // TailMid
                        b1 = 7; // TailTip
                        float t = Mathf.Clamp01((-60.0f - z) / 8.0f);
                        w0 = 1f - t;
                        w1 = t;
                    }
                }
                else if (z < -20.0f)
                {
                    // Pelvis / Rump
                    b0 = 0; // Pelvis
                    b1 = (z > -35.0f) ? 1 : 5; // Spine or TailBase
                    float t = (z > -35.0f) ? Mathf.Clamp01((z + 35.0f) / 15.0f) : Mathf.Clamp01((-35.0f - z) / 20.0f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else if (z < 6.0f)
                {
                    // Mid Spine
                    b0 = 1; // Spine
                    b1 = (z < -7.0f) ? 0 : 2; // Pelvis or Chest
                    float t = (z < -7.0f) ? Mathf.Clamp01((-7.0f - z) / 13.0f) : Mathf.Clamp01((z + 7.0f) / 13.0f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else if (z < 26.0f)
                {
                    // Chest / Withers
                    b0 = 2; // Chest
                    b1 = (z < 16.0f) ? 1 : 3; // Spine or Neck
                    float t = (z < 16.0f) ? Mathf.Clamp01((16.0f - z) / 10.0f) : Mathf.Clamp01((z - 16.0f) / 10.0f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else if (z < 42.0f)
                {
                    // Neck
                    b0 = 3; // Neck
                    b1 = (z < 34.0f) ? 2 : 4; // Chest or Head
                    float t = (z < 34.0f) ? Mathf.Clamp01((34.0f - z) / 8.0f) : Mathf.Clamp01((z - 34.0f) / 8.0f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else
                {
                    // Head & Antlers: z >= 42
                    b0 = 4; // Head
                    b1 = 3; // Neck
                    float t = Mathf.Clamp01((55.0f - z) / 13.0f);
                    w0 = 1f - t * 0.35f;
                    w1 = t * 0.35f;
                }
            }

            var bw = new BoneWeight();
            bw.boneIndex0 = b0;
            bw.weight0 = w0;
            bw.boneIndex1 = b1;
            bw.weight1 = w1;
            return bw;
        }
    }
}
