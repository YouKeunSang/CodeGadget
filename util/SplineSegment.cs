using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SplineSegment : MonoBehaviour {

    [HideInInspector]
    public float gizmoScale = 0.1f;

    public bool isStart = false;


    [HideInInspector]
    public bool dirty;

    Vector3 prevpos;
    Quaternion prevrot;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

     #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (prevpos != transform.position) dirty = true;
        if (prevrot != transform.rotation) dirty = true;

        Gizmos.DrawIcon(transform.position,  isStart?"controlpoint.png":"entrypoint.png", false);

        prevpos = transform.position;
        prevrot = transform.rotation;
    }

    void OnDrawGizmosSelected()
    {
        DrawSphere(0.05f);
    }

    private void DrawSphere(float gizmoHeight)
    {
        if (Application.isEditor)
            if (Camera.current == SceneView.lastActiveSceneView.camera)
            {
                float dist = Vector3.Distance(Camera.current.transform.position, transform.position);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, gizmoHeight * dist);
            }
    }
    #endif


}
