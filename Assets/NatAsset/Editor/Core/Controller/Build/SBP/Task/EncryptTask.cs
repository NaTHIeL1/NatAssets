#if NATASSET_SBP_SUPPORT
using NATFrameWork.NatAsset.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace NATFrameWork.NatAsset.Editor
{
    public class EncryptTask : IBuildTask
    {
        [InjectContext(ContextUsage.In)] private INatBundleBuildConfigParam configParam;
        [InjectContext(ContextUsage.In)] private IBundleBuildParameters buildParam;

        public ReturnCode Run()
        {
            BundleBuildParameters bundleParams = (BundleBuildParameters) buildParam;
            if (bundleParams.Target == BuildTarget.WebGL)
                return ReturnCode.SuccessNotRun;

            Dictionary<string, BundleBuildInfo> bundleInventoryBuilds = configParam.NatBundleInventoryBuilds;
            try
            {
                foreach (KeyValuePair<string,BundleBuildInfo> inventoryBuild in bundleInventoryBuilds)
                {
                    BundleBuildInfo build = inventoryBuild.Value;
                    switch (NatAssetEditorUtil.SwitchBundleEncrypt(build.BundleEncrypt))
                    {
                        case BundleEncrypt.Nono: break;
                        case BundleEncrypt.Offset:
                            EncryptOffset(build.BundlePath, buildParam.Target);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                return ReturnCode.Error;
            }

            return ReturnCode.Success;
        }

        /// <summary>
        /// 启用地址偏移加密
        /// </summary>
        /// <param name="bundleInventory"></param>
        private void EncryptOffset(string bundlePath, BuildTarget buildTarget)
        {
            string filePath = Path
                .Combine(NatAssetEditorUtil.GetEditorPath, buildTarget.ToString(), configParam.BuildVersion.ToString(), bundlePath)
                .Replace("/", @"\");
            EncryptionUtil.EncryptByOffSet(filePath);
        }

        public int Version { get; }
    }
}
#endif