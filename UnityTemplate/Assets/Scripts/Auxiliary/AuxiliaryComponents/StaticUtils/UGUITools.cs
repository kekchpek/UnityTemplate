#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AuxiliaryComponents.StaticUtils
{
    public class UGUITools : MonoBehaviour
    {
        [MenuItem("CONTEXT/" + nameof(RectTransform) + "/Anchors to corners")]
        static void AnchorsToCorners(MenuCommand command)
        {
            RectTransform t = (RectTransform)command.context;
            RectTransform pt = t!.parent as RectTransform;
            
            Undo.RecordObject(t, "Reset anchors");
    
            if (t == null || pt == null) return;
    
            var rect = pt.rect;
            Vector2 newAnchorsMin = new Vector2(t.anchorMin.x + t.offsetMin.x / rect.width,
                t.anchorMin.y + t.offsetMin.y / rect.height);
            Vector2 newAnchorsMax = new Vector2(t.anchorMax.x + t.offsetMax.x / rect.width,
                t.anchorMax.y + t.offsetMax.y / rect.height);
    
            t.anchorMin = newAnchorsMin;
            t.anchorMax = newAnchorsMax;
            t.offsetMin = t.offsetMax = new Vector2(0, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        
        [MenuItem("CONTEXT/" + nameof(RectTransform) + "/Fix NaN")]
        static void FixNaN(MenuCommand command)
        {
            var transformsToFix = new Queue<RectTransform>();
            transformsToFix.Enqueue((RectTransform)command.context);
            while (transformsToFix.Count > 0)
            {
                var t = transformsToFix.Dequeue();
                t.localPosition = FixVectorNaN(t.position);
                t.sizeDelta = FixVectorNaN(t.position);
                t.anchoredPosition = FixVectorNaN(t.anchoredPosition);
                t.offsetMax = FixVectorNaN(t.offsetMax);
                t.offsetMin = FixVectorNaN(t.offsetMin);
                foreach (Transform child in t)
                {
                    if (child is RectTransform rtChild)
                        transformsToFix.Enqueue(rtChild);
                }
            }
        }
        
        [MenuItem("CONTEXT/" + nameof(TMP_Text) + "/Fix NaN")]
        static void FixTextNaN(MenuCommand command)
        {
            var text = (TMP_Text)command.context;
            text.margin = FixVectorNaN(text.margin);
        }

        private static Vector2 FixVectorNaN(Vector2 input)
        {
            if (float.IsNaN(input.y))
            {
                input.y = 0;
            }
            if (float.IsNaN(input.x))
            {
                input.x = 0;
            }

            return input;
        }

        private static Vector3 FixVectorNaN(Vector3 input)
        {
            if (float.IsNaN(input.y))
            {
                input.y = 0;
            }
            if (float.IsNaN(input.x))
            {
                input.x = 0;
            }
            if (float.IsNaN(input.z))
            {
                input.z = 0;
            }

            return input;
        }

        private static Vector2 FixVectorNaN(Vector4 input)
        {
            if (float.IsNaN(input.y))
            {
                input.y = 0;
            }
            if (float.IsNaN(input.x))
            {
                input.x = 0;
            }
            if (float.IsNaN(input.z))
            {
                input.z = 0;
            }
            if (float.IsNaN(input.w))
            {
                input.w = 0;
            }

            return input;
        }
    }
}
#endif