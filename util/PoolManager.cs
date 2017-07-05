using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CachedObject
{
    public GameObject prefab;
    public int cacheSize = 10;

    GameObject _inactiveHolder;
    
    List<GameObject> _instances = new List<GameObject>();
    public void Initialize ()
    {
        _inactiveHolder = new GameObject();
        _inactiveHolder.name = prefab.name + "_inactiveHolder";
        
        _inactiveHolder.transform.parent = PoolManager.instance.transform;

        // Instantiate the objects in the array and set them to be inactive
        for (int i = 0; i < cacheSize; i++)
        {
            GameObject obj = UnityEngine.Object.Instantiate(prefab) as GameObject;
            obj.name = prefab.name + "_None_" + i;
            PushToCache(obj);
        }
    }

    public GameObject PopFromCache()
    {
        GameObject _ret = null;
        if(0<_instances.Count)
        {
            _ret = _instances[0];
            _ret.SetActive(true);
            _instances.Remove(_ret);
            return _ret;
        }

        //여기까지 오면 캐쉬가 모자라다는 말이다.
        _ret = UnityEngine.Object.Instantiate(prefab) as GameObject;
        _ret.name = prefab.name+"_None_" + cacheSize;
        cacheSize++;
        //Debug.LogWarning("캐쉬된 오브젝트 " + prefab + "이 모자름: " + cacheSize);
        return _ret;
    }
    public void PushToCache(GameObject _obj)
    {
        //_obj.transform.position = PoolManager.instance.farFarAway;
        _obj.transform.parent = _inactiveHolder.transform;
        _obj.SetActive(false);
        _instances.Add(_obj);
    }
}

public class PoolManager : MonoBehaviour {
    [HideInInspector]
    static public PoolManager instance = null;
    [HideInInspector]
    public Vector3 farFarAway = new Vector3(1000, 1000, 1000);

    public List<CachedObject> caches;
    public bool isReady = false;

    GameObject _roninHolder;
    void Awake()
    {
        _roninHolder = new GameObject();
        _roninHolder.transform.SetParent(transform);
        _roninHolder.name = "_RoninHolder";

        instance = this;
        foreach(CachedObject c in caches)
        {
            c.Initialize();
        }
        isReady = true;
    }
    void OnDisable()
    {
        instance = null;
        isReady = false;
    }
    void OnApplicationQuit()
    {
        instance = null;
        isReady = false;
    }
    public void ManualRegister(GameObject prefab,int reuseCnt = 2)
    {
        CachedObject _newObj;
        _newObj = caches.Find(x => x.prefab == prefab);
        if (null == _newObj)
        {
            _newObj = new CachedObject();
            _newObj.cacheSize = reuseCnt;
            _newObj.prefab = prefab;
            _newObj.Initialize();
            caches.Add(_newObj);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="isAbleReuse">미리 리스트에 있지는 않으나 동적으로 풀을 만들어 이후부터 사용할겨우</param>
    /// <param name="reuseCnt">동적 관리 풀을 만들때 캐싱 갯수</param>
    /// <returns></returns>
    public GameObject PoolInstantiate(GameObject prefab,bool isAbleReuse = false,int reuseCnt = 2)
    {
        if (!isReady)
            return null;

        foreach(CachedObject c in caches)
        {
            if(prefab.Equals(c.prefab))
            {
                GameObject _obj = c.PopFromCache();
                _obj.SendMessage("SoftEnable",SendMessageOptions.DontRequireReceiver);
                return _obj;
            }
        }
        if(isAbleReuse)
        {
            CachedObject _newObj = new CachedObject();
            _newObj.cacheSize = reuseCnt;
            _newObj.prefab = prefab;
            _newObj.Initialize();
            GameObject _obj = _newObj.PopFromCache();
            caches.Add(_newObj);
            _obj.SendMessage("SoftEnable", SendMessageOptions.DontRequireReceiver);
            return _obj;
        }
        //Debug.LogWarning("prefab이 목록에 없습니다. 확인바람:"+prefab);
        return Instantiate(prefab) as GameObject;
    }
    
    public GameObject PoolInstantiate(string prefabName)
    {
        if (!isReady)
            return null;

        foreach (CachedObject c in caches)
        {
            if (prefabName.Equals(c.prefab.name))
            {
                GameObject _obj = c.PopFromCache();
                _obj.SendMessage("SoftEnable",SendMessageOptions.DontRequireReceiver);
                return _obj;
            }
        }
        Debug.LogError("prefab이 목록에 없습니다. 확인바람:" + prefabName);
        return null;
    }

    public void PoolDestroy(GameObject obj)
    {
        foreach (CachedObject c in caches)
        {
            if (obj.name.StartsWith(c.prefab.name+"_"))
            {
                obj.SendMessage("SoftDisable", SendMessageOptions.DontRequireReceiver);
                obj.name = obj.name.Replace("_Enemy_", "_None_");
                obj.name = obj.name.Replace("_PC_", "_None_");
                c.PushToCache(obj);
                return;
            }
        }
        Destroy(obj);
    }
    /// <summary>
    /// 파티클을 생성할때 bullet의 hit난 character의 die는 모체가 없어져서 문제가 된다.
    /// 이럴경우 PoolManger에 위탁해 지연하여 없애는 방법밖에는 없다.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="lifeTime"></param>
    /// <returns></returns>
    public GameObject MakeSelfDestructObject(GameObject prefab, float lifeTime = 2.0f)
    {
        GameObject _obj = PoolManager.instance.PoolInstantiate(prefab);
        _obj.transform.SetParent(PoolManager.instance._roninHolder.transform);
        SelfDestoryPool _selfTimer = _obj.AddComponent<SelfDestoryPool>();
        _selfTimer.Init(lifeTime);
        return _obj;
    }
}
