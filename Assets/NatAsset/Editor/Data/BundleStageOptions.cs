namespace NATFrameWork.NatAsset.Editor
{
    [System.Serializable]
    public enum BundleStageOptions
    {
        //特殊类型，没有任何数据时调用
        Nono = 0,

        //不能有文件嵌套
        FileAsBundle = 1,

        //只对资源处理，不对文件处理
        AssetAsBundle = 2,

        Ignore = 3,

        //不纳入打包判定中,只作为文件名出现
        FileName = 4,
    }
}