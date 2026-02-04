using UnityEditor;
using UnityEngine;
using UnityTools.ObjectGrouper.Core;
using UnityTools.ObjectGrouper.Data;
using System.Collections.Generic;

namespace UnityTools.ObjectGrouper.UI
{
    [InitializeOnLoad]
    public class GroupHierarchyDrawer
    {
        static GroupHierarchyDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            // Basic performance check: convert instanceID to object
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            // Check if object is in any group
            List<ObjectGroup> groups = ObjectGroupManager.instance.GetGroupsForObject(obj);
            if (groups != null && groups.Count > 0)
            {
                // Calculate the width of the GameObject's name
                GUIContent nameContent = new GUIContent(obj.name);
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                float nameWidth = labelStyle.CalcSize(nameContent).x;
                
                // Calculate position: base position + name width + small padding
                // selectionRect.x accounts for hierarchy indentation
                float startX = selectionRect.x + nameWidth + 30;
                
                // Draw a colored dot for each group
                Rect iconRect = new Rect(startX, selectionRect.y, 12, selectionRect.height);
                
                foreach (var group in groups)
                {
                    Color oldColor = GUI.color;
                    GUI.color = group.GroupColor;
                    string icon = group.IsLocked ? "🔒" : "●";
                    GUI.Label(iconRect, icon, EditorStyles.miniLabel); 
                    GUI.color = oldColor;
                    
                    // Move to the right for next group indicator
                    iconRect.x += 12;
                }
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        private static void DrawGroupGizmos(GameObject obj, GizmoType gizmoType)
        {
            if (!ObjectGroupManager.instance.IsInitialized) return; // Static singleton check if needed

            List<ObjectGroup> groups = ObjectGroupManager.instance.GetGroupsForObject(obj);
            if (groups != null && groups.Count > 0)
            {
                foreach (var group in groups)
                {
                    if (!group.IsVisible) continue;
                    
                    Gizmos.color = group.GroupColor;
                    
                    // Draw a small sphere at the object's position with group color
                    Gizmos.DrawWireSphere(obj.transform.position, 0.2f);
                }
            }
        }
    }
}
