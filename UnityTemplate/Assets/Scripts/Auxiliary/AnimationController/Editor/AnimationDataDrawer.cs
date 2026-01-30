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
            
            // Get the animation type
            SerializedProperty typeProperty = property.FindPropertyRelative("Type");
            AnimationType animationType = (AnimationType)typeProperty.enumValueIndex;
            
            // Calculate the height for the property
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float currentY = position.y;
            
            // Draw the type field
            Rect typeRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(typeRect, typeProperty);
            currentY += lineHeight + spacing;
            
            // Draw the parallel flag
            SerializedProperty parallelProperty = property.FindPropertyRelative("ExecuteInParallel");
            Rect parallelRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(parallelRect, parallelProperty);
            currentY += lineHeight + spacing;
            
            // Draw fields based on animation type
            switch (animationType)
            {
                case AnimationType.Unity:
                    // Unity Animation fields
                    SerializedProperty animatorProperty = property.FindPropertyRelative("UnityAnimator");
                    Rect animatorRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(animatorRect, animatorProperty, new GUIContent("Animator"));
                    currentY += lineHeight + spacing;
                    
                    SerializedProperty animationStateNameProperty = property.FindPropertyRelative("AnimationStateName");
                    Rect animationStateNameRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(animationStateNameRect, animationStateNameProperty, new GUIContent("Animation State"));
                    break;
                    
                case AnimationType.Spine:
                    // Spine Animation fields
                    SerializedProperty skeletonGraphicProperty = property.FindPropertyRelative("SpineSkeleton");
                    Rect skeletonGraphicRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(skeletonGraphicRect, skeletonGraphicProperty, new GUIContent("Skeleton Graphic"));
                    currentY += lineHeight + spacing;
                    
                    SerializedProperty skeletonAnimationProperty = property.FindPropertyRelative("SpineSkeletonAnimation");
                    Rect skeletonAnimationRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(skeletonAnimationRect, skeletonAnimationProperty, new GUIContent("Skeleton Animation"));
                    currentY += lineHeight + spacing;
                    
                    SerializedProperty animationNameProperty = property.FindPropertyRelative("AnimationName");
                    Rect animationNameRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(animationNameRect, animationNameProperty, new GUIContent("Animation Name"));
                    currentY += lineHeight + spacing;
                    
                    SerializedProperty animationLayerProperty = property.FindPropertyRelative("SpineAnimationLayer");
                    Rect animationLayerRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(animationLayerRect, animationLayerProperty, new GUIContent("Animation Layer"));
                    break;
                    
                case AnimationType.AnimationController:
                    // AnimationController fields
                    SerializedProperty targetControllerProperty = property.FindPropertyRelative("TargetAnimationController");
                    Rect targetControllerRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(targetControllerRect, targetControllerProperty, new GUIContent("Target Controller"));
                    currentY += lineHeight + spacing;
                    
                    SerializedProperty targetSequenceProperty = property.FindPropertyRelative("TargetSequenceName");
                    Rect targetSequenceRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.PropertyField(targetSequenceRect, targetSequenceProperty, new GUIContent("Target Sequence"));
                    break;
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Base height for type and parallel flag
            float height = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
            
            // Add height for type-specific fields
            SerializedProperty typeProperty = property.FindPropertyRelative("Type");
            AnimationType animationType = (AnimationType)typeProperty.enumValueIndex;
            
            switch (animationType)
            {
                case AnimationType.Unity:
                    // Two additional fields for Unity animations (Animator, AnimationState)
                    height += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
                    break;
                    
                case AnimationType.Spine:
                    // Four additional fields for Spine animations (SkeletonGraphic, SkeletonAnimation, AnimationName, AnimationLayer)
                    height += EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
                    break;
                    
                case AnimationType.AnimationController:
                    // Two additional fields for AnimationController type
                    height += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }
            
            return height;
        }
    }
} 
