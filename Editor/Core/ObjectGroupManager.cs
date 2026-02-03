using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityTools.ObjectGrouper.Data;
using UnityEditor.SceneManagement;

namespace UnityTools.ObjectGrouper.Core
{
    [FilePath("ProjectSettings/ObjectGrouperData.json", FilePathAttribute.Location.ProjectFolder)]
    public class ObjectGroupManager : ScriptableSingleton<ObjectGroupManager>
    {
        public bool IsInitialized => _currentData != null;
        private GrouperData _currentData = new GrouperData();
        
        // Cache mapping GameObject InstanceID -> List of Groups it belongs to
        // Helper for Hierarchy drawing and quick lookups
        private Dictionary<int, List<ObjectGroup>> _objectGroupCache = new Dictionary<int, List<ObjectGroup>>();

        private void OnEnable()
        {
            LoadData();
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            
            // Rebuild cache initially
            RebuildCache();
        }
        
        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            SaveData();
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            // Might need scene-specific data separation later, for now we share one global file 
            // per project but tracking GlobalObjectIds ensures we only see relevant objects.
            RebuildCache();
        }

        private void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            RebuildCache();
        }

        public List<ObjectGroup> GetGroups()
        {
            return _currentData.Groups;
        }

        public ObjectGroup CreateGroup(string name, GameObject[] initialObjects = null)
        {
            var newGroup = new ObjectGroup(name);
            _currentData.AddGroup(newGroup);
            
            if (initialObjects != null && initialObjects.Length > 0)
            {
                AddObjectsToGroup(newGroup, initialObjects);
            }

            SaveData();
            return newGroup;
        }

        public void DeleteGroup(ObjectGroup group)
        {
            _currentData.RemoveGroup(group);
            RebuildCache();
            SaveData();
        }

        public void AddObjectsToGroup(ObjectGroup group, GameObject[] objects)
        {
            bool changed = false;
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                
                string globalId = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
                if (!group.ObjectGlobalIDs.Contains(globalId))
                {
                    group.ObjectGlobalIDs.Add(globalId);
                    changed = true;
                }
            }

            if (changed)
            {
                RebuildCache();
                SaveData();
            }
        }

        public void RemoveObjectsFromGroup(ObjectGroup group, GameObject[] objects)
        {
             bool changed = false;
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                
                string globalId = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
                if (group.ObjectGlobalIDs.Contains(globalId))
                {
                    group.ObjectGlobalIDs.Remove(globalId);
                    changed = true;
                }
            }

            if (changed)
            {
                RebuildCache();
                SaveData();
            }
        }

        public void SetGroupVisibility(ObjectGroup group, bool visible)
        {
            group.IsVisible = visible;
            
            // Apply to objects directly in this group
            ApplyVisibilityToObjects(group, visible);

            // Apply recursively to children
            var children = GetChildren(group);
            foreach (var child in children)
            {
                SetGroupVisibility(child, visible);
            }
            
            SaveData();
        }

        private void ApplyVisibilityToObjects(ObjectGroup group, bool visible)
        {
            List<GameObject> objects = GetObjectsInGroup(group);
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                Undo.RecordObject(obj, visible ? "Show Group Objects" : "Hide Group Objects");
                obj.SetActive(visible);
            }
        }
        
        public void SetGroupLock(ObjectGroup group, bool locked)
        {
            group.IsLocked = locked;
            
            // Apply to objects directly in this group
            ApplyLockToObjects(group, locked);

            // Apply recursively to children
            var children = GetChildren(group);
            foreach (var child in children)
            {
                SetGroupLock(child, locked);
            }
            
            SaveData();
        }

        private void ApplyLockToObjects(ObjectGroup group, bool locked)
        {
            List<GameObject> objects = GetObjectsInGroup(group);
            foreach (var obj in objects)
            {
                if (obj == null) continue;
#if UNITY_2019_3_OR_NEWER
                if (locked)
                    SceneVisibilityManager.instance.DisablePicking(obj, false);
                else
                    SceneVisibilityManager.instance.EnablePicking(obj, false);
#endif
            }
        }

        public List<GameObject> GetObjectsInGroup(ObjectGroup group)
        {
            List<GameObject> result = new List<GameObject>();
            List<string> staleIds = new List<string>();

            foreach (var idStr in group.ObjectGlobalIDs)
            {
                if (GlobalObjectId.TryParse(idStr, out GlobalObjectId guid))
                {
                   var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(guid) as GameObject;
                   if (obj != null)
                   {
                       result.Add(obj);
                   }
                   // If obj is null, it might be unloaded or deleted. We don't remove it immediately 
                   // because the scene might just be unloaded.
                }
            }
            return result;
        }

        public List<ObjectGroup> GetGroupsForObject(GameObject obj)
        {
            if (obj == null) return null;
            if (_objectGroupCache.TryGetValue(obj.GetInstanceID(), out var groups))
            {
                return groups;
            }
            return null;
        }

        public List<ObjectGroup> GetChildren(ObjectGroup parent)
        {
            return _currentData.Groups.Where(g => g.ParentID == parent.ID).ToList();
        }

        public List<ObjectGroup> GetRootGroups()
        {
            return _currentData.Groups.Where(g => string.IsNullOrEmpty(g.ParentID)).ToList();
        }

        public void SetGroupParent(ObjectGroup child, ObjectGroup parent)
        {
            child.ParentID = parent?.ID;
            SaveData();
        }

        private void RebuildCache()
        {
            _objectGroupCache.Clear();
            foreach (var group in _currentData.Groups)
            {
                var objects = GetObjectsInGroup(group);
                foreach (var obj in objects)
                {
                    int id = obj.GetInstanceID();
                    if (!_objectGroupCache.ContainsKey(id))
                    {
                        _objectGroupCache[id] = new List<ObjectGroup>();
                    }
                    _objectGroupCache[id].Add(group);
                }
            }
        }

        private void LoadData()
        {
            // Simple JSON persistence
            string path = GetFilePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _currentData = JsonUtility.FromJson<GrouperData>(json) ?? new GrouperData();
            }
        }

        private void SaveData()
        {
            string path = GetFilePath();
            string json = JsonUtility.ToJson(_currentData, true);
            File.WriteAllText(path, json);
        }

        private string GetFilePath()
        {
            // We can rely on ScriptableSingleton's FilePath attribute but since we want custom JSON 
            // inside ProjectSettings, let's just construct it manually to be safe and simple 
            // or use standard IO.
            return Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "ObjectGrouperData.json");
        }
    }
}
