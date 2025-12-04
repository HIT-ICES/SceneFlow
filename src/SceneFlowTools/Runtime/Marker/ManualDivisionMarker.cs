using UnityEngine;

namespace SceneFlowTools.Runtime.Marker
{
    public class ManualAllocationMarker: MonoBehaviour
    {
        public int partId;
        public SceneDeployment deployment = SceneDeployment.Device;
        public bool includeChildren = true;
    }

    public enum SceneDeployment
    {
        Device,
        Edge,
    }
}