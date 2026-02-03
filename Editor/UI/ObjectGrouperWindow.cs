using UnityEditor;
using UnityEngine;

namespace UnityTools.ObjectGrouper.UI
{
    public class ObjectGrouperWindow : EditorWindow
    {
        [SerializeField] private ObjectGrouperUI ui = new ObjectGrouperUI();

        [MenuItem("Tools/GameDevTools/Object Grouper", false, 210)]
        public static void ShowWindow()
        {
            var window = GetWindow<ObjectGrouperWindow>("Object Grouper");
            window.Show();
        }

        [MenuItem("Tools/GameDevTools/Group Selected %g")]
        public static void QuickGroup()
        {
            if (Selection.gameObjects.Length > 0)
            {
                Core.ObjectGroupManager.instance.CreateGroup($"QuickGroup_{Selection.activeGameObject.name}", Selection.gameObjects);
            }
        }

        [MenuItem("Tools/GameDevTools/Select Objects in Group %#g")]
        public static void QuickSelectGroup()
        {
             if (Selection.activeGameObject != null)
             {
                 var groups = Core.ObjectGroupManager.instance.GetGroupsForObject(Selection.activeGameObject);
                 if (groups != null && groups.Count > 0)
                 {
                     var objs = Core.ObjectGroupManager.instance.GetObjectsInGroup(groups[0]);
                     Selection.objects = objs.ToArray();
                 }
             }
        }

        private void OnEnable()
        {
            ui.Initialize();
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }
        
        private void OnSelectionChange() { Repaint(); }
        private void OnInspectorUpdate() { Repaint(); }

        private void OnGUI()
        {
            ui.Draw();
        }
    }
}
