using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Editor
{
    public static class SetupRiverNetsAndAquaticChallenges
    {
        private const string NetRootName = "[River_Nets_And_Hazards]";

        [MenuItem("Sundarban/Setup River Nets & Aquatic Challenges")]
        public static void BuildRiverChallenges()
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            // 1. Ensure net assets & materials exist
            BuildRiverFishingNets.GenerateNetAssets();

            var curtainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FishingNet_GreenCurtain.mat");
            var accordionMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AccordionNet_Mesh.mat");
            var bambooMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Bamboo_Pole.mat");
            var corkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Net_Cork_Buoy.mat");
            var urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 2. Remove existing net root if present
            var existingRoot = GameObject.Find(NetRootName);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var netRoot = new GameObject(NetRootName);
            netRoot.transform.position = Vector3.zero;

            // -------------------------------------------------------------
            // NET BARRIER 1: Estuary Mouth Illegal Gillnet (Z = -250)
            // Wide monofilament curtain spanning estuary with a boat passage gap at X = [-35 .. -15]
            // -------------------------------------------------------------
            BuildCurtainGillnet(
                parent: netRoot.transform,
                name: "Gillnet_Line_1_EstuaryMouth",
                z: -250f,
                leftX: -140f,
                rightX: 55f,
                gapLeftX: -35f,
                gapRightX: -12f, // 23m clear boat navigation passage
                curtainMat: curtainMat,
                bambooMat: bambooMat,
                corkMat: corkMat
            );

            // -------------------------------------------------------------
            // ACCORDION BOX NETS (চাই / চায়না জাল) - Cluster 1 at Z = -238
            // Long rectangular tunnel traps on riverbed margins (Image 2 & 5)
            // -------------------------------------------------------------
            BuildAccordionBoxNet(
                parent: netRoot.transform,
                name: "ChinaBoxNet_WestMargin_1",
                startPos: new Vector3(-125f, -1.85f, -238f),
                direction: new Vector3(0.3f, 0f, 1f).normalized,
                length: 14f,
                width: 2.2f,
                height: 1.4f,
                accordionMat: accordionMat,
                bambooMat: bambooMat
            );

            BuildAccordionBoxNet(
                parent: netRoot.transform,
                name: "ChinaBoxNet_EastMargin_1",
                startPos: new Vector3(42f, -1.85f, -242f),
                direction: new Vector3(-0.2f, 0f, 1f).normalized,
                length: 12f,
                width: 2.0f,
                height: 1.3f,
                accordionMat: accordionMat,
                bambooMat: bambooMat
            );

            // -------------------------------------------------------------
            // CHALLENGE 2: Mechanized Diesel Trawler (Z = -222)
            // Engine acoustic noise zone disturbing echolocation
            // -------------------------------------------------------------
            BuildAcousticTrawler(
                parent: netRoot.transform,
                pos: new Vector3(-10f, 0.2f, -222f),
                litShader: urpLit
            );

            // -------------------------------------------------------------
            // NET BARRIER 2: Mid-Estuary Staggered Gillnets (Z = -198 and Z = -188)
            // Forces player to slalom through an S-curve navigation channel
            // -------------------------------------------------------------
            BuildCurtainGillnet(
                parent: netRoot.transform,
                name: "Gillnet_Line_2A_WestWing",
                z: -198f,
                leftX: -145f,
                rightX: -38f,
                gapLeftX: 999f, // Solid wing, channel is on right
                gapRightX: 999f,
                curtainMat: curtainMat,
                bambooMat: bambooMat,
                corkMat: corkMat
            );

            BuildCurtainGillnet(
                parent: netRoot.transform,
                name: "Gillnet_Line_2B_EastWing",
                z: -188f,
                leftX: -18f,
                rightX: 62f,
                gapLeftX: 999f, // Solid wing, channel is on left
                gapRightX: 999f,
                curtainMat: curtainMat,
                bambooMat: bambooMat,
                corkMat: corkMat
            );

            // -------------------------------------------------------------
            // ACCORDION BOX NETS (চাই) - Cluster 2 at Z = -175
            // -------------------------------------------------------------
            BuildAccordionBoxNet(
                parent: netRoot.transform,
                name: "ChinaBoxNet_DeepChannel_2",
                startPos: new Vector3(-68f, -1.95f, -175f),
                direction: Vector3.forward,
                length: 16f,
                width: 2.4f,
                height: 1.5f,
                accordionMat: accordionMat,
                bambooMat: bambooMat
            );

            // -------------------------------------------------------------
            // CHALLENGE 3: River Crocodiles at Z = -182
            // -------------------------------------------------------------
            BuildRiverCrocodiles(netRoot.transform, urpLit);

            // -------------------------------------------------------------
            // NET BARRIER 3: Choke Point Gillnet Maze (Z = -155)
            // High-density net barrier before the Sanctuary confluence
            // -------------------------------------------------------------
            BuildCurtainGillnet(
                parent: netRoot.transform,
                name: "Gillnet_Line_3_SanctuaryChoke",
                z: -155f,
                leftX: -130f,
                rightX: 45f,
                gapLeftX: -65f,
                gapRightX: -45f, // 20m narrow gap in center
                curtainMat: curtainMat,
                bambooMat: bambooMat,
                corkMat: corkMat
            );

            // -------------------------------------------------------------
            // TOOL PICKUP: Net-Cutter Knives floating in wooden crate at Z = -262
            // -------------------------------------------------------------
            BuildFloatingToolCrate(
                parent: netRoot.transform,
                pos: new Vector3(-28f, 0.15f, -262f),
                litShader: urpLit
            );

            // -------------------------------------------------------------
            // 4. Update scene animal positions for Dolphin and Hilsha
            // -------------------------------------------------------------
            UpdateAnimalSpawnPositions();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[SetupRiverNetsAndAquaticChallenges] River Fishing Nets & Aquatic Challenges Built Successfully!</color>");
        }

        private static void BuildCurtainGillnet(Transform parent, string name, float z, float leftX, float rightX,
            float gapLeftX, float gapRightX, Material curtainMat, Material bambooMat, Material corkMat)
        {
            var netGo = new GameObject(name);
            netGo.transform.SetParent(parent, false);
            netGo.transform.position = new Vector3(0f, 0f, z);

            float totalSpan = rightX - leftX;
            float poleHeight = 6.2f;
            float netDepth = 2.4f; // Water surface Y=0 down to bed Y=-2.4

            // Segment 1: Left of Gap
            bool hasGap = gapRightX > gapLeftX && gapLeftX >= leftX && gapRightX <= rightX;
            if (hasGap)
            {
                BuildNetWallSection(netGo.transform, $"{name}_LeftSection", leftX, gapLeftX, z, netDepth, curtainMat, corkMat);
                BuildNetWallSection(netGo.transform, $"{name}_RightSection", gapRightX, rightX, z, netDepth, curtainMat, corkMat);

                // Gap Marker Buoys (White / Green passage indicators)
                BuildPassageMarker(netGo.transform, new Vector3(gapLeftX, 0.1f, z), Color.green);
                BuildPassageMarker(netGo.transform, new Vector3(gapRightX, 0.1f, z), Color.green);
            }
            else
            {
                BuildNetWallSection(netGo.transform, $"{name}_FullSection", leftX, rightX, z, netDepth, curtainMat, corkMat);
            }

            // Bamboo End Stakes driven into riverbank
            BuildBambooStake(netGo.transform, new Vector3(leftX, 0.5f, z), poleHeight, bambooMat);
            BuildBambooStake(netGo.transform, new Vector3(rightX, 0.5f, z), poleHeight, bambooMat);

            // Intermediate Bamboo Stakes every 30m
            for (float x = leftX + 30f; x < rightX - 10f; x += 30f)
            {
                if (hasGap && x >= gapLeftX && x <= gapRightX) continue;
                BuildBambooStake(netGo.transform, new Vector3(x, 0.3f, z), poleHeight * 0.85f, bambooMat);
            }
        }

        private static void BuildNetWallSection(Transform parent, string name, float xStart, float xEnd, float z,
            float depth, Material curtainMat, Material corkMat)
        {
            float width = xEnd - xStart;
            if (width <= 1.0f) return;

            float centerX = (xStart + xEnd) * 0.5f;

            var sectionGo = new GameObject(name);
            sectionGo.transform.SetParent(parent, false);
            sectionGo.transform.position = new Vector3(centerX, 0f, z);

            // 1. Mesh Quad for the hanging diamond net curtain
            var meshGo = new GameObject("Curtain_Mesh");
            meshGo.transform.SetParent(sectionGo.transform, false);
            meshGo.transform.localPosition = new Vector3(0f, -depth * 0.5f, 0f);

            var mf = meshGo.AddComponent<MeshFilter>();
            var mr = meshGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = curtainMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Generate double-sided quad mesh with repeating UVs
            Mesh mesh = new Mesh();
            mesh.name = "Gillnet_Curtain_Mesh";
            float halfW = width * 0.5f;
            float halfH = depth * 0.5f;

            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfW,  halfH, 0f),
                new Vector3( halfW,  halfH, 0f),
                new Vector3( halfW, -halfH, 0f),
                new Vector3(-halfW, -halfH, 0f),
                // Back face
                new Vector3(-halfW,  halfH, 0f),
                new Vector3( halfW,  halfH, 0f),
                new Vector3( halfW, -halfH, 0f),
                new Vector3(-halfW, -halfH, 0f)
            };

            float uvTilingX = width * 0.5f;
            float uvTilingY = depth * 0.8f;
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, uvTilingY),
                new Vector2(uvTilingX, uvTilingY),
                new Vector2(uvTilingX, 0f),
                new Vector2(0f, 0f),
                // Back face UVs
                new Vector2(0f, uvTilingY),
                new Vector2(uvTilingX, uvTilingY),
                new Vector2(uvTilingX, 0f),
                new Vector2(0f, 0f)
            };

            mesh.triangles = new int[]
            {
                0, 1, 2, 0, 2, 3, // Front
                6, 5, 4, 7, 6, 4  // Back
            };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;

            // 2. Floating Cork Buoys strung along top float line
            int buoyCount = Mathf.Max(2, Mathf.RoundToInt(width / 2.2f));
            float step = width / (buoyCount + 1);

            for (int i = 1; i <= buoyCount; i++)
            {
                var buoy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                buoy.name = $"CorkBuoy_{i}";
                buoy.transform.SetParent(sectionGo.transform, false);
                float bx = -halfW + i * step;
                buoy.transform.localPosition = new Vector3(bx, 0.06f, 0f);
                buoy.transform.localScale = new Vector3(0.38f, 0.26f, 0.38f);
                Object.DestroyImmediate(buoy.GetComponent<Collider>());
                buoy.GetComponent<MeshRenderer>().sharedMaterial = corkMat;
            }

            // 3. Trigger Collider & IllegalNetObstacle Component
            var col = sectionGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(width, depth, 1.2f);
            col.center = new Vector3(0f, -depth * 0.5f, 0f);

            var netObs = sectionGo.AddComponent<IllegalNetObstacle>();
            netObs.netType = RiverNetType.MonofilamentGillnet;
            netObs.hitDamage = 25f;
            netObs.knockbackDistance = 3.5f;
        }

        private static void BuildBambooStake(Transform parent, Vector3 pos, float height, Material bambooMat)
        {
            var stake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stake.name = "Bamboo_Stake";
            stake.transform.SetParent(parent, false);
            stake.transform.position = pos;
            stake.transform.localScale = new Vector3(0.24f, height * 0.5f, 0.24f);
            Object.DestroyImmediate(stake.GetComponent<Collider>());
            stake.GetComponent<MeshRenderer>().sharedMaterial = bambooMat;

            // Red warning flag on top of stake
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Fisherman_Flag";
            flag.transform.SetParent(stake.transform, false);
            flag.transform.localPosition = new Vector3(0.5f, 0.88f, 0f);
            flag.transform.localScale = new Vector3(0.9f, 0.22f, 0.08f);
            Object.DestroyImmediate(flag.GetComponent<Collider>());
            var flagMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            flagMat.SetColor("_BaseColor", new Color(0.9f, 0.15f, 0.15f, 1f));
            flag.GetComponent<MeshRenderer>().sharedMaterial = flagMat;
        }

        private static void BuildPassageMarker(Transform parent, Vector3 pos, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Passage_Buoy_Marker";
            marker.transform.SetParent(parent, false);
            marker.transform.position = pos;
            marker.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            var mMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mMat.SetColor("_BaseColor", color);
            marker.GetComponent<MeshRenderer>().sharedMaterial = mMat;
        }

        private static void BuildAccordionBoxNet(Transform parent, string name, Vector3 startPos, Vector3 direction,
            float length, float width, float height, Material accordionMat, Material bambooMat)
        {
            var netGo = new GameObject(name);
            netGo.transform.SetParent(parent, false);
            netGo.transform.position = startPos;
            netGo.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            // Outer Tunnel Mesh (Box with Accordion Net texture)
            var tunnel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnel.name = "Accordion_Tunnel_Mesh";
            tunnel.transform.SetParent(netGo.transform, false);
            tunnel.transform.localPosition = new Vector3(0f, height * 0.5f, length * 0.5f);
            tunnel.transform.localScale = new Vector3(width, height, length);
            Object.DestroyImmediate(tunnel.GetComponent<Collider>());
            tunnel.GetComponent<MeshRenderer>().sharedMaterial = accordionMat;

            // Wire hoop ribs every 1.2m along the accordion (Image 2 & 3)
            int hoopCount = Mathf.RoundToInt(length / 1.2f);
            for (int i = 0; i <= hoopCount; i++)
            {
                float hz = i * (length / hoopCount);
                var hoop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hoop.name = $"Rib_Hoop_{i}";
                hoop.transform.SetParent(netGo.transform, false);
                hoop.transform.localPosition = new Vector3(0f, height * 0.5f, hz);
                hoop.transform.localScale = new Vector3(width * 1.04f, height * 1.04f, 0.08f);
                Object.DestroyImmediate(hoop.GetComponent<Collider>());
                hoop.GetComponent<MeshRenderer>().sharedMaterial = bambooMat;
            }

            // Tapered funnel entrance wedge at the front
            var funnel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            funnel.name = "Trap_Funnel_Entrance";
            funnel.transform.SetParent(netGo.transform, false);
            funnel.transform.localPosition = new Vector3(0f, height * 0.5f, -0.6f);
            funnel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            funnel.transform.localScale = new Vector3(width * 0.8f, 0.5f, height * 0.8f);
            Object.DestroyImmediate(funnel.GetComponent<Collider>());
            funnel.GetComponent<MeshRenderer>().sharedMaterial = accordionMat;

            // Trigger & Hazard Component
            var col = netGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(width * 1.1f, height * 1.1f, length + 1.2f);
            col.center = new Vector3(0f, height * 0.5f, length * 0.5f);

            var netObs = netGo.AddComponent<IllegalNetObstacle>();
            netObs.netType = RiverNetType.AccordionBoxTrap;
            netObs.hitDamage = 22f;
            netObs.knockbackDistance = 4f;
        }

        private static void BuildAcousticTrawler(Transform parent, Vector3 pos, Shader litShader)
        {
            var trawlerGo = new GameObject("Mechanized_Trawler_Hazard");
            trawlerGo.transform.SetParent(parent, false);
            trawlerGo.transform.position = pos;
            trawlerGo.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

            // Hull
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Trawler_Hull";
            hull.transform.SetParent(trawlerGo.transform, false);
            hull.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            hull.transform.localScale = new Vector3(3.2f, 1.4f, 9.5f);
            Object.DestroyImmediate(hull.GetComponent<Collider>());
            var hullMat = new Material(litShader);
            hullMat.SetColor("_BaseColor", new Color(0.18f, 0.24f, 0.28f, 1f));
            hull.GetComponent<MeshRenderer>().sharedMaterial = hullMat;

            // Cabin
            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(trawlerGo.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 1.6f, -1.2f);
            cabin.transform.localScale = new Vector3(2.4f, 1.5f, 3.2f);
            Object.DestroyImmediate(cabin.GetComponent<Collider>());
            var cabinMat = new Material(litShader);
            cabinMat.SetColor("_BaseColor", new Color(0.72f, 0.65f, 0.52f, 1f));
            cabin.GetComponent<MeshRenderer>().sharedMaterial = cabinMat;

            // Exhaust Stack emitting engine smoke
            var exhaust = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exhaust.name = "Exhaust_Pipe";
            exhaust.transform.SetParent(cabin.transform, false);
            exhaust.transform.localPosition = new Vector3(0.4f, 0.9f, 0.6f);
            exhaust.transform.localScale = new Vector3(0.15f, 0.6f, 0.15f);
            Object.DestroyImmediate(exhaust.GetComponent<Collider>());

            // Acoustic Hazard Zone Trigger
            var col = trawlerGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 22f;

            var hazard = trawlerGo.AddComponent<SundarbanGuardian.Core.HazardZoneTrigger>();
            hazard.riskTier = "high";
            hazard.rawDataValue = 88f;
            hazard.rawDataUnit = "dB Trawler Motor Acoustic Pollution";
        }

        private static void BuildRiverCrocodiles(Transform parent, Shader litShader)
        {
            var crocGroup = new GameObject("River_Saltwater_Crocodiles");
            crocGroup.transform.SetParent(parent, false);

            var crocDefs = new (Vector3 pos, Vector3 patrolOffset, float rotY)[]
            {
                (new Vector3(-92f, -0.1f, -185f), new Vector3(14f, 0f, 8f), 45f),
                (new Vector3(45f, -0.1f, -205f), new Vector3(-12f, 0f, 10f), 225f)
            };

            var crocPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Animals/decorate/Crocodile by Poly by Google - 2an6E2WjW3z/Mesh_Crocodile.obj");

            for (int i = 0; i < crocDefs.Length; i++)
            {
                GameObject croc = null;
                if (crocPrefab != null)
                {
                    croc = Object.Instantiate(crocPrefab, crocGroup.transform);
                }
                else
                {
                    croc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    croc.transform.localScale = new Vector3(1.4f, 0.4f, 4.2f);
                }

                croc.name = $"Saltwater_Crocodile_{i + 1}";
                croc.transform.position = crocDefs[i].pos;
                croc.transform.rotation = Quaternion.Euler(0f, crocDefs[i].rotY, 0f);

                var threat = croc.GetComponent<CrocodileThreat>() ?? croc.AddComponent<CrocodileThreat>();
                threat.patrolOffset = crocDefs[i].patrolOffset;
                threat.patrolSpeed = 2.2f;
                threat.biteDamage = 35f;

                // Ensure it has trigger collider
                var boxCol = croc.GetComponent<Collider>() ?? croc.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
            }
        }

        private static void BuildFloatingToolCrate(Transform parent, Vector3 pos, Shader litShader)
        {
            var crateGo = new GameObject("Floating_NetCutter_Pickup");
            crateGo.transform.SetParent(parent, false);
            crateGo.transform.position = pos;

            var crateMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crateMesh.name = "Wooden_Crate";
            crateMesh.transform.SetParent(crateGo.transform, false);
            crateMesh.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
            Object.DestroyImmediate(crateMesh.GetComponent<Collider>());
            var woodMat = new Material(litShader);
            woodMat.SetColor("_BaseColor", new Color(0.55f, 0.38f, 0.22f, 1f));
            crateMesh.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Trigger & Pickup component
            var col = crateGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.5f;

            var pickup = crateGo.AddComponent<ToolPickupItem>();
            pickup.toolType = ToolType.NetCutter;
            pickup.bobSpeed = 2.2f;
        }

        private static void UpdateAnimalSpawnPositions()
        {
            // Dolphin: Estuary open water
            var dolphin = GameObject.Find("Dolphin");
            if (dolphin != null)
            {
                dolphin.transform.position = new Vector3(-25f, -0.45f, -270f);
                dolphin.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Facing north upstream
                EditorUtility.SetDirty(dolphin);
            }

            // Hilsha: Bay mouth offshore deep water
            var hilsha = GameObject.Find("Hilsha");
            if (hilsha != null)
            {
                hilsha.transform.position = new Vector3(-25f, -0.45f, -275f);
                hilsha.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Facing north upstream
                EditorUtility.SetDirty(hilsha);
            }
        }
    }
}
