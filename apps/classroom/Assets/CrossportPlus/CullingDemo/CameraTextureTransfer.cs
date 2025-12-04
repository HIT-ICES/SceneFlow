using System.Collections;
using System.Collections.Generic;
using CrossportPlus.CullingDemo;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;


namespace CrossportPlus
{
    public class CameraTextureTransfer : MonoBehaviour
    {
        public SourceCamera srcCamera;
        public ReceiverCamera dstCamera;
        private RenderTexture _crossTexture;
        private RenderTexture _targetTexture;

        // Start is called before the first frame update
        void Start()
        {
            _crossTexture = new RenderTexture(srcCamera.renderFaceSize * 4, srcCamera.renderFaceSize * 3, 24);
            Debug.Log($"_crossTexture.dimension: {_crossTexture.dimension}");
            dstCamera.overlapTexture = _crossTexture;
            
            _targetTexture = new RenderTexture(srcCamera.renderFaceSize * 3, srcCamera.renderFaceSize * 2, 24);
            dstCamera.skyboxTexture = _targetTexture;
            
            // RenderSettings.skybox.SetTexture("_Tex", _renderTexture);
        
        
        
        }

        // Update is called once per frame
        void Update()
        {
            TextureUtils.CubeTo2DCross(srcCamera.renderTexture, _crossTexture);
            TextureUtils.CubeToTex2D(srcCamera.renderTexture,_targetTexture);
        }
    
    
    }
}
