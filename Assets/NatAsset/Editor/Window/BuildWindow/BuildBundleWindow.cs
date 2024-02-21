using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class BuildBundleWindow : EditorWindow
    {
        private string[] toolBars = {"BundleSetting", "PreBundlesView", "Build", "Version"};
        private int selectedTab;
        private Vector2 scrollPos;

        [MenuItem("NatAsset/BuildBundleWindow", false, 1)]
        private static void ShowWindow()
        {
            var window = GetWindow<BuildBundleWindow>();
            window.titleContent = new GUIContent("NatAssetBuild");
            window.Show();
            window.minSize = new Vector2(1000, 600);
        }

        private Dictionary<Type, ISubView> SubViewDic = new Dictionary<Type, ISubView>()
        {
            {typeof(VirtualFileView), new VirtualFileView()},
            {typeof(BuildView), new BuildView()},
            {typeof(PreBundleView), new PreBundleView()},
            //{typeof(BundleView), new BundleView()},
            {typeof(VersionView), new VersionView()}
        };

        private void OnEnable()
        {
            foreach (KeyValuePair<Type, ISubView> keyValuePair in SubViewDic)
            {
                keyValuePair.Value.EditorWindow(this);
                keyValuePair.Value.OnEnable();
            }
        }

        private void Awake()
        {
            //ViewAwake();
        }

        private void OnGUI()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, toolBars);

            switch (selectedTab)
            {
                case 0:
                    RunGUI<VirtualFileView>();
                    break;
                case 1:
                    RunGUI<PreBundleView>();
                    break;
                case 2:
                    RunGUI<BuildView>();
                    break;
                case 3:
                    RunGUI<VersionView>();
                    break;
            }
        }

        private void RunGUI<T>() where T : ISubView
        {
            if (SubViewDic.TryGetValue(typeof(T), out ISubView subView))
            {
                subView.OnGUI();
            }
        }

        private void OnDestroy()
        {
            
        }
    }
}