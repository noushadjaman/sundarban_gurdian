using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.Editor
{
    public static class TigerRigBuilder
    {
        [MenuItem("Sundarban/Rig Tiger (Procedural Skeleton & Gait)")]
        public static void BuildRig()
        {
            var tiger = GameObject.Find("Tiger");
            if (tiger == null)
            {
                Debug.LogError("Tiger GameObject not found in scene!");
                return;
            }

            // Remove old visual children
            for (int i = tiger.transform.childCount - 1; i >= 0; i--)
            {
                var ch = tiger.transform.GetChild(i);
                Object.DestroyImmediate(ch.gameObject);
            }

            // Load Tiger base mesh
            string objPath = "Assets/ExternalModels/Animals/animal/tiger/source/Tiger/Tiger.obj";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (prefab == null)
            {
                objPath = "Assets/ExternalModels/Animals/animal/tiger/source/Tiger/Realistic_Tiger.obj";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            }
            if (prefab == null)
            {
                Debug.LogError("Failed to load Tiger.obj at " + objPath);
                return;
            }

            var srcMesh = prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Realistic_Tiger.mat");

            // Create VisualModel root container
            var visualModel = new GameObject("VisualModel");
            visualModel.transform.SetParent(tiger.transform, false);
            visualModel.transform.localPosition = Vector3.zero;
            visualModel.transform.localRotation = Quaternion.identity;
            visualModel.transform.localScale = Vector3.one * 0.72f;

            // Create Skeleton Hierarchy
            var rootBone = new GameObject("Tiger_Skeleton").transform;
            rootBone.SetParent(visualModel.transform, false);
            rootBone.localPosition = Vector3.zero;
            rootBone.localRotation = Quaternion.identity;

            var vmTransform = visualModel.transform;

            // Pelvis (Hips)
            var pelvis = CreateBone("Bone_Pelvis", rootBone, vmTransform, new Vector3(0f, 1.13f, -0.40f));
            // Spine
            var spine = CreateBone("Bone_Spine", pelvis, vmTransform, new Vector3(0f, 1.15f, 0.00f));
            // Chest
            var chest = CreateBone("Bone_Chest", spine, vmTransform, new Vector3(0f, 1.20f, 0.48f));
            // Neck
            var neck = CreateBone("Bone_Neck", chest, vmTransform, new Vector3(0f, 1.33f, 0.82f));
            // Head
            var head = CreateBone("Bone_Head", neck, vmTransform, new Vector3(0f, 1.48f, 1.45f));

            // Tail
            var tailBase = CreateBone("Bone_Tail_Base", pelvis, vmTransform, new Vector3(0f, 1.07f, -0.73f));
            var tailMid = CreateBone("Bone_Tail_Mid", tailBase, vmTransform, new Vector3(0f, 0.55f, -1.15f));
            var tailTip = CreateBone("Bone_Tail_Tip", tailMid, vmTransform, new Vector3(0f, 0.28f, -1.60f));

            // Front-Left Leg
            var legFLUpper = CreateBone("Bone_Leg_FL_Upper", chest, vmTransform, new Vector3(-0.20f, 1.05f, 0.55f));
            var legFLLower = CreateBone("Bone_Leg_FL_Lower", legFLUpper, vmTransform, new Vector3(-0.18f, 0.55f, 0.80f));
            var pawFL = CreateBone("Bone_Paw_FL", legFLLower, vmTransform, new Vector3(-0.175f, 0.07f, 1.025f));

            // Front-Right Leg
            var legFRUpper = CreateBone("Bone_Leg_FR_Upper", chest, vmTransform, new Vector3(0.20f, 1.05f, 0.55f));
            var legFRLower = CreateBone("Bone_Leg_FR_Lower", legFRUpper, vmTransform, new Vector3(0.18f, 0.55f, 0.58f));
            var pawFR = CreateBone("Bone_Paw_FR", legFRLower, vmTransform, new Vector3(0.150f, 0.13f, 0.617f));

            // Back-Left Leg
            var legBLUpper = CreateBone("Bone_Leg_BL_Upper", pelvis, vmTransform, new Vector3(-0.20f, 1.05f, -0.45f));
            var legBLLower = CreateBone("Bone_Leg_BL_Lower", legBLUpper, vmTransform, new Vector3(-0.18f, 0.55f, -0.70f));
            var pawBL = CreateBone("Bone_Paw_BL", legBLLower, vmTransform, new Vector3(-0.130f, 0.07f, -0.896f));

            // Back-Right Leg
            var legBRUpper = CreateBone("Bone_Leg_BR_Upper", pelvis, vmTransform, new Vector3(0.20f, 1.05f, -0.45f));
            var legBRLower = CreateBone("Bone_Leg_BR_Lower", legBRUpper, vmTransform, new Vector3(0.18f, 0.55f, -0.40f));
            var pawBR = CreateBone("Bone_Paw_BR", legBRLower, vmTransform, new Vector3(0.138f, 0.14f, -0.331f));

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
                pawFL,         // 10
                legFRUpper,    // 11
                legFRLower,    // 12
                pawFR,         // 13
                legBLUpper,    // 14
                legBLLower,    // 15
                pawBL,         // 16
                legBRUpper,    // 17
                legBRLower,    // 18
                pawBR          // 19
            };

            // Clone mesh and compute skinning
            var skinnedMesh = Object.Instantiate(srcMesh);
            skinnedMesh.name = "Skinned_BengalTiger";

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
                boneWeights[i] = CalculateWeightForVertex(verts[i], bones, visualModel.transform);
            }
            skinnedMesh.boneWeights = boneWeights;

            // Create SkinnedMeshRenderer GameObject
            var smrGo = new GameObject("Mesh_SkinnedTiger");
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

            // Setup QuadrupedProceduralAnimator on Tiger
            var anim = tiger.GetComponent<QuadrupedProceduralAnimator>();
            if (anim == null) anim = tiger.AddComponent<QuadrupedProceduralAnimator>();

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
            anim.pawFL = pawFL;

            anim.legFRUpper = legFRUpper;
            anim.legFRLower = legFRLower;
            anim.pawFR = pawFR;

            anim.legBLUpper = legBLUpper;
            anim.legBLLower = legBLLower;
            anim.pawBL = pawBL;

            anim.legBRUpper = legBRUpper;
            anim.legBRLower = legBRLower;
            anim.pawBR = pawBR;

            anim.CacheInitialTransforms();

            EditorUtility.SetDirty(tiger);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tiger.scene);

            Debug.Log("<color=green>[TigerRigBuilder] Successfully rigged Royal Bengal Tiger with 20 bones and QuadrupedProceduralAnimator!</color>");
        }

        private static Transform CreateBone(string name, Transform parent, Transform visualModel, Vector3 localPosInModel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = visualModel.TransformPoint(localPosInModel);
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        private static BoneWeight CalculateWeightForVertex(Vector3 v, Transform[] bones, Transform modelRoot)
        {
            // Vertex v is in modelRoot local space
            float x = v.x;
            float y = v.y;
            float z = v.z;

            // Check if vertex belongs to a leg:
            // Legs are outer (|x| > 0.08) and lower (y < 0.95)
            bool isLeg = (Mathf.Abs(x) > 0.08f) && (y < 0.95f);

            int b0 = 0, b1 = 0;
            float w0 = 1f, w1 = 0f;

            if (isLeg)
            {
                bool isLeft = x < 0f;
                bool isFront = z > 0.05f;

                int upper = isLeft ? (isFront ? 8 : 14) : (isFront ? 11 : 17);
                int lower = isLeft ? (isFront ? 9 : 15) : (isFront ? 12 : 18);
                int paw   = isLeft ? (isFront ? 10 : 16) : (isFront ? 13 : 19);

                // Blend along Y axis
                if (y < 0.20f)
                {
                    // Paw
                    b0 = paw;
                    b1 = lower;
                    float t = Mathf.Clamp01((y - 0.10f) / 0.10f);
                    w0 = 1f - t * 0.4f;
                    w1 = t * 0.4f;
                }
                else if (y < 0.60f)
                {
                    // Lower leg / Knee
                    b0 = lower;
                    if (y < 0.35f)
                    {
                        b1 = paw;
                        float t = Mathf.Clamp01((0.35f - y) / 0.15f);
                        w0 = 1f - t * 0.4f;
                        w1 = t * 0.4f;
                    }
                    else
                    {
                        b1 = upper;
                        float t = Mathf.Clamp01((y - 0.35f) / 0.25f);
                        w0 = 1f - t * 0.45f;
                        w1 = t * 0.45f;
                    }
                }
                else
                {
                    // Upper leg / Shoulder
                    b0 = upper;
                    int torsoBone = isFront ? 2 : 0; // Chest or Pelvis
                    float t = Mathf.Clamp01((y - 0.65f) / 0.30f);
                    b1 = torsoBone;
                    w0 = 1f - t * 0.5f;
                    w1 = t * 0.5f;
                }
            }
            else
            {
                // Body / Torso / Tail / Neck / Head
                if (z < -0.70f)
                {
                    // Tail: -0.70 to -1.77
                    if (z > -1.10f)
                    {
                        b0 = 5; // TailBase
                        b1 = 6; // TailMid
                        float t = Mathf.Clamp01((-0.70f - z) / 0.40f);
                        w0 = 1f - t;
                        w1 = t;
                    }
                    else
                    {
                        b0 = 6; // TailMid
                        b1 = 7; // TailTip
                        float t = Mathf.Clamp01((-1.10f - z) / 0.50f);
                        w0 = 1f - t;
                        w1 = t;
                    }
                }
                else if (z < -0.20f)
                {
                    // Pelvis: -0.70 to -0.20
                    b0 = 0; // Pelvis
                    b1 = (z > -0.40f) ? 1 : 5; // Spine or TailBase
                    float t = (z > -0.40f) ? Mathf.Clamp01((z + 0.40f) / 0.20f) : Mathf.Clamp01((-0.40f - z) / 0.30f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else if (z < 0.25f)
                {
                    // Spine: -0.20 to 0.25
                    b0 = 1; // Spine
                    b1 = (z < 0.05f) ? 0 : 2; // Pelvis or Chest
                    float t = (z < 0.05f) ? Mathf.Clamp01((0.05f - z) / 0.25f) : Mathf.Clamp01((z - 0.05f) / 0.20f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else if (z < 0.65f)
                {
                    // Chest: 0.25 to 0.65
                    b0 = 2; // Chest
                    b1 = (z < 0.45f) ? 1 : 3; // Spine or Neck
                    float t = (z < 0.45f) ? Mathf.Clamp01((0.45f - z) / 0.20f) : Mathf.Clamp01((z - 0.45f) / 0.20f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else if (z < 1.05f)
                {
                    // Neck: 0.65 to 1.05
                    b0 = 3; // Neck
                    b1 = (z < 0.85f) ? 2 : 4; // Chest or Head
                    float t = (z < 0.85f) ? Mathf.Clamp01((0.85f - z) / 0.20f) : Mathf.Clamp01((z - 0.85f) / 0.20f);
                    w0 = 1f - t * 0.45f;
                    w1 = t * 0.45f;
                }
                else
                {
                    // Head: > 1.05
                    b0 = 4; // Head
                    b1 = 3; // Neck
                    float t = Mathf.Clamp01((1.25f - z) / 0.20f);
                    w0 = 1f - t * 0.4f;
                    w1 = t * 0.4f;
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
