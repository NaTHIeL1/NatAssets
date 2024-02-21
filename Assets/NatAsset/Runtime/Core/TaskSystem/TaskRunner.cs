using System;
using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class TaskRunner
    {
        //Level12345为数组索引，反向遍历加载
        private List<TaskGroup> _taskGroups = new List<TaskGroup>();
        private int _farmeLimit = -1;
        public int Count => _taskGroups.Count;

        internal List<TaskGroup> TaskGroups => _taskGroups;

        internal void InitTaskRunner(int farmeLimit)
        {
            _farmeLimit = farmeLimit;
            int count = Enum.GetNames(typeof(Priority)).Length;
            for (int i = 0; i < count; i++)
            {
                TaskGroup taskGroup = new TaskGroup();
                taskGroup.Init(this, (Priority) i, _farmeLimit);
                _taskGroups.Add(taskGroup);
            }
        }

        //执行Update
        internal void Update()
        {
            PreUpdate();
            RunTasks();
        }

        internal void Release()
        {
            _taskGroups.Clear();
        }

        private void PreUpdate()
        {
            int index = 0;
            for (int i = 0; i < _taskGroups.Count; i++)
            {
                _taskGroups[i].PreUpdate(ref index);
            }
        }

        private void RunTasks()
        {
            int index = 0;
            for (int i = _taskGroups.Count - 1; i >= 0 && (_farmeLimit == -1 ? true : index < _farmeLimit); i--)
            {
                _taskGroups[i].Update(ref index);
            }
        }

        internal void AddTask(BaseTask baseTask)
        {
            TaskGroup taskGroup = _taskGroups[(int) baseTask.TaskPriority];
            if (!taskGroup.ContainsKey(baseTask.TaskGUID))
            {
                taskGroup.AddTask(baseTask);
                baseTask.SetTaskRunner(this);
            }
            else
            {
                Debug.LogWarning($"不能添加同名任务:{baseTask.TaskGUID}");
            }
        }

        internal void RemoveTask(BaseTask task)
        {
            TaskGroup taskGroup = _taskGroups[(int) task.TaskPriority];
            if (taskGroup.ContainsKey(task.TaskGUID))
            {
                taskGroup.RemoveTask(task);
                ReferencePool.Release(task);
            }
        }

        internal void ChangeTaskPriority(BaseTask baseTask, Priority targetPriority)
        {
            if (baseTask.TaskPriority < targetPriority)
            {
                RemoveTask(baseTask);
                AddTask(baseTask);
            }
        }

        internal bool ContainsKey(string taskName)
        {
            for (int i = 0; i < _taskGroups.Count; i++)
            {
                if (_taskGroups[i].ContainsKey(taskName))
                    return true;
            }

            return false;
        }

        internal bool TryGetValue(string taskName, out BaseTask baseTask)
        {
            for (int i = 0; i < _taskGroups.Count; i++)
            {
                if (_taskGroups[i].TryGetValue(taskName, out baseTask))
                    return true;
            }

            baseTask = null;
            return false;
        }

        internal void CancelAllTask()
        {
            for (int i = _taskGroups.Count - 1; i >= 0; i--)
            {
                _taskGroups[i].CancelAllTask();
            }
        }

        internal void SwitchRunModel(RunModel runModel)
        {
            for (int i = _taskGroups.Count - 1; i >= 0; i--)
            {
                _taskGroups[i].SwitchRunModel(runModel);
            }
        }
    }
}