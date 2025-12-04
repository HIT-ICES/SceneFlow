using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    [Serializable]
    public class GlobalDivisionResult : ScriptableObject, ISerializationCallbackReceiver
    {
        public List<List<int>> groups;
        public List<int> groupsData;


        public void OnBeforeSerialize()
        {
            groups ??= new List<List<int>>();
            groupsData = new List<int>();
            foreach (var group in groups)
            {
                groupsData.Add(-1);
                foreach (var objId in group)
                {
                    groupsData.Add(objId);
                }
            }
        }

        public void OnAfterDeserialize()
        {
            groupsData ??= new List<int>();
            groups = new List<List<int>>();
            foreach (var objId in groupsData)
            {
                if (objId == -1)
                {
                    groups.Add(new List<int>());
                    continue;
                }

                if (groups.Count == 0)
                {
                    groups.Add(new List<int>());
                }

                groups[^1].Add(objId);
            }
        }
    }
}