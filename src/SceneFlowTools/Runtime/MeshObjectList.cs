using System;
using System.Collections.Generic;
using SceneFlowTools.Runtime.Utils;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    [ExecuteAlways]
    public class MeshObjectList : MonoBehaviour
    {
        [HideInInspector] public List<MeshFilter> meshObjects = new List<MeshFilter>();
        [NonSerialized] public bool isOutdated;
        private HierarchyChangeDebounce hierarchyChangeDebounce;

        private void OnEnable()
        {
            if (hierarchyChangeDebounce == null)
            {
                hierarchyChangeDebounce = new HierarchyChangeDebounce(0.5f);
                hierarchyChangeDebounce.hierarchyChanged += CheckIfOutdated;
            }

            hierarchyChangeDebounce.Enable();
            CheckIfOutdated();
        }


        private void OnDisable()
        {
            hierarchyChangeDebounce.Disable();
        }

        public void CheckIfOutdated()
        {
            List<MeshFilter> nowList = CollectMeshes();
            if (!new HashSet<MeshFilter>(nowList).SetEquals(meshObjects))
            {
                isOutdated = true;
            }
        }

        public void UpdateMeshObjects()
        {
            meshObjects = CollectMeshes();
        }

        public List<MeshFilter> CollectMeshes()
        {
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            
            foreach (var meshFilter in FindObjectsOfType<MeshFilter>())
            {
                if (meshFilter.gameObject.activeInHierarchy && meshFilter.sharedMesh != null)
                {
                    meshFilters.Add(meshFilter);
                }
            }

            Debug.Log($"collect: {meshFilters.Count} mesh filters found.");
            isOutdated = false;
            return meshFilters;
        }
    }
}