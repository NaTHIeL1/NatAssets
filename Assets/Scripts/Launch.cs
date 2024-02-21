using NATFrameWork.NatAsset.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launch : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StandaloneModeLoader standaloneModeLoader = new StandaloneModeLoader();
        NatAssetUtil.Init(standaloneModeLoader, Callback);
    }

    private void Callback()
    {
        //启动完成时开始回调
    }

    // Update is called once per frame
    void Update()
    {
        NatAssetUtil.Update();
    }

    private void OnDestroy()
    {
        NatAssetUtil.Release();
    }
}
