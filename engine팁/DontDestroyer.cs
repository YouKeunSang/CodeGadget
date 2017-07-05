using UnityEngine;
using System.Collections;

public class DontDestroyer : MonoBehaviour 
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
