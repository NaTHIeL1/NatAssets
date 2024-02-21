using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    public class DependenciesTask
    {
        private List<BaseTask> _dependenciesTask = new List<BaseTask>();

        public void AddTask(BaseTask baseTask)
        {
            _dependenciesTask.Add(baseTask);
        }

        public void RemoveTask(BaseTask baseTask)
        {
            _dependenciesTask.Remove(baseTask);
        }

        public bool ContainsKey(string taskName)
        {
            foreach (BaseTask baseTask in _dependenciesTask)
            {
                if (baseTask.TaskGUID == taskName)
                    return true;
            }

            return false;
        }

        public void SetPriority(Priority priority)
        {
            for (int i = 0; i < _dependenciesTask.Count; i++)
            {
                BaseTask baseTask = _dependenciesTask[i];
                if (!baseTask.IsDone)
                {
                    baseTask.SetPriority(priority);
                }
            }
        }
        public void SetRunModel(RunModel runModel)
        {
            for (int i = 0; i < _dependenciesTask.Count; i++)
            {
                BaseTask baseTask = _dependenciesTask[i];
                if (!baseTask.IsDone)
                {
                    baseTask.SetRunMode(runModel);
                }
            }
        }

        public bool IsDone()
        {
            for (int i = 0; i < _dependenciesTask.Count; i++)
            {
                if (!_dependenciesTask[i].IsDone)
                    return false;
            }
            return true;
        }

        public bool IsSuccess()
        {
            for (int i = 0; i < _dependenciesTask.Count; i++)
            {
                if (!_dependenciesTask[i].IsSuccess)
                    return false;
            }
            return true;
        }

        // /// <summary>
        // /// 增加引用计数
        // /// </summary>
        // public void Reference()
        // {
        //     for (int i = 0; i < _dependenciesTask.Count; i++)
        //     {
        //         _dependenciesTask[i].AddRefCount();
        //     }
        // }

        /// <summary>
        /// 释放引用计数
        /// </summary>
        public void Release()
        {
            for (int i = 0; i < _dependenciesTask.Count; i++)
            {
                _dependenciesTask[i].DesRefCount();
            }
            _dependenciesTask.Clear();
        }
    }
}
