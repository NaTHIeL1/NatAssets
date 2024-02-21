namespace NATFrameWork.NatAsset.Runtime
{
    public enum TaskState
    {
        Idel,         //闲置
        Waiting,      //等待执行
        Running,      //正在执行
        Finish,       //加载完毕
        End,          //任务结束
    }

    public enum TaskResult
    {
        Nono,         //未获取到加载结果
        Success,      //任务成功
        Faild,        //任务失败
    }
    
    public enum TaskType
     {
         Asset = 0,
         Scene = 1,
         Bundle = 2,
         Web = 3,
     }

    public enum ProviderResult
    {
        Nono,         //未获取到加载结果
        Success,      //成功
        Faild,        //失败
    }

    public enum ProviderState
    {
        Waiting,     //未执行
        Running,     //正在执行
        Finish,      //执行完成
    }

    //用于辅助处理异步同步加载冲突问题
    public enum RunModel
    {
        Sync,//同步
        Async,//异步
    }

    public enum Priority
    {
        Bottom = 0,
        Low = 1,
        Middle = 2,
        High = 3,
        Top = 4,
    }
}