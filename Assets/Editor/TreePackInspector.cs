using UnityEditor;
using UnityEngine;

namespace SundarbanGuardian.Editor
{
    // [InitializeOnLoad] DISABLED
    public static class TreePackInspector
    {
        static TreePackInspector()
        {
            EditorApplication.delayCall += Inspect;
        }

        [MenuItem("Sundarban/Inspect New 7 Trees Pack")]
        public static void Inspect()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalModels/Nature/decorator/FBX/Fbx/Trees.fbx");
            if (fbx == null)
            {
                Debug.LogError("[TreePackInspector] Failed to load Trees.fbx!");
                return;
            }

            Debug.Log($"[TreePackInspector] Trees.fbx loaded! Root: {fbx.name}, Children count: {fbx.transform.childCount}");
            for (int i = 0; i < fbx.transform.childCount; i++)
            {
                var child = fbx.transform.GetChild(i);
                var mf = child.GetComponent<MeshFilter>();
                var mr = child.GetComponent<MeshRenderer>();
                string meshInfo = (mf != null && mf.sharedMesh != null) 
                    ? $"Mesh: {mf.sharedMesh.name}, Verts: {mf.sharedMesh.vertexCount}, Bounds: {mf.sharedMesh.bounds}" 
                    : "No mesh";
                string matInfo = (mr != null) ? $"Mats: {mr.sharedMaterials.Length}" : "No renderer";
                Debug.Log($"  Tree [{i}]: '{child.name}', Pos: {child.localPosition}, {meshInfo}, {matInfo}");
            }
        }
    }
}
