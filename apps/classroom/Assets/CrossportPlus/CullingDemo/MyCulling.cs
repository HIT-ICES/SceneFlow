using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace CrossportPlus
{
    [RequireComponent(typeof(SourceCamera))]
    public class MyCulling : MonoBehaviour
    {
        private SourceCamera _srcCam;
        private System.Threading.Tasks.Task _cullingTask;
        private MyCullingTarget[] _targets;
        private Vector3 _lastCameraPosition, _lastCameraForward;
        private bool _needUpdateCulling = true;

        private void Start()
        {
            _srcCam = GetComponent<SourceCamera>();

            Renderer[] renderers = FindObjectsOfType<Renderer>();
            _targets = new MyCullingTarget[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                _targets[i] = new MyCullingTarget
                {
                    renderer = renderer,
                    excluded = false,
                    position = renderer.bounds.center,
                    radius = renderer.bounds.extents.magnitude,
                    visible = true
                };
                // Debug.Log($"MyCulling: Found renderer: {renderer.name}, position: {_targets[i].position}, radius: {_targets[i].radius}");
            }

            Debug.Log($"MyCulling: Found {_targets.Length} renderers.");
        }

        void Update()
        {
            if (_srcCam.transform.position != _lastCameraPosition || _srcCam.transform.forward != _lastCameraForward)
            {
                // Debug.Log($"MyCulling: Camera moved. Update culling.");
                _lastCameraPosition = _srcCam.transform.position;
                _lastCameraForward = _srcCam.transform.forward;
                _needUpdateCulling = true;
            }

            if (_cullingTask?.IsCompleted ?? false)
            {
                // Debug.Log($"MyCulling: frame[{Time.frameCount}], Culling task completed. apply...");
                foreach (MyCullingTarget target in _targets)
                {
                    target.renderer.enabled = target.visible;
                }

                _cullingTask = null;
            }

            if (_needUpdateCulling && (_cullingTask?.IsCompleted ?? true))
            {
                // Debug.Log($"MyCulling: frame[{Time.frameCount}], Start culling task.");
                _needUpdateCulling = false;
                var camPos = _srcCam.transform.position;
                var camLook = _srcCam.transform.forward;
                // Debug.DrawLine(camPos, camPos + camLook * 1000, Color.red, 1);
                var fov = _srcCam.fov;
                _cullingTask = System.Threading.Tasks.Task.Run(() => { Culling(camPos, camLook, fov, _targets); });
            }
        }

        static void Culling(Vector3 camPos, Vector3 camLook, float fov, MyCullingTarget[] targets)
        {
            camLook.Normalize();
            for (int i = 0; i < targets.Length; i++)
            {
                MyCullingTarget target = targets[i];
                if (target.excluded)
                {
                    continue;
                }

                // demo：剔除在背面的物体
                Plane plane = new Plane(camLook, camPos);
                float distance = plane.GetDistanceToPoint(target.position);
                if (distance > 0 || Mathf.Abs(distance) < target.radius)
                {
                    targets[i].visible = true;
                }
                else
                {
                    targets[i].visible = false;
                }




                // Debug.Log($"MyCulling: {target.renderer.name} position: {target.position}");
                //
                // // 球心投影到四棱锥轴上
                // float t = Vector3.Dot(target.position - camPos, camLook);
                // Debug.Log($"MyCulling: t={t}");
                // // if (t < 0)
                // // {
                // //     // 球心在相机后面
                // //     targets[i].visible = false;
                // //     continue;
                // // }
                // Vector3 p = camPos + t * camLook;
                // // 计算投影点对应的四棱锥截面半径
                // float r = t * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                // Debug.Log($"MyCulling: r={r}");
                // // 计算球心到投影点的水平距离
                // float d = Vector3.Distance(p, target.position);
                // Debug.Log($"MyCulling: d={d}");
                // // 检查球是否在该截面范围
                // targets[i].visible = d < r + target.radius;
                // Debug.Log($"MyCulling: {target.renderer.name} is visible: {d < r + target.radius}");
                ////Debug.Log($"MyCulling: {target.renderer.name} is visible: {d < r + target.radius}");
            }
        }
    }

    struct MyCullingTarget
    {
        public Renderer renderer;
        public bool excluded;
        public Vector3 position;
        public float radius;
        public bool visible;
    }
}