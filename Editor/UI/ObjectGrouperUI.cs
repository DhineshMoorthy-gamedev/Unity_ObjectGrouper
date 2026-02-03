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
        private string[] _tabs = { "Manual", "Smart", "Transform", "Settings" };
        private Vector2 _scrollPosition;
        private string _newGroupName = "New Group";
        
        // Naming State
        private string _groupNamingTemplate = "Group_{type}_{count}";
        private int _nameCounter = 1;
        
        // Transform State
        private GroupTransformUtility.PivotMode _selectedPivotMode = GroupTransformUtility.PivotMode.Center;
        private float _snapGridSize = 1.0f;
        
        // Smart Grouping State
        private float _proximityThreshold = 1.0f;
        private float _gridSize = 5.0f;
        private string _tagFilter = "Untagged";
        private int _layerFilter = 0;

        public void Initialize()
        {
            // Initial setup if needed
        }

        public void Draw()
        {
            _currentTab = GUILayout.Toolbar(_currentTab, _tabs);
            
            if (_currentTab == 0)
            {
                DrawToolbar();
                DrawGroupList();
                DrawDropArea();
            }
            else if (_currentTab == 1)
            {
                DrawSmartGrouping();
            }
            else if (_currentTab == 2)
            {
                DrawTransformTools();
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
            GUILayout.Label("Batch Operations", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Cleanup Empty Groups"))
            {
                var groups = ObjectGroupManager.instance.GetGroups().ToList();
                int removed = 0;
                foreach (var group in groups)
                {
                    if (group.ObjectGlobalIDs.Count == 0 && ObjectGroupManager.instance.GetChildren(group).Count == 0)
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

        private void DrawTransformTools()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Transform & Pivot Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _selectedPivotMode = (GroupTransformUtility.PivotMode)EditorGUILayout.EnumPopup("Pivot Mode", _selectedPivotMode);
            
            if (GUILayout.Button("Log Calculated Pivot (Selection)"))
            {
                Vector3 pivot = GroupTransformUtility.CalculatePivot(Selection.gameObjects, _selectedPivotMode);
                Debug.Log($"Calculated {_selectedPivotMode} Pivot: {pivot}");
            }

            EditorGUILayout.Space();
            GUILayout.Label("Snapping", EditorStyles.miniBoldLabel);
            _snapGridSize = EditorGUILayout.FloatField("Snap Grid Size", _snapGridSize);
            if (GUILayout.Button("Snap Selection to Grid"))
            {
                GroupTransformUtility.SnapToGrid(Selection.gameObjects, _snapGridSize);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Alignment", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Align X (to Active Object)"))
            {
                if (Selection.activeGameObject != null)
                    GroupTransformUtility.AlignObjects(Selection.gameObjects, Vector3.right, Selection.activeGameObject.transform.position.x);
            }
            if (GUILayout.Button("Align Y (to Active Object)"))
            {
                if (Selection.activeGameObject != null)
                    GroupTransformUtility.AlignObjects(Selection.gameObjects, Vector3.up, Selection.activeGameObject.transform.position.y);
            }
            if (GUILayout.Button("Align Z (to Active Object)"))
            {
                if (Selection.activeGameObject != null)
                    GroupTransformUtility.AlignObjects(Selection.gameObjects, Vector3.forward, Selection.activeGameObject.transform.position.z);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSmartGrouping()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Smart Grouping Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();

            // Material Grouping
            if (GUILayout.Button("Group Selection by Material"))
            {
                var groups = GroupingEngine.GroupByMaterial(Selection.gameObjects);
                foreach (var kvp in groups)
                {
                    ObjectGroupManager.instance.CreateGroup($"Mat_{kvp.Key.name}", kvp.Value.ToArray());
                }
            }

            EditorGUILayout.Space();

            // Proximity Grouping
            _proximityThreshold = EditorGUILayout.FloatField("Proximity Threshold", _proximityThreshold);
            if (GUILayout.Button("Group Selection by Proximity"))
            {
                var clusters = GroupingEngine.GroupByProximity(Selection.gameObjects, _proximityThreshold);
                int i = 1;
                foreach (var cluster in clusters)
                {
                    ObjectGroupManager.instance.CreateGroup($"Cluster_{i++}", cluster.ToArray());
                }
            }

            EditorGUILayout.Space();

            // Grid Grouping
            _gridSize = EditorGUILayout.FloatField("Grid Size", _gridSize);
            if (GUILayout.Button("Group Selection by Grid"))
            {
                var cells = GroupingEngine.GroupByGrid(Selection.gameObjects, _gridSize);
                foreach (var kvp in cells)
                {
                    ObjectGroupManager.instance.CreateGroup($"Grid_{kvp.Key.x}_{kvp.Key.y}_{kvp.Key.z}", kvp.Value.ToArray());
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Automatic Scene Analysis", EditorStyles.miniBoldLabel);

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
            var rootGroups = ObjectGroupManager.instance.GetRootGroups();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (rootGroups.Count == 0 && ObjectGroupManager.instance.GetGroups().Count == 0)
            {
                EditorGUILayout.HelpBox("No groups created. Create one or drag objects here.", MessageType.Info);
            }
            else
            {
                foreach (var group in rootGroups)
                {
                    DrawGroupHierarchy(group, 0);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupHierarchy(ObjectGroup group, int indent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUI.indentLevel = indent;
            
            Rect headerRect = EditorGUILayout.BeginHorizontal();
            
            // Expansion Toggle
            group.IsExpanded = EditorGUILayout.Foldout(group.IsExpanded, "", true);
            
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
            group.GroupColor = EditorGUILayout.ColorField(group.GroupColor, GUILayout.Width(40));

            // Name
            group.Name = EditorGUILayout.TextField(group.Name);

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
            HandleDragDrop(headerRect, group);

            if (group.IsExpanded)
            {
                var children = ObjectGroupManager.instance.GetChildren(group);
                foreach (var child in children)
                {
                    DrawGroupHierarchy(child, indent + 1);
                }
            }

            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();
        }

        private void ShowGroupContextMenu(ObjectGroup group)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Add New Subgroup"), false, () => {
                var newSub = ObjectGroupManager.instance.CreateGroup("New Subgroup");
                ObjectGroupManager.instance.SetGroupParent(newSub, group);
            });
            
            menu.AddSeparator("");
            
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
            GUILayout.FlexibleSpace();
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
