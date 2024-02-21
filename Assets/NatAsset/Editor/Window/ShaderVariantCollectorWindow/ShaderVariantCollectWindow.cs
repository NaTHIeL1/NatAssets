using System.IO;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class ShaderVariantCollectWindow : EditorWindow
    {
        private ShaderVariantCollection svc, otherSvc;
        private DefaultAsset targetFile;

        [MenuItem("NatAsset/ShaderVariantCollectionWindow", false, priority = 3)]
        private static void OpenWindow()
        {
            ShaderVariantCollectWindow window = GetWindow<ShaderVariantCollectWindow>(false, "ShaderVariantCollectionWindow");
            window.minSize = new Vector2(800, 300);
            window.Show();
        }

        private void OnGUI()
        {
            svc = (ShaderVariantCollection)EditorGUILayout.ObjectField("Chose SVC：", svc, typeof(ShaderVariantCollection), false);
            EditorGUILayout.LabelField($"SVC ShaderNum：{ShaderVariantCollector.GetCurrentShaderVariantCollectionShaderCount()}");
            EditorGUILayout.LabelField($"SVC ShaderVariantNum：{ShaderVariantCollector.GetCurrentShaderVariantCollectionVariantCount()}");
            if (GUILayout.Button("CollectionShaderVariant"))
            {
                ShaderVariantCollector.CollectVariant(svc);
            }
            
            targetFile = (DefaultAsset)EditorGUILayout.ObjectField("Choose Folder:", targetFile, typeof(DefaultAsset));
            otherSvc = (ShaderVariantCollection) EditorGUILayout.ObjectField("Chose SVC:", otherSvc,
                typeof(ShaderVariantCollection), false);
            if (GUILayout.Button("CollectionShaderVariant By File"))
            {
                if (targetFile == null)
                {
                    Debug.LogWarning($"未选择目标文件夹");
                    return;
                }
                string path = AssetDatabase.GetAssetPath(targetFile);
                ShaderVariantCollector.CollectVariant(otherSvc, path);
            }
        }

        // public void SetSVC(ShaderVariantCollection svc)
        // {
        //     this.svc = svc;
        // }
    }
}