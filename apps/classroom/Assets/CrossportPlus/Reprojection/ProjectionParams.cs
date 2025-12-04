using System;
using UnityEngine;

namespace CrossportPlus.Reprojection
{
    [Serializable]
    public struct ProjectionParams
    {
        /// <summary>
        /// fov in degrees, vertical
        /// </summary>
        [Range(1, 179)] public float fieldOfView;

        public float aspect;
        public float near;
        public float far;

        public ProjectionParams(float fieldOfView, float aspect, float near, float far)
        {
            this.fieldOfView = fieldOfView;
            this.aspect = aspect;
            this.near = near;
            this.far = far;
        }

        public static ProjectionParams FromCamera(Camera camera)
        {
            return new ProjectionParams(camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(fieldOfView * Mathf.Deg2Rad, aspect, near, far);
        }

        public Matrix4x4 PerspectiveMatrix()
        {
            return Matrix4x4.Perspective(fieldOfView, aspect, near, far);
        }
    }
}