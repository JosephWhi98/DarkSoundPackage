using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using nn.fs;
using static DarkSound.DSRoom;
using System.IO;
using nn.oe;
using System;


namespace DarkSound
{
	public class AudabilityData
	{
		public float lastAudibleTime;
		public float lastAudibleVolume;
		public float lastAudibleLowPass;

		public AudabilityData(float lastAudibleTime, float lastAudibleVolume, float lastAudibleLowPass)
		{
			this.lastAudibleTime = lastAudibleTime;
			this.lastAudibleVolume = lastAudibleVolume;
			this.lastAudibleLowPass = lastAudibleLowPass;
		}

		public void UpdateValues(float volume, float lowPass)
		{
			lastAudibleTime = volume > 0.01f ? Time.time : lastAudibleTime;
			lastAudibleVolume = volume;
			lastAudibleLowPass = lowPass;
		}
	}

	public static class DSAudioManager
	{ 
		public static DSAudioListener PrimaryListener
		{
			get { return primaryListener; }
			set { primaryListener = value;  }
		}

		private static DSAudioListener primaryListener;

		private static List<DSAudioListener> allDSListeners;
		private static List<DSAudioSource> allDSSources;
		private static List<DSRoom> allDSRooms;
		private static List<DSPortal> allDSPortals;


		/// <summary>
		/// Update all propagation for a specified DSAudioListener. 
		/// </summary>
		/// <param name="audioListener">The listener to update sources for.</param>
		public static void UpdateSourcesForListener(DSAudioListener audioListener)
		{
			foreach (DSAudioSource audioSource in allDSSources)
			{
				if (audioSource.audibleToSecondaryListeners || audioListener == PrimaryListener)
				{
					if (!audioListener.audibleSources.TryGetValue(audioSource.GUID, out AudabilityData data))
					{
						data = new AudabilityData(0, 0, 0);
						audioListener.audibleSources.Add(audioSource.GUID, data);
					}

					audioSource.UpdatePropagation(audioListener, ref data);
				}
			}
		}

		/// <summary>
		/// Adds a listener to the list of all listeners
		/// </summary>
		/// <param name="listener">The listener to add</param>
		public static void AddListener(DSAudioListener listener)
		{
			if (allDSListeners == null)
			{
				allDSListeners = new List<DSAudioListener>();
			}

			allDSListeners.Add(listener);

			if (listener.isPrimaryListener)
			{
				if (PrimaryListener == null)
				{
					PrimaryListener = listener;
				}
				else
				{
					Debug.LogError("Trying to assign a primary listener when one already exists!");
				}
			}
		}

		/// <summary>
		/// Removes a listener from the list of all listener
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		public static void RemoveListener(DSAudioListener listener)
		{
			if (allDSListeners != null)
			{
				allDSListeners.Remove(listener);
			}
		}

		/// <summary>
		/// Adds a DSAudioSource to the list of all sources
		/// </summary>
		/// <param name="source">The source to add</param>
		public static void AddSource(DSAudioSource source)
		{
			if (allDSSources == null)
			{
				allDSSources = new List<DSAudioSource>();
			}

			allDSSources.Add(source);
		}

		/// <summary>
		/// Removes a DSAudioSource from the list of all sources
		/// </summary>
		/// <param name="source">The source to remove</param>
		public static void RemoveSource(DSAudioSource source)
		{
			if (allDSSources != null)
			{
				allDSSources.Remove(source);
			}

			foreach (DSAudioListener listener in allDSListeners)
			{
				listener.audibleSources.Remove(source.GUID);
			}
		}


		/// <summary>
		/// Get a specific source based on the source ID.
		/// </summary>
		/// <param name="id">The ID of the source to find.</param>
		/// <returns></returns>
		public static DSAudioSource GetSourceForID(string id)
		{
			DSAudioSource source = allDSSources.FirstOrDefault(e => e.GUID == id);
			return source;
		}

		/// <summary>
		/// Adds a room to the list of all rooms
		/// </summary>
		/// <param name="room">The room to add</param>
		public static void AddRoom(DSRoom room)
		{
			if (allDSRooms == null)
			{
				allDSRooms = new List<DSRoom>();
			}

			allDSRooms.Add(room);
		}

		/// <summary>
		/// Removes a room from the list of all rooms
		/// </summary>
		/// <param name="room">The current room to remove</param>
		public static void RemoveRoom(DSRoom room)
		{
			if (allDSRooms != null)
			{
				allDSRooms.Remove(room);
			}
		}

		/// <summary>
		/// Clears the list of rooms.
		/// </summary>
		public static void ClearRoomList()
		{
			allDSRooms = new List<DSRoom>();
		}


		/// <summary>
		/// Calculates the current room based on the given position
		/// </summary>
		/// <param name="worldPosition">Current position in world space</param>
		/// <returns>The room that this position is in</returns>
		public static DSRoom GetRoomForPosition(Vector3 worldPosition)
		{
			DSRoom outdoorRoom = null;

			foreach (DSRoom dsRoom in allDSRooms)
			{
				if (dsRoom.PositionIsInRoomBounds(worldPosition) && !dsRoom.isOutdoorRoom)
				{
					return dsRoom;
				}

				if (dsRoom.isOutdoorRoom)
				{
					outdoorRoom = dsRoom;
				}
			}

			if (!outdoorRoom)
			{
				outdoorRoom = GetOutsideRoom();
			}

			return outdoorRoom;
		}

		public static DSRoom GetOutsideRoom()
		{
			DSRoom outdoorRoom = null;

			foreach (DSRoom dsRoom in allDSRooms)
			{
				if (dsRoom.isOutdoorRoom)
				{
					outdoorRoom = dsRoom;
				}
			}

			if (outdoorRoom == null) 
			{
				GameObject roomObject = new GameObject("Outdoor Room");

				outdoorRoom = roomObject.AddComponent<DSRoom>();
				outdoorRoom.isOutdoorRoom = true;  
			}

			return outdoorRoom;
		}

		/// <summary>
		/// Adds a Portal to the list of all portals
		/// </summary>
		/// <param name="dSPortal">The portal to add</param>
		public static void AddPortal(DSPortal dSPortal)
		{
			if (allDSPortals == null)
			{
				allDSPortals = new List<DSPortal>();
			}

			allDSPortals.Add(dSPortal);
		}

		/// <summary>
		/// Removes a portal from the list of all portals
		/// </summary>
		/// <param name="dSPortal">The portal to remove</param>
		public static void RemovePortal(DSPortal dSPortal)
		{
			if (allDSPortals != null)
			{
				allDSPortals.Remove(dSPortal);
			}
		}

		/// <summary>
		/// Find the shortest path between the specified start node to the specified end node. 
		/// </summary>
		/// <param name="startNode">The starting path node (usually the source node)</param>
		/// <param name="targetNode">The target path node (usually the listner)</param>
		/// <param name="skipSourceToFirstPortal">Should we skip the distance between the source node and the first path node (used for outdoor foley sources to skip the distance between the souce and the enterence door)</param>
		/// <param name="totalCost">Output for the total distancde cost to complete this path.</param>
		/// <returns></returns>
		public static List<DSPathNode> FindShortestPath(DSPathNode startNode, DSPathNode targetNode, bool skipSourceToFirstPortal, out float totalCost)
		{
			List<DSPathNode> openList = new List<DSPathNode>();
			HashSet<DSPathNode> closedSet = new HashSet<DSPathNode>();
			openList.Add(startNode); 
			startNode.gCost = 0;
			 
			//Skipping source to first portal cost will treat the source like its coming from the first portal. Used to exteriror global foley SFX etc. 
			//if (!skipSourceToFirstPortal)
			//{
				startNode.hCost = Vector3.Distance(startNode.worldPosition, targetNode.worldPosition);
			//}

			while (openList.Count > 0)
			{
				// Get the node with the lowest fCost
				DSPathNode currentNode = openList[0];

				for (int i = 1; i < openList.Count; i++)
				{
					if (openList[i].fCost < currentNode.fCost || (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
					{
						currentNode = openList[i];
					}
				}

				openList.Remove(currentNode);
				closedSet.Add(currentNode);

				// Check if we reached the target
				if (currentNode == targetNode)
				{
					return RetracePath(startNode, targetNode, out totalCost);
				}

				// Evaluate each connection
				foreach (DSPathNode neighbor in currentNode.connections)
				{
					if (closedSet.Contains(neighbor))
						continue;
					 
					float newCostToNeighbor = currentNode.gCost + Vector3.Distance(currentNode.worldPosition, neighbor.worldPosition) + neighbor.additionalTravelCost;

					if (currentNode == startNode && skipSourceToFirstPortal) 
						newCostToNeighbor = neighbor.additionalTravelCost; 

					if (newCostToNeighbor < neighbor.gCost || !openList.Contains(neighbor))
					{
						neighbor.gCost = newCostToNeighbor;
						neighbor.hCost = Vector3.Distance(neighbor.worldPosition, targetNode.worldPosition);
						neighbor.parent = currentNode;

						if (!openList.Contains(neighbor))
							openList.Add(neighbor);
					}
				}
			}

			totalCost = 0; 
			// No path found
			return new List<DSPathNode>();
		}

		// Retrace the path from targetNode to startNode
		private static List<DSPathNode> RetracePath(DSPathNode startNode, DSPathNode endNode, out float totalCost)
		{
			List<DSPathNode> path = new List<DSPathNode>();
			DSPathNode currentNode = endNode;

			while (currentNode != startNode)
			{
				path.Add(currentNode);
				currentNode = currentNode.parent;
			}

			path.Reverse(); // Reverse the path to get it from start to end 
			totalCost = endNode.gCost; // The cost to reach the end node
			return (path);
		}
	}
}

