using UnityEngine;

public class CameraKick : MonoBehaviour
{
    public static CameraKick I;
    public float freq = 28f;
    float t, dur, amp;
    Vector3 basePos;

    void Awake(){ I = this; basePos = transform.localPosition; }

    public void Kick(float amplitude = 0.06f, float duration = 0.1f)
    { amp = amplitude; dur = duration; t = 0f; }

    void LateUpdate()
    {
        if (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = 1f - (t / dur);
            float offX = Mathf.Sin(t * freq) * amp * p;
            float offY = Mathf.Cos(t * freq * 0.8f) * amp * p;
            transform.localPosition = basePos + new Vector3(offX, offY, 0);
        }
        else transform.localPosition = basePos;
    }
}
