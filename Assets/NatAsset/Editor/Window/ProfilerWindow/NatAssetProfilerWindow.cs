using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class NatAssetProfilerWindow : EditorWindow
    {
        [MenuItem("NatAsset/Profiler", false, 2)]
        private static void ShowWindow()
        {
            var window = GetWindow<NatAssetProfilerWindow>();
            window.titleContent = new GUIContent("NatAssetProfiler");
            window.Show();
            window.minSize = new Vector2(800, 550);
            firstSplit = window.position.width * splitHandle;
            secondSplit = window.position.width * splitInfo;
            taskSplitVertical = window.position.height * splitTaskVertical;
        }

        private static float splitHandle = 0.24f;
        private static float splitInfo = 0.6f;
        private static float splitTaskVertical = 0.4f;
        private float splitHight = 20f;

        private static float firstSplit, secondSplit, taskSplitVertical;
        private bool _firstDraging = false;
        private bool _secondDraging = false;
        private bool _taskVerticalDraging = false;

        [SerializeField] private TreeViewState _treeViewStateHandle;
        [SerializeField] private TreeViewState _treeViewStateLoadTask;
        [SerializeField] private TreeViewState _treeViewStateUnLoadTask;
        [SerializeField] private TreeViewState _treeViewStateInfo;
        [SerializeField] private MultiColumnHeaderState _handle_MultiColumnHeaderState;
        [SerializeField] private MultiColumnHeaderState _loadtask_MultiColumnHeaderState;
        [SerializeField] private MultiColumnHeaderState _unloadtask_MultiColumnHeaderState;
        [SerializeField] private MultiColumnHeaderState _info_MultiColumnHeaderState;

        private InfoTreeView _InfoTreeView;
        private HandleTreeView _handleTreeView;
        private LoadTaskTreeView _loadTaskTreeView;
        private UnLoadTaskTreeView _unLoadTaskTreeView;

        private Rect GetHandleRect
        {
            get { return new Rect(0, splitHight, FirstSplit.x, position.height); }
        }

        private Rect GetLoadTaskRect
        {
            get
            {
                return new Rect(SecondSplit.x + SecondSplit.width, splitHight,
                    position.width - SecondSplit.x - SecondSplit.width, TaskVerticalSplit.y - splitHight);
            }
        }

        private Rect GetUnLoadTaskRect
        {
            get
            {
                return new Rect(SecondSplit.x + SecondSplit.width,
                    GetLoadTaskRect.height + GetLoadTaskRect.y + TaskVerticalSplit.height,
                    position.width - SecondSplit.x - SecondSplit.width, position.height - TaskVerticalSplit.y);
            }
        }

        private Rect GetInfoRect
        {
            get
            {
                return new Rect(FirstSplit.x + FirstSplit.width, splitHight, SecondSplit.x - FirstSplit.x - 2,
                    position.height - splitHight);
            }
        }


        private Rect FirstSplit, SecondSplit, TaskVerticalSplit;

        private void OnEnable()
        {
            _treeViewStateHandle = _treeViewStateHandle == null ? new TreeViewState(): _treeViewStateHandle;
            _treeViewStateLoadTask = _treeViewStateLoadTask == null ? new TreeViewState() : _treeViewStateLoadTask;
            _treeViewStateInfo = _treeViewStateInfo == null ? new TreeViewState() : _treeViewStateInfo;
            _treeViewStateUnLoadTask = _treeViewStateUnLoadTask == null ? new TreeViewState() : _treeViewStateUnLoadTask;

            EnableInfoViewTree();
        }

        private void EnableInfoViewTree()
        {
            bool firstInit = _info_MultiColumnHeaderState == null;
            MultiColumnHeaderState infoHeaderState = InfoTreeView.CreateDefaultMultiColumnHeaderState(100);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_info_MultiColumnHeaderState, infoHeaderState))
                MultiColumnHeaderState.OverwriteSerializedFields(_info_MultiColumnHeaderState, infoHeaderState);
            _info_MultiColumnHeaderState = infoHeaderState;
            MultiColumnHeader infoHeader = new MultiColumnHeader(infoHeaderState);

            MultiColumnHeaderState loadtaskHeaderState = LoadTaskTreeView.CreateDefaultMultiColumnHeaderState(100);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_loadtask_MultiColumnHeaderState,
                loadtaskHeaderState))
                MultiColumnHeaderState.OverwriteSerializedFields(_loadtask_MultiColumnHeaderState, loadtaskHeaderState);
            _loadtask_MultiColumnHeaderState = loadtaskHeaderState;
            MultiColumnHeader taskHeader = new MultiColumnHeader(loadtaskHeaderState);

            MultiColumnHeaderState unLoadtaskHeaderState = UnLoadTaskTreeView.CreateDefaultMultiColumnHeaderState(100);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_unloadtask_MultiColumnHeaderState,
                unLoadtaskHeaderState))
                MultiColumnHeaderState.OverwriteSerializedFields(_unloadtask_MultiColumnHeaderState,
                    unLoadtaskHeaderState);
            _unloadtask_MultiColumnHeaderState = unLoadtaskHeaderState;
            MultiColumnHeader unLoadtaskHeader = new MultiColumnHeader(unLoadtaskHeaderState);

            MultiColumnHeaderState handleHeaderState = UnLoadTaskTreeView.CreateDefaultMultiColumnHeaderState(100);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_handle_MultiColumnHeaderState,
                handleHeaderState))
                MultiColumnHeaderState.OverwriteSerializedFields(_handle_MultiColumnHeaderState,
                    handleHeaderState);
            _handle_MultiColumnHeaderState = handleHeaderState;
            MultiColumnHeader handletaskHeader = new MultiColumnHeader(handleHeaderState);
            if (firstInit)
            {
                infoHeader.ResizeToFit();
                taskHeader.ResizeToFit();
                unLoadtaskHeader.ResizeToFit();
                handletaskHeader.ResizeToFit();
            }

            _handleTreeView = new HandleTreeView(_treeViewStateHandle,handletaskHeader);
            _InfoTreeView = new InfoTreeView(_treeViewStateInfo, infoHeader);
            _loadTaskTreeView = new LoadTaskTreeView(_treeViewStateLoadTask, taskHeader);
            _unLoadTaskTreeView = new UnLoadTaskTreeView(_treeViewStateUnLoadTask, unLoadtaskHeader);
        }

        private void OnGUI()
        {
            if (firstSplit == 0 || secondSplit == 0)
            {
                firstSplit = position.width * splitHandle;
                secondSplit = position.width * splitInfo;
                taskSplitVertical = position.height * splitTaskVertical;
            }

            FirstSplit = new Rect(firstSplit, 0, 2, position.height);
            SecondSplit = new Rect(secondSplit, 0, 2, position.height);
            TaskVerticalSplit = new Rect(secondSplit, taskSplitVertical, position.width - secondSplit, 2);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Handle", new[] {GUILayout.Width(firstSplit)});
            GUILayout.Label("RefInfo", new[] {GUILayout.Width(secondSplit - firstSplit)});
            GUILayout.Label("Task", new[] {GUILayout.Width(position.width - secondSplit)});
            GUILayout.EndHorizontal();

            EditorGUI.DrawRect(FirstSplit, Color.gray);
            GUIHelperUtil.DragRectH(ref FirstSplit, ref _firstDraging, ref firstSplit, position, 0.15f, 0.45f);

            EditorGUI.DrawRect(SecondSplit, Color.gray);
            GUIHelperUtil.DragRectH(ref SecondSplit, ref _secondDraging, ref secondSplit, position, 0.55f, 0.8f);

            EditorGUI.DrawRect(TaskVerticalSplit, Color.gray);
            GUIHelperUtil.DragRectV(ref TaskVerticalSplit, ref _taskVerticalDraging, ref taskSplitVertical,
                position, 0.3f, 0.7f);

            _handleTreeView.OnGUI(GetHandleRect);
            _InfoTreeView.OnGUI(GetInfoRect);
            _loadTaskTreeView.OnGUI(GetLoadTaskRect);
            _unLoadTaskTreeView.OnGUI(GetUnLoadTaskRect);
        }

        private void Update()
        {
            if (EditorApplication.isPlaying)
            {
                _InfoTreeView?.Update();
                _loadTaskTreeView?.Update();
                _unLoadTaskTreeView?.Update();
            }
        }
    }
}