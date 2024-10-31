using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DarkSound
{
    public class DSPathNode : IHeapItem<DSPathNode>
    {
        public Vector3 worldPosition;
        public float gCost;
        public float hCost;

        public DSPathNode parent;

        int heapIndex;

        public float additionalTravelCost = 0f; //Cost to be added to any travel through this node.

        public List<DSPathNode> connections; 

        public DSPathNode(Vector3 _worldPos)
        {
            worldPosition = _worldPos;
        }

        public int index
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

        public float fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public void AddConnectedNode(DSPathNode node) 
        {
            if (connections == null)
                connections = new List<DSPathNode>();

            if (!connections.Contains(node))
            { 
                connections.Add(node);
            }
        }

        public void RemoveConnectedNode(DSPathNode node)
        {
            if(connections != null) 
                connections.Remove(node);
        }

        public int CompareTo(DSPathNode nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);

            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }

            return -compare;
        }

    }

}