using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class TaskSystem
    {
        private static ListDic<string, BaseProvider> _providerList = new ListDic<string, BaseProvider>(1000);
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
            UpdateTaskRunner();
        }

        internal static void Release()
        {
            ReleaseProvider();
            ReleaseTaskRunner();
            Resources.UnloadUnusedAssets();
        }

        //轮询Provider
        private static void UpdateProvider()
        {
            for (int i = 0; i < _providerList.Count; i++)
            {
                BaseProvider baseProvider = _providerList.GetElementByIndex(i);
                baseProvider.OnUpdate();
                if (baseProvider.IsDone)
                {
                    //已完成的任务启动回收
                    _providerList.Remove(baseProvider.ProviderGUID);
                    BaseProvider.Release(baseProvider);
                    i--;
                }
            }
        }

        private static void ReleaseProvider()
        {
            for (int i = 0; i < _providerList.Count; i++)
            {
                BaseProvider baseProvider = _providerList.GetElementByIndex(i);
                BaseProvider.Release(baseProvider);
            }
        }

        private static void InitTaskRunner()
        {
            _loadTaskRunner.InitTaskRunner(NatAssetSetting.TaskFrameLimit);
            _netLoadTaskRunner.InitTaskRunner(NatAssetSetting.TaskFrameLimit);
            _unLoadTaskRunner.InitTaskRunner(NatAssetSetting.TaskFrameLimit);
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

        internal static void AddProvider(BaseProvider baseProvider)
        {
            _providerList.Add(baseProvider.ProviderGUID, baseProvider);
        }

        internal static bool TryGetProvider(string key, out BaseProvider baseProvider)
        {
            if (_providerList.TryGetValue(key, out baseProvider))
            {
                return true;
            }

            return false;
        }
    }
}