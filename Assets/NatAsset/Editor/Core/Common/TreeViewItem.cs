using System;
using UnityEditor.IMGUI.Controls;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    public class TreeViewItem<T> : TreeViewItem
    {
        public T data;

        public TreeViewItem(int id, int depth, string displayName, T data) : base(id, depth, displayName)
        {
            this.data = data;
        }
    }
}