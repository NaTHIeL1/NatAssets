using NATFrameWork.NatAsset.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDemo : MonoBehaviour
{
    [SerializeField]private Button buttonCreate;
    [SerializeField]private Button buttonDelete;

    [SerializeField] private List<TestPackage> assetHandles = new List<TestPackage>();
    // Start is called before the first frame update
    void Start()
    {
        buttonCreate.onClick.AddListener(() =>
        {
            AssetHandle assetHandle = NatAssetMgr.LoadAssetAsync("Assets/Res/Prefabs/Cube.prefab", typeof(GameObject));
            assetHandle.OnLoaded += (handle) => {
                if (handle.IsSuccess)
                {
                    GameObject obj = (GameObject)handle.Asset;
                    GameObject insObj = Instantiate(obj);
                    handle.OnBind(insObj);
                    assetHandles.Add(new TestPackage()
                    {
                        handle = assetHandle,
                        obj = insObj
                    });
                }
                else
                {
                    handle.Unload();
                }
            };
        });
        buttonDelete.onClick.AddListener(() =>
        {
            if(assetHandles.Count > 0)
            {
                TestPackage testStruct = assetHandles[0];
                Destroy(testStruct.obj);
                testStruct.handle.Unload();
                assetHandles.RemoveAt(0);
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public class TestPackage
    {
        public AssetHandle handle;
        public GameObject obj;
    }
}
