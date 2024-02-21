using System;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class TaskRandom
    {
        private static uint index = UInt32.MinValue;

        //todo:用于确保产生不重复数，由于uint范围很大
        internal static uint GetTaskRandom()
        {
            return ++index;
        }
    }
}