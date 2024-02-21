using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Editor
{
    public interface OnExcludeFilePath
    {
        /// <summary>
        /// 在配置表的MapNode节点下排除指定文件夹下文件的全加载路径
        /// </summary>
        /// <returns></returns>
        List<string> GenerateExcludeFilName();
    }
}