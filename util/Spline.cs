using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spline : MonoBehaviour {

    public bool closedCurve = false;

    public bool alignTangent = false;

    public List<SplineSegment> segments = new List<SplineSegment>();

    List<Vector3> modCV = new List<Vector3>();

    public bool showDebugAxisLocalRight = false;
    public bool showDebugAxisLocalUp = false;

    public bool flipTangent = false;

    [Range(0.01f, 10.0f)]
    public float gizmoSize = 1.0f;
    
    float gizmoSizePrev = 1.0f;

    [Range(10, 80)]
    public int betweenNodeCount = 10;
    int prevBetweenNodeCount = 10;

    public float wholelength = 0;

    public int samples = 0;

    public struct TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float distance;
    }

    public List<TransformData> cache = new List<TransformData>();


   
    public void DistanceToWorldPosition(float positionInTrack, out Vector3 outpos, out Quaternion outrot, bool useCustomUpVector)
    {
        if (cache == null && cache.Count <= 0)  CalculateCache(); 

        outpos = cache[0].position;
        outrot = cache[0].rotation;

        if (positionInTrack <= 0)return;
        if (positionInTrack >= wholelength) positionInTrack = wholelength;


        float accumDistance = 0;
        for(int i=0; i<cache.Count-1; i++)
        {
            float currentsegmentDistance = cache[i + 1].distance;
            if(currentsegmentDistance==0.0f) continue;

            float d2 = accumDistance + currentsegmentDistance;
            if(accumDistance<positionInTrack && positionInTrack<=d2)
            {
                float posInSegment = (positionInTrack-accumDistance)/currentsegmentDistance;
                outpos = Vector3.Lerp(cache[i].position,cache[i+1].position,posInSegment);
                outrot = Quaternion.Slerp(cache[i].rotation, cache[i + 1].rotation, posInSegment);
                break;                
            }
            accumDistance = d2;
        }
    }




    public bool isStart(SplineSegment seg)
    {
        if(ValidateCount())
        {
            if(segments[0] == seg) return true;
        }

        return false;
    }

    public bool isEnd(SplineSegment seg)
    {
        if (ValidateCount())
        {
            if (segments[segments.Count-1] == seg) return true;
        }
        return false;
    }

    public void AddNext(SplineSegment seg)
    {
        if (!ValidateCount()) return;
        Vector3 createPos = seg.transform.position;

        for (int i = 0; i < segments.Count-1; i++)
        {
            if (segments[i] == seg)
            {
                //Vector3 nextPos = segments[i + 1].transform.position;
                createPos = CalcPositionAtTimeMod(0.5f, i);
                GameObject go = new GameObject();
                SplineSegment newseg = go.AddComponent<SplineSegment>();
                go.transform.parent = transform;
                go.transform.position = createPos;
                go.name = "Segment";
                segments.Insert(i + 1, newseg);
                break;
            }
        }
    }

    public void AddPrev(SplineSegment seg)
    {
        if (!ValidateCount()) return;
        Vector3 createPos = seg.transform.position;

        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] == seg)
            {
                //Vector3 nextPos = segments[i - 1].transform.position;
                createPos = CalcPositionAtTimeMod(0.5f, i-1);
                GameObject go = new GameObject();
                SplineSegment newseg = go.AddComponent<SplineSegment>();
                go.transform.parent = transform;
                go.transform.position = createPos;
                go.name = "Segment";
                segments.Insert(i, newseg);
                break;
            }
        }        
    }

    public SplineSegment GetNextSegment(SplineSegment seg)
    {
        if (!ValidateCount()) return null;
        for (int i = 0; i < segments.Count - 1; i++)
        {
            if (segments[i] == seg)
            {
                return segments[i+1];
            }
        }
        return seg;
    }

    public SplineSegment GetPrevSegment(SplineSegment seg)
    {
        if (!ValidateCount()) return null;
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] == seg)
            {
                return segments[i - 1];
            }
        }
        return seg;
    }

    public SplineSegment GetStartSegment()
    {
        if (!ValidateCount()) return null;
        return segments[0];
    }

    public SplineSegment GetEndSegment()
    {
        if (!ValidateCount()) return null;
        return segments[segments.Count-1];
    }

    public void RecalcGizmowScale()
    {
        Vector3 min = segments[0].transform.position;
        Vector3 max = segments[0].transform.position;
        foreach(SplineSegment seg in segments)
        {
            if (min.x > seg.transform.position.x) min.x = seg.transform.position.x;
            if (min.y > seg.transform.position.y) min.y = seg.transform.position.y;
            if (min.z > seg.transform.position.z) min.z = seg.transform.position.z;
            if (max.x < seg.transform.position.x) max.x = seg.transform.position.x;
            if (max.y < seg.transform.position.y) max.y = seg.transform.position.y;
            if (max.z < seg.transform.position.z) max.z = seg.transform.position.z;
        }

        Bounds bound = new Bounds();
        bound.SetMinMax(min, max);
        Vector3 size = bound.size;

        float maxSize = Mathf.Max(size.x, size.y);
        maxSize = Mathf.Max(maxSize, size.z);

        float gizmosScale = maxSize * 0.3f;
        foreach (SplineSegment seg in segments)
        {
            seg.gizmoScale = gizmosScale;
        }
    }

    bool ValidateCount()
    {
        if (segments.Count > 1)
        {
            return true;
        }
        return false;
    }

    public void AddCV(SplineSegment seg)
    {
        segments.Add(seg);
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        // 미싱 세그먼트 삭제
        segments.RemoveAll(item => item == null);
	}

    public float CalculateCache(bool dirty = false)
    {
        segments.RemoveAll(item => item == null);        
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].dirty)
            {                
                dirty = true;
                segments[i].dirty = false;
            }
        }

        if (cache == null || cache.Count == 0) dirty = true;

        if (prevBetweenNodeCount != betweenNodeCount) dirty = true;
        prevBetweenNodeCount = betweenNodeCount;


        if (!dirty) return wholelength;
          
        Debug.Log("Resampling...");

        float length = 0;
        samples = 0;

        Vector3 prevPos = segments[0].transform.position;

        cache.Clear();
        modCV.Clear();

        if (!closedCurve) modCV.Add(prevPos);
        else modCV.Add(segments[segments.Count-1].transform.position);

        for (int i = 0; i < segments.Count; i++)
        {
            modCV.Add(segments[i].transform.position);
        }

        if (closedCurve)
        {
            modCV.Add(segments[0].transform.position);
            modCV.Add(segments[1].transform.position);
        }
        else modCV.Add(segments[segments.Count - 1].transform.position);

        if (closedCurve)
        {
            prevPos = CalcPositionAtTimeMod((float)(betweenNodeCount - 1) / (float)betweenNodeCount, modCV.Count - 4);
            //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //go.transform.position = prevPos;
        }

        float deltaSub = 1.0f / betweenNodeCount;

        for (int cvidx = 2; cvidx < modCV.Count - 1; cvidx++)
        {
            Vector3 up1 = segments[cvidx - 2 < segments.Count ? cvidx - 2 : segments.Count-1 ].transform.up;
            Vector3 up2 = segments[cvidx - 1 < segments.Count ? cvidx - 1 : 0].transform.up;

            for (int sampleIdx = 0; sampleIdx <= betweenNodeCount; sampleIdx++)
            {
                Vector3 currPos = CalcPositionAtTimeMod((float)sampleIdx / (float)betweenNodeCount, cvidx-2);
                Vector3 currPosm01 = CalcPositionAtTimeMod((float)sampleIdx / (float)betweenNodeCount - deltaSub, cvidx - 2);
                Vector3 currUp = Vector3.Slerp(up1, up2, (float)sampleIdx / (float)betweenNodeCount);
                Vector3 tangent = currPosm01 - currPos;
                tangent.Normalize();
                currUp.Normalize();
                Vector3 right = Vector3.Cross(tangent, currUp);
                right.Normalize();

                TransformData td = new TransformData();
                td.position = currPos;
                td.distance = Vector3.Distance(currPos, prevPos);
                Vector3 orthUp = Vector3.Cross(right, tangent);
                orthUp.Normalize();

                td.rotation = Quaternion.LookRotation(tangent, orthUp);

                if (sampleIdx < betweenNodeCount)
                {
                    cache.Add(td);
                    length += td.distance;
                    prevPos = currPos;
                    samples++;
                }
                else if (cvidx == modCV.Count - 2)
                {
                    cache.Add(td);
                    if (!closedCurve) length += td.distance;
                    prevPos = currPos;
                    samples++;
                }
            }
        }

        wholelength = length;
        return length;
    }


    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        segments.RemoveAll(item => item == null);

        if (gizmoSizePrev != gizmoSize)
        {
            foreach (var seg in segments)
            {
                seg.gizmoScale = gizmoSize;
            }
            gizmoSizePrev = gizmoSize;
        }

        if(segments.Count < 2) return;

        segments[0].isStart = true;
        for (int i = 1; i < segments.Count; i++) segments[i].isStart = false;


        CalculateCache();

        Vector3 prevPos = segments[0].transform.position;
        for(int i=1; i<cache.Count; i++)
        {
            Vector3 currPos = cache[i].position;
            Vector3 up = cache[i].rotation * Vector3.up;
            Vector3 right = cache[i].rotation * Vector3.right;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(prevPos, currPos);

            if (showDebugAxisLocalUp)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(currPos, currPos + up * gizmoSize * 3.0f);
            }

            if (showDebugAxisLocalRight)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(currPos, currPos + right * gizmoSize * 3.0f);
            }

            prevPos = currPos;
        }        

    }

    void OnDrawGizmosSelected()
    {
    }

    #endif

    private Vector3 CalcPositionAtTimeMod(float t, int segidx)
    {
        float u = t;
        Vector3 a = modCV[segidx + 0];
        Vector3 b = modCV[segidx + 1];
        Vector3 c = modCV[segidx + 2];
        Vector3 d = modCV[segidx + 3];

        return .5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u)
            + (2f * a - 5f * b + 4f * c - d) * (u * u)
            + (-a + c) * u
            + 2f * b
        );
    }

    private Vector3 CalcPositionAtTime(float t, int segidx)
    {
        float u = t;
        Vector3 a = segments[segidx + 0].transform.position;
        Vector3 b = segments[segidx + 1].transform.position;
        Vector3 c = segments[segidx + 2].transform.position;
        Vector3 d = segments[segidx + 3].transform.position;

        return .5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u)
            + (2f * a - 5f * b + 4f * c - d) * (u * u)
            + (-a + c) * u
            + 2f * b
        );
    }
}
