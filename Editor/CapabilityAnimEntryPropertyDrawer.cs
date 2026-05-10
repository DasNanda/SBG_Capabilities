using SBG.Capabilities.Animation;
using UnityEditor;
using UnityEngine;

namespace SBG.Capabilities.Editor
{
    [CustomPropertyDrawer(typeof(CapAnimClipEntry))]
    public class CapabilityAnimEntryPropertyDrawer : PropertyDrawer
	{
        private static CapAnimClipEntry dummy = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate Rects
            float x = position.x;
            var idLabelRect = new Rect(x, position.y, 20, position.height);
            x += idLabelRect.width;
            var idRect = new Rect(x, position.y, 200, position.height-1);
            x += idRect.width + 15;
            var clipLabelRect = new Rect(x, position.y, 30, position.height);
            x += clipLabelRect.width;
            var clipRect = new Rect(x, position.y, position.width - x + position.x, position.height-1);

            // Fetch Properties
            var idProp = property.FindPropertyRelative(nameof(dummy.SpecifierId));
            var clipProp = property.FindPropertyRelative(nameof(dummy.Clip));

            // Draw
            EditorGUI.LabelField(idLabelRect, new GUIContent("Id"));
            EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
            EditorGUI.LabelField(clipLabelRect, new GUIContent("Clip"));
            EditorGUI.PropertyField(clipRect, clipProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}