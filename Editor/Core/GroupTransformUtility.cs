using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityTools.ObjectGrouper.Core
{
    public static class GroupTransformUtility
    {
        public enum PivotMode
        {
            Center,
            GeometricCenter,
            Bottom,
            WeightedCenter
        }

        public static Vector3 CalculatePivot(IEnumerable<GameObject> objects, PivotMode mode)
        {
            if (objects == null || !objects.Any()) return Vector3.zero;

            switch (mode)
            {
                case PivotMode.Center:
                    return CalculateAveragePosition(objects);
                case PivotMode.GeometricCenter:
                    return CalculateBoundsCenter(objects);
                case PivotMode.Bottom:
                    Vector3 center = CalculateBoundsCenter(objects);
                    Bounds b = CalculateBounds(objects);
                    return new Vector3(center.x, b.min.y, center.z);
                case PivotMode.WeightedCenter:
                    return CalculateWeightedCenter(objects);
                default:
                    return Vector3.zero;
            }
        }

        private static Vector3 CalculateAveragePosition(IEnumerable<GameObject> objects)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                sum += obj.transform.position;
                count++;
            }
            return count > 0 ? sum / count : Vector3.zero;
        }

        private static Vector3 CalculateBoundsCenter(IEnumerable<GameObject> objects)
        {
            return CalculateBounds(objects).center;
        }

        private static Bounds CalculateBounds(IEnumerable<GameObject> objects)
        {
            Bounds bounds = new Bounds();
            bool initialized = false;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                
                var renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (!initialized)
                    {
                        bounds = r.bounds;
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }

                if (!initialized) // Fallback to transform position if no renderers
                {
                    if (!initialized)
                    {
                        bounds = new Bounds(obj.transform.position, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(obj.transform.position);
                    }
                }
            }
            return bounds;
        }

        private static Vector3 CalculateWeightedCenter(IEnumerable<GameObject> objects)
        {
            // Weighted by mesh volume or simplified by bounds volume
            Vector3 weightedSum = Vector3.zero;
            float totalWeight = 0;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                
                float weight = 1.0f;
                var meshFilter = obj.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    weight = meshFilter.sharedMesh.bounds.size.magnitude;
                }

                weightedSum += obj.transform.position * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? weightedSum / totalWeight : CalculateAveragePosition(objects);
        }

        public static void AlignObjects(IEnumerable<GameObject> objects, Vector3 axis, float value)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                Undo.RecordObject(obj.transform, "Align Objects");
                Vector3 pos = obj.transform.position;
                if (axis.x != 0) pos.x = value;
                if (axis.y != 0) pos.y = value;
                if (axis.z != 0) pos.z = value;
                obj.transform.position = pos;
            }
        }

        public static void SnapToGrid(IEnumerable<GameObject> objects, float gridSize)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                Undo.RecordObject(obj.transform, "Snap to Grid");
                Vector3 pos = obj.transform.position;
                pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
                pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
                pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
                obj.transform.position = pos;
            }
        }
    }
}
