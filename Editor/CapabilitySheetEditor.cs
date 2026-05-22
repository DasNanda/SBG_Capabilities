using SBG.Capabilites.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace SBG.Capabilities.Editor
{
	[CustomEditor(typeof(CapabilitySheet))]
	public class CapabilitySheetEditor : UnityEditor.Editor
	{
		private SerializedProperty capabilities;

        private CapabilitySheet sheet;
        private Type[] capabilityTypes;
        private GenericMenu addItemContextMenu;
        private GenericMenu addChildItemContextMenu;

        private Color fixedUpdateCol = Color.green;
        private Color updateCol = Color.cyan;
        private Color customEventCol = Color.yellow;

        private Vector2 scroll;

        private Capability selectedCapability;
        private UnityEditor.Editor selectedCapabilityEditor;
        private AnimatedCapabilityObjectPreview selectedPreview;

        private Texture2D tabIcon;
        private Texture2D downIcon;
        private Texture2D rightIcon;
        private Texture2D warningIcon;

        private const float DarkOffset = 0.1f;

        private void OnEnable()
        {
            tabIcon = Resources.Load<Texture2D>("Capabilities/ChildIcon");
            downIcon = EditorGUIUtility.IconContent("d_icon dropdown").image as Texture2D;
            rightIcon = EditorGUIUtility.IconContent("d_forward").image as Texture2D;
            warningIcon = EditorGUIUtility.IconContent("Warning").image as Texture2D;

            sheet = (CapabilitySheet)target;

            capabilityTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Capability)) && !type.IsAbstract)
                .ToArray();

            addItemContextMenu = new GenericMenu();
            for (int i = 0; i < capabilityTypes.Length; i++)
            {
                string name = ObjectNames.NicifyVariableName($"{capabilityTypes[i].BaseType.Name}/{capabilityTypes[i].Name}");
                GUIContent content = new GUIContent(name);
                int typeIndex = i;
                addItemContextMenu.AddItem(content, false, OnAddCapability, typeIndex);
            }

            capabilities = serializedObject.FindProperty(nameof(CapabilitySheet.Capablities));

            Undo.undoRedoPerformed += RefreshAssetState;

            RefreshAssetState();
            SortTickOrder();

            selectedCapability = null;
            ClearEditor();
        }

        private void PopulateAddChildContextMenu(Capability parent)
        {
            addChildItemContextMenu = new GenericMenu();
            for (int i = 0; i < capabilityTypes.Length; i++)
            {
                var instance = ScriptableObject.CreateInstance(capabilityTypes[i]) as Capability;
                bool mismatch = instance.TickGroup != parent.TickGroup;
                DestroyImmediate(instance);
                if (mismatch) continue;

                string name = ObjectNames.NicifyVariableName($"{capabilityTypes[i].BaseType.Name}/{capabilityTypes[i].Name}");
                GUIContent content = new GUIContent(name);
                int typeIndex = i;
                addChildItemContextMenu.AddItem(content, false, OnAddChildCapability, typeIndex);
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RefreshAssetState;

            ClearEditor();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Find Sheet In Hierarchy")) EditorGUIUtility.PingObject(sheet);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Capabilities", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            TickGroup currentGroup = 0;
            Color col = GUI.color;
            Color bgCol = GUI.backgroundColor;
            Color groupCol = GetTickGroupCol(currentGroup);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (capabilities.arraySize <= 0)
            {
                EditorGUILayout.LabelField("None");
            }

            for (int i = 0; i < capabilities.arraySize; i++)
            {
                var cap = capabilities.GetArrayElementAtIndex(i).objectReferenceValue as Capability;

                if (cap == null)
                {
                    Debug.LogError("Null Element found!");
                    continue;
                }

                if (i == 0 || cap.TickGroup != currentGroup)
                {
                    if (i > 0) EditorGUILayout.Space();
                    currentGroup = cap.TickGroup;
                    groupCol = GetTickGroupCol(currentGroup);
                    GUI.color = groupCol;
                    EditorGUILayout.LabelField($"[{currentGroup}]", EditorStyles.boldLabel);
                    GUI.color = col;
                }

                DrawCapabilityEntry(cap, groupCol, i, 0);
                GUI.backgroundColor = bgCol;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Capabilities: {capabilities.arraySize}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add", EditorStyles.toolbarDropDown, GUILayout.Width(75)))
            {
                addItemContextMenu.ShowAsContext();
            }
            if (selectedCapability != null && selectedCapability.IsCompound &&
                GUILayout.Button("Add Child", EditorStyles.toolbarDropDown, GUILayout.Width(75)))
            {
                addChildItemContextMenu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Separator();

            if (selectedCapabilityEditor != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(selectedCapability.DisplayName, EditorStyles.boldLabel);
                if (GUILayout.Button("Find In Hierarchy")) EditorGUIUtility.PingObject(selectedCapability);
                EditorGUILayout.EndHorizontal();
                string warning = CheckForAnimEventWarning(selectedCapability);
                if (!string.IsNullOrEmpty(warning)) EditorGUILayout.HelpBox(warning, MessageType.Warning);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                selectedCapabilityEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawCapabilityEntry(Capability cap, Color color, int index, int depth)
        {
            bool showWarningIcon = !ValidateEventDrivenCapability(cap);

            Color.RGBToHSV(color, out float h, out float s, out float v);

            if (cap == selectedCapability) v += 0.75f; // Apparently hsv can go beyond 1, not sure why

            if (index % 2 == 0) v -= DarkOffset;

            EditorGUILayout.BeginHorizontal();
            if (cap.IsCompound)
            {
                GUI.backgroundColor = Color.HSVToRGB(h, 0.4f, v-0.1f);
                GUIContent iconContent = new GUIContent(cap.IsExpanded ? downIcon : rightIcon);
                if (GUILayout.Button(iconContent, EditorStyles.toolbarButton, GUILayout.Width(30))) cap.IsExpanded = !cap.IsExpanded;
            }

            GUI.backgroundColor = Color.HSVToRGB(h, s, v);

            using (var hor = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUI.Button(hor.rect, GUIContent.none, EditorStyles.toolbarButton))
                {
                    ClearEditor();

                    if (selectedCapability != cap)
                    {
                        selectedCapability = cap;
                        selectedCapabilityEditor = CreateEditor(cap);
                        selectedPreview = new();
                        selectedPreview.Initialize(new[] { selectedCapability });

                        if (selectedCapability.IsCompound) PopulateAddChildContextMenu(selectedCapability);
                    }
                    else
                    {
                        selectedCapability = null;
                        selectedCapabilityEditor = null;
                        selectedPreview = null;
                    }
                }

                Rect r = hor.rect;

                if (!cap.IsCompound)
                {
                    r.x += 30;
                    r.width -= 30;
                    EditorGUILayout.Space(30, false);
                }

                if (depth > 0)
                {
                    EditorGUI.LabelField(r, new GUIContent(tabIcon));
                    EditorGUILayout.Space(16, false);
                }

                EditorGUILayout.LabelField(cap.DisplayName);

                GUIStyle rightAligned = new GUIStyle(EditorStyles.label);
                rightAligned.alignment = TextAnchor.MiddleRight;

                if (showWarningIcon) EditorGUILayout.LabelField(new GUIContent(warningIcon), rightAligned, GUILayout.Width(40));
                EditorGUILayout.LabelField($"{cap.TickOrder:000}", rightAligned, GUILayout.Width(75));

                EditorGUILayout.Space(20, false);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                if (cap.Parent == null) RemoveElement(index);
                else RemoveChild(cap);
                EditorGUILayout.EndHorizontal();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (!cap.IsCompound) return;
            if (cap.Children == null || !cap.IsExpanded) return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < cap.Children.Count; i++)
            {
                DrawCapabilityEntry(cap.Children[i], color, i, depth+1);
            }
            EditorGUI.indentLevel--;
        }

        public override bool HasPreviewGUI() => selectedPreview?.HasPreviewGUI() ?? false;
        public override GUIContent GetPreviewTitle() => selectedPreview?.GetPreviewTitle() ?? GUIContent.none;
        public override void OnPreviewSettings() => selectedPreview?.OnPreviewSettings();
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background) => selectedPreview?.OnInteractivePreviewGUI(r, background);

        private void ClearEditor()
        {
            if (selectedCapabilityEditor != null) DestroyImmediate(selectedCapabilityEditor);
            if (selectedPreview != null) selectedPreview.Cleanup();
        }

        private Color GetTickGroupCol(TickGroup group)
        {
            return group switch
            {
                TickGroup.Update => updateCol,
                TickGroup.FixedUpdate => fixedUpdateCol,
                TickGroup.CustomEvent => customEventCol,
                _ => Color.white,
            };
        }

        private void RemoveElement(int index)
        {
            capabilities.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            RefreshAssetState();
        }

        private void RemoveChild(Capability child)
        {
            if (child.Parent == null) return;
            child.Parent.Children.Remove(child);
            serializedObject.ApplyModifiedProperties();
            RefreshAssetState();
        }

        private void OnAddCapability(object target)
        {
            Type t = capabilityTypes[(int)target];
            Capability c = (Capability)ScriptableObject.CreateInstance(t);
            c.name = t.Name;
            AddCapabilitySO(c);
            AddCapabilityToList(c);
        }

        private void OnAddChildCapability(object target)
        {
            Type t = capabilityTypes[(int)target];
            Capability c = (Capability)ScriptableObject.CreateInstance(t);
            c.name = $"{selectedCapability.name}_{t.Name}";
            AddCapabilitySO(c);

            Capability parent = selectedCapability;

            if (parent.Children == null) parent.Children = new();
            parent.Children.Add(c);
            parent.Children.Sort((a, b) => a.CompareTo(b));
            c.Parent = parent;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(this);
        }

        private void AddCapabilitySO(Capability capability)
        {
            string sheetAssetPath = AssetDatabase.GetAssetPath(sheet);
            var capAssets = AssetDatabase.LoadAllAssetsAtPath(sheetAssetPath);

            if (capAssets != null &&
                capAssets.Any(c => c.name == capability.name && c.GetType() == capability.GetType()))
            {
                Debug.LogError($"Duplicate Capability: {capability.DisplayName}");
                return;
            }

            AssetDatabase.AddObjectToAsset(capability, sheet);
            EditorUtility.SetDirty(capability);
            AssetDatabase.SaveAssets();
        }

        private void AddCapabilityToList(Capability capability)
        {
            if (sheet.Capablities != null &&
                sheet.Capablities.Any(c => c.name == capability.name && c.GetType() == capability.GetType()))
            {
                Debug.LogError($"Duplicate Capability: {capability.DisplayName}");
                return;
            }

            capabilities.InsertArrayElementAtIndex(0);
            SerializedProperty element = capabilities.GetArrayElementAtIndex(0);
            element.objectReferenceValue = capability;

            serializedObject.ApplyModifiedProperties();

            SortTickOrder();

            EditorUtility.SetDirty(this);
        }

        private void SortTickOrder()
        {
            serializedObject.Update();

            if (sheet.Capablities == null) return;

            List<Capability> capList = sheet.Capablities.ToList();

            capList.Sort((a,b) => a.CompareTo(b));

            sheet.Capablities = capList.ToArray();

            serializedObject.ApplyModifiedProperties();
        }

        private bool ValidateEventDrivenCapability(Capability c)
        {
            if (c == null) return true;
            var animCap = c as AnimatedCapability;
            if (animCap == null || !animCap.DrivenByAnimationEvent) return true;

            var clips = animCap.Animation.Clips.ToList();
            clips.Add(new() { Clip = animCap.Animation.fallbackClip, SpecifierId = "fallback" });

            foreach (var clipEntry in clips)
            {
                if (clipEntry.Clip == null) return false;
                if (clipEntry.Clip.events == null || clipEntry.Clip.events.Length < 1) return false;
            }

            return true;
        }

        private string CheckForAnimEventWarning(Capability c)
        {
            if (c == null) return null;
            var animCap = c as AnimatedCapability;
            if (animCap == null || !animCap.DrivenByAnimationEvent) return null;

            var clips = animCap.Animation.Clips.ToList();
            clips.Add(new() { Clip = animCap.Animation.fallbackClip, SpecifierId = "fallback" });

            foreach (var clipEntry in clips)
            {
                if (clipEntry.Clip == null)
                {
                    return $"Capability is Animation-Event driven but not all clips are assigned.";
                }

                if (clipEntry.Clip.events == null || clipEntry.Clip.events.Length < 1)
                {
                    return $"Capability is Animation-Event driven, but Animation Clip for specifier '{clipEntry.SpecifierId}' does not contain events";
                }
            }

            return null;
        }

        private void RefreshAssetState()
        {
            serializedObject.Update();

            string sheetAssetPath = AssetDatabase.GetAssetPath(sheet);
            var capAssets = AssetDatabase.LoadAllAssetsAtPath(sheetAssetPath);

            if (capAssets == null || sheet.Capablities== null) return;

            for (int i = 0; i < sheet.Capablities.Length; i++)
            {
                if (!capAssets.Contains(sheet.Capablities[i]))
                {
                    AddCapabilitySO(sheet.Capablities[i]);
                }
            }

            for (int i = capAssets.Length - 1; i >= 0; i--)
            {
                if (capAssets[i] is not Capability) continue;
                Capability c = capAssets[i] as Capability;

                if (sheet.Capablities.Contains(c)) continue;

                if (c.Parent == null || !c.Parent.Children.Contains(capAssets[i]))
                {
                    AssetDatabase.RemoveObjectFromAsset(capAssets[i]);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}