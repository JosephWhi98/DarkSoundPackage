namespace DarkSound
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public class DSRoom : MonoBehaviour
    {
        [System.Serializable]
        public class ConnectedRoom
        {
            public DSPortal portal; 
            public DSRoom room;

            public ConnectedRoom(DSPortal portal, DSRoom room)
            {
                this.portal = portal;
                this.room = room; 
            }
        }

        public Collider[] boundsColliders;

        public bool isOutdoorRoom; 

        //Connections and pathfinding
        public List<ConnectedRoom> connectedRooms = new List<ConnectedRoom>();

        public void Awake()
        {
            InitialiseRoom();

            connectedRooms = new List<ConnectedRoom>();
        }

        /// <summary>
        /// Initialises the room, setting it up for pathfinding etc. 
        /// </summary>
        public void InitialiseRoom()
        {
            if (boundsColliders != null)
            {
                foreach (Collider boundsCollider in boundsColliders)
                {
                    boundsCollider.isTrigger = true;
                }
            }

            DSAudioManager.AddRoom(this);
        }

      
        /// <summary>
        /// Adds a connection from this room to another through a specified portal. 
        /// </summary>
        /// <param name="portal">The portal that connects this room to the other. </param>
        /// <param name="room"> The room that this room connects to throught the portal </param>
        public void AddRoomConnection(DSPortal portal, DSRoom room)
        {
            ConnectedRoom connection = new ConnectedRoom(portal, room);

            if (connectedRooms == null)
            {
                new List<ConnectedRoom>();
            }

            connectedRooms.Add(connection);

            if (portal.debugConnections)
                Debug.Log("Connected portal: " + portal.name + " to room: " + room.name);
        }

        /// <summary>
        /// Checks whether a specified world position is in the bounds of this room. 
        /// </summary>
        /// <param name="worldPosition">The position to check</param>
        /// <returns>Whether the specified position is within the bounds of this room. </returns>
        public bool PositionIsInRoomBounds(Vector3 worldPosition)
        {
            if (boundsColliders != null)
            {
                foreach (Collider boundsCollider in boundsColliders)
                {
                    if (boundsCollider.bounds.Contains(worldPosition))
                    {
                        return true;
                    }
                }
            }
             
            return isOutdoorRoom;
        }

        /// <summary>
        /// Gets the portal that connects this room to the specified room. 
        /// </summary>
        /// <param name="room">The connected room to find a portal for. </param>
        /// <returns>The portal connecting this room to the specified room </returns>
        public DSPortal GetPortal(DSRoom room)
        {
            foreach (ConnectedRoom connection in connectedRooms)
            {
                if (connection.room == room)
                {
                    return connection.portal;
                }
            }

            return null;
        }


        public void OnDisable()
        {
            DSAudioManager.RemoveRoom(this);
        }
    }
}
