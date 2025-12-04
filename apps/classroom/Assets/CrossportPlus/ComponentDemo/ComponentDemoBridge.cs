using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrossportPlus.ComponentDemo
{
    public class ComponentDemoBridge : MonoBehaviour
    {
        public List<ComponentCamera> componentCameras;
        public FinalCamera finalCamera;

        void Update()
        {
            if (finalCamera.componentRgbdTextures.Count == 0)
            {
                finalCamera.componentRgbdTextures = componentCameras.Select((x) => (Texture)x.packedTexture).ToList();
            }
        }
    }
}