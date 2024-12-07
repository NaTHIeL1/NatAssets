using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class TaskGroup
    {
        private TaskRunner _taskRunner;
        private ListDic<string, BaseTask> taskDic = new ListDic<string, BaseTask>(1000);
        private bool needSort = false;
        private int _taskLimit = -1;
        internal Priority TaskPriority { get; private set; }

        internal ListDic<string, BaseTask> TaskDic => taskDic;
        internal void Init(TaskRunner taskRunner, Priority taskPriority, int limit = -1)
        {
            _taskRunner = taskRunner;
            TaskPriority = taskPriority;
            _taskLimit = limit;
        }

        internal void Release()
        {
            taskDic.Clear();
            _taskRunner = null;
        }

        internal void PreUpdate(ref int index)
        {
            // //优化是否进行排序的逻辑
            // int selfCount = taskDic.Count;
            // if (_taskLimit != -1)
            // {
            //     if (index >= _taskLimit)
            //     {
            //         return;
            //     }
            //     if (selfCount >= (_taskLimit - index) && needSort)
            //     {
            //         needSort = false;
            //         taskDic.Sort();
            //     }
            // }
        }

        internal void Update(ref int index)
        {
            if (index < _taskLimit)
            {
                for (int i = 0; i < taskDic.Count && index < _taskLimit; i++)
                {
                    BaseTask task = taskDic.GetElementByIndex(i);
                    try
                    {
                        task.TaskUpdate();
                    }
                    catch (Exception e)
                    {
                        task.TaskState = TaskState.End;
                        Debug.LogError($"任务:{task.TaskGUID},类型:{task.GetType().Name},Error:{e}:，出现异常");
                        throw;
                    }
                    finally
                    {
                        if (task.IsDone && task.CheckCanDestroy())
                        {
                            RemoveTask(task);
                            BaseTask.ReleaseTask(task);
                            i--;
                        }
                        else if(!task.IsDone)
                            index++;
                    }
                }
            }
        }

        internal void CancelAllTask()
        {
            for (int i = 0; i < taskDic.Count; i++)
            {
                BaseTask task = taskDic.GetElementByIndex(i);
                task.CancelTask();
                if (task.IsDone)
                {
                    RemoveTask(task);
                    BaseTask.ReleaseTask(task);
                    i--;
                }
            }
        }

        internal void CancelTask(string taskGUID)
        {
            if(taskDic.TryGetValue(taskGUID, out BaseTask task))
            {
                task.CancelTask();
                if(task.IsDone)
                {
                    RemoveTask(task);
                    BaseTask.ReleaseTask(task);
                }
            }
        }

        /// <summary>
        /// 添加加载任务
        /// </summary>
        /// <param name="task"></param>
        internal void AddTask(BaseTask task)
        {
            needSort = true;
            taskDic.Add(task.TaskGUID, task);
        }

        /// <summary>
        /// 移除加载任务
        /// </summary>
        /// <param name="task"></param>
        internal void RemoveTask(BaseTask task)
        {
            taskDic.Remove(task.TaskGUID);
        }

        internal bool ContainsKey(string taskName)
        {
            return taskDic.ContainsKey(taskName);
        }

        internal bool TryGetValue(string taskName, out BaseTask task)
        {
            if (taskDic.TryGetValue(taskName, out task))
            {
                return true;
            }
            return false;
        }

        internal void SwitchRunModel(RunModel runModel)
        {
            for (int i = 0; i < taskDic.Count; i++)
            {
                BaseTask task = taskDic.GetElementByIndex(i);
                task.SetRunMode(runModel);
                if (task.IsDone)
                {
                    RemoveTask(task);
                    BaseTask.ReleaseTask(task);
                    i--;
                }
            }
        }

        internal int GetTaskNum()
        {
            if(taskDic!= null)
                return taskDic.Count;
            return 0;
        }
    }
}