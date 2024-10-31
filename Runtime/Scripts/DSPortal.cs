namespace DarkSound
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using NaughtyAttributes;

	[RequireComponent(typeof(BoxCollider))]
    public class DSPortal : DSPathfinderBehaviour
    {
        public DSRoom firstRoom;
        public DSRoom secondRoom; 

        public DSRoom FirstRoom => firstRoom == null ? DSAudioManager.GetOutsideRoom() : firstRoom;
        public DSRoom SecondRoom => secondRoom == null ? DSAudioManager.GetOutsideRoom() : secondRoom;

        [Range(0, 1)] public float openCloseAmount;
        [Range(0, 1)] public float audioObstructionAmount;

        private Coroutine openCloseRoutine;
        private Collider boundsCollider;

        public bool opened = true;
		public bool debugConnections;

        public bool initialised = false; 

		public override void Awake()
		{
            base.Awake();

            if (FirstRoom != SecondRoom)
            {
                AddRoomConnections();
            }
		}

		public void Start()
        {
            if (boundsCollider = GetComponent<Collider>())
            {
                boundsCollider.isTrigger = true; 
            }

            if (FirstRoom != SecondRoom)
            {
                SetUpPortalConnections();
            }
		}

		[Button]
		public void AddRoomConnections()
		{
			FirstRoom.AddRoomConnection(this, SecondRoom);
			SecondRoom.AddRoomConnection(this, FirstRoom);
		}

		[Button]
        public void SetUpPortalConnections()
        {
			SetUpConnectedPortals(FirstRoom);
			SetUpConnectedPortals(SecondRoom);
             
            initialised = true; 
		}

		public override void Update()
		{
			base.Update();

            pathNode.additionalTravelCost = GetAudioObstructionAmount(); 
		}

		/// <summary>
		/// Calculated the amount of obstruction this portal will contribute to audio traveling through it. 
		/// </summary>
		/// <returns></returns>
		public float GetAudioObstructionAmount()
        {
            float obstructionAmount = openCloseAmount * audioObstructionAmount * 10f;

            if (obstructionAmount < 0.1f)
                obstructionAmount = 0.1f;

            return obstructionAmount;
        }


        /// <summary>
        /// Gets the closest point to a position within the bounds of the portal. 
        /// </summary>
        /// <param name="position"></param>
        /// <returns>Vector3 - closest point to the given position with the portal bounds</returns>
        public Vector3 GetClosestPointInBounds(Vector3 position)
        {
            return boundsCollider.ClosestPoint(position);
        }

        /// <summary>
        /// Checks whether a specified world position is in the bounds of this portal. 
        /// </summary>
        /// <param name="worldPosition">The position to check</param>
        /// <returns>Whether the specified position is within the bounds of this portal. </returns>
        public bool PositionIsBounds(Vector3 worldPosition)
        {
            if (boundsCollider.bounds.Contains(worldPosition))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Toggles this portal between open and closed states. 
        /// </summary>
        public void ToggleOpenClose()
        {
            if (!opened)
                OpenPortal(1f);
            else
                ClosePortal(1f);
        }

        /// <summary>
        /// Sets portal state to be open over a specified duration. 
        /// </summary>
        /// <param name="duration">Duration to lerp over</param>
        public void OpenPortal(float duration)
        {
            opened = true;

            if (openCloseRoutine != null)
            {
                StopCoroutine(openCloseRoutine);
            }

            openCloseRoutine = StartCoroutine(LerpPortalOpenCloseAmount(0, duration));
        }


        /// <summary>
        /// Sets portal state to be closed over a specified duration. 
        /// </summary>
        /// <param name="duration">Duration to lerp over</param>
        public void ClosePortal(float duration)
        {
            opened = false;

            if (openCloseRoutine != null)
            {
                StopCoroutine(openCloseRoutine);
            }

            openCloseRoutine = StartCoroutine(LerpPortalOpenCloseAmount(1, duration));

        }

        /// <summary>
        /// Lerps portal openClosedValue over duration. 
        /// </summary>
        /// <param name="target">Target to lerp to. </param>
        /// <param name="duration">Time to lerp over. </param>
        /// <returns></returns>
        public IEnumerator LerpPortalOpenCloseAmount(float target, float duration)
        {
            float start = openCloseAmount;

            for (float t = 0.0f; t < duration; t += Time.deltaTime)
            {
                openCloseAmount = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
        }


		private void SetUpConnectedPortals(DSRoom room)
		{
			// Ignore outdoor rooms
			if (room == null || room.isOutdoorRoom) return;

			// Iterate through the connected rooms of the given room
			foreach (var connectedRoom in room.connectedRooms)
			{
				// Ignore connections where the destination room is an outdoor room
				if (connectedRoom.portal != null && connectedRoom.room != null)
				{
                    pathNode.AddConnectedNode(connectedRoom.portal.pathNode);

                    if(connectedRoom.portal.initialised)
                    { 
                        connectedRoom.portal.pathNode.AddConnectedNode(pathNode);
                    }
				}
			}
		}
	}
}