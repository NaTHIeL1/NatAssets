using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace NATFrameWork.NatAsset.Runtime
{
    [CreateAssetMenu(fileName = "NatAssetSObj", menuName = "NATFramework/NatAssetSObj", order = 3)]
    public class NatAssetSObj : ScriptableObject
    {
        private static NatAssetSObj instance = null;
        public static NatAssetSObj Instance
        {
            get
            {
                if (instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(NatAssetSObj)}");
                    if (guids.Length == 0)
                    {
                        AssetDatabase.CreateAsset(new NatAssetSObj(), "Assets/NatAssetSObj.asset");
                        guids = AssetDatabase.FindAssets($"t:{nameof(NatAssetSObj)}");
                    }

                    string path;
                    if (guids.Length > 1)
                    {
                        Debug.LogWarning($"创建了多个{nameof(NatAssetSObj)}");
                        foreach (string guid in guids)
                        {
                            path = AssetDatabase.GUIDToAssetPath(guid);
                            Debug.LogWarning(path);
                        }
                    }

                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    instance = AssetDatabase.LoadAssetAtPath<NatAssetSObj>(path);
                }

                return instance;
            }
        }
        public RunWay RunWay = RunWay.Editor;

        //public string ResPath = "Res";
    }
}
#endif
