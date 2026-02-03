using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityTools.ObjectGrouper.Core
{
    public static class GroupingEngine
    {
        public static List<GameObject> FilterByTag(IEnumerable<GameObject> objects, string tag)
        {
            return objects.Where(o => o.CompareTag(tag)).ToList();
        }

        public static List<GameObject> FilterByLayer(IEnumerable<GameObject> objects, int layer)
        {
            return objects.Where(o => o.layer == layer).ToList();
        }

        public static List<GameObject> FilterByName(IEnumerable<GameObject> objects, string pattern, bool useRegex = false)
        {
            if (useRegex)
            {
                var regex = new System.Text.RegularExpressions.Regex(pattern);
                return objects.Where(o => regex.IsMatch(o.name)).ToList();
            }
            return objects.Where(o => o.name.Contains(pattern)).ToList();
        }

        public static List<GameObject> FilterByComponent<T>(IEnumerable<GameObject> objects) where T : Component
        {
            return objects.Where(o => o.GetComponent<T>() != null).ToList();
        }

        public static Dictionary<Material, List<GameObject>> GroupByMaterial(IEnumerable<GameObject> objects)
        {
            var result = new Dictionary<Material, List<GameObject>>();
            foreach (var obj in objects)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    if (!result.ContainsKey(renderer.sharedMaterial))
                        result[renderer.sharedMaterial] = new List<GameObject>();
                    
                    result[renderer.sharedMaterial].Add(obj);
                }
            }
            return result;
        }

        public static List<List<GameObject>> GroupByProximity(IEnumerable<GameObject> objects, float distanceThreshold)
        {
            var result = new List<List<GameObject>>();
            var remaining = new HashSet<GameObject>(objects);

            while (remaining.Count > 0)
            {
                var currentGroup = new List<GameObject>();
                var start = remaining.First();
                remaining.Remove(start);
                currentGroup.Add(start);

                var queue = new Queue<GameObject>();
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var neighbors = remaining.Where(o => Vector3.Distance(current.transform.position, o.transform.position) <= distanceThreshold).ToList();
                    
                    foreach (var neighbor in neighbors)
                    {
                        remaining.Remove(neighbor);
                        currentGroup.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
                result.Add(currentGroup);
            }
            return result;
        }

        public static Dictionary<Vector3Int, List<GameObject>> GroupByGrid(IEnumerable<GameObject> objects, float cellSize)
        {
            var result = new Dictionary<Vector3Int, List<GameObject>>();
            foreach (var obj in objects)
            {
                Vector3 pos = obj.transform.position;
                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt(pos.x / cellSize),
                    Mathf.FloorToInt(pos.y / cellSize),
                    Mathf.FloorToInt(pos.z / cellSize)
                );

                if (!result.ContainsKey(cell))
                    result[cell] = new List<GameObject>();
                
                result[cell].Add(obj);
            }
            return result;
        }
    }
}
