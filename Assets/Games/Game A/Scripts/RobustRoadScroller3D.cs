using System.Linq;
using UnityEngine;

public class RobustRoadScroller3D : MonoBehaviour
{
    public Transform[] segments;
    public float speed = 10f;

    float segLength;
    bool initialized;

    void Awake()
    {
        if (segments == null || segments.Length < 2)
        {
            enabled = false; return;
        }

        segLength = CalcSegmentLength(segments[0]);
        if (segLength <= 0.001f) segLength = 25f; // fallback

        segments = segments.OrderBy(t => t.position.z).ToArray();
        for (int i = 1; i < segments.Length; i++)
        {
            var prev = segments[i - 1];
            var cur = segments[i];
            cur.position = new Vector3(cur.position.x, cur.position.y, prev.position.z + segLength);
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        float dz = speed * Time.deltaTime;
        for (int i = 0; i < segments.Length; i++)
            segments[i].position += Vector3.back * dz;

        float furthestZ = float.MinValue;
        foreach (var s in segments) if (s.position.z > furthestZ) furthestZ = s.position.z;

        foreach (var s in segments)
        {
            if (s.position.z < -segLength)
            {
                s.position = new Vector3(s.position.x, s.position.y, furthestZ + segLength);
                furthestZ = s.position.z;
            }
        }
    }

    float CalcSegmentLength(Transform t)
    {
        var rend = t.GetComponentInChildren<Renderer>();
        return rend ? rend.bounds.size.z : 25f;
    }

    public void SetSpeed(float newSpeed) => speed = newSpeed;
    public float GetSpeed() => speed;
}
