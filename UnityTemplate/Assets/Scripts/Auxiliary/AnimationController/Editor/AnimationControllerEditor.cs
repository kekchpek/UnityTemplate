using UnityEditor;
using UnityEngine;

namespace kekchpek.Auxiliary.AnimationControllerTool.Editor
{
    [CustomEditor(typeof(AnimationController))]
    public class AnimationControllerEditor : UnityEditor.Editor
    {
        private const float UpButtonWidth = 36f;
        private const float DownButtonWidth = 50f;
        private const float RemoveButtonWidth = 70f;
        private const float HeaderButtonsSpacing = 4f;

        private SerializedProperty _sequencesProperty;

        private void OnEnable()
        {
            _sequencesProperty = serializedObject.FindProperty("_sequences");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = previousIndent + 1;
            DrawSequences();
            EditorGUI.indentLevel = previousIndent;

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSequences()
        {
            EditorGUILayout.LabelField("Animation Sequences", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            for (int sequenceIndex = 0; sequenceIndex < _sequencesProperty.arraySize; sequenceIndex++)
            {
                SerializedProperty sequenceProperty = _sequencesProperty.GetArrayElementAtIndex(sequenceIndex);
                DrawSequence(sequenceProperty, sequenceIndex);
            }

            if (GUILayout.Button("Add Sequence"))
            {
                int newIndex = _sequencesProperty.arraySize;
                _sequencesProperty.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newSequence = _sequencesProperty.GetArrayElementAtIndex(newIndex);
                EnsureSequenceAnimationsHaveOwnTypeData(newSequence);
                newSequence.isExpanded = true;
            }
        }

        private void DrawSequence(SerializedProperty sequenceProperty, int sequenceIndex)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var sequenceName = sequenceProperty.FindPropertyRelative("SequenceName").stringValue;
                if (string.IsNullOrEmpty(sequenceName))
                {
                    sequenceName = $"Sequence {sequenceIndex + 1}";
                }
                HeaderAction action = DrawHeaderFoldout(sequenceProperty.isExpanded, sequenceName, 0);
                sequenceProperty.isExpanded = action.IsExpanded;

                if (action.Type == HeaderActionType.MoveUp && sequenceIndex > 0)
                {
                    _sequencesProperty.MoveArrayElement(sequenceIndex, sequenceIndex - 1);
                    return;
                }

                if (action.Type == HeaderActionType.MoveDown && sequenceIndex < _sequencesProperty.arraySize - 1)
                {
                    _sequencesProperty.MoveArrayElement(sequenceIndex, sequenceIndex + 1);
                    return;
                }

                if (action.Type == HeaderActionType.Remove)
                {
                    _sequencesProperty.DeleteArrayElementAtIndex(sequenceIndex);
                    return;
                }

                if (!sequenceProperty.isExpanded)
                {
                    return;
                }

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = previousIndent + 1;
                EditorGUILayout.Space(2f);
                EditorGUILayout.PropertyField(sequenceProperty.FindPropertyRelative("SequenceName"));

                SerializedProperty animationsProperty = sequenceProperty.FindPropertyRelative("Animations");
                DrawAnimations(animationsProperty);
                EditorGUI.indentLevel = previousIndent;
            }
        }

        private void DrawAnimations(SerializedProperty animationsProperty)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);

            for (int animationIndex = 0; animationIndex < animationsProperty.arraySize; animationIndex++)
            {
                SerializedProperty animationProperty = animationsProperty.GetArrayElementAtIndex(animationIndex);
                DrawAnimation(animationProperty, animationsProperty, animationIndex);
            }

            if (GUILayout.Button("Add Animation"))
            {
                int newIndex = animationsProperty.arraySize;
                animationsProperty.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newAnimation = animationsProperty.GetArrayElementAtIndex(newIndex);
                EnsureNewAnimationHasOwnTypeData(newAnimation);
                newAnimation.isExpanded = true;
            }
        }

        private void DrawAnimation(SerializedProperty animationProperty, SerializedProperty animationsProperty, int animationIndex)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                HeaderAction action = DrawHeaderFoldout(animationProperty.isExpanded, $"Animation {animationIndex + 1}", 1);
                animationProperty.isExpanded = action.IsExpanded;

                if (action.Type == HeaderActionType.MoveUp && animationIndex > 0)
                {
                    animationsProperty.MoveArrayElement(animationIndex, animationIndex - 1);
                    return;
                }

                if (action.Type == HeaderActionType.MoveDown && animationIndex < animationsProperty.arraySize - 1)
                {
                    animationsProperty.MoveArrayElement(animationIndex, animationIndex + 1);
                    return;
                }

                if (action.Type == HeaderActionType.Remove)
                {
                    animationsProperty.DeleteArrayElementAtIndex(animationIndex);
                    return;
                }

                if (!animationProperty.isExpanded)
                {
                    return;
                }

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = previousIndent + 2;
                EditorGUILayout.PropertyField(animationProperty, GUIContent.none, true);
                EditorGUI.indentLevel = previousIndent;
            }
        }

        private static HeaderAction DrawHeaderFoldout(bool isExpanded, string title, int nestingLevel)
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float foldoutIndent = nestingLevel * 15f;
            float buttonsWidth = UpButtonWidth + DownButtonWidth + RemoveButtonWidth + HeaderButtonsSpacing * 2f;
            Rect foldoutRect = new Rect(
                headerRect.x + foldoutIndent,
                headerRect.y,
                headerRect.width - buttonsWidth - HeaderButtonsSpacing - foldoutIndent,
                headerRect.height);
            Rect upRect = new Rect(foldoutRect.xMax + HeaderButtonsSpacing, headerRect.y, UpButtonWidth, headerRect.height);
            Rect downRect = new Rect(upRect.xMax + HeaderButtonsSpacing, headerRect.y, DownButtonWidth, headerRect.height);
            Rect removeRect = new Rect(downRect.xMax + HeaderButtonsSpacing, headerRect.y, RemoveButtonWidth, headerRect.height);

            bool expanded = EditorGUI.Foldout(foldoutRect, isExpanded, title, true);
            if (GUI.Button(upRect, "Up"))
            {
                return HeaderAction.ForMoveUp(expanded);
            }

            if (GUI.Button(downRect, "Down"))
            {
                return HeaderAction.ForMoveDown(expanded);
            }

            if (GUI.Button(removeRect, "Remove"))
            {
                return HeaderAction.ForRemove(expanded);
            }

            return HeaderAction.ForNone(expanded);
        }

        private static void EnsureNewAnimationHasOwnTypeData(SerializedProperty animationProperty)
        {
            SerializedProperty typeProperty = animationProperty.FindPropertyRelative("Type");
            SerializedProperty animationTypeDataProperty = animationProperty.FindPropertyRelative("_animationTypeData");
            AnimationType animationType = (AnimationType)typeProperty.enumValueIndex;
            object sourceTypeData = animationTypeDataProperty.managedReferenceValue;

            if (sourceTypeData == null)
            {
                animationTypeDataProperty.managedReferenceValue = CreateTypeData(animationType);
                return;
            }

            animationTypeDataProperty.managedReferenceValue = CloneTypeData(sourceTypeData, animationType);
        }

        private static void EnsureSequenceAnimationsHaveOwnTypeData(SerializedProperty sequenceProperty)
        {
            SerializedProperty animationsProperty = sequenceProperty.FindPropertyRelative("Animations");
            if (animationsProperty == null)
            {
                return;
            }

            for (int i = 0; i < animationsProperty.arraySize; i++)
            {
                SerializedProperty animationProperty = animationsProperty.GetArrayElementAtIndex(i);
                EnsureNewAnimationHasOwnTypeData(animationProperty);
            }
        }

        private static object CreateTypeData(AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.Unity:
                    return new UnityAnimationTypeData();
                case AnimationType.Spine:
                    return new SpineAnimationTypeData();
                case AnimationType.SpineClearTrack:
                    return new SpineClearTrackAnimationTypeData();
                case AnimationType.AnimationController:
                    return new AnimationControllerAnimationTypeData();
                default:
                    return null;
            }
        }

        private static object CloneTypeData(object sourceData, AnimationType animationType)
        {
            string json = JsonUtility.ToJson(sourceData);
            switch (animationType)
            {
                case AnimationType.Unity:
                    return JsonUtility.FromJson<UnityAnimationTypeData>(json);
                case AnimationType.Spine:
                    return JsonUtility.FromJson<SpineAnimationTypeData>(json);
                case AnimationType.SpineClearTrack:
                    return JsonUtility.FromJson<SpineClearTrackAnimationTypeData>(json);
                case AnimationType.AnimationController:
                    return JsonUtility.FromJson<AnimationControllerAnimationTypeData>(json);
                default:
                    return CreateTypeData(animationType);
            }
        }

        private readonly struct HeaderAction
        {
            public readonly bool IsExpanded;
            public readonly HeaderActionType Type;

            private HeaderAction(bool isExpanded, HeaderActionType type)
            {
                IsExpanded = isExpanded;
                Type = type;
            }

            public static HeaderAction ForNone(bool isExpanded) => new(isExpanded, HeaderActionType.None);
            public static HeaderAction ForMoveUp(bool isExpanded) => new(isExpanded, HeaderActionType.MoveUp);
            public static HeaderAction ForMoveDown(bool isExpanded) => new(isExpanded, HeaderActionType.MoveDown);
            public static HeaderAction ForRemove(bool isExpanded) => new(isExpanded, HeaderActionType.Remove);

        }

        private enum HeaderActionType
        {
            None,
            MoveUp,
            MoveDown,
            Remove
        }
    }
}
