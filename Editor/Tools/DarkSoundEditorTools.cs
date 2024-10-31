using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DarkSound;

public class DarkSoundEditorTools : EditorWindow
{
    [MenuItem("GameObject/Audio/DarkSound/DSAudioSource")]
    private static void CreateDarkSoundAudioSource()
    {
        GameObject audioSource = new GameObject("DSAudioSource");
        audioSource.AddComponent<DSAudioSource>();
    }

    [MenuItem("GameObject/Audio/DarkSound/DSRoom")]
    private static void CreateDarkSoundAudioRoom()
    {
        GameObject audioSource = new GameObject("DSRoom");
        audioSource.AddComponent<DSRoom>();
    }

    [MenuItem("GameObject/Audio/DarkSound/DSPortal")]
    private static void CreateDarkSoundAudioPortal() 
    {
        GameObject audioSource = new GameObject("DSPortal");
        audioSource.AddComponent<DSPortal>();
    }

	// Direction for snapping
	public enum SnapDirection { X, Y, Z } 

	//[MenuItem("DarkSound/Snap Colliders")]
	public static void SnapColliders()
	{
		SnapDirection snapDirection = SnapDirection.X;
		Snap(snapDirection);

		snapDirection = SnapDirection.Y; 
		Snap(snapDirection);

		snapDirection = SnapDirection.Z;
		Snap(snapDirection);
	}


	public static void Snap(SnapDirection snapDirection)
	{
		// Get all BoxColliders in selection
		List<BoxCollider> colliderList = new List<BoxCollider>();

		foreach (GameObject gameObject in Selection.gameObjects)
		{
			BoxCollider[] boxColliders = gameObject.GetComponentsInChildren<BoxCollider>();

			foreach (BoxCollider col in boxColliders)
			{ 
				if (!colliderList.Contains(col))
					colliderList.Add(col);
			}
		}

		colliderList.Sort((a, b) => GetPositionInDirection(a, snapDirection).CompareTo(GetPositionInDirection(b, snapDirection)));

		// Snap colliders based on sorted order
		for (int i = 1; i < colliderList.Count; i++)
		{
			BoxCollider previous = colliderList[i - 1];
			BoxCollider current = colliderList[i];

			// Calculate the target position to snap to
			Vector3 snapPosition = current.transform.position;
			float previousMax = GetBoundsMaxInDirection(previous, snapDirection); 
			float currentMin = GetBoundsMinInDirection(current, snapDirection);

			float offset = previousMax - currentMin;

			// Move the current collider to snap with the previous one
			snapPosition += GetDirectionVector(snapDirection) * offset;
			current.transform.position = snapPosition;
		}
	}

	// Helper function to get the max bounds in the chosen direction
	private static float GetBoundsMaxInDirection(BoxCollider collider, SnapDirection snapDirection)
	{
		Bounds bounds = collider.bounds;
		switch (snapDirection)
		{
			case SnapDirection.X: return bounds.max.x;
			case SnapDirection.Y: return bounds.max.y;
			case SnapDirection.Z: return bounds.max.z;
			default: return 0f;
		}
	}

	// Helper function to get the min bounds in the chosen direction
	private static float GetBoundsMinInDirection(BoxCollider collider, SnapDirection snapDirection)
	{
		Bounds bounds = collider.bounds;
		switch (snapDirection)
		{
			case SnapDirection.X: return bounds.min.x;
			case SnapDirection.Y: return bounds.min.y;
			case SnapDirection.Z: return bounds.min.z;
			default: return 0f;
		}
	}

	// Helper function to get the center position in the chosen direction
	private static float GetPositionInDirection(BoxCollider collider, SnapDirection snapDirection)
	{
		switch (snapDirection)
		{
			case SnapDirection.X: return collider.transform.position.x;
			case SnapDirection.Y: return collider.transform.position.y;
			case SnapDirection.Z: return collider.transform.position.z;
			default: return 0f;
		}
	}

	// Helper function to get the direction vector for the chosen axis
	private static Vector3 GetDirectionVector(SnapDirection snapDirection)
	{
		switch (snapDirection)
		{
			case SnapDirection.X: return Vector3.right;
			case SnapDirection.Y: return Vector3.up;
			case SnapDirection.Z: return Vector3.forward;
			default: return Vector3.zero;
		}
	}
}
