using UnityEngine;

namespace SceneFlowTools.Runtime
{
    // Used to indicate the location of the client connection.
    // This serves to correctly identify the sub-scenes that should be rendered on the client,
    // as well as the scenes that need to be culled at the edge server.
    // Theoretically, this could be determined dynamically, but due to time constraints,
    // we had to use static indicators to conduct the experiment.
    public class ClientConnectMarker: MonoBehaviour
    {
        
    }
}