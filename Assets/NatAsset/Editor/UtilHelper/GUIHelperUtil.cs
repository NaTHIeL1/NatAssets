using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    internal static class GUIHelperUtil
    {
        internal static Rect HorizontalRect(Rect rect, float offset)
        {
            Rect temp = rect;
            temp.x += offset;
            temp.width -= offset;
            return temp;
        }

        internal static Rect HorizontalRect(Rect rect, float offset, out Rect resRect)
        {
            Rect temp = rect;
            temp.x += offset;
            temp.width -= offset;
            resRect = temp;
            return temp;
        }

        internal static Rect DragRectH(ref Rect rect, ref bool draging, ref float split, Rect position, float leftLimt = 0, float rightLimt = 1)
        {
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(e.mousePosition))
                    {
                        draging = true;
                    }

                    break;
                case EventType.MouseDrag:
                    if (draging)
                    {
                        split += e.delta.x;
                        split = Mathf.Clamp(split, position.width * leftLimt, position.width * rightLimt);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (draging)
                    {
                        draging = false;
                    }

                    break;
            }
            return position;
        }
        
        internal static Rect DragRectV(ref Rect rect, ref bool draging, ref float split, Rect position, float topLimt = 0, float downLimt = 1)
        {
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(e.mousePosition))
                    {
                        draging = true;
                    }

                    break;
                case EventType.MouseDrag:
                    if (draging)
                    {
                        split += e.delta.y;
                        split = Mathf.Clamp(split, position.height * topLimt, position.height * downLimt);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (draging)
                    {
                        draging = false;
                    }

                    break;
            }
            return position;
        }
    }
}