using System;
using UnityEngine;

namespace CrossportPlus
{
    [RequireComponent(typeof(SourceCamera))]
    public class MyCullingWithGroup : MonoBehaviour
    {
        private SourceCamera _srcCam;
        private Camera _camCulling;
        private CullingGroup _cullingGroup;
        private Renderer[] _renderers;
        private BoundingSphere[] _boundingSpheres;

        private void Start()
        {
            // 在SourceCamera下创建一个子Camera，用于Culling
            _srcCam = GetComponent<SourceCamera>();
            GameObject childCamera = new GameObject("Culling Camera");
            childCamera.transform.SetParent(transform);
            _camCulling = childCamera.AddComponent<Camera>();
            _camCulling.CopyFrom(_srcCam.GetComponent<Camera>());
            _camCulling.projectionMatrix = Matrix4x4.Perspective(_srcCam.fov, 1,
                _srcCam.GetComponent<Camera>().nearClipPlane, _srcCam.GetComponent<Camera>().farClipPlane);
            // render nothing
            _camCulling.cullingMask = 0;
            _camCulling.clearFlags = CameraClearFlags.Nothing;

            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = _camCulling;
            _renderers = FindObjectsOfType<Renderer>();
            Debug.Log($"Culling: there are [{_renderers.Length}] renderers in scene.");
            _boundingSpheres = new BoundingSphere[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _boundingSpheres[i] =
                    new BoundingSphere(_renderers[i].bounds.center, _renderers[i].bounds.extents.magnitude);
                // GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // sphere.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f);
                // sphere.transform.position = _renderers[i].bounds.center;
                // sphere.transform.localScale = Vector3.one * _renderers[i].bounds.extents.magnitude * 2;
                // Debug.DrawLine(_renderers[i].bounds.center, _renderers[i].bounds.center + Vector3.up * _renderers[i].bounds.extents.magnitude, Color.red, 100000);
                _renderers[i].enabled = false;
            }

            _cullingGroup.SetDistanceReferencePoint(_camCulling.transform);
            _cullingGroup.SetBoundingDistances(new float[] { _camCulling.nearClipPlane, _camCulling.farClipPlane });
            Debug.Log($"Culling: nearClipPlane: {_camCulling.nearClipPlane}, farClipPlane: {_camCulling.farClipPlane}");
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(_boundingSpheres.Length);
            _cullingGroup.onStateChanged += OnCullingGroupStateChanged;
        }
        

        private void OnCullingGroupStateChanged(CullingGroupEvent evt)
        {
            Debug.Log($"Culling: evt.index: {evt.index}, evt.isVisible: {evt.isVisible}");
            if (evt.hasBecomeVisible)
            {
                // Debug.Log($"Culling: renderer [{evt.index}] has become visible.");
                _renderers[evt.index].enabled = true;
            }
            else if (evt.hasBecomeInvisible)
            {
                // Debug.Log($"Culling: renderer [{evt.index}] has become invisible.");
                _renderers[evt.index].enabled = false;
            }
        }

        private void OnDestroy()
        {
            _cullingGroup?.Dispose();
            _cullingGroup = null;
        }
    }
}