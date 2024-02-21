using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NATFrameWork.NatAsset.Editor
{
    public static class ShaderVariantCollector
    {
        private const float WaitMilliseconds = 1000f;
        private static bool isCollecting = false;
        private static Stopwatch sw = new Stopwatch();
        private static ShaderVariantCollection svc;

        private static void EditorUpdate()
        {
            if (isCollecting && sw.ElapsedMilliseconds > WaitMilliseconds)
            {
                isCollecting = false;
                sw.Stop();
                EditorApplication.update -= EditorUpdate;
                string savePath = AssetDatabase.GetAssetPath(svc);
                AssetDatabase.DeleteAsset(savePath);
                svc = null;

                SaveCurrentShaderVariantCollection(savePath);

                ShaderVariantCollectWindow window = EditorWindow.GetWindow<ShaderVariantCollectWindow>();
                // if (window != null)
                //     window.SetSVC(AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(savePath));

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Shader变体收集完毕");
            }
        }

        public static void CollectVariant(ShaderVariantCollection svc, string targetFile = null)
        {
            if (svc == null || isCollecting)
                return;

            isCollecting = true;
            ShaderVariantCollector.svc = svc;

            System.Type T = Assembly.Load("UnityEditor").GetType("UnityEditor.GameView");
            EditorWindow.GetWindow(T, false, "GameView", true);

            ClearCurrentShaderVariantCollection();
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

            List<Material> materials = null;
            if (targetFile == null)
                materials = GetAllBundledMaterialList();
            else
                materials = GetTargetFileMaterialList(targetFile);
            CollectVariant(materials);

            EditorApplication.update += EditorUpdate;
            sw.Reset();
            sw.Start();
        }

        /// <summary>
        /// 收集材质的Shader的变体
        /// </summary>
        private static void CollectVariant(List<Material> materials)
        {
            Camera camera = Camera.main;

            float aspect = camera.aspect;
            int totalMaterials = materials.Count;
            float height = Mathf.Sqrt(totalMaterials / aspect) + 1;
            float width = Mathf.Sqrt(totalMaterials / aspect) * aspect + 1;
            float halfHeight = Mathf.CeilToInt(height / 2f);
            float halfWidth = Mathf.CeilToInt(width / 2f);
            camera.orthographic = true;
            camera.orthographicSize = halfHeight;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            int xMax = (int) (width - 1);
            int x = 0, y = 0;
            int progressValue = 0;
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                Vector3 position = new Vector3(x - halfWidth + 1f, y - halfHeight + 1f, 0f);
                CreateSphere(material, position, i);
                if (x == xMax)
                {
                    x = 0;
                    y++;
                }
                else
                {
                    x++;
                }

                progressValue++;
                EditorUtility.DisplayProgressBar("TestMaterial", "Collecting...",
                    (float) progressValue / materials.Count);
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 获取所有被构建为资源包的材质的列表
        /// </summary>
        public static List<Material> GetAllBundledMaterialList()
        {
            List<Material> results = new List<Material>();
            List<BundleBuildInfo> bundles = NatAssetBuildUtil.CollectBundles();
            int progress = 0;
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleBuildInfo bundleIns = bundles[i];
                progress++;
                EditorUtility.DisplayProgressBar("TestMaterial", "Collecting...",
                    (float) progress / bundles.Count);

                List<AssetBuildInfo> assetInventories = bundleIns.AssetBuildInfos;
                for (int j = 0; j < assetInventories.Count; j++)
                {
                    AssetBuildInfo assetInfo = assetInventories[j];
                    if (assetInfo.Type != typeof(Material)) continue;
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(assetInfo.EditorPath);
                    if (material == null) continue;
                    Shader shader = material.shader;
                    if (shader == null) continue;
                    results.Add(material);
                }
            }

            EditorUtility.ClearProgressBar();
            return results;
        }

        public static List<Material> GetTargetFileMaterialList(string targetPath)
        {
            List<Material> results = new List<Material>();
            DirectoryInfo directoryInfo =
                new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), targetPath));
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            int progress = 0;
            for (int i = 0; i < fileInfos.Length; i++)
            {
                progress++;
                EditorUtility.DisplayProgressBar("TestMaterial", "Collecting...",
                    (float) progress / fileInfos.Length);
                if (Path.GetExtension(fileInfos[i].FullName) == ".meta")
                {
                    continue;
                }

                FileInfo fileInfo = fileInfos[i];
                if (AssetDatabase.GetMainAssetTypeAtPath(fileInfo.Name) != typeof(Material)) continue;
                Material material = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(targetPath, fileInfo.Name));
                if (material == null) continue;
                Shader shader = material.shader;
                if (shader == null) continue;
                results.Add(material);
            }

            EditorUtility.ClearProgressBar();
            return results;
        }

        /// <summary>
        /// 创建测试球体
        /// </summary>
        private static void CreateSphere(Material material, Vector3 position, int index)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.GetComponent<Renderer>().material = material;
            go.transform.position = position;
            go.name = $"Sphere_{index}|{material.name}";
        }

        /// <summary>
        /// 清空SVC
        /// </summary>
        public static void ClearCurrentShaderVariantCollection()
        {
            NatAssetEditorUtil.InvokeNonPublicStaticMethod(typeof(ShaderUtil), "ClearCurrentShaderVariantCollection");
        }

        /// <summary>
        /// 保存SVC
        /// </summary>
        public static void SaveCurrentShaderVariantCollection(string savePath)
        {
            NatAssetEditorUtil.InvokeNonPublicStaticMethod(typeof(ShaderUtil), "SaveCurrentShaderVariantCollection",
                savePath);
        }

        /// <summary>
        /// 获取SVC中的Shader数量
        /// </summary>
        public static int GetCurrentShaderVariantCollectionShaderCount()
        {
            return (int) NatAssetEditorUtil.InvokeNonPublicStaticMethod(typeof(ShaderUtil),
                "GetCurrentShaderVariantCollectionShaderCount");
        }

        /// <summary>
        /// 获取SVC中的Shader变体数量
        /// </summary>
        public static int GetCurrentShaderVariantCollectionVariantCount()
        {
            return (int) NatAssetEditorUtil.InvokeNonPublicStaticMethod(typeof(ShaderUtil),
                "GetCurrentShaderVariantCollectionVariantCount");
        }
    }
}