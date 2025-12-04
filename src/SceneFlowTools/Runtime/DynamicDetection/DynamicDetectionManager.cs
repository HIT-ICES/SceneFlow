using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicDetection
{
    [ExecuteAlways]
    public class DynamicDetectionManager : MonoBehaviour
    {
        public DynamicDetectionData data;
        [NonSerialized] public bool gizmosDynamicObjects;
        [NonSerialized] public List<GameObject> cachedGizmosObjects;

        // private void Start()
        // {
        //     if (data != null) return;
        //     var sceneData = SceneUtils.LoadSceneAsset<DynamicDetectionData>(gameObject.scene, "DynamicDetectionResults");
        //     if (sceneData == null) return;
        //     data = sceneData;
        // }

        private void OnDrawGizmos()
        {
            if (!gizmosDynamicObjects || data == null || data.ObjectsDynamicInfo == null) return;
            if (cachedGizmosObjects == null)
            {
                UpdateCachedGizmosObjects();
            }

            foreach (var obj in cachedGizmosObjects)
            {
                // Debug.Log($"Draw gizmos for dynamic object: {obj.name}");
                Gizmos.color = Color.green;
                var meshes = obj.GetComponentsInChildren<MeshFilter>();
                foreach (var mesh in meshes)
                {
                    if (mesh == null || mesh.sharedMesh == null) continue;
                    Gizmos.DrawWireMesh(mesh.sharedMesh,
                        mesh.transform.position,
                        mesh.transform.rotation, mesh.transform.lossyScale);
                }
            }
        }

        private void UpdateCachedGizmosObjects()
        {
            List<GameObject> allObjects = MetaInfo.CollectAll().Select(x => x.obj).ToList();
            cachedGizmosObjects = new List<GameObject>();
            foreach (var dynamicObjInfo in data.ObjectsDynamicInfo)
            {
                if (!dynamicObjInfo.DynamicType.IsDynamic()) continue;
                var obj = allObjects.Find(o =>
                    o.TryGetComponent(out MetaInfo meta) && meta.uid == dynamicObjInfo.ObjectId);
                if (obj != null)
                {
                    cachedGizmosObjects.Add(obj);
                }
            }
        }
    }
}