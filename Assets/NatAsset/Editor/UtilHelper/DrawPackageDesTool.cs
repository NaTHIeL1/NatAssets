using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    internal static class DrawPackageDesTool
    {
        private static Color dirColor = Color.gray;
        private static Color assetColor = Color.black;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            EditorApplication.projectWindowItemOnGUI += OnGUI;
        }

        private static void OnGUI(string guid, Rect selectionRect)
        {
            List<PackageVirtualRoot> virtualRoots = VirtualFileUtil.VirtualFiles;
            if (virtualRoots == null)
            {
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            for (int i = 0; i < virtualRoots.Count; i++)
            {
                if (virtualRoots[i].FileName == path)
                {
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        DrawDesc("FolderPackage", selectionRect, dirColor);
                    }
                    else
                    {
                        DrawDesc("FilePackage", selectionRect, assetColor);
                    }
                    return;
                }
            }
        }

        private static void DrawDesc(string desc, Rect selectionRect, Color descColor)
        {
            if (selectionRect.height > 16)
            {
                //图标视图
                return;
            }

            GUIStyle label = EditorStyles.label;
            GUIContent content = new GUIContent(desc);

            Rect pos = selectionRect;

            float width = label.CalcSize(content).x + 10;
            pos.x = pos.xMax - width;
            pos.width = width;
            pos.yMin++;

            Color color = GUI.color;
            GUI.color = descColor;
            GUI.DrawTexture(pos, EditorGUIUtility.whiteTexture);
            GUI.color = color;
            GUI.Label(pos, desc);
        }
    }
}
