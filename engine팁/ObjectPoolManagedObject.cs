using UnityEngine;
using System.Collections;

public class ObjectPoolManagedObject : MonoBehaviour 
{
    protected bool onceStarted = false;

	// Use this for initialization
	protected virtual void Start () 
    {
        doReset();
        onceStarted = true;
	}

    // broadcast from objectpool
    protected virtual void OnSoftReset()
    {
        if (onceStarted == false)
        {
            return;
        }
        doReset();
    }

    protected virtual void OnSoftDestroy()
    {
    }

    protected virtual void doReset()
    {
    }	
}
