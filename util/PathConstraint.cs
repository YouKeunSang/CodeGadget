using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class PathConstraint : MonoBehaviour {

    public Spline spline;

    public bool followTangent = false;
    public Vector3 offsetPosition = Vector3.zero;
    public Vector3 offsetRotation = Vector3.zero;
    public bool useCustomUpVector = false;

    public bool isAnimate = false;
    public float speed = 50;
    public bool loop = false;

    public float currentPos = 0;
    public float splineLength = 0;

    float tubeRadius = 25.0f;


    bool borderContacted = false;

	// Use this for initialization
	void Start () {
        
	}

	
	// Update is called once per frame
	void Update () {
        splineLength = spline.CalculateCache(!true);	

		if (spline == null) return;
        if (splineLength <= 0) return;

        if(isAnimate && Application.isPlaying)
        { 
            currentPos += speed * Time.deltaTime;
        }

        if (currentPos < 0)
        {
            if (loop) currentPos = splineLength + (currentPos % splineLength);
        }

        if (currentPos > splineLength)
        {
            if (loop)currentPos = currentPos % splineLength;
            else currentPos = splineLength;
        }

        Vector3 currposition;
        Quaternion currrotation;
        spline.DistanceToWorldPosition(currentPos, out currposition, out currrotation, useCustomUpVector);

        transform.position = currposition;
        if (followTangent) transform.rotation = currrotation * Quaternion.Euler(offsetRotation);
        else transform.rotation = Quaternion.Euler(offsetRotation);
	}


    public void GoForward(Vector3 position)
    {
        Matrix4x4 matW2L = transform.worldToLocalMatrix;
        Vector3 localPosition = matW2L.MultiplyPoint(position);
        currentPos -= localPosition.z;
    }

    // up이 진행방향
    public bool IsContactedBorder(Vector3 position, Vector3 forward, ref Vector3 nextPosition)
    {
        borderContacted = true;

        float frontDistance = 20;
        nextPosition = position - transform.forward * frontDistance;

        float distanceSqr = (transform.position - position).sqrMagnitude;
        float tubeRadiusSqr = tubeRadius * tubeRadius;

        if (distanceSqr > tubeRadiusSqr)
        {
            Vector3 outdir = position - transform.position;
            outdir = outdir.normalized * tubeRadius;

            nextPosition = transform.position + outdir - transform.forward * frontDistance;
            return true;
        }
        else if (distanceSqr == tubeRadiusSqr)
        {
            return true;
        }

        if (Mathf.Acos(Vector3.Dot(forward.normalized, transform.forward.normalized)) * Mathf.Rad2Deg > 80)
        {
            return true;
        }

        borderContacted = false;

        return false;
    }

    void OnDrawGizmos()
    {

        float step = Mathf.PI * 0.25f * 0.25f;

        Gizmos.color = Color.white;
        if (borderContacted) Gizmos.color = Color.red;

        for (float r = 0; r < Mathf.PI * 2; r += step)
        {
            Vector3 p0 = transform.position + (transform.up * Mathf.Sin(r) + transform.right * Mathf.Cos(r)).normalized * tubeRadius;
            Vector3 p1 = transform.position + (transform.up * Mathf.Sin(r + step) + transform.right * Mathf.Cos(r + step)).normalized * tubeRadius;
            Gizmos.DrawLine(p0, p1);
        }
    }
}
