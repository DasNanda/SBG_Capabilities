using SBG.Capabilities;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SBG.Capabilities.Editor
{
	public class CapabilityDebugger : EditorWindow
	{
		private CapabilityController target;

		private readonly Color activeColor = new Color(.7f, 1f, .7f);
		private readonly Color inactiveColor = Color.white;
		private readonly Color activatedColor = Color.green;
		private readonly Color deactivatedColor = Color.red;

        private const float darkOffset = 0.1f;

		private const float stateChangeLinger = 0.25f;
		private const float controllerCheckInterval = 1f;

        private const string adhdPrefKey = "capdebug_adhd";
        private const string checkPrefKey = "capdebug_checkforcontroller";
        private const string collapsePrefKey = "capdebug_collapseCompounds";
        private Texture2D tabIcon;

        private bool adhdMode = false;
        private bool checkForControllerOnPlay = true;
        private bool collapseInactiveCompounds = true;
		private Vector2 scroll = Vector2.zero;

        private float checkTimerStart;

        [MenuItem("SBG/Debugging/Capability Debugger")]
		public static void ShowWindow()
		{
			GetWindow<CapabilityDebugger>("Capability Debugger");
		}

        private void OnEnable()
        {
			Selection.selectionChanged += UpdateSelection;

            tabIcon = Resources.Load<Texture2D>("Capabilities/ChildIcon");
            adhdMode = EditorPrefs.GetBool(adhdPrefKey, false);
            checkForControllerOnPlay = EditorPrefs.GetBool(checkPrefKey, true);
            collapseInactiveCompounds = EditorPrefs.GetBool(collapsePrefKey, true);
        }

        private void OnDisable()
        {
			Selection.selectionChanged -= UpdateSelection;

            EditorPrefs.SetBool(adhdPrefKey, adhdMode);
            EditorPrefs.SetBool(checkPrefKey, checkForControllerOnPlay);
            EditorPrefs.SetBool(collapsePrefKey, collapseInactiveCompounds);
        }

        private void UpdateSelection()
        {
			if (Selection.activeGameObject == null)
			{
				target = null;
				return;
			}

			target = Selection.activeGameObject.GetComponent<CapabilityController>();
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying && target == null && checkForControllerOnPlay)
            {
                if (Time.realtimeSinceStartup - checkTimerStart >= controllerCheckInterval)
                {
                    var controller = GameObject.FindFirstObjectByType<CapabilityController>();
                    if (controller != null) Selection.activeGameObject = controller.gameObject;
                    else checkTimerStart = Time.realtimeSinceStartup;
                }
            }

			Repaint();
        }

        private void OnGUI()
		{
            if (target == null)
			{
				EditorGUILayout.HelpBox("No CapabilityController selected", MessageType.Info);
                checkForControllerOnPlay = EditorGUILayout.Toggle("Find Target on Play", checkForControllerOnPlay);
                collapseInactiveCompounds = EditorGUILayout.Toggle("Collapse Inactive", collapseInactiveCompounds);
                adhdMode = EditorGUILayout.Toggle("ADHD Mode", adhdMode);
                EditorGUILayout.Space();

                if (GUILayout.Button("Find Controller", GUILayout.Height(40)))
				{
					var controller = GameObject.FindFirstObjectByType<CapabilityController>();
					if (controller != null) Selection.activeGameObject = controller.gameObject;
				}

                return;
			}

            EditorGUILayout.LabelField($"Capability Controller: {target.gameObject.name}", EditorStyles.boldLabel);
            checkForControllerOnPlay = EditorGUILayout.Toggle("Find Target on Play", checkForControllerOnPlay);
            collapseInactiveCompounds = EditorGUILayout.Toggle("Collapse Inactive", collapseInactiveCompounds);
            adhdMode = EditorGUILayout.Toggle("ADHD Mode", adhdMode);
            EditorGUILayout.Space();

            if (!EditorApplication.isPlaying)
			{
                EditorGUILayout.HelpBox("Capabilities are loaded at runtime", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Capabilities", EditorStyles.largeLabel);
            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);

            Color prev = GUI.color;
            Color prevBg = GUI.backgroundColor;
            TickGroup currentGroup = 0;

            var groups = target.TickGroups.Keys.ToArray();

			for (int g = 0; g < groups.Length; g++)
			{
                currentGroup = groups[g];
                var capabilities = target.TickGroups[currentGroup];

                GUI.color = Color.cyan;
                EditorGUILayout.LabelField($"[{currentGroup}]", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                for (int c = 0; c < capabilities.Count; c++)
                {
                    Capability capability = target.TickGroups[currentGroup][c];
                    DrawCapabilityLine(capability, c, 0);
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            GUI.color = prev;
            GUI.backgroundColor = prevBg;

            EditorGUILayout.LabelField($"Blocked Tags", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(50));

            if (target.TagBlocks != null && target.TagBlocks.Count > 0)
            {
                if (!adhdMode) GUI.backgroundColor = Color.red;
                foreach (var block in target.TagBlocks)
                {
                    string instigators = string.Empty;
                    for (int i = 0; i < block.Instigators.Count; i++)
                    {
                        if (i > 0) instigators += ", ";
                        instigators += block.Instigators[i].GetType().Name;
                    }

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.LabelField($"[{block.Tag}]", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"(Instigators: {instigators})");
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField($"None", EditorStyles.boldLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;

            GUI.backgroundColor = prevBg;

            EditorGUI.indentLevel--;
			EditorGUILayout.EndScrollView();
        }

        private void DrawCapabilityLine(Capability capability, int indexInGroup, int depth)
        {
            string tags;
            Color bgColor;

            if (Time.realtimeSinceStartup - capability.LastStateChangeTime < stateChangeLinger && !adhdMode)
            {
                GUI.color = capability.IsActive ? Color.white : Color.red;
                bgColor = capability.IsActive ? activatedColor : deactivatedColor;
            }
            else
            {
                GUI.color = capability.IsActive ? Color.white : Color.gray;
                bgColor = capability.IsActive ? activeColor : inactiveColor;
            }

            if (indexInGroup % 2 == 0)
            {
                Color.RGBToHSV(bgColor, out var h, out var s, out var v);
                v -= darkOffset;
                bgColor = Color.HSVToRGB(h, s, v);
            }

            tags = "Tags: ";
            for (int t = 0; t < capability.Tags.Length; t++)
            {
                if (t > 0) tags += ", ";
                tags += capability.Tags[t];
            }

            GUI.backgroundColor = bgColor;
            Rect hor = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (depth > 0)
            {
                EditorGUI.LabelField(hor, new GUIContent(tabIcon));
                EditorGUILayout.Space(16, false);
            }
            EditorGUILayout.LabelField(capability.DisplayName, EditorStyles.largeLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField(new GUIContent(tags, tags));
            EditorGUILayout.EndHorizontal();

            if (!capability.IsCompound || capability.Children == null) return;

            if (!capability.IsActive && collapseInactiveCompounds) return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < capability.Children.Count; i++)
            {
                Capability child = capability.Children[i];
                DrawCapabilityLine(child, i, depth+1);
            }
            EditorGUI.indentLevel--;
        }
    }
}