using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class BallJuice : MonoBehaviour
{
    public float stretchAmount = 0.08f;
    public float returnSpeed = 8f;

    Rigidbody2D rb;
    Vector3 baseScale;

    void Awake(){ rb = GetComponent<Rigidbody2D>(); baseScale = transform.localScale; }

    void Update()
    {
        float s = Mathf.Clamp01(rb.linearVelocity.magnitude / 8f) * stretchAmount;
        Vector3 target = new Vector3(baseScale.x * (1f - s), baseScale.y * (1f + s), 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * returnSpeed);
    }

    public void Pop(float percent = 0.06f, float dur = 0.08f)
    { StartCoroutine(PopCo(percent, dur)); }

    IEnumerator PopCo(float percent, float dur)
    {
        Vector3 a = transform.localScale;
        Vector3 b = a * (1f + percent);
        float t = 0f;
        while (t < dur){ t += Time.deltaTime; transform.localScale = Vector3.Lerp(a,b,t/dur); yield return null; }
        t = 0f;
        while (t < dur){ t += Time.deltaTime; transform.localScale = Vector3.Lerp(b,a,t/dur); yield return null; }
    }
}
