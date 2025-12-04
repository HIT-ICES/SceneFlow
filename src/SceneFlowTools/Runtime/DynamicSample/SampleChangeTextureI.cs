using UnityEngine;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTextureI : MonoBehaviour
    {

        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;


        public Camera playerCamera;


        public float rayDistance = 5f;

        void Awake()
        {

            selfRenderer = GetComponent<Renderer>();
        

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        void Update()
        {

            if (Input.GetMouseButtonDown(0))
            {

                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, rayDistance))
                {
                    if (hit.collider.gameObject == gameObject)
                    {

                        useA = !useA;
                        Texture chosen = useA ? textureA : textureB;

                        if (selfRenderer != null && selfRenderer.material != null)
                        {
                            selfRenderer.material.SetTexture("_MainTex", chosen);
                        }
                        if (targetRenderer != null && targetRenderer.material != null)
                        {
                            targetRenderer.material.SetTexture("_MainTex", chosen);
                        }
                    }
                }
            }
        }
    }
}