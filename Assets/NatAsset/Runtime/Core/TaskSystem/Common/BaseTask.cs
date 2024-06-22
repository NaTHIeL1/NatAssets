using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public abstract class BaseTask : IComparable<BaseTask>, IReference
    {
        private int _refCount;
        private TaskRunner _parentRunner = null;
        //默认异步加载
        private RunModel _runModel = RunModel.Async;
        private TaskState _taskState = TaskState.Idel;
        private Priority _priority = Priority.Low;
        
        protected TaskType _taskType = TaskType.Asset;
        protected TaskResult _taskResult = TaskResult.Nono;
        protected TaskRunner TaskRunner => _parentRunner;
        protected ITaskParam _taskParam = null;

        public string TaskGUID { get; protected set; }
        public string TaskName { get; protected set; }
        public string OtherTaskName { get; protected set; }
        public int TaskID { get; protected set; }
        public virtual float Progress { get; protected set; }
        public long CreateTime { get; protected set; }
        public object Result { get; protected set; }

        public int RefCount => _refCount;
        public Priority TaskPriority => _priority;
        public RunModel RunModel => _runModel;
        public TaskType TaskType => _taskType;
        
        internal TaskState TaskState
        {
            get => _taskState;
            set => _taskState = value;
        }

        //任务状态
        public bool IsDone => _taskState == TaskState.End;
        public bool IsSuccess => _taskResult == TaskResult.Success;

        //internal static T CreateTask<T>(string name, Priority taskPriority) where T : BaseTask, new()
        //{
        //    T task = ReferencePool.Get<T>();
        //    task.TaskGUID = name;
        //    task.TaskID = task.GetHashCode();
        //    if (string.IsNullOrEmpty(name))
        //        task.TaskGUID = task.TaskID.ToString();
        //    task.Progress = 0;
        //    task.CreateTime = DateTime.Now.ToUniversalTime().Ticks;
        //    task._priority = taskPriority;
        //    task._taskState = TaskState.Waiting;
        //    task.OnCreate();
        //    return task;
        //}

        internal static T CreateTask<T>(ITaskParam taskParam, Priority taskPriority) where T : BaseTask, new()
        {
            T task = ReferencePool.Get<T>();
            task.TaskGUID = taskParam.TaskGUID;
            task.TaskName = taskParam.TaskName;
            task.TaskID = task.GetHashCode();
            task.Progress = 0;
            task.CreateTime = DateTime.Now.ToUniversalTime().Ticks;
            task._priority = taskPriority;
            task._taskState = TaskState.Waiting;
            task._taskParam = taskParam;
            task.OnCreate();
            return task;
        }

        internal void SetOtherTaskName(string otherTaskName)
        {
            OtherTaskName = otherTaskName;
        }

        internal static void ReleaseTask(BaseTask baseTask)
        {
            ReferencePool.Release(baseTask);
        }
        
        internal void CancelTask()
        {
            OnCancelTask();
        }

        protected void SetTaskState(TaskState taskState)
        {
            _taskState = taskState;
        }

        internal void SetTaskRunner(TaskRunner taskRunner)
        {
            _parentRunner = taskRunner;
        }

        internal void SetPriority(Priority priority)
        {
            if (this._priority < priority)
            {
                this._priority = priority;
                TaskRunner.ChangeTaskPriority(this, priority);
                OnChangePridrity();
            }
        }

        internal virtual void SetRunMode(RunModel runModel)
        {
            if (CanChangeRunModel())
            {
                if (_runModel == RunModel.Sync) return;
                if (runModel == RunModel.Sync)
                {
                    _runModel = runModel;
                    try
                    {
                        OnSwitchToSync();
                    }
                    catch (Exception e)
                    {
                        _taskState = TaskState.End;
                        Debug.LogError($"任务:{TaskGUID},类型:{GetType().Name},在异步转同步时出现异常，Error:{e}");
                        throw;
                    }
                    finally
                    {
                        _taskState = TaskState.End;
                    }
                }
            }
        }

        /// <summary>
        /// 是否允许切换加载状态，false时只能异步加载
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanChangeRunModel()
        {
            return true;
        }

        /// <summary>
        /// 任务创建
        /// </summary>
        protected abstract void OnCreate();

        /// <summary>
        /// 任务轮询
        /// </summary>
        internal abstract void TaskUpdate();

        /// <summary>
        /// 执行任务取消
        /// </summary>
        protected abstract void OnCancelTask();

        /// <summary>
        /// 异步同步切换
        /// </summary>
        protected abstract void OnSwitchToSync();

        /// <summary>
        /// 切换加载优先级
        /// </summary>
        protected virtual void OnChangePridrity(){}

        /// <summary>
        /// 任务回收
        /// </summary>
        protected abstract void OnClear();

        /// <summary>
        /// 用于任务上锁
        /// </summary>
        internal void AddRefCount()
        {
            this._refCount++;
        }

        /// <summary>
        /// 用于任务解锁，即释放
        /// </summary>
        internal void DesRefCount()
        {
            this._refCount--;
        }

        internal bool CheckCanDestroy()
        {
            if (this._refCount <= 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 计算加载排序
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(BaseTask other)
        {
            if (TaskPriority > other.TaskPriority)
                return 1;
            if (CreateTime > other.CreateTime)
                return 1;
            return -1;
        }

        public void Clear()
        {
            OnClear();
            Result = null;
            TaskGUID = string.Empty;
            TaskID = 0;
            CreateTime = 0;
            Progress = 0;
            _parentRunner = null;
            _refCount = 0;
            _taskState = TaskState.Idel;
            _runModel = RunModel.Async;
            _taskType = TaskType.Asset;
            _priority = Priority.Low;
            _taskResult = TaskResult.Nono;
            _taskParam = null;
        }
    }
}