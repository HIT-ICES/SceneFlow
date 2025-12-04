using UnityEngine;

namespace SceneFlowTools.Runtime.Utils
{
    public class MyGizmosUtils
    {
        public static void GizmosObjectWireMesh(GameObject obj, Color color, bool includeChildren = false)
        {
            if (obj == null) return;

            var prevColor = Gizmos.color;
            Gizmos.color = color;
            if (!includeChildren)
            {
                var meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Gizmos.DrawWireMesh(meshFilter.sharedMesh, obj.transform.position, obj.transform.rotation,
                        obj.transform.lossyScale);
                }
            }
            else
            {
                var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
                foreach (var mf in meshFilters)
                {
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Gizmos.DrawWireMesh(mf.sharedMesh, mf.transform.position, mf.transform.rotation,
                            mf.transform.lossyScale);
                    }
                }
            }

            Gizmos.color = prevColor;
        }
    }
}