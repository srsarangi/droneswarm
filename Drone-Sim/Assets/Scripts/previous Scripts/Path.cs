using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    public Color  linecolor;
    private List<Transform> nodes = new List<Transform>();

    // OnDrawGizmos function runs in Screen view and behaves same way as update function (update function runs in game view at 60 fps)
    // we want lines to be visible in screen only thats why this function other wiase update will make lines visible in game view
    void OnDrawGizmos(){
    // only when the path is selected then only lines will be visible
 	// void OnDrawGizmosSelected(){
 		Gizmos.color =linecolor;
        // path is list of nodes 
 		Transform[] pathTransforms = GetComponentsInChildren<Transform>(); 
 		nodes = new List<Transform>();
 		for(int i =0; i<pathTransforms.Length;i++)
 		{
 			if (pathTransforms[i] != transform)
 			{
 				nodes.Add(pathTransforms[i]);
 			}
 		}
        // draw line node ot node
 		for (int i = 0; i<nodes.Count; i++)
 		{
 			Vector3 currentNode = nodes[i].position;
 			Vector3 previousNode = Vector3.zero;
 			if (i>0)
 			{
 				previousNode = nodes[i-1].position;

 			}
 			else if (i==0 && nodes.Count>1)
 			{
 				previousNode = nodes[nodes.Count-1].position;
 			} 
            // Draw line connecting nodes
 			Gizmos.DrawLine(previousNode,currentNode);
            // Draw Sphere at nodes 
 			Gizmos.DrawWireSphere(currentNode,0.5f);
 		}


 	}


}
