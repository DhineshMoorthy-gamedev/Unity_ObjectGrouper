using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityTools.ObjectGrouper.Core;
using UnityTools.ObjectGrouper.Data;

namespace UnityTools.ObjectGrouper.UI
{
    [System.Serializable]
    public class ObjectGrouperUI
    {
        private int _currentTab = 0;
        private string[] _tabs = { "Group", "Settings" };
        private Vector2 _scrollPosition;
        private string _newGroupName = "New Group";
        
        // Naming State
        private string _groupNamingTemplate = "Group_{type}_{count}";
        private int _nameCounter = 1;
        
        // Tag Grouping State
        private string _tagFilter = "Untagged";

        public void Initialize()
        {
            // Initial setup if needed
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _currentTab = GUILayout.Toolbar(_currentTab, _tabs, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            GUILayout.Label("v1", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Object Grouper: Organise your scene by grouping GameObjects. Use the Group tab to manage clusters and Settings for automation.", MessageType.Info);
            
            if (_currentTab == 0)
            {
                DrawToolbar();
                DrawGroupList();
                DrawDropArea();
            }
            else
            {
                DrawSettings();
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Automation & Naming Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _groupNamingTemplate = EditorGUILayout.TextField("Naming Template", _groupNamingTemplate);
            EditorGUILayout.HelpBox("Variables: {count}, {type}, {tag}, {date}, {scene}, {name}", MessageType.None);
            
            if (GUILayout.Button("Create Group from Selection (Template)"))
            {
                string groupName = NamingTemplateManager.GenerateName(_groupNamingTemplate, ObjectGroupManager.instance.GetGroups().Count + 1, Selection.activeGameObject);
                ObjectGroupManager.instance.CreateGroup(groupName, Selection.gameObjects);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Tag-Based Grouping", EditorStyles.miniBoldLabel);

            // Tag Grouping
            _tagFilter = EditorGUILayout.TagField("Filter by Tag", _tagFilter);
            if (GUILayout.Button("Group All in Scene by Tag"))
            {
                var allObjects = GameObject.FindObjectsOfType<GameObject>();
                var filtered = GroupingEngine.FilterByTag(allObjects, _tagFilter);
                if (filtered.Count > 0)
                {
                    ObjectGroupManager.instance.CreateGroup($"Tag_{_tagFilter}", filtered.ToArray());
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Batch Operations", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Cleanup Empty Groups"))
            {
                var groups = ObjectGroupManager.instance.GetGroups().ToList();
                int removed = 0;
                foreach (var group in groups)
                {
                    if (group.ObjectGlobalIDs.Count == 0)
                    {
                        ObjectGroupManager.instance.DeleteGroup(group);
                        removed++;
                    }
                }
                Debug.Log($"Removed {removed} empty groups.");
            }

            if (GUILayout.Button("Batch Rename Selected GameObjects"))
            {
                int i = 1;
                foreach (var go in Selection.gameObjects)
                {
                    Undo.RecordObject(go, "Batch Rename");
                    go.name = NamingTemplateManager.GenerateName(_groupNamingTemplate, i++, go);
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Statistics", EditorStyles.miniBoldLabel);
            var allGroups = ObjectGroupManager.instance.GetGroups();
            int totalObjects = allGroups.Sum(g => g.ObjectGlobalIDs.Count);
            EditorGUILayout.LabelField("Total Groups", allGroups.Count.ToString());
            EditorGUILayout.LabelField("Total Objects in Groups", totalObjects.ToString());

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("Create:", EditorStyles.miniLabel, GUILayout.Width(45));
            _newGroupName = EditorGUILayout.TextField(_newGroupName, EditorStyles.toolbarTextField, GUILayout.Width(150));
            
            if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                ObjectGroupManager.instance.CreateGroup(_newGroupName, Selection.gameObjects);
                _newGroupName = "New Group"; // reset
            }

            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Create from Selection", EditorStyles.toolbarButton))
            {
                ObjectGroupManager.instance.CreateGroup($"Group_{Selection.activeGameObject?.name ?? "Selection"}", Selection.gameObjects);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGroupList()
        {
            var groups = ObjectGroupManager.instance.GetGroups();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (groups.Count == 0)
            {
                EditorGUILayout.HelpBox("No groups created. Create one or drag objects here.", MessageType.Info);
            }
            else
            {
                foreach (var group in groups)
                {
                    DrawGroup(group);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawGroup(ObjectGroup group)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // Visibility Toggle
            bool newVis = GUILayout.Toggle(group.IsVisible, "👁", EditorStyles.miniButton, GUILayout.Width(25));
            if (newVis != group.IsVisible)
            {
                ObjectGroupManager.instance.SetGroupVisibility(group, newVis);
            }

            // Lock Toggle
            bool newLock = GUILayout.Toggle(group.IsLocked, "🔒", EditorStyles.miniButton, GUILayout.Width(25));
            if (newLock != group.IsLocked)
            {
                ObjectGroupManager.instance.SetGroupLock(group, newLock);
            }

            // Color Picker
            EditorGUI.BeginChangeCheck();
            var newColor = EditorGUILayout.ColorField(group.GroupColor, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(ObjectGroupManager.instance, "Change Group Color");
                group.GroupColor = newColor;
                ObjectGroupManager.instance.SaveGroupData();
                EditorApplication.RepaintHierarchyWindow();
            }

            // Name
            EditorGUI.BeginChangeCheck();
            var newName = EditorGUILayout.TextField(group.Name);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(ObjectGroupManager.instance, "Change Group Name");
                group.Name = newName;
                ObjectGroupManager.instance.SaveGroupData();
                EditorApplication.RepaintHierarchyWindow();
            }

            // Select Objects Button
            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                var objs = ObjectGroupManager.instance.GetObjectsInGroup(group);
                if (objs.Count > 0)
                    Selection.objects = objs.ToArray();
            }

            // Add Selected to Group Button
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(25)))
            {
                ObjectGroupManager.instance.AddObjectsToGroup(group, Selection.gameObjects);
            }

            // Menu for more options
            if (GUILayout.Button("⋮", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                ShowGroupContextMenu(group);
            }
            
            EditorGUILayout.EndHorizontal();

            // Drag and drop handling
            Rect lastRect = GUILayoutUtility.GetLastRect();
            HandleDragDrop(lastRect, group);
        }

        private void ShowGroupContextMenu(ObjectGroup group)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Remove Selected Objects"), false, () => {
                ObjectGroupManager.instance.RemoveObjectsFromGroup(group, Selection.gameObjects);
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Delete Group"), false, () => {
                if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{group.Name}'? Objects will remain in scene.", "Yes", "No"))
                {
                    ObjectGroupManager.instance.DeleteGroup(group);
                }
            });
            
            menu.ShowAsContext();
        }

        private void DrawDropArea()
        {
            EditorGUILayout.Space();
            Rect dropRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag Objects Here to Create New Group", EditorStyles.helpBox);
            
            HandleDragDrop(dropRect, null); // Null group means create new
        }

        private void HandleDragDrop(Rect rect, ObjectGroup targetGroup)
        {
            Event evt = Event.current;
            if (rect.Contains(evt.mousePosition))
            {
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            var gos = new List<GameObject>();
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                if (obj is GameObject go)
                                    gos.Add(go);
                            }

                            if (gos.Count > 0)
                            {
                                if (targetGroup != null)
                                {
                                    ObjectGroupManager.instance.AddObjectsToGroup(targetGroup, gos.ToArray());
                                }
                                else
                                {
                                    ObjectGroupManager.instance.CreateGroup($"Group_{gos[0].name}", gos.ToArray());
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
