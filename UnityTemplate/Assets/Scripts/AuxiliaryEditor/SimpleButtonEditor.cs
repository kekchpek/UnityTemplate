using kekchpek.AuxiliaryComponents.SimpleButton;
using UnityEditor;
using UnityEngine;

namespace kekchpek.Auxiliary.Editor
{
    [CustomEditor(typeof(SimpleButton))]
    public class SimpleButtonEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetGraphicProp;
        private SerializedProperty _interactableProp;
        private SerializedProperty _onClickProp;
        private SerializedProperty _useColorTransitionProp;
        private SerializedProperty _colorBlendModeProp;
        private SerializedProperty _baseColorProp;
        private SerializedProperty _useScaleTransitionProp;
        private SerializedProperty _targetTransformProp;
        private SerializedProperty _scaleBlendModeProp;
        private SerializedProperty _baseScaleProp;
        private SerializedProperty _normalColorProp;
        private SerializedProperty _normalDisabledColorProp;
        private SerializedProperty _hoveredColorProp;
        private SerializedProperty _hoveredDisabledColorProp;
        private SerializedProperty _pressedColorProp;
        private SerializedProperty _pressedDisabledColorProp;
        private SerializedProperty _normalScaleProp;
        private SerializedProperty _normalDisabledScaleProp;
        private SerializedProperty _hoveredScaleProp;
        private SerializedProperty _hoveredDisabledScaleProp;
        private SerializedProperty _pressedScaleProp;
        private SerializedProperty _pressedDisabledScaleProp;

        private void OnEnable()
        {
            _targetGraphicProp = serializedObject.FindProperty("_targetGraphic");
            _interactableProp = serializedObject.FindProperty("_interactable");
            _onClickProp = serializedObject.FindProperty("_onClick");
            _useColorTransitionProp = serializedObject.FindProperty("_useColorTransition");
            _colorBlendModeProp = serializedObject.FindProperty("_colorBlendMode");
            _baseColorProp = serializedObject.FindProperty("_baseColor");
            _normalColorProp = serializedObject.FindProperty("_normalColor");
            _normalDisabledColorProp = serializedObject.FindProperty("_normalDisabledColor");
            _hoveredColorProp = serializedObject.FindProperty("_hoveredColor");
            _hoveredDisabledColorProp = serializedObject.FindProperty("_hoveredDisabledColor");
            _pressedColorProp = serializedObject.FindProperty("_pressedColor");
            _pressedDisabledColorProp = serializedObject.FindProperty("_pressedDisabledColor");
            _useScaleTransitionProp = serializedObject.FindProperty("_useScaleTransition");
            _targetTransformProp = serializedObject.FindProperty("_targetTransform");
            _scaleBlendModeProp = serializedObject.FindProperty("_scaleBlendMode");
            _baseScaleProp = serializedObject.FindProperty("_baseScale");
            _normalScaleProp = serializedObject.FindProperty("_normalScale");
            _normalDisabledScaleProp = serializedObject.FindProperty("_normalDisabledScale");
            _hoveredScaleProp = serializedObject.FindProperty("_hoveredScale");
            _hoveredDisabledScaleProp = serializedObject.FindProperty("_hoveredDisabledScale");
            _pressedScaleProp = serializedObject.FindProperty("_pressedScale");
            _pressedDisabledScaleProp = serializedObject.FindProperty("_pressedDisabledScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspectorScriptField();

            EditorGUILayout.PropertyField(_interactableProp);
            EditorGUILayout.PropertyField(_onClickProp);
            EditorGUILayout.PropertyField(_useColorTransitionProp);

            if (_useColorTransitionProp.boolValue)
            {
                EditorGUILayout.PropertyField(_targetGraphicProp);
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_colorBlendModeProp);
                var colorBlendMode = (SimpleButtonColorBlendMode)_colorBlendModeProp.enumValueIndex;
                if (colorBlendMode != SimpleButtonColorBlendMode.Override)
                    EditorGUILayout.PropertyField(_baseColorProp);
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("State colors", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_normalColorProp);
                EditorGUILayout.PropertyField(_normalDisabledColorProp);
                EditorGUILayout.PropertyField(_hoveredColorProp);
                EditorGUILayout.PropertyField(_hoveredDisabledColorProp);
                EditorGUILayout.PropertyField(_pressedColorProp);
                EditorGUILayout.PropertyField(_pressedDisabledColorProp);
            }

            EditorGUILayout.PropertyField(_useScaleTransitionProp);

            if (_useScaleTransitionProp.boolValue)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_targetTransformProp);
                EditorGUILayout.PropertyField(_scaleBlendModeProp);
                var scaleBlendMode = (SimpleButtonColorBlendMode)_scaleBlendModeProp.enumValueIndex;
                if (scaleBlendMode != SimpleButtonColorBlendMode.Override)
                    EditorGUILayout.PropertyField(_baseScaleProp);
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("State scales", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_normalScaleProp);
                EditorGUILayout.PropertyField(_normalDisabledScaleProp);
                EditorGUILayout.PropertyField(_hoveredScaleProp);
                EditorGUILayout.PropertyField(_hoveredDisabledScaleProp);
                EditorGUILayout.PropertyField(_pressedScaleProp);
                EditorGUILayout.PropertyField(_pressedDisabledScaleProp);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultInspectorScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((SimpleButton)target), typeof(SimpleButton), false);
            }
        }
    }
}
