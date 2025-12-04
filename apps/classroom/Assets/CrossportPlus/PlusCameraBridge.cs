using System;
using UnityEngine;

namespace CrossportPlus
{
    public class PlusCameraBridge : MonoBehaviour
    {
        public PlusSourceCamera sourceCamera;
        public PlusReceiverCamera receiverCamera;

        public void Update()
        {
            if (receiverCamera.receivedTexture != sourceCamera.renderedTexture)
            {
                receiverCamera.receivedTexture = sourceCamera.renderedTexture;
            }
        }
    }
}