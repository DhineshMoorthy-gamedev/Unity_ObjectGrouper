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
                // Draw a colored dot for each group
                Rect iconRect = new Rect(selectionRect.xMax - 10, selectionRect.y, 10, selectionRect.height);
                
                foreach (var group in groups)
                {
                    // Shift left for each group
                    iconRect.x -= 12;
                    
                    Color oldColor = GUI.color;
                    GUI.color = group.GroupColor;
                    string icon = group.IsLocked ? "🔒" : "●";
                    GUI.Label(iconRect, icon, EditorStyles.miniLabel); 
                    GUI.color = oldColor;
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
