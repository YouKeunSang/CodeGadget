using UnityEngine;
using System.Collections;

public class SelfDestoryPool : MonoBehaviour {
    public float interval = 0.1f;
    float _lifeTime = 0;

	public void Init(float lifeTime)
    {
        _lifeTime = lifeTime;
        StartCoroutine(CustomUpdate());
    }
	
	// Update is called once per 0.1sec
	IEnumerator CustomUpdate () {
        while(0 < _lifeTime)
        {
            yield return new WaitForSeconds(interval);
            _lifeTime -= interval;
        }
        SelfDestory();
	}

    public void SelfDestory()
    {
        PoolManager.instance.PoolDestroy(gameObject);
    }
}
