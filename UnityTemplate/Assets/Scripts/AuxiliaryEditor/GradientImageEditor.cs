using UnityEditor;
using UnityEngine;
using AuxiliaryComponents;
using UnityEditor.UI;

namespace AuxiliaryComponents.Editor
{
    [CustomEditor(typeof(GradientImage))]
    public class GradientImageEditor : ImageEditor
    {
        private SerializedProperty _gradientColorProperty;
        private SerializedProperty _directionProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            _gradientColorProperty = serializedObject.FindProperty("_gradientColor");
            _directionProperty = serializedObject.FindProperty("_direction");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gradient", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_gradientColorProperty);
            EditorGUILayout.PropertyField(_directionProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
