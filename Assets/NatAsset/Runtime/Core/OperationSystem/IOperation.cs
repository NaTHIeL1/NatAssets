namespace NATFrameWork.NatAsset.Runtime
{
    public interface IOperation
    {
        public bool IsDone{ get; set; }
        public void OnUpdate();
    }
}
