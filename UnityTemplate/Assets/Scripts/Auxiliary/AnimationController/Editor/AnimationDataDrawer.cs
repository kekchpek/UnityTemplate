using UnityEngine;
using UnityEditor;

namespace kekchpek.Auxiliary.AnimationControllerTool.Editor
{
    [CustomPropertyDrawer(typeof(AnimationData))]
    public class AnimationDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty typeProperty = property.FindPropertyRelative("Type");
            SerializedProperty parallelProperty = property.FindPropertyRelative("ExecuteInParallel");
            SerializedProperty animationTypeDataProperty = property.FindPropertyRelative("_animationTypeData");
            
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float currentY = position.y;
            
            Rect typeRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(typeRect, typeProperty);
            if (EditorGUI.EndChangeCheck())
            {
                AssignAnimationTypeData(animationTypeDataProperty, (AnimationType)typeProperty.enumValueIndex);
                property.serializedObject.ApplyModifiedProperties();
            }
            currentY += lineHeight + spacing;
            
            Rect parallelRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(parallelRect, parallelProperty);
            currentY += lineHeight + spacing;

            bool animationTypeDataChanged = EnsureAnimationTypeData(animationTypeDataProperty, (AnimationType)typeProperty.enumValueIndex);
            if (animationTypeDataChanged)
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            DrawTypeSpecificFields(position, animationTypeDataProperty, (AnimationType)typeProperty.enumValueIndex, lineHeight, spacing, ref currentY);
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeProperty = property.FindPropertyRelative("Type");
            int typeSpecificFieldsCount = GetTypeSpecificFieldsCount((AnimationType)typeProperty.enumValueIndex);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float lines = 2 + typeSpecificFieldsCount;
            float spaces = 1 + typeSpecificFieldsCount;
            float height = (lineHeight * lines) + (spacing * spaces);

            return height;
        }

        private static void DrawTypeSpecificFields(Rect position, SerializedProperty animationTypeDataProperty, AnimationType animationType, float lineHeight, float spacing, ref float currentY)
        {
            switch (animationType)
            {
                case AnimationType.Unity:
                    DrawField(position, animationTypeDataProperty, "UnityAnimator", "Animator", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "AnimationStateName", "Animation State", lineHeight, spacing, ref currentY);
                    break;
                case AnimationType.Spine:
                    DrawField(position, animationTypeDataProperty, "SpineSkeleton", "Skeleton Graphic", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "SpineSkeletonAnimation", "Skeleton Animation", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "AnimationName", "Animation Name", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "SpineAnimationLayer", "Animation Layer", lineHeight, spacing, ref currentY);
                    break;
                case AnimationType.SpineClearTrack:
                    DrawField(position, animationTypeDataProperty, "SpineSkeleton", "Skeleton Graphic", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "SpineSkeletonAnimation", "Skeleton Animation", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "TrackIndex", "Track Index", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "Duration", "Duration", lineHeight, spacing, ref currentY);
                    break;
                case AnimationType.AnimationController:
                    DrawField(position, animationTypeDataProperty, "TargetAnimationController", "Target Controller", lineHeight, spacing, ref currentY);
                    DrawField(position, animationTypeDataProperty, "TargetSequenceName", "Target Sequence", lineHeight, spacing, ref currentY);
                    break;
            }
        }

        private static int GetTypeSpecificFieldsCount(AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.Unity:
                    return 2;
                case AnimationType.Spine:
                    return 4;
                case AnimationType.SpineClearTrack:
                    return 4;
                case AnimationType.AnimationController:
                    return 2;
                default:
                    return 0;
            }
        }

        private static void DrawField(Rect position, SerializedProperty parentProperty, string relativePropertyName, string label, float lineHeight, float spacing, ref float currentY)
        {
            SerializedProperty childProperty = parentProperty.FindPropertyRelative(relativePropertyName);
            if (childProperty == null)
            {
                return;
            }

            Rect fieldRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(fieldRect, childProperty, new GUIContent(label));
            currentY += lineHeight + spacing;
        }

        private static bool EnsureAnimationTypeData(SerializedProperty animationTypeDataProperty, AnimationType animationType)
        {
            if (animationTypeDataProperty.managedReferenceValue is UnityAnimationTypeData && animationType == AnimationType.Unity)
            {
                return false;
            }

            if (animationTypeDataProperty.managedReferenceValue is SpineAnimationTypeData && animationType == AnimationType.Spine)
            {
                return false;
            }

            if (animationTypeDataProperty.managedReferenceValue is SpineClearTrackAnimationTypeData && animationType == AnimationType.SpineClearTrack)
            {
                return false;
            }

            if (animationTypeDataProperty.managedReferenceValue is AnimationControllerAnimationTypeData && animationType == AnimationType.AnimationController)
            {
                return false;
            }

            AssignAnimationTypeData(animationTypeDataProperty, animationType);
            return true;
        }

        private static void AssignAnimationTypeData(SerializedProperty animationTypeDataProperty, AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.Unity:
                    animationTypeDataProperty.managedReferenceValue = new UnityAnimationTypeData();
                    break;
                case AnimationType.Spine:
                    animationTypeDataProperty.managedReferenceValue = new SpineAnimationTypeData();
                    break;
                case AnimationType.SpineClearTrack:
                    animationTypeDataProperty.managedReferenceValue = new SpineClearTrackAnimationTypeData();
                    break;
                case AnimationType.AnimationController:
                    animationTypeDataProperty.managedReferenceValue = new AnimationControllerAnimationTypeData();
                    break;
            }
        }
    }
} 
