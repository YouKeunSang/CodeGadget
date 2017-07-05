using UnityEngine;
using System.Collections;

[AddComponentMenu("Effects/SelfDestructer")]
public class SelfDestructer : MonoBehaviour
{
    public float selfDestructTime;
    public bool useDefaultAnimClipTime;
    public bool isFadeout = false;

    float destroyTime;
    UIPanel panel;

    // Use this for initialization
    void Start()
    {
        Animation animComp = GetComponent<Animation>();
        if (animComp && useDefaultAnimClipTime)
        {
            selfDestructTime = animComp.clip.length;
        }
       
        OnSoftReset();
    }

    void FixedUpdate()
    {
        if (destroyTime + selfDestructTime < Time.time)
        {
            ObjectPool.Destroy(gameObject);
        }
        if (isFadeout&&null!=panel)
        {
            panel.alpha = 1.0f - (Time.time - destroyTime) / selfDestructTime;
        }
    }

    void OnSoftReset()
    {
        destroyTime = Time.time;
        if (isFadeout)
        {
            panel = GetComponent<UIPanel>();
            if (panel)
            {
                panel.alpha = 1.0f;
            }
        }
        if (!ObjectPool.instance)
        {
            return;
        }

        // 파티클이 종속되어있을경우 리셋해줌
        ParticleSystem [] particleSystems = GetComponentsInChildren<ParticleSystem>();
        if (particleSystems.Length>0)
        {
            foreach (ParticleSystem psys in particleSystems)
            {
                psys.time = 0;
                psys.Clear(true);
            }
        }
    }
}