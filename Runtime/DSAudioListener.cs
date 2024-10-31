using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DarkSound
{
    /// <summary>
    /// DSAudioListener represents the position of the listeners head. This acts seperate from the base Listener position to handle audio propagation effects (Occlusion etc) - Seperate from the spatialisation. 
    /// </summary> 

    public class DSAudioListener : DSPathfinderBehaviour
	{
        private DSRoom currentRoom;

		public DSRoom CurrentRoom 
		{
			get
			{ 
				return currentRoom; 
			}
			set
			{

				if (currentRoom != value)
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

					if (currentRoom != null)  
					{
						foreach (DSRoom.ConnectedRoom connection in currentRoom.connectedRooms)
						{
							connection.portal.pathNode.AddConnectedNode(pathNode);
							pathNode.AddConnectedNode(connection.portal.pathNode);
						}
					}
				}
			}
		}

		public bool isPrimaryListener;

        public Dictionary<string, AudabilityData> audibleSources;

		public override void Awake()
		{
			base.Awake();  

			DSAudioManager.AddListener(this);
			audibleSources = new Dictionary<string, AudabilityData>();
		}

        public override void Update()
        {
			base.Update(); 

			DSRoom room = DSAudioManager.GetRoomForPosition(transform.position);
			CurrentRoom = room ? room : CurrentRoom;

			DSAudioManager.UpdateSourcesForListener(this);
        }

		public void OnDestroy()
		{
            DSAudioManager.RemoveListener(this);
		}
	}
}