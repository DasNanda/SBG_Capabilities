using SBG.Capabilities.Animation;
using System;
using UnityEditor;
using UnityEngine;

namespace SBG.Capabilities.Editor
{
    [CustomPropertyDrawer(typeof(TransitionLength))]
    public class TransitionLengthDrawer : PropertyDrawer
	{
        private static TransitionLength dummy = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            // Calculate Rects
            float x = position.x - 30;
            var toggleRect = new Rect(x, position.y, 10, position.height);
            x += toggleRect.width + 15;

            var prefLabelRect = new Rect(x, position.y, 60, position.height);
            x += prefLabelRect.width;

            var prefLengthRect = new Rect(x, position.y, 100, position.height);
            x += prefLengthRect.width;

            var maxLabelRect = new Rect(x, position.y, 60, position.height);
            x += maxLabelRect.width;

            var maxLengthRect = new Rect(x, position.y, 100, position.height);
            x += maxLengthRect.width + 10;

            var crossfadeRect = new Rect(x, position.y, position.width - x + position.x, position.height);

            // Fetch Properties
            var isUsed = property.FindPropertyRelative(nameof(dummy.IsUsed));
            var preferedLength = property.FindPropertyRelative(nameof(dummy.PreferedLength));
            var maxLength = property.FindPropertyRelative(nameof(dummy.MaxLength));
            var crossfade = property.FindPropertyRelative(nameof(dummy.ForceCrossfade));

            // Draw
            EditorGUI.PropertyField(toggleRect, isUsed, GUIContent.none);
            EditorGUI.BeginDisabledGroup(!isUsed.boolValue);

            EditorGUI.LabelField(prefLabelRect, "Pref.");

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(prefLengthRect, preferedLength, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                if (preferedLength.floatValue < 0) preferedLength.floatValue = 0;
                if (preferedLength.floatValue > maxLength.floatValue) maxLength.floatValue = preferedLength.floatValue;
            }

            EditorGUI.LabelField(maxLabelRect, "Max.");

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(maxLengthRect, maxLength, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                if (maxLength.floatValue < 0) maxLength.floatValue = 0;
                if (preferedLength.floatValue > maxLength.floatValue) preferedLength.floatValue = maxLength.floatValue;
            }

            GUI.color = crossfade.boolValue ? Color.cyan : Color.green;

            if (GUI.Button(crossfadeRect, crossfade.boolValue ? "Crossfade" : "Out-In"))
            {
                crossfade.boolValue = !crossfade.boolValue;
            }

            GUI.color = Color.white;

            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }
    }
}