#define USE_ACTIVE_POOL
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

//
// object pool for the unity game objects
//
// basic feature from the angrybot sample
// improved and rearranged by spamu
//

[AddComponentMenu("SilverStorm/System/ObjectPool")]
public class ObjectPool : MonoBehaviour 
{
    static public ObjectPool instance = null;
    
    public ObjectCache[] caches;
    public bool enableObjectPool = false;

    GameObject cacheHolder;
    GameObject CacheHolder { get { return cacheHolder; } }
    Hashtable activeCachedObjects;

    GameObject instantHolder;
    GameObject InstantHolder { get { return instantHolder; } }
	
	// 2013/06/16 - reporting feature. cache hit report
	int initCachesLength;
#if UNITY_EDITOR
    [NonSerialized]
	public bool debugEnableReport = false;
    [NonSerialized]
	public bool debugReportOnlyUncached = false;
    [NonSerialized]
	Dictionary<string, ReportInfo> reports = new Dictionary<string, ReportInfo>();

    
	
	public class ReportInfo
	{
		public GameObject gameObject;
		public string gameObjectName = "";
		public bool cached = false;
		public int refCount = 0;
		public float createdTime = 0;
		public float lastAccessTime = 0;
		public float averageHitTime = 0;
		
		public void WriteReport()
		{
			string txt;
			
			txt = string.Format("Name:{0}, Referenced:{1}, Cached:{2}", gameObjectName, refCount, cached);
			
            //Debug.Log (txt);
		}
	}
	
	void addToReport(GameObject obj, bool cached)
	{
		ReportInfo info = null;
		
		// failed to use obj's guid. it returns empty string
				
		if( reports.ContainsKey(obj.name) )
		{
			info = reports[obj.name];
			info.refCount++;
			info.lastAccessTime = Time.time;			
			return;
		}
		
		// add to new object
		info = new ReportInfo();
		info.gameObject = obj;
		info.gameObjectName = obj.name;
		info.refCount++;
		info.createdTime = Time.time;
		info.lastAccessTime = Time.time;
		info.cached = cached;
		
		reports.Add (obj.name, info);
	}

	void report()
	{
		foreach(ReportInfo info in reports.Values)
		{
			if (debugReportOnlyUncached && info.cached)
			{
				continue;
			}
			info.WriteReport();
		}
	}
#endif	

    [System.Serializable]
    public class ObjectCache
    {
	    public GameObject prefab;
	    public int cacheSize = 10;

	    private GameObject[] objects;
	    private int cacheIndex = 0;

	    public void Initialize ()
	    {
		    objects = new GameObject[cacheSize];

		    // Instantiate the objects in the array and set them to be inactive
		    for (int i = 0; i < cacheSize; i++)
		    {
			    objects[i] = UnityEngine.Object.Instantiate (prefab) as GameObject;
			    objects[i].SetActive (false);
			    objects[i].name = objects[i].name + i;

                objects[i].transform.parent = ObjectPool.instance.CacheHolder.transform;
		    }
	    }

	    public GameObject GetNextObjectInCache()
        {
		    GameObject obj = null;

		    // The cacheIndex starts out at the position of the object created
		    // the longest time ago, so that one is usually free,
		    // but in case not, loop through the cache until we find a free one.
		    for (int i = 0; i < cacheSize; i++)
            {
                //Debug.Log("POOLIDX: " + i + "/" + cacheSize);
			    obj = objects[cacheIndex];                

			    // If we found an inactive object in the cache, use that.
			    if (!obj.activeSelf)
				    break;

			    // If not, increment index and make it loop around
			    // if it exceeds the size of the cache
			    cacheIndex = (cacheIndex + 1) % cacheSize;
		    }

		    // The object should be inactive. If it's not, log a warning and use
		    // the object created the longest ago even though it's still active.
		    if (obj.activeSelf)
            {
			    Debug.LogWarning (
				    "Spawn of " + prefab.name +
				    " exceeds cache size of " + cacheSize +
				    "! Reusing already active object.", obj);
			    ObjectPool.Destroy (obj);
		    }

		    // Increment index and make it loop around
		    // if it exceeds the size of the cache
		    cacheIndex = (cacheIndex + 1) % cacheSize;

		    return obj;
	    }
    }
		
	void OnDisable()
	{
#if UNITY_EDITOR
		if (debugEnableReport)
		{
			report();
		}
#endif
	}
	
    void Awake () 
    {
	    // Set the global variable
	    instance = this;

        enableObjectPool = false; // 기본상태에선 꺼짐

	    // Total number of cached objects
	    int amount = 0;

        // create cache holder
        cacheHolder = new GameObject();
        cacheHolder.name = "_CacheHolder";
        cacheHolder.transform.parent = this.transform;

	    // Loop through the caches
        for (int i = 0; i < caches.Length; i++)
        {
            // Initialize each cache
            caches[i].Initialize();

            // Count
            amount += caches[i].cacheSize;
        }

        initCachesLength = caches.Length;

	    // Create a hashtable with the capacity set to the amount of cached objects specified
	    activeCachedObjects = new Hashtable (amount);

        // 2013/06/02 create instant holder
        instantHolder = new GameObject();
        instantHolder.name = "_instantHolder";
        instantHolder.transform.parent = this.transform;

        // 2013/06/08 clear ammo object array
        //Ammo.ClearAmmoObjects();
    }

    static void Log(string text)
    {
        //Debug.Log(text);
    }

    static public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!instance)
        {
            return (GameObject)UnityEngine.Object.Instantiate(prefab);
        }

        if (instance && instance.enabled)
        {
            //인스턴스가 올라가 있는 상태에만 풀에서 스폰처리
            return ConcreteSpawn(prefab, position, rotation, parent);
        }

        Log("SPAWN Disabled! : " + prefab.name);

        return (GameObject)UnityEngine.Object.Instantiate(prefab);
    }

    static public GameObject ConcreteSpawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {       

	    ObjectCache cache = null;
        GameObject obj;

        if (prefab == null)
        {
            return null;
        }

	    // Find the cache for the specified prefab
	    if (ObjectPool.instance) 
        {
		    for ( int i = 0; i < ObjectPool.instance.caches.Length; i++)
            {
			    if (ObjectPool.instance.caches[i].prefab.name == prefab.name)
                {
				    cache = ObjectPool.instance.caches[i];
                    break;
			    }
		    }
	    }

	    // If there's no cache for this prefab type, just instantiate normally
        if (cache == null) 
        {
            Debug.LogWarning("Cache not found in Object Pool! " + prefab.name);

		    obj = Instantiate (prefab, position, rotation) as GameObject;
            // 2013/06/02 set parent if exist otherwise set to default manager parent to clean up hierarchy
            if (parent)
            {
                obj.transform.parent = parent;
            }
            else
            {
                obj.transform.parent = ObjectPool.instance.InstantHolder.transform;
            }
			
#if UNITY_EDITOR
            if (ObjectPool.instance)
            {
                ObjectPool.instance.addToReport(obj, false);
            }
#endif
            return obj;
        }

        Log("SPAWN: " + prefab.name);

	    // Find the next object in the cache

        //Log(prefab.name);

	    obj = cache.GetNextObjectInCache ();

	    // Set the position and rotation of the object
	    obj.transform.position = position;
	    obj.transform.rotation = rotation;

	    // Set the object to be active
        if(!obj.activeSelf)
	        obj.SetActive (true);

	    ObjectPool.instance.activeCachedObjects[obj] = true;

        // broadcast reset message
        //obj.BroadcastMessage("OnSoftReset", SendMessageOptions.DontRequireReceiver);
        obj.SendMessage("OnSoftReset", SendMessageOptions.DontRequireReceiver);

        //// 2013/05/30 if tk2d animated sprite with play automatically option. rewind it
        //tk2dAnimatedSprite animcomp = obj.GetComponent<tk2dAnimatedSprite>();
        //if (animcomp)
        //{
        //    if (animcomp.playAutomatically)
        //    {
        //        animcomp.Play();
        //    }
        //}

        //// 2013/06/18 if tk2d sprite animator with play automatically option. rewind it
        //tk2dSpriteAnimator sanim = obj.GetComponent<tk2dSpriteAnimator>();
        //if (sanim)
        //{
        //    if (sanim.playAutomatically)
        //    {
        //        sanim.Stop();
        //        sanim.Play();
        //    }
        //}

        // 2013/06/02 set parent if exist otherwise set to default manager parent to clean up hierarchy
        if (parent)
        {
            obj.transform.parent = parent;
        }
        else
        {
            obj.transform.parent = ObjectPool.instance.InstantHolder.transform;
        }

#if USE_ACTIVE_POOL
        //오브젝트가 살아있는 대신에 하위의 에니메이션이나 파티클은 수동으로 시작해야 한다.
        //하위가 아니고 오브젝트 본체에 붙어있는 경우는 어찌?
        ParticleSystem[] pts = obj.GetComponentsInChildren<ParticleSystem>();
        Animation ani = obj.GetComponentInChildren<Animation>();
        if (null != ani)
        {
            ani.Play();
        }
        foreach (ParticleSystem ps in pts)
        {
            //ps.gameObject.SetActive(true);
            ps.Play(true);
        }

        pts = obj.GetComponents<ParticleSystem>();
        ani = obj.GetComponent<Animation>();
        if (null != ani)
        {
            ani.Play();
        }
        foreach (ParticleSystem ps in pts)
        {
            //ps.gameObject.SetActive(true);
            ps.Play(true);
        }
#endif
#if UNITY_EDITOR
		ObjectPool.instance.addToReport(obj, true);
#endif
	    return obj;
    }

    static public void Destroy(GameObject objectToDestroy)
    {
        if (!instance)
        {
            UnityEngine.Object.Destroy(objectToDestroy);
            return;
        }

        // 풀을 사용하는 분기가 있는 경우 spawn을 하지 않는 분기를 타더라도
        // ObjectPool.Destroy 를 사용할것
	    if (ObjectPool.instance && ObjectPool.instance.activeCachedObjects.ContainsKey (objectToDestroy)) 
        {
#if !USE_ACTIVE_POOL
		    objectToDestroy.SetActive (false); ken test 20140109
#else
            if (objectToDestroy.name.StartsWith("item_get_01"))
            {
                objectToDestroy.SetActive(false); //이거 하나만 예외로 한다 ㅠㅠ
            }
            else
            {
                //오브젝트가 살아있는 대신에 하위의 에니메이션이나 파티클은 수동으로 멈춰야 한다.
                //하위가 아니고 오브젝트 본체에 붙어있는 경우는 어찌?
                objectToDestroy.transform.position = new Vector3(-1000, -1000, 0);
                ParticleSystem[] pts = objectToDestroy.GetComponentsInChildren<ParticleSystem>();
                Animation ani = objectToDestroy.GetComponentInChildren<Animation>();
                if (null != ani)
                {
                    ani.Stop();
                }
                foreach (ParticleSystem ps in pts)
                {
                    //ps.gameObject.SetActive(false);
                    ps.Stop();
                }
                pts = objectToDestroy.GetComponents<ParticleSystem>();
                ani = objectToDestroy.GetComponent<Animation>();
                if (null != ani)
                {
                    ani.Stop();
                }
                foreach (ParticleSystem ps in pts)
                {
                    //ps.gameObject.SetActive(false);
                    ps.Stop();
                }
            }
#endif
		    ObjectPool.instance.activeCachedObjects[objectToDestroy] = false;

            // return to cache holder
            if (objectToDestroy.transform.parent != ObjectPool.instance.CacheHolder)
            {
                objectToDestroy.transform.parent = ObjectPool.instance.CacheHolder.transform;
            }

            //Debug.Log("풀로반환:" + objectToDestroy);
	    }
	    else
        {
	        UnityEngine.Object.Destroy(objectToDestroy);
            //Debug.Log("삭제:" + objectToDestroy);
	    }
    }

    static public IEnumerator DestroyDelayed(GameObject objectToDestroy, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(objectToDestroy);
    }

    bool ContainObject(GameObject go)
    {
        foreach (ObjectCache ocnow in ObjectPool.instance.caches)
        {
            if (ocnow.prefab.name == go.name) return false;
        }
        return true;
    }

    public void ScanPrefabInEvent(List<GameObject> listMonsters)
    {
        if (!instance)
        {
            return;
        }

        // 오브젝트풀 spawn 가능상태로 전환
        ObjectPool.instance.enableObjectPool = true;
        

        Log("ScanPrefabInEvent()");
        // 플레이어의 ActorControllerAnimEvent들
        ActorControllerAnimEvent[] aniEventCtrls = (ActorControllerAnimEvent[]) FindObjectsOfType(typeof(ActorControllerAnimEvent));

        // 액터 전체의 ActorControllerAnimEvent들
        List<ActorControllerAnimEvent> listAnimEvent = new List<ActorControllerAnimEvent>(aniEventCtrls);

        foreach (GameObject go in listMonsters)
        {
            ActorControllerAnimEvent[] aniEventCtrlsMonster = (ActorControllerAnimEvent[])go.GetComponentsInChildren<ActorControllerAnimEvent>(true); // 비활성 포함

            foreach (ActorControllerAnimEvent now in aniEventCtrlsMonster)
            {
                listAnimEvent.Add(now);
            }
        }

        Log("ActorControllerAnimEvent COUNT : " + listAnimEvent.Count);

        foreach (ActorControllerAnimEvent now in listAnimEvent)
        {
            Log("character:" + now.name);

            string names = "PUSH IN POOL: " + now.animEventData.Length + " Objects\n";

            foreach (AnimEvent ae in now.animEventData)
            {
                names += ae.animName + "\n";
                if (ae.goEvent != null && ContainObject(ae.goEvent))
                {
                    Array.Resize(ref ObjectPool.instance.caches, ObjectPool.instance.caches.Length + 1);
                    int endidx = ObjectPool.instance.caches.Length - 1;

                    ObjectCache cache = new ObjectCache();
                    cache.cacheSize = 2; // 기본 2개
                    cache.prefab = ae.goEvent;
                    ObjectPool.instance.caches[endidx] = cache;
                    
                }
            }
            Log(names);
        }

        int amount = 0;

        // 수동 입력 이후의 프리팹들 인스턴스화
        for (int i = initCachesLength; i < caches.Length; i++)
        {
            // Initialize each cache
            caches[i].Initialize();
        }

        for (int i = 0; i < caches.Length; i++)
        {
            // Count
            amount += caches[i].cacheSize;
        }

        // Create a hashtable with the capacity set to the amount of cached objects specified
        activeCachedObjects = new Hashtable(amount);

    }

    public void ScanPrefabInEventPVP(GameObject enemy)
    {
        if (!instance)
        {
            return;
        }

        // 오브젝트풀 spawn 가능상태로 전환
        ObjectPool.instance.enableObjectPool = true;


        Log("ScanPrefabInEventPVP()");
        // 플레이어의 ActorControllerAnimEvent들
        ActorControllerAnimEvent[] aniEventCtrls = (ActorControllerAnimEvent[])FindObjectsOfType(typeof(ActorControllerAnimEvent));

        // 액터 전체의 ActorControllerAnimEvent들
        List<ActorControllerAnimEvent> listAnimEvent = new List<ActorControllerAnimEvent>(aniEventCtrls);

        ActorControllerAnimEvent[] aniEventCtrlsMonster = (ActorControllerAnimEvent[])enemy.GetComponentsInChildren<ActorControllerAnimEvent>(true);

        foreach (ActorControllerAnimEvent now in aniEventCtrlsMonster)
        {
            listAnimEvent.Add(now);
        }

        Log("ActorControllerAnimEvent COUNT : " + listAnimEvent.Count);

        foreach (ActorControllerAnimEvent now in listAnimEvent)
        {
            Log("character:" + now.name);

            string names = "PUSH IN POOL: " + now.animEventData.Length + " Objects\n";

            foreach (AnimEvent ae in now.animEventData)
            {
                names += ae.animName + "\n";
                if (ae.goEvent != null && ContainObject(ae.goEvent))
                {
                    Array.Resize(ref ObjectPool.instance.caches, ObjectPool.instance.caches.Length + 1);
                    int endidx = ObjectPool.instance.caches.Length - 1;

                    ObjectCache cache = new ObjectCache();
                    cache.cacheSize = 2; // 기본 2개
                    cache.prefab = ae.goEvent;
                    ObjectPool.instance.caches[endidx] = cache;

                }
            }
            Log(names);
        }

        int amount = 0;

        // 수동 입력 이후의 프리팹들 인스턴스화
        for (int i = initCachesLength; i < caches.Length; i++)
        {
            // Initialize each cache
            caches[i].Initialize();
        }

        for (int i = 0; i < caches.Length; i++)
        {
            // Count
            amount += caches[i].cacheSize;
        }

        // Create a hashtable with the capacity set to the amount of cached objects specified
        activeCachedObjects = new Hashtable(amount);

    }

}