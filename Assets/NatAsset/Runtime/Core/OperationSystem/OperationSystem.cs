using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class OperationSystem
    {
        private static List<IOperation> _operations = new List<IOperation>();
        internal static void Init()
        {

        }

        internal static void Update()
        {
            for(int i = 0;i < _operations.Count;i++)
            {
                IOperation op = _operations[i];
                if (op.IsDone)
                    continue;
                op.OnUpdate();
            }
        }

        internal static void Release()
        {
            _operations.Clear();
        }

        internal static void AddOperation(IOperation operation)
        {
            if(operation != null)
            {
                _operations.Add(operation);
            }
        }

        internal static void RemoveOperation(IOperation operation)
        {
            if(operation != null && _operations.Contains(operation))
            {
                _operations.Remove(operation);
            }
        }
    }
}
