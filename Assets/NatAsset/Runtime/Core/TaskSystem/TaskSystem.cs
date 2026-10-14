using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class TaskSystem
    {
        //todo:增加type作为额外判断条件来选择provide
        private static ListDic<string, BaseProvider> _providerList = new ListDic<string, BaseProvider>(1000);
        private static ListDic<string, BaseProvider> _netProviderList = new ListDic<string, BaseProvider>();
        private static TaskRunner _loadTaskRunner = new TaskRunner();
        private static TaskRunner _unLoadTaskRunner = new TaskRunner();
        private static TaskRunner _netLoadTaskRunner = new TaskRunner();
        internal static TaskRunner LoadTaskRunner => _loadTaskRunner;
        internal static TaskRunner UnLoadTaskRunner => _unLoadTaskRunner;
        internal static TaskRunner NetLoadTaskRunner => _netLoadTaskRunner;

        internal static void Init()
        {
            InitTaskRunner();
        }

        internal static void Update()
        {
            UpdateProvider();
            RuntimeData.ScheduleUnusedLifetimes();
            UpdateTaskRunner();
        }

        internal static void Release()
        {
            ReleaseProvider();
            ReleaseTaskRunner();
        }

        internal static async System.Threading.Tasks.Task DrainProvidersAsync()
        {
            CancelProviderHandles(_providerList);
            CancelProviderHandles(_netProviderList);
            while (_providerList.Count != 0 || _netProviderList.Count != 0)
            {
                UpdateProvider();
                _loadTaskRunner.Update();
                _netLoadTaskRunner.Update();
                SceneSystem.Update();
                if (_providerList.Count != 0 || _netProviderList.Count != 0)
                    await System.Threading.Tasks.Task.Yield();
            }
        }

        private static void CancelProviderHandles(ListDic<string, BaseProvider> providers)
        {
            for (int i = 0; i < providers.Count; i++)
            {
                var handles = providers.GetElementByIndex(i).Handles;
                if (handles == null) continue;
                foreach (IHandle handle in handles.ToArray())
                {
                    if (!handle.IsValid) continue;
                    if (handle is SceneHandle scene) scene.ReleaseLogic();
                    else handle.Dispose();
                }
            }
        }

        //轮询Provider
        private static void UpdateProvider()
        {
            UpdateProvider(_providerList);
            UpdateProvider(_netProviderList);
        }

        private static void UpdateProvider(ListDic<string, BaseProvider> providerList)
        {
            for (int i = 0; i < providerList.Count; i++)
            {
                BaseProvider baseProvider = providerList.GetElementByIndex(i);
                baseProvider.OnUpdate();
                if (baseProvider.IsDone)
                {
                    //已完成的任务启动回收
                    providerList.Remove(baseProvider.ProviderGUID);
                    BaseProvider.Release(baseProvider);
                    i--;
                }
            }
        }

        private static void ReleaseProvider()
        {
            ReleaseProvider(_providerList);
            ReleaseProvider(_netProviderList);
        }

        private static void ReleaseProvider(ListDic<string, BaseProvider> providerList)
        {
            for (int i = 0; i < providerList.Count; i++)
            {
                BaseProvider baseProvider = providerList.GetElementByIndex(i);
                BaseProvider.Release(baseProvider);
            }
            providerList.Clear();
        }

        private static void InitTaskRunner()
        {
            _loadTaskRunner.InitTaskRunner(NatAssetSetting.TaskFrameLimitLoaded);
            _netLoadTaskRunner.InitTaskRunner(NatAssetSetting.TaskFrameLimitNetLoad);
            _unLoadTaskRunner.InitTaskRunner(NatAssetSetting.TaskFrameLimitUnLoad);
        }

        private static void UpdateTaskRunner()
        {
            _loadTaskRunner.Update();
            _netLoadTaskRunner.Update();
            _unLoadTaskRunner.Update();
        }

        private static void ReleaseTaskRunner()
        {
            //卸载时取消所有正在执行的任务,bundle直接卸载
            _loadTaskRunner.CancelAllTask();
            _loadTaskRunner.Release();

            _netLoadTaskRunner.CancelAllTask();
            _netLoadTaskRunner.Release();

            _unLoadTaskRunner.SwitchRunModel(RunModel.Sync);
            _unLoadTaskRunner.Release();
        }

        //todo:后续准备重构这一部分代码
        internal static void AddProvider(BaseProvider baseProvider)
        {
            _providerList.Add(baseProvider.ProviderGUID, baseProvider);
        }

        //todo:后续准备重构这一部分代码
        internal static bool TryGetProvider(string key, out BaseProvider baseProvider)
        {
            if (_providerList.TryGetValue(key, out baseProvider))
            {
                return true;
            }

            return false;
        }

        internal static void AddNetProvider(BaseProvider baseProvider)
        {
            _netProviderList.Add(baseProvider.ProviderGUID, baseProvider);
        }

        internal static bool TryGetNetProvider(string key, out BaseProvider baseProvider)
        {
            if (_netProviderList.TryGetValue(key, out baseProvider))
            {
                return true;
            }

            return false;
        }

        internal static void FreeUnUseAsset()
        {
            do
            {
                RuntimeData.ScheduleUnusedLifetimes();
                _unLoadTaskRunner.SwitchRunModel(RunModel.Sync);
            } while (RuntimeData.HasLifetimeChanges && !RuntimeData.IsReleasing);
        }
    }
}
