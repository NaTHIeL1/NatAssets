using UnityEditor;

namespace NATFrameWork.NatAsset.Editor
{
    public interface ISubView
    {
        void EditorWindow(EditorWindow editorWindow);
        void OnEnable();
        void OnGUI();
    }
}