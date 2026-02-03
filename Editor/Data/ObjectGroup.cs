using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTools.ObjectGrouper.Data
{
    [Serializable]
    public class ObjectGroup
    {
        public string ID;
        public string Name;
        public Color GroupColor = Color.white;
        
        public string ParentID; // Support for nested groups
        public GroupType Type = GroupType.Selection;
        public string Description;
        public List<string> Tags = new List<string>();

        // We store GlobalObjectId strings to be scene-agnostic and persistent
        public List<string> ObjectGlobalIDs = new List<string>();

        public bool IsVisible = true;
        public bool IsLocked = false;
        public bool IsExpanded = true; // UI state

        public ObjectGroup()
        {
            ID = Guid.NewGuid().ToString();
            Name = "New Group";
            GroupColor = GetRandomColor();
        }

        public ObjectGroup(string name) : this()
        {
            Name = name;
        }

        private static Color GetRandomColor()
        {
            return Color.HSVToRGB(UnityEngine.Random.value, 0.6f, 0.9f);
        }
    }

    public enum GroupType
    {
        Selection, // Manual grouping
        Folder,    // Organizational only
        Smart      // Rule-based grouping
    }
}
