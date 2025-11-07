using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class UIRipple : MonoBehaviour
{
    [SerializeField] float duration = 0.3f;
    [SerializeField] float endScale = 2.8f;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * 0.1f;
    }

    public void Play() => StartCoroutine(Run());

    IEnumerator Run()
    {
        float t = 0f; var c0 = sr.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            transform.localScale = Vector3.one * Mathf.Lerp(0.1f, endScale, k);
            var c = c0; c.a = Mathf.Lerp(c0.a, 0f, k); sr.color = c;

            yield return null;
        }
        Destroy(gameObject);
    }
}
