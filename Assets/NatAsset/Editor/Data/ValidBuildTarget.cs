namespace NATFrameWork.NatAsset.Editor
{
    [System.Serializable]
    public enum ValidBuildTarget
    {
        //NoTarget = -2,        --doesn't make sense
        //iPhone = -1,          --deprecated
        //BB10 = -1,            --deprecated
        //MetroPlayer = -1,     --deprecated

        //StandaloneOSXUniversal = 2,
        //StandaloneOSXIntel = 4,
        //StandaloneWindows = 5,
        //WebPlayer = 6,
        //WebPlayerStreamed = 7,
        iOS = 9,
        //PS3 = 10,
        //XBOX360 = 11,
        Android = 13,
        //StandaloneLinux = 17,
        StandaloneWindows64 = 19,
        //WebGL = 20,
        //WSAPlayer = 21,
        //StandaloneLinux64 = 24,
        //StandaloneLinuxUniversal = 25,
        //WP8Player = 26,
        //StandaloneOSXIntel64 = 27,
        //BlackBerry = 28,
        //Tizen = 29,
        //PSP2 = 30,
        //PS4 = 31,
        //PSM = 32,
        //XboxOne = 33,
        //SamsungTV = 34,
        //N3DS = 35,
        //WiiU = 36,
        //tvOS = 37,
        //Switch = 38
    }
    
    [System.Serializable]
    public enum CompressOptions
    {
        Uncompressed = 0,
        StandardCompression = 1,
        ChunkBasedCompression = 2,
    }

    // [System.Serializable]
    // public enum Collector
    // {
    //     NAssetToOneBundle = 0,
    //     NAssetToNBundle = 1,
    //     NFolderToNBundle = 2,
    //     N2FolderToNBundle = 3,
    //     CustomRuleToBundle = 4,
    //     NoCollector = 5,
    // }
}