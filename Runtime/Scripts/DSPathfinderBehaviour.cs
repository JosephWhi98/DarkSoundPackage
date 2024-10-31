namespace DarkSound
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class DSPathfinderBehaviour : MonoBehaviour
    {
        public DSPathNode pathNode;

        public virtual void Awake()
        {
            pathNode = new DSPathNode(transform.position);
        }

        public virtual void Update()
        {
            pathNode.worldPosition = transform.position;
        }


        public virtual void OnDrawGizmos()
        { 
            if (Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(pathNode.worldPosition, 0.5f);

                if (pathNode.connections != null)
                {
                    foreach (DSPathNode connection in pathNode.connections)
                    {
                        Gizmos.DrawLine(pathNode.worldPosition, connection.worldPosition);
                    }
                }

            }
        }
    }
}
