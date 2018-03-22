//#define DATA_MANAGER_IS_GAME_OBJECT
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// todo: 에셋자체가 너무 커질경우 동적으로 읽고, 필요없으면 내리는 작업을 추가한다.
/// </summary>
#if DATA_MANAGER_IS_GAME_OBJECT
public class DataManager : MonoBehaviour
{
#region Singleton

    private static DataManager s_Instance = null;

    public static DataManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType(typeof(DataManager)) as DataManager;
            }

            if (s_Instance == null)
            {
                GameObject obj = new GameObject("_DataManager");
                s_Instance = obj.AddComponent(typeof(DataManager)) as DataManager;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(obj);
                }
            }

            return s_Instance;
        }
    }
    #endregion Singleton
    
    public void CleanUp()
    {
       
        if (!Application.isPlaying)
        {
            _isInit = false;
            DestroyImmediate(this.gameObject);
        }
    }
#else
public class DataManager
{
    #region Singleton

    private static DataManager s_Instance = null;

    public static DataManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                //s_Instance = FindObjectOfType(typeof(DataManager)) as DataManager;
                s_Instance = new DataManager();
            }

            return s_Instance;
        }
    }

    #endregion Singleton

    public void CleanUp()
    {

        if (!Application.isPlaying)
        {
            _isInit = false;
            //DestroyImmediate(this.gameObject);
        }
    }
#endif
    public List<ScriptableObject> _allDatas = new List<ScriptableObject>();
    private static bool _isInit = false;

    public void Init()
    {
        _allDatas.AddRange(Resources.LoadAll<ScriptableObject>("DataAssets"));
    }

    public T GetData<T>(int key)
    {
        if (!_isInit)
        {
            _isInit = true;
            Init();
        }
        foreach (ScriptableObject s in _allDatas)
        {
            if (s.GetType().GetField("arrayData").FieldType.GetElementType() == typeof(T))
            {
                return (T)s.GetType().GetMethod("FindByKey").Invoke(s, new object[] { key });
            }
        }
        Debug.LogError("not find data type " + typeof(T) + " key=" + key);
        return default(T);
    }
    public T GetData<T>(string key)
    {
        if (!_isInit)
        {
            _isInit = true;
            Init();
        }
        foreach (ScriptableObject s in _allDatas)
        {
            if (s.GetType().GetField("arrayData").FieldType.GetElementType() == typeof(T))
            {
                return (T)s.GetType().GetMethod("FindByKey").Invoke(s, new object[] { key });
            }
        }
        Debug.LogError("not find data type " + typeof(T) + " key=" + key);
        return default(T);
    }

    public T[] GetDataTable<T>()
    {
        if (!_isInit)
        {
            _isInit = true;
            Init();
        }
        foreach (ScriptableObject s in _allDatas)
        {
            if (s.GetType().GetField("arrayData").FieldType.GetElementType() == typeof(T))
            {
                return (T[])s.GetType().GetField("arrayData").GetValue(s);
            }
        }
        Debug.LogError("not find data type " + typeof(T));
        return default(T[]);
    }
    public List<T> GetDataTable<T>(Predicate<T> match)
    {
        if (!_isInit)
        {
            _isInit = true;
            Init();
        }
        foreach (ScriptableObject s in _allDatas)
        {
            if (s.GetType().GetField("arrayData").FieldType.GetElementType() == typeof(T))
            {
                return ((T[])s.GetType().GetField("arrayData").GetValue(s)).ToList().FindAll(match);
            }
        }
        Debug.LogError("not find data type " + typeof(T));
        return null;
    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed
    /// 싱글톤이 부서질 일이 없지만 Reset을 겪을 경우 새로 만들어 지기 때문에 필요하다.
    /// </summary>
    public void OnDestroy()
    {
        _isInit = false;
    }
}