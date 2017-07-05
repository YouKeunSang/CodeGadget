using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//[System.Serializable]
public class SCSampleList : ScriptableObject {

	public SCSampleInfo[] arrayData;
	public SCSampleInfo FindByKey(int key)
	{
		return Array.Find(arrayData,d=>d.dropgroup_index ==key);
	}
	void OnEnable()
	{
		if(null == arrayData)
		{
			arrayData = new SCSampleInfo[0];
		}
	}
}