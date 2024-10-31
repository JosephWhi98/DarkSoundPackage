namespace DarkSound.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using DarkSound;
	using UnityEditor.IMGUI.Controls;
	using UnityEngine.UIElements;
	using System.IO;

	[CustomEditor(typeof(DSAudioSource))]
	public class DSAudioSourceInspector : Editor
	{
		public static string IconsFolder
		{
			get
			{
				//if (Directory.Exists("Assets/DarkSound/DSResources/"))
				//{
				//	return "Assets/DarkSound/DSResources/";
				//}

				return "Assets/DarkSound/DSResources/";
			}
		}

		private Texture logo;

		private DSAudioSource audioSource;

		//Gizmo Controls.
		private bool drawDirectivity;

		public Color innerAngleColor; 
		public Color outerAngleColor;

		public float InnerAngle
		{
			get
			{
				return audioSource.innerAngle;
			}
			set
			{
				if (audioSource.innerAngle != value)
				{
					audioSource.innerAngle = value;

					if (DrawDirectivity)
						SceneView.RepaintAll();
				}
			}
		}

		public float OuterAngle
		{
			get
			{
				return audioSource.outerAngle;
			}
			set
			{
				if (audioSource.outerAngle != value)
				{
					audioSource.outerAngle = value;

					if (DrawDirectivity)
						SceneView.RepaintAll();
				}
			}
		}

		public bool DrawDirectivity
		{
			get
			{
				return drawDirectivity;
			}
			set
			{
				if (drawDirectivity != value)
				{
					drawDirectivity = value;

					SceneView.RepaintAll();
				}
			}
		}


		public override void OnInspectorGUI()
		{
			audioSource = (DSAudioSource)target;

			innerAngleColor = new Color(0.165f, 0.6f, 0f, 0.5f);
			outerAngleColor = new Color(0.165f, 0.6f, 1f, 0.5f);

			// Load and display the logo
			logo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconsFolder}Logo.png");
			DisplayLogo();

			GUILayout.Space(20);
			DrawDefaultInspector(); // Draw the default inspector fields

			GUILayout.Space(20); // Space before the attenuation section 
			DrawAttenuationSection();

			GUILayout.Space(20); // Space before the directivity section
			DrawDirectivitySection();
		}

		private void DisplayLogo()
		{
			if (logo != null)
			{
				GUILayoutOption[] options = new GUILayoutOption[]
				{
					GUILayout.MaxWidth(200),
					GUILayout.MaxHeight(60)
				};

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(logo, options);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
			}
		}

		private void DrawDirectivitySection()
		{
			// Create a box with a subheading for the directivity section
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Directivity Settings", EditorStyles.boldLabel); // Subheading

			// Start horizontal layout for graph and sliders
			EditorGUILayout.BeginHorizontal();
			DrawDirectivityGraph();
			GUILayout.Space(10);
			DrawDirectivitySliders(audioSource);
			EditorGUILayout.EndHorizontal(); // End the horizontal layout (graph + sliders)

			EditorGUILayout.EndVertical(); // End the directivity section box
		}

		private void DrawDirectivitySliders(DSAudioSource audioSource)
		{
			// Draw the sliders for inner and outer angles in a vertical layout
			EditorGUILayout.BeginVertical();

			// Draw and handle the outer angle  
			EditorGUI.BeginChangeCheck();

			DrawDirectivity = EditorGUILayout.Toggle("Draw Directivity Gizmos:", DrawDirectivity);

			float innerAngle;
			float outerAngle; 

			EditorGUILayout.LabelField("Inner Angle");
			innerAngle = EditorGUILayout.Slider(InnerAngle, 0f, 360f);

			EditorGUILayout.LabelField("Outer Angle");
			outerAngle = EditorGUILayout.Slider(OuterAngle, 0f, 360f);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(audioSource, "Change Angles");

				if (outerAngle != OuterAngle)
				{
					if (outerAngle < innerAngle)
						innerAngle = outerAngle;
				}
				else if (innerAngle != InnerAngle)
				{
					if (innerAngle > outerAngle)
						outerAngle = innerAngle;
				}


				InnerAngle = innerAngle;
				OuterAngle = outerAngle; 
			}

			EditorGUILayout.EndVertical(); // End the sliders vertical layout
		}

		private void DrawDirectivityGraph()
		{
			EditorGUILayout.BeginVertical("box");
			Rect graphRect = GUILayoutUtility.GetRect(200, 200, GUILayout.Width(200), GUILayout.Height(200));
			Handles.BeginGUI();

			DrawGraphBackground(graphRect, true);

			// Center point and radius for the graph
			Vector2 center = new Vector2(graphRect.x + graphRect.width / 2, graphRect.y + graphRect.height / 2);
			float radius = Mathf.Min(graphRect.width, graphRect.height) / 2 - 10;

			// Draw thin wireframe grid (concentric circles)
			Handles.color = Color.gray;
			for (int i = 1; i <= 4; i++)
			{
				Handles.DrawWireDisc(center, Vector3.forward, (radius / 4) * i);
			}

			// Draw thin wireframe grid (spokes/radial lines)
			int radialSegments = 8;
			for (int i = 0; i < radialSegments; i++)
			{
				float angle = (360f / radialSegments) * i;
				Vector2 point = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
				Handles.DrawLine(center, point);
			}

			// Draw thick X and Y axes
			Handles.color = Color.white * 0.5f;
			Handles.DrawLine(new Vector3(graphRect.x, center.y), new Vector3(graphRect.x + graphRect.width, center.y), 1); // X-axis (horizontal)
			Handles.DrawLine(new Vector3(center.x, graphRect.y), new Vector3(center.x, graphRect.y + graphRect.height), 1); // Y-axis (vertical)

			// Draw the angle segment based on inner and outer angles
			DrawAngleSegment(center, radius, InnerAngle, OuterAngle);

			Handles.EndGUI();
			EditorGUILayout.EndVertical(); // End the graph box
		}


		private void DrawGraphBackground(Rect graphRect, bool drawGrid)
		{
			// Draw a gray background
			Color backgroundColor = Color.gray * 0.3f;
			backgroundColor.a = 1f;
			EditorGUI.DrawRect(graphRect, backgroundColor);

			if (drawGrid)
			{
				// Draw the thin grid of small squares
				DrawSquareGrid(graphRect, 20); // Adjust size of squares by changing the second argument
			}
		}

		private void DrawSquareGrid(Rect graphRect, float squareSize)
		{
			Handles.color = Color.white * 0.3f; // Light gray for the grid

			// Draw vertical lines
			for (float x = graphRect.x; x < graphRect.xMax; x += squareSize)
			{
				Handles.DrawLine(new Vector3(x, graphRect.y), new Vector3(x, graphRect.yMax));
			}

			// Draw horizontal lines
			for (float y = graphRect.y; y < graphRect.yMax; y += squareSize)
			{
				Handles.DrawLine(new Vector3(graphRect.x, y), new Vector3(graphRect.xMax, y));
			}
		}

		private void DrawBoundaryCircle(Rect graphRect)
		{
			Vector2 center = graphRect.center;
			float radius = Mathf.Min(graphRect.width, graphRect.height) / 2 - 10;
			Handles.color = Color.gray;
			Handles.DrawWireDisc(center, Vector3.forward, radius);
		}

		private void DrawAngleSegment(Vector2 center, float radius, float innerAngle, float outerAngle)
		{
			int segments = 400; // Number of segments for smoothness
			float halfInnerAngle = innerAngle / 2;
			float halfOuterAngle = outerAngle / 2;

			// Draw the outer angle segments in blue
			Handles.color = innerAngleColor;
			DrawSegments(center, radius, halfOuterAngle, outerAngle, segments);

			// Draw the inner angle segments in green
			Handles.color = outerAngleColor;
			DrawSegments(center, radius, halfInnerAngle, innerAngle, segments);
		}

		private void DrawSegments(Vector2 center, float radius, float halfAngle, float angle, int segments)
		{
			for (int i = 0; i < segments; i++)
			{
				float angleStart = -halfAngle + (i * (angle / segments));
				float angleEnd = angleStart + (angle / segments);

				Vector2 point1 = center + new Vector2(Mathf.Cos(angleStart * Mathf.Deg2Rad), Mathf.Sin(angleStart * Mathf.Deg2Rad)) * radius;
				Vector2 point2 = center + new Vector2(Mathf.Cos(angleEnd * Mathf.Deg2Rad), Mathf.Sin(angleEnd * Mathf.Deg2Rad)) * radius;

				Handles.DrawLine(center, point1);
				Handles.DrawLine(center, point2);
			}
		}


		private void DrawAttenuationSection()
		{
			// Create a box with a subheading for the attenuation section
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Audio Falloff", EditorStyles.boldLabel); // Subheading

			// Start horizontal layout for graph and sliders
			EditorGUILayout.BeginHorizontal();
			DrawAttenuationGraph();
			EditorGUILayout.EndHorizontal(); // End the horizontal layout (graph + sliders)

			EditorGUILayout.EndVertical(); // End the attenuation section box

		}

		private void DrawAttenuationGraph()
		{
			// Rect for the graph taking full width of the inspector
			Rect graphRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true), GUILayout.Height(150));
			Handles.BeginGUI();

			DrawGraphBackground(graphRect, false);

			// Draw the linear grid for distance and attenuation
			DrawLinearGrid(graphRect, 0, audioSource.maxDistance);

			// Define graph boundaries
			Vector2 graphOrigin = new Vector2(graphRect.x + 40, graphRect.y + graphRect.height - 20); // Bottom-left corner origin
			float graphWidth = graphRect.width - 60; // Width minus padding
			float graphHeight = graphRect.height - 40; // Height minus padding

			// Number of points for the curve
			int numPoints = 100;
			Vector3[] curvePoints = new Vector3[numPoints];

			// Compute curve points for the attenuation graph
			ComputeCurvePoints(audioSource, numPoints, graphOrigin, graphWidth, graphHeight, curvePoints);

			// Draw the curve
			Handles.color = Color.green;
			Handles.DrawAAPolyLine(2.0f, curvePoints);

			// Draw labels, markers for 0, minDistance, and maxDistance
			DrawGraphLabels(graphRect, graphOrigin, graphWidth, graphHeight, audioSource.minDistance, audioSource.maxDistance, audioSource);

			Handles.EndGUI();
		}

		private void ComputeCurvePoints(DSAudioSource audioSource, int numPoints, Vector2 graphOrigin, float graphWidth, float graphHeight, Vector3[] curvePoints)
		{
			float maxDistance = audioSource.maxDistance;
			float minDistance = audioSource.minDistance;

			for (int i = 0; i < numPoints; i++)
			{
				// Interpolate distance linearly between 0 and maxDistance
				float t = (float)i / (numPoints - 1);
				float distance = Mathf.Lerp(0, maxDistance, t); // Start from 0 distance

				// Get the attenuation value (only apply between minDistance and maxDistance)
				float attenuation = distance < minDistance ? 1.0f : audioSource.CalculateFallOffVolume(distance);

				// Map distance to the X-axis and attenuation to the Y-axis (Linear)
				float x = Mathf.Lerp(graphOrigin.x, graphOrigin.x + graphWidth, t); // Linear X-axis from 0 to maxDistance
				float y = Mathf.Lerp(graphOrigin.y, graphOrigin.y - graphHeight, attenuation); // Linear Y-axis (attenuation between 0 and 1

				curvePoints[i] = new Vector3(x, y, 0);
			}
		}

		private void DrawLinearGrid(Rect graphRect, float startDistance, float maxDistance)
		{
			// Draw linear grid and labels for distance and attenuation
			int numVerticalLines = 5; // Grid lines for distance
			int numHorizontalLines = 5; // Grid lines for attenuation

			Handles.color = Color.gray * 0.5f;

			// Vertical lines (Distance)
			for (int i = 0; i < numVerticalLines; i++)
			{
				float t = (float)i / (numVerticalLines - 1);
				float x = Mathf.Lerp(graphRect.x + 40, graphRect.xMax - 20, t); // Start from graph origin
				Handles.DrawLine(new Vector3(x, graphRect.y + 20), new Vector3(x, graphRect.yMax - 20));
			}

			// Horizontal lines (Attenuation)
			for (int i = 0; i < numHorizontalLines; i++)
			{
				float t = (float)i / (numHorizontalLines - 1);
				float y = Mathf.Lerp(graphRect.yMax - 20, graphRect.y + 20, t); // Inverted for graphical representation
				Handles.DrawLine(new Vector3(graphRect.x + 40, y), new Vector3(graphRect.xMax - 20, y));
			}
		}

		private void DrawGraphLabels(Rect graphRect, Vector2 graphOrigin, float graphWidth, float graphHeight, float minDistance, float maxDistance, DSAudioSource audioSource)
		{
			GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
			float labelOffset = 5;

			// X-Axis Labels for Distance
			for (int i = 0; i <= 4; i++)
			{
				float t = (float)i / 4;
				float x = Mathf.Lerp(graphOrigin.x + 40, graphOrigin.x + graphWidth + 40, t);
				float labelDistance = Mathf.Lerp(0, maxDistance, t);
				GUI.Label(new Rect(x - labelOffset, graphOrigin.y + 1, 50, 20), labelDistance.ToString("F1"), labelStyle);
			}

			// Y-Axis Labels for Attenuation
			for (int i = 0; i <= 4; i++)
			{
				float t = (float)i / 4;
				float y = Mathf.Lerp(graphOrigin.y - labelOffset, graphOrigin.y - graphHeight - labelOffset, t);
				float labelValue = Mathf.Lerp(0, 1, t); // Assuming attenuation is between 0 and 1
				GUI.Label(new Rect(graphOrigin.x - 40, y - labelOffset, 50, 20), labelValue.ToString("F2"), labelStyle);
			}
		}

		void OnSceneGUI() 
		{
			if (audioSource != null)
			{
				Color tempColor = Handles.color;

				if (audioSource.enabled)
					Handles.color = new Color(0.50f, 0.70f, 1.00f, 0.5f);
				else
					Handles.color = new Color(0.30f, 0.40f, 0.60f, 0.5f);

				Vector3 position = audioSource.ActualPosition;

				// Draw and handle min/max distance
				EditorGUI.BeginChangeCheck();
				float minDistance = Handles.RadiusHandle(audioSource.transform.rotation, position, audioSource.minDistance, false);
				float maxDistance = Handles.RadiusHandle(audioSource.transform.rotation, position, audioSource.maxDistance, false);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(audioSource, "DSAudioSource Distance");
					audioSource.minDistance = minDistance;
					audioSource.maxDistance = maxDistance;
				}

				if (drawDirectivity)
				{
					// Draw inner and outer angle segments
					DrawAngleSegments(position, audioSource.transform.rotation, audioSource.minDistance, ref audioSource.innerAngle, ref audioSource.outerAngle);
				}

				Handles.color = tempColor;
			}
		}

		private void DrawAngleSegments(Vector3 position, Quaternion rotation, float radius, ref float innerAngle, ref float outerAngle)
		{
			// Draw the outer angle segment
			DrawSphereSegments(position, rotation, radius, innerAngle, outerAngle, outerAngleColor, innerAngleColor, 0.5f);

			// Draw handles for inner and outer angles
			DrawAngleHandles(position, rotation, radius, ref innerAngle, ref outerAngle);
		} 

		Vector3 innerHandlePos;
		Vector3 outerHandlePos;

		private void DrawAngleHandles(Vector3 position, Quaternion rotation, float radius, ref float innerAngle, ref float outerAngle)
		{
			// Calculate handle positions based on current angles

			// Draw and handle the inner angle
			EditorGUI.BeginChangeCheck();

			Handles.color = outerAngleColor;

			float cameraDistance = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, position);

			// Draw and handle the outer angle  
			EditorGUI.BeginChangeCheck();

			Handles.color = innerAngleColor;

			var outerAngleHandle = Quaternion.identity; outerHandlePos = Handles.FreeMoveHandle(outerHandlePos, Quaternion.identity, 0.01f * cameraDistance, outerHandlePos, Handles.CubeHandleCap);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(audioSource, "Change Outer Angle");

				// Calculate the new angle based on the handle's new position
				Vector3 direction = outerHandlePos - position; // Direction from center to handle
				OuterAngle = Vector3.Angle(audioSource.transform.forward, direction) * 2; // Calculate the new angle
			}


			var innerAngleHandleHandle = Quaternion.identity; innerHandlePos = Handles.FreeMoveHandle(innerHandlePos, Quaternion.identity, 0.01f * cameraDistance, innerHandlePos, Handles.CubeHandleCap);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(audioSource, "Change Inner Angle");

				// Calculate the new angle based on the handle's new position
				Vector3 direction = innerHandlePos - position; // Direction from center to handle
				InnerAngle = Vector3.Angle(audioSource.transform.forward, direction) * 2; // Calculate the new angle
			}

			
			innerHandlePos = position + rotation * Quaternion.Euler(0, innerAngle / 2f, 0) * Vector3.forward * radius;
			outerHandlePos = position + rotation * Quaternion.Euler(0, outerAngle / 2f, 0) * Vector3.forward * radius;
		}


		private void DrawSphereSegments(Vector3 position, Quaternion rotation, float radius, float innerAngle, float outerAngle, Color outerColor, Color innerColor, float alpha)
		{
			int segments = 360; // Number of segments to draw the sphere segment
			float angleStep = outerAngle / segments;

			// Draw the outer arc for the sphere segment
			for (int i = 0; i <= segments; i++)
			{
				float currentAngle = -outerAngle / 2f + i * angleStep;
				Vector3 startPoint = position + rotation * Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * radius * 0.2f;
				Vector3 endPoint = position + rotation * Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * radius;

				if (Mathf.Abs(currentAngle) < innerAngle - (innerAngle / 2f) || Mathf.Abs(currentAngle) > 360 - (innerAngle / 2f))
				{
					Handles.color = outerColor;
				}
				else
				{
					Handles.color = innerColor;
				}

				// Draw the line from the center of the sphere to the edge
				Handles.DrawLine(startPoint, endPoint, 5f);
			}
		}


	}

}