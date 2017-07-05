using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Trajectory : MonoBehaviour {

    List<Vector3> pts = new List<Vector3>();

    public bool dropcube;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        pts.Add(transform.position);

        if (dropcube)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = new Vector3(0.1f, 1, 0.1f);
            go.transform.Translate(-0.5f, 0, 0);

            GameObject go2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go2.transform.position = transform.position;
            go2.transform.rotation = transform.rotation;
            go2.transform.localScale = new Vector3(0.1f, 1, 0.1f);
            go2.transform.Translate(0.5f, 0, 0);
        }
	}

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if(pts.Count > 1)
        {
            for(int i=1; i<pts.Count; i++)
            {
                Gizmos.DrawLine(pts[i-1],pts[i]);
            }
        }
    }
}
