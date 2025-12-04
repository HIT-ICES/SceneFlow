using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SceneFlowTools.Utils
{
    public static class BoundsUtils
    {
        public static Bounds? From(GameObject gameObject, bool includeChildren = false)
        {
            var renderers = includeChildren
                ? gameObject.GetComponentsInChildren<Renderer>()
                : gameObject.GetComponents<Renderer>();
            if (renderers.Length == 0)
            {
                return null;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        public static Bounds? From(IEnumerable<GameObject> objects)
        {
            return Union(
                objects.Select(o => From(o))
                    .Where(b => b.HasValue)
                    .Select(b => b.Value)
            );
        }

        public static Bounds Union(Bounds a, Bounds b)
        {
            var min = Vector3.Min(a.min, b.min);
            var max = Vector3.Max(a.max, b.max);
            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static Bounds Intersect(Bounds a, Bounds b)
        {
            var min = Vector3.Max(a.min, b.min);
            var max = Vector3.Min(a.max, b.max);
            if (min.x > max.x || min.y > max.y || min.z > max.z)
            {
                return new Bounds();
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static Bounds? Union(IEnumerable<Bounds> bounds)
        {
            using var enumerator = bounds.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return null;
            }

            var min = enumerator.Current.min;
            var max = enumerator.Current.max;
            while (enumerator.MoveNext())
            {
                min = Vector3.Min(min, enumerator.Current.min);
                max = Vector3.Max(max, enumerator.Current.max);
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static bool Contains(this Bounds a, Bounds b)
        {
            return a.min.x <= b.min.x && a.min.y <= b.min.y && a.min.z <= b.min.z
                   && a.max.x >= b.max.x && a.max.y >= b.max.y && a.max.z >= b.max.z;
        }

        public static bool ContainsXZ(this Bounds a, Bounds b)
        {
            return a.min.x <= b.min.x && a.min.z <= b.min.z
                                      && a.max.x >= b.max.x && a.max.z >= b.max.z;
        }

        public static bool Contains(this Bounds a, Bounds b, double eps, bool includeX, bool includeY, bool includeZ)
        {
            return (!includeX || (a.min.x <= b.min.x && a.max.x >= b.max.x))
                   && (!includeY || (a.min.y <= b.min.y && a.max.y >= b.max.y))
                   && (!includeZ || (a.min.z <= b.min.z && a.max.z >= b.max.z));
        }

        public static bool Contains(this Bounds a, Vector3 b, bool includeX, bool includeY, bool includeZ)
        {
            return (!includeX || (a.min.x <= b.x && a.max.x >= b.x))
                   && (!includeY || (a.min.y <= b.y && a.max.y >= b.y))
                   && (!includeZ || (a.min.z <= b.z && a.max.z >= b.z));
        }

        public static bool Overlaps(this Bounds a, Bounds b)
        {
            return a.min.x <= b.max.x && a.max.x >= b.min.x
                                      && a.min.y <= b.max.y && a.max.y >= b.min.y
                                      && a.min.z <= b.max.z && a.max.z >= b.min.z;
        }
        

        public static float Volume(this Bounds a)
        {
            return a.size.x * a.size.y * a.size.z;
        }
    }
}