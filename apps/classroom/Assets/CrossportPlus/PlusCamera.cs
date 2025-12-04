using UnityEngine;

namespace CrossportPlus
{
    public abstract class PlusSourceCamera : MonoBehaviour
    {
        public abstract RenderTexture renderedTexture { get; }
    }

    public abstract class PlusReceiverCamera : MonoBehaviour
    {
        public abstract RenderTexture receivedTexture { get; set; }
    }
}