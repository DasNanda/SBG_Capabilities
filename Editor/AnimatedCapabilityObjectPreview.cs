using SBG.Capabilities;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SBG.Capabilites.Editor
{
    [CustomPreview(typeof(AnimatedCapability))]
	public class AnimatedCapabilityObjectPreview : ObjectPreview
	{
        private static FieldInfo cachedAvatarPreviewField;
        private static FieldInfo cachedTimeControlField;
        private static FieldInfo cachedStopTimeField;

        private UnityEditor.Editor preview;
        private int animationClipId;
        private int animSpecifierIndex = 0;
        private bool specifierChanged;

        public override void Initialize(Object[] targets)
        {
            base.Initialize(targets);

            if (targets.Length > 1 || Application.isPlaying) return;
            var animCap = target as AnimatedCapability;
            if (animCap == null || animCap.Animation == null) return;

            SourceAnimationClipEditorFields();

            AnimationClip clip = GetCurrentClip(animCap);
            if (clip != null)
            {
                preview = UnityEditor.Editor.CreateEditor(clip);
                animationClipId = clip.GetInstanceID();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            CleanupPreviewEditor();
        }

        public override bool HasPreviewGUI() => preview?.HasPreviewGUI() ?? false;

        public override GUIContent GetPreviewTitle() => new GUIContent((target as AnimatedCapability).DisplayName);

        public override void OnPreviewSettings()
        {
            base.OnPreviewSettings();
            EditorGUI.BeginChangeCheck();
            animSpecifierIndex = EditorGUILayout.Popup(animSpecifierIndex, FetchSpecifiers(), GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck()) specifierChanged = true;
        }

        private string[] FetchSpecifiers()
        {
            var cap = target as AnimatedCapability;
            return cap.Animation.Clips.Select(c => c.SpecifierId).Prepend("Fallback").ToArray();
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnInteractivePreviewGUI(r, background);

            AnimationClip clip = GetCurrentClip(target as AnimatedCapability);
            if (clip != null && (clip.GetInstanceID() != animationClipId || specifierChanged))
            {
                CleanupPreviewEditor();
                preview = UnityEditor.Editor.CreateEditor(clip);
                animationClipId = clip.GetInstanceID();
                specifierChanged = false;
                return;
            }

            if (preview != null)
            {
                UpdateAnimationClipEditor(preview, clip);
                preview.OnInteractivePreviewGUI(r, background);
            }
        }

        private void UpdateAnimationClipEditor(UnityEditor.Editor editor, AnimationClip clip)
        {
            if (cachedAvatarPreviewField == null || cachedTimeControlField == null || cachedStopTimeField == null) return;

            var avatarPreview = cachedAvatarPreviewField.GetValue(editor);
            var timeControl = cachedTimeControlField.GetValue(avatarPreview);
            
            cachedStopTimeField.SetValue(timeControl, clip.length);
        }

        private AnimationClip GetCurrentClip(AnimatedCapability target)
        {
            if (animSpecifierIndex - 1 >= target.Animation.Clips.Length) animSpecifierIndex = 0;

            AnimationClip clip;

            if (animSpecifierIndex == 0)
            {
                clip = target?.Animation.fallbackClip;
                if (clip != null) return clip;
            }
            else
            {
                clip = target.Animation.Clips[animSpecifierIndex - 1].Clip;
                if (clip != null) return clip;
            }

            for (int i = 0; i < target.Animation.Clips.Length; i++)
            {
                clip = target.Animation.Clips[i].Clip;

                if (clip != null)
                {
                    animSpecifierIndex = i + 1;
                    return clip;
                }
            }

            return null;
        }

        private void CleanupPreviewEditor()
        {
            if (preview == null) return;

            Object.DestroyImmediate(preview);
            preview = null;
            animationClipId = 0;
        }

        private static void SourceAnimationClipEditorFields()
        {
            if (cachedAvatarPreviewField != null) return;

            cachedAvatarPreviewField = Type.GetType("UnityEditor.AnimationClipEditor, UnityEditor").GetField("m_AvatarPreview", BindingFlags.NonPublic | BindingFlags.Instance);
            cachedTimeControlField = Type.GetType("UnityEditor.AvatarPreview, UnityEditor").GetField("timeControl", BindingFlags.Public | BindingFlags.Instance);
            cachedStopTimeField = Type.GetType("UnityEditor.TimeControl, UnityEditor").GetField("stopTime", BindingFlags.Public | BindingFlags.Instance);
        }
	}
}