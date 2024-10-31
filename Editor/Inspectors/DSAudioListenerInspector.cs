namespace DarkSound.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using DarkSound;
	using FMODUnity;

	[CustomEditor(typeof(DSAudioListener))]
	public class DSAudioListenerInspector : UnityEditor.Editor
	{
		Texture logo;
		Vector2 scrollPosition;
		private List<Color> indexedColors = new List<Color>();

		public static string IconsFolder => "Assets/DarkSound/DSResources/";

		private void OnEnable()
		{
			EditorApplication.update += UpdateInspector;
		}

		private void OnDisable()
		{
			EditorApplication.update -= UpdateInspector;
		}

		private void UpdateInspector()
		{
			if (Application.isPlaying)
			{
				Repaint();
			}
		}

		bool listSources; 

		public override void OnInspectorGUI()
		{
			logo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconsFolder}Logo.png");

			GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.MaxWidth(200) };
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(logo, options);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			DrawDefaultInspector();
			DSAudioListener audioListener = (DSAudioListener)target;

			if (EditorApplication.isPlaying)
			{  
				GUILayout.Space(20);
				  
				GUILayout.BeginHorizontal();
				listSources = EditorGUILayout.Foldout(listSources, " ");
				GUILayout.Space(-400f); // Adjust spacing as needed
				GUILayout.Label("Audible Sources", EditorStyles.boldLabel);
				GUILayout.EndHorizontal();

				if (audioListener.audibleSources != null && audioListener.audibleSources.Count > 0)
				{
					// Calculate the total volume
					float totalVolume = 0;
					foreach (var entry in audioListener.audibleSources)
					{
						totalVolume += entry.Value.lastAudibleVolume;
					}

					// Generate consistent colors based on index
					GenerateIndexedColors(audioListener.audibleSources.Count);

					if (listSources)
					{ 
						DrawSourceInfo(totalVolume);
					}

					GUILayout.Space(20);

					DrawPieChart(totalVolume);
				}
				else
				{
					GUILayout.Label("No audible sources available.");
				}
			}
			else
			{ 
				GUILayout.Space(20);
				GUILayout.Label("Enter play mode to see audible sources.");
			}
		}

		private void DrawSourceInfo(float totalVolume)
		{
			DSAudioListener audioListener = (DSAudioListener)target;

			float maxHeight = Mathf.Clamp(audioListener.audibleSources.Count * 100f, 20f, 600f);
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(maxHeight));

			int index = 0;
			foreach (KeyValuePair<string, AudabilityData> entry in audioListener.audibleSources)
			{
				DSAudioSource source = DSAudioManager.GetSourceForID(entry.Key);
				float weight = totalVolume > 0 ? (entry.Value.lastAudibleVolume / totalVolume) * 100 : 0;

				Color sourceColor = indexedColors[index];
				EditorGUI.DrawRect(GUILayoutUtility.GetRect(10, 10), sourceColor);

				GUILayout.Label($"Source: {source.name}", EditorStyles.boldLabel);
				GUILayout.Label($"  Last Audible Time: {entry.Value.lastAudibleTime}");
				GUILayout.Label($"  Last Audible Volume: {entry.Value.lastAudibleVolume}");
				GUILayout.Label($"  Last Audible Low Pass: {entry.Value.lastAudibleLowPass}");
				GUILayout.Label($"  Volume Contribution: {weight:F2}%");

				GUILayout.Space(5);
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
				index++;
			}

			EditorGUILayout.EndScrollView();
		}

		private void GenerateIndexedColors(int count)
		{
			indexedColors.Clear();
			for (int i = 0; i < count; i++)
			{
				float hue = i / (float)count;
				indexedColors.Add(Color.HSVToRGB(hue, 0.8f, 0.8f)); // Use a consistent color pattern based on hue
			}
		}

		private void DrawPieChart(float totalVolume)
		{
			if (totalVolume <= 0) return;

			DSAudioListener audioListener = (DSAudioListener)target;

			Rect pieChartRect = GUILayoutUtility.GetRect(200, 200);
			Handles.BeginGUI();
			Vector2 center = pieChartRect.center;
			float radius = Mathf.Min(pieChartRect.width, pieChartRect.height) * 0.5f;
			float startAngle = 0;
			int index = 0;

			foreach (var entry in audioListener.audibleSources)
			{
				float sliceAngle = (entry.Value.lastAudibleVolume / totalVolume) * 360f;
				Color sliceColor = indexedColors[index];
				Handles.color = sliceColor;
				Handles.DrawSolidArc(center, Vector3.forward, Quaternion.Euler(0, 0, startAngle) * Vector2.up, sliceAngle, radius);

				startAngle += sliceAngle;
				index++;
			}

			startAngle = 0;
			index = 0;

			foreach (var entry in audioListener.audibleSources)
			{
				float sliceAngle = (entry.Value.lastAudibleVolume / totalVolume) * 360f;

				// Draw label if contribution is above a threshold (e.g., 5%)
				float contributionPercentage = (entry.Value.lastAudibleVolume / totalVolume) * 100;
				if (contributionPercentage >= 5f)
				{
					Vector2 labelPosition = center + (Vector2)(Quaternion.Euler(0, 0, startAngle + sliceAngle / 2) * Vector2.up * (radius * 0.7f));

					// Define and draw the background box for the label 
					Vector2 boxSize = new Vector2(100, 20); // Set box size (adjust as needed)
					Rect labelRect = new Rect(labelPosition.x - boxSize.x / 2, labelPosition.y - boxSize.y / 2, boxSize.x, boxSize.y);
					EditorGUI.DrawRect(labelRect, new Color(0.2f, 0.2f, 0.2f, 0.8f)); // Semi-transparent gray

					// Draw the label with text over the box
					GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
					{
						alignment = TextAnchor.MiddleCenter,
						fontSize = 10,
						normal = { textColor = Color.white }
					};


					DSAudioSource source = DSAudioManager.GetSourceForID(entry.Key);

					EditorGUI.LabelField(labelRect, $"{source.name} ({contributionPercentage:F1}%)", labelStyle);
				}

				startAngle += sliceAngle;
				index++;
			}

			Handles.EndGUI(); 
		}
	}
}
