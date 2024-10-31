namespace DarkSound
{
	using UnityEngine;

	using System; 
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;

	using Burrito;
	using NaughtyAttributes;
	using FMOD.Studio;
	using FMODUnity;
	using Random = UnityEngine.Random;


	public class DSAudioSource : DSPathfinderBehaviour
    { 
		public enum AttenuationType { Logarithmic, Linear, OutdoorFoley, Custom }

        public string GUID => guid.Value; 
		[ReadOnly] private GUID guid;  

		[Header("FMOD")] 
        [SerializeField] private EventReference EventReference;
        [SerializeField, DropDownList(nameof(PropertyDrawersHelper.GetAllFMODReferenceLocalVariables), nameof(EventReference))] private string LowPassParamater = "LowPass"; 
		protected FMOD.Studio.EventInstance instance;

		[Header("Settings")]
        [SerializeField, Tooltip("Start playing this source as soon as it is enabled")] private bool playOnAwake = false;
		[SerializeField, Tooltip("Should this source loop?")] private bool loop = false;
        [SerializeField] public bool audibleToSecondaryListeners; 
        [HideInInspector] public float innerAngle = 360f;
		[HideInInspector] public float outerAngle = 360f;  

		[Header("Attenuation")]
		public AttenuationType attenuationType;
		[Tooltip("Volume at minimum distance"), Range(0,1)] public float maxVolume = 1f;
		[Tooltip("Minimum distance")] public float minDistance = 1f;   
        [Tooltip("Maximum distance")] public float maxDistance = 20f;
		[Tooltip("A scale representing how much this source can travel directly through the wall)"), Range(0, 1)] public float directTravelThroughWallScale = 0.2f;
		[Tooltip("A scale representing how much this source can travel directly through the floor)"), Range(0, 1)] public float directTravelThroughFloorScale = 0f;
		[ShowIf("CustomAttenuation"), AllowNesting] public AnimationCurve customAttenuationCurve; // Animation curve for custom attenuatio

        [Tooltip("Layers to use for audio obstruction")]public LayerMask obstructionLayerMask;


		//Positioning
		public Vector3 ActualPosition => transform.position;

		private Vector3 movedPosition;
		private Vector3 occlusionPosition; 

        //Other   
		private float obstructionLeftRightScale = 0.25f; //The distance to the left and right to check occlusion from.
		private float cachedObstruction;
        private float cachedDistance;
        private DSRoom cachedListenerRoom; 
        private List<DSPathNode> optimalPath;
        private float nextUpdatePathTime;
        public bool forceAlwaysUpdatePath;
		private Vector3 previousPosition; 

		private DSRoom currentRoom;

		public DSRoom CurrentRoom 
		{
			get
			{
				return currentRoom; 
			}
			set 
			{
				if (currentRoom != null)
				{
					foreach (DSRoom.ConnectedRoom connection in currentRoom.connectedRooms)
					{
						connection.portal.pathNode.RemoveConnectedNode(pathNode);
						pathNode.RemoveConnectedNode(connection.portal.pathNode);
					}
				}

				currentRoom = value;

				List<DSPathNode> otherPathNodes = new List<DSPathNode>();

				bool useOutdoorToEntryPorpagationPathing = currentRoom.isOutdoorRoom && attenuationType != AttenuationType.OutdoorFoley;

				foreach (DSRoom.ConnectedRoom connection in currentRoom.connectedRooms)
				{
					float distanceToPathNode = Vector3.Distance(pathNode.worldPosition, connection.portal.pathNode.worldPosition);

					if (!useOutdoorToEntryPorpagationPathing || distanceToPathNode <= maxDistance * 0.4f)
					{
						otherPathNodes.Add(connection.portal.pathNode);
					}
				}

				foreach (DSPathNode otherPathNode in otherPathNodes)
				{
					otherPathNode.AddConnectedNode(pathNode); 
					pathNode.AddConnectedNode(otherPathNode);
				}
			}
		}

		//Properties
		public FMOD.Studio.EventInstance EventInstance { get { return instance; } }

		private bool CustomAttenuation => attenuationType == AttenuationType.Custom;
		public bool IsActive { get; private set; }

        public bool IsPlaying 
        {
            get 
            {
				if (instance.isValid())
				{
					FMOD.Studio.PLAYBACK_STATE playbackState;
					instance.getPlaybackState(out playbackState);
					return (playbackState == FMOD.Studio.PLAYBACK_STATE.PLAYING);
				}

				return false; 
			}
        }

		//================/DEBUG/=================//
		public bool debugMode; 

		public override void Awake()
        {
            DSAudioManager.AddSource(this);

            guid = new GUID(); 
            guid.displayName = name;

            base.Awake();
		}  
         
        public void Start()
        {
            CalculatePropagation(DSAudioManager.PrimaryListener, out float volume, out float LowPass, true);
        } 

		public void OnEnable() 
		{
			if (playOnAwake)
			{
				Play();
			}
		}

		public void Play()
        {
			if (EventReference.IsNull)
			{
				return;
			}

			if (EventReference.IsNull)
			{
				return;
			}

			IsActive = true;


			PlayInstance();
		}

		public override void Update() 
		{ 
			base.Update(); 

			if (ActualPosition != previousPosition)
			{
				CurrentRoom = DSAudioManager.GetRoomForPosition(ActualPosition);
			}

			previousPosition = ActualPosition;
		}

		/// <summary>
		/// Update the propagation for this source for the observing listener.
		/// </summary>
		/// <param name="audioListener">The listener observing this source.</param>
		/// <param name="data">Output for the data (volume and lowpass) for this source to the observing listener </param>
		/// <param name="initialisationCall">Is this call the sources initialisation call? Should the effects be applied instantly?</param>
		public void UpdatePropagation(DSAudioListener audioListener, ref AudabilityData data, bool initialisationCall = false)
        {
			if (IsPlaying)
			{
				bool updateSource = true;

				//if (cachedDistance > 0.8f * maxDistance)
				//{
				//    updateSource = (Time.frameCount % 5 == 0); //if distance is too far away, only update source values every 5 frames.
				//}

				if (updateSource)
				{
					float volume = 0, lowPass = 1;

					CalculatePropagation(audioListener, out volume, out lowPass);

					data.UpdateValues(volume, lowPass);

					if (audioListener.isPrimaryListener)
					{
						// Set the falloff volume using the calculated attenuation value 
						if (initialisationCall)
						{
							EventInstance.setVolume(volume);
						}
						else
						{
							EventInstance.getVolume(out float previousVolume);
							EventInstance.setVolume(Mathf.Lerp(previousVolume, volume, 3f * Time.deltaTime));
						}

						EventInstance.getParameterByName(LowPassParamater, out float currentLowPass);
						EventInstance.setParameterByName(LowPassParamater, !initialisationCall ? Mathf.Lerp(currentLowPass, lowPass, 3f * Time.deltaTime) : lowPass);

						EventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(movedPosition));
					}
				}
			}
			else
			{
				data.UpdateValues(0f, 0f);
			}
		}

		/// <summary>
		/// Calculates the propagation of audio from this source. The calculated values are then applied to the source to effect the audio. 
		/// </summary>
		/// <param name="listener">The listener hearing this source</param>
		/// <param name="volume">Out the volume that this souce is percieved by the listener</param>
		/// <param name="lowPass">The low pass effect for this source as percieved by the listener</param>
		/// <param name="initialisationCall">Is this call the sources initialisation call? Should the effects be applied instantly? </param>
		public void CalculatePropagation(DSAudioListener listener, out float volume, out float lowPass, bool initialisationCall = false)
        {
			if (!listener)
			{
				lowPass = 0;
				volume = 0; 
				return;
			}

            DSRoom currentListenerRoom = listener.CurrentRoom;
		
            float propagationDistance = 0f; 

			bool listenerOnSameFloor = CurrentRoom == null || currentListenerRoom == null || currentListenerRoom.isOutdoorRoom || Mathf.Abs(currentListenerRoom.transform.position.y - CurrentRoom.transform.position.y) < 5f;


			if (currentListenerRoom == CurrentRoom) //Calculates propagation when the audioListener and the audioSource are in the same room.
            {
                cachedListenerRoom = currentListenerRoom;

			     propagationDistance = Vector3.Distance(ActualPosition, listener.transform.position);

				if (innerAngle != 360f)
				{
					Vector3 listenerPosition = listener.transform.position;
					Vector3 sourcePosition = ActualPosition;
					Vector3 emitterToListenerDirection = listenerPosition - sourcePosition;
					float angle = Vector3.Angle(transform.forward, emitterToListenerDirection);

					if (angle > outerAngle)
					{
						propagationDistance += 4f;
					}
					else if (angle > innerAngle)
					{
						propagationDistance += 2f;
					}
				}

				if (listener.isPrimaryListener)
				{
                    if (attenuationType != AttenuationType.OutdoorFoley)
                    {
                        movedPosition = ActualPosition;
                    }
                    else
                    {
                        movedPosition = listener.transform.position;
						propagationDistance = 0f; 
                    }

					occlusionPosition = movedPosition;  
				}
                   
				volume = CalculateFallOffVolume(propagationDistance);
                lowPass = attenuationType != AttenuationType.OutdoorFoley ? GetObstruction(listener, 0) : 1f;
			} 
			else 
            {
                if (cachedListenerRoom != currentListenerRoom || Time.time > nextUpdatePathTime || forceAlwaysUpdatePath)
                {  
                    optimalPath = DSAudioManager.FindShortestPath(pathNode, listener.pathNode, attenuationType == AttenuationType.OutdoorFoley, out propagationDistance);
                    cachedListenerRoom = currentListenerRoom;
                    nextUpdatePathTime = Time.time + Random.Range(0,1);
                }

                Vector3 startPos = ActualPosition;
                float portalObstruction = 0f; 

				if (optimalPath != null && optimalPath.Count > 0)
				{
					foreach (DSPathNode pathNode in optimalPath)
					{
						if (debugMode)
							GLDebug.DrawLine(startPos, pathNode.worldPosition, Color.blue);

						//propagationDistance += Vector3.Distance(startPos, closestPointInBounds) + pathPortal.GetAudioObstructionAmount();

						portalObstruction += pathNode.additionalTravelCost / 10f;

						startPos = pathNode.worldPosition;
					}
				}
				else
				{ 
					propagationDistance = float.MaxValue;
				}

				if (!listenerOnSameFloor)
				{
					portalObstruction += 0.5f; 
				}
				 
				portalObstruction = Mathf.Clamp01(portalObstruction);
				 

				//Check direct distance. 
				float directDistance = Vector3.Distance(listener.transform.position, ActualPosition); 

				float directScale = Mathf.Clamp01(directDistance / (maxDistance / 3f));  

				float impactScale = listenerOnSameFloor ? directTravelThroughWallScale : directTravelThroughFloorScale;

				propagationDistance =  propagationDistance - (impactScale * 100f * (1f - directScale)); 
				propagationDistance = Mathf.Clamp(propagationDistance, 0f, float.MaxValue);

                if (listener.isPrimaryListener)
                {
                    Vector3 newMovePosition; 

                    if (attenuationType != AttenuationType.OutdoorFoley)
                    {
						//Same floor perception. 
						if (listenerOnSameFloor) //Listener is on the same floor. 
						{
							if (optimalPath != null)
							{
								Vector3[] pathPositions = new Vector3[optimalPath.Count + 2];

								pathPositions[0] = ActualPosition;  
								 
								for (int i = 0; i < optimalPath.Count; i++)
								{
									pathPositions[i + 1] = optimalPath[i].worldPosition;
								} 

								pathPositions[pathPositions.Length - 1] = listener.transform.position; 

								newMovePosition = ActualPosition;
								  
								// Calculate an interpolated position along the path based on the listener's distance
								float listenerDistance = propagationDistance;
								float distanceRatio = 0.25f; //We should precieve the source as if its coming from halfway along the path. 

								float accumulatedDistance = 0f;
								 
								// Find the exact segment in the path to interpolate on based on the distance ratio
								for (int i = 0; i < pathPositions.Length - 1; i++)
								{ 
									Vector3 startPosition = pathPositions[i];
									Vector3 endPosition = pathPositions[i + 1];
									 
									Debug.DrawLine(startPosition, endPosition, Color.red);

									float segmentDistance = Vector3.Distance(startPosition, endPosition);

									if (accumulatedDistance + segmentDistance >= listenerDistance * distanceRatio)
									{
										float segmentRatio = ((listenerDistance * distanceRatio) - accumulatedDistance) / segmentDistance;
										newMovePosition = Vector3.Lerp(startPosition, endPosition, segmentRatio);
										break;
									}

									accumulatedDistance += segmentDistance;
								}
							}
							else
							{
								newMovePosition = ActualPosition;
							}
						}
						else
						{
							newMovePosition = ActualPosition;
						}
                    } 
                    else
                    { 
						if (optimalPath != null && optimalPath.Count > 0)
						{
							Vector3 position1 = optimalPath[0].worldPosition;
							Vector3 position2 = listener.transform.position;  

							newMovePosition = optimalPath[0].worldPosition - ((position2 - position1).normalized * 3f);
						}
						else
						{
							newMovePosition = ActualPosition; 
						}
                    }

					occlusionPosition = newMovePosition;  
                    movedPosition = Vector3.Lerp(movedPosition, newMovePosition, Time.deltaTime);
                }

                if (debugMode) 
                    GLDebug.DrawLine(startPos, listener.transform.position, Color.blue);

				//Debug.Log(propagationDistance); 

				if (innerAngle != 360f)
				{
					Vector3 listenerPosition = listener.transform.position;
					Vector3 sourcePosition = ActualPosition;
					Vector3 emitterToListenerDirection = listenerPosition - sourcePosition;
					float angle = Vector3.Angle(transform.forward, emitterToListenerDirection);

					if (angle > outerAngle)
					{
						propagationDistance += 4f;
					}
					else if (angle > innerAngle)
					{
						propagationDistance += 2f;
					}
				}

				volume = CalculateFallOffVolume(propagationDistance); 
				lowPass = GetObstruction(listener, portalObstruction);
			}


			if (debugMode)
            {
                Color a = Color.yellow;
                a.a = 0.5f;
                GLDebug.DrawCube(ActualPosition, Vector3.zero, Vector3.one/3f, a);

                a = Color.magenta;
                a.a = 0.5f;

                GLDebug.DrawCube(transform.position, Vector3.zero, Vector3.one / 3f, a);
            }
        }

		/// <summary>
		/// Retrurns the fall off volume based on the fall off technique selected. 
		/// </summary> 
		/// <param name="distance">The pathed distance between the source and the target listener</param>
		/// <returns></returns>
		public float CalculateFallOffVolume(float distance)
		{
			// Ensure the distance stays within the min and max range.
			distance = Mathf.Clamp(distance, minDistance, maxDistance);

			float attenuation = 1.0f;

			// Choose attenuation method based on selected attenuation type
			switch (attenuationType)
			{
				case AttenuationType.Linear:
					// Linear attenuation
					attenuation = 1 - ((distance - minDistance) / (maxDistance - minDistance));
					break;

				case AttenuationType.Logarithmic:
					// Logarithmic attenuation
					attenuation = Mathf.Log10(maxDistance / distance) / Mathf.Log10(maxDistance / minDistance);
					break; 

				case AttenuationType.Custom:
					// Custom attenuation using an AnimationCurve
					float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distance);
					attenuation = customAttenuationCurve.Evaluate(normalizedDistance);
					break;

				case AttenuationType.OutdoorFoley: 
					attenuation = Mathf.Log10(maxDistance / distance) / Mathf.Log10(maxDistance / minDistance);
					break;
			}

			// Clamp attenuation between 0 and 1 
			attenuation = Mathf.Clamp01(attenuation);

            return attenuation * maxVolume; 
		}

		/// <summary>
		/// Returns the maximum obstruction value, whether that be from the linecasts of the portals themselves. 
		/// </summary>
		/// <param name="portalObstruction"> Obstruction level given from portal traversal </param>
		public float GetObstruction(DSAudioListener listener, float portalObstruction)
        {
            float minLowPass = 0f;
            float maxLowPass = 1f;

            float rayObstructionPercentage =  ObstructionCheck(listener);

            cachedObstruction = 0.5f * (rayObstructionPercentage + portalObstruction);

            return maxLowPass - ((maxLowPass - minLowPass) * cachedObstruction);
        }

        /// <summary>
        /// Checks obstruction between listener and source. This value is calculated from 9 individual linecast values. 
        /// </summary>
        /// <returns>Returns a value between 0 and 1 to represent the amount of obstruction </returns>
        public float ObstructionCheck(DSAudioListener listener)
        {
            float numberOfRaysObstructed = 0; // Out of 9.  
			 
            Vector3 listenerPosition = listener.transform.position;
            Vector3 sourcePosition = attenuationType == AttenuationType.OutdoorFoley ? occlusionPosition : ActualPosition;

            Vector3 listenerToEmitterDirection = sourcePosition - listenerPosition;
            Vector3 emitterToListenerDirection = listenerPosition - sourcePosition;

            Vector3 leftFromListenerDirection = Vector3.Cross(listenerToEmitterDirection, Vector3.up).normalized;
            Vector3 leftFromListenerPosition = listenerPosition + (leftFromListenerDirection * obstructionLeftRightScale);

            Vector3 leftFromSourceDirection = Vector3.Cross(emitterToListenerDirection, Vector3.up).normalized;
            Vector3 leftFromSourcePosition = sourcePosition + (leftFromSourceDirection * obstructionLeftRightScale);

            Vector3 rightFromListenerPosition = listenerPosition + (-leftFromListenerDirection * obstructionLeftRightScale);
            Vector3 rightFromSourcePosition = sourcePosition + (-leftFromSourceDirection * obstructionLeftRightScale);

            numberOfRaysObstructed += ObstructionLinecast(sourcePosition, listenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(leftFromSourcePosition, leftFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(rightFromSourcePosition, leftFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(leftFromSourcePosition, rightFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(rightFromSourcePosition, rightFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(sourcePosition, leftFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(sourcePosition, rightFromListenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(leftFromSourcePosition, listenerPosition);
            numberOfRaysObstructed += ObstructionLinecast(rightFromSourcePosition, listenerPosition);

            float obstructionPercentage = numberOfRaysObstructed / 9;

            if (innerAngle != 360f)
            {
                float angle = Vector3.Angle(transform.forward, emitterToListenerDirection);

				if (angle > outerAngle)
				{
					obstructionPercentage += 0.3f; 
				}
				else if (angle > innerAngle) 
                {
                    obstructionPercentage += 0.8f;
                } 
            }

            obstructionPercentage = Math.Clamp(obstructionPercentage, 0f, 1f);


			return obstructionPercentage;
        }


        /// <summary>
        /// Performs a linecast from point start to point end to check for obstructions. 
        /// </summary>
        /// <param name="start">Start position of linecast</param>
        /// <param name="end">End position of linecast </param>
        /// <returns> Returns value to represent obstruction, 1 == no obstruction, 0 == Obstructed </returns>
        private int ObstructionLinecast(Vector3 start, Vector3 end)
        {
            //Potentially change this to use a raycastAll rather than linecast. Can more accurately propagate through walls etc, but will result in a significant performance hit. Additionally, this
            //is already our largest performance drain. Could maybe use 9 linecasts + 1 direct raycastAll to reduce the hit of this, however it would increase set up complexity as to get the maximum benefit
            // from this would require an additional script on wall collider to hold the properties of walls/materials. 

            if (Physics.Linecast(start, end, out RaycastHit hit, obstructionLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (debugMode)
                    GLDebug.DrawLine(start, end, Color.red);

                return 1;
            }
            else
            {
                if (debugMode)
                    GLDebug.DrawLine(start, end, Color.green);

                return 0;
            }
        }

		private void PlayInstance()
		{
			if (!instance.isValid())
			{
				instance.clearHandle();
			}

			if (instance.isValid())
			{
				instance.release();
				instance.clearHandle();
			} 
             
			if (!instance.isValid())
			{
				instance = gameObject.PlayAudio(EventReference);
                CalculatePropagation(DSAudioManager.PrimaryListener, out float volume, out float lowPass, true);
			}
		}

        public void Stop(bool fade = false)
		{
			IsActive = false;
			StopInstance(fade);
		} 

		private void StopInstance(bool fade)
		{
			if (instance.isValid())
			{
				instance.stop(fade ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
				instance.release();
				if (!fade)
				{
					instance.clearHandle();
				}
			}
		}

		public void OnDisable()
		{
			Stop();
		}

		public void OnDestroy()
		{ 
			Stop();
			DSAudioManager.RemoveSource(this);
		}

		public void OnValidate() 
		{
            if (outerAngle < innerAngle)
            {
                outerAngle = innerAngle; 
            }

			if (minDistance < 0.1f)
			{
				minDistance = 0.1f;
			}

			if (maxDistance < minDistance)
			{
				maxDistance = minDistance;
			}
		}

#if UNITY_EDITOR
		public static string IconsFolder
		{
			get
			{
				if (Directory.Exists("Packages/com.wolfandwood.DarkSound/"))
				{
					return "Packages/com.wolfandwood.darksound/Editor/Icons/";
				}

				return "Assets/DarkSound/Editor/Icons/";
			}
		}

		public override void OnDrawGizmos()   
		{ 
			base.OnDrawGizmos();
			Gizmos.DrawIcon(Application.isPlaying ? movedPosition : transform.position, $"{IconsFolder}Gizmo.png", true);
		}
#endif
    }
}
