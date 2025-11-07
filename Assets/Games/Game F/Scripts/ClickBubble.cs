using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ClickBubble : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] float defaultStartScale = 1.0f; // override gelmezse
    [SerializeField] float endScale          = 0.9f; // çok küçük olmasın, görünür kalsın

    SpriteRenderer sr;
    float life;
    bool playing, finished;
    BubbleSpawner owner;

    float initialScale; // sabit başlangıç ölçeği

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * defaultStartScale;
    }

    // lifeSeconds: bu bubble'ın yaşam süresi
    // startScaleOverride: >0 ise başlangıç ölçeğini set eder
    public void Play(float lifeSeconds, BubbleSpawner spawner, float startScaleOverride = -1f)
    {
        owner = spawner;
        life  = lifeSeconds;

        if (startScaleOverride > 0f)
            transform.localScale = Vector3.one * startScaleOverride;

        initialScale = transform.localScale.x; // BAŞLANGICI KİLİTLE
        playing = true;
        StartCoroutine(LifeRoutine());
    }

    IEnumerator LifeRoutine()
    {
        float t = 0f;
        var c0 = sr.color;

        while (t < life && !finished)
        {
            t += Time.deltaTime;
            float k = t / life;

            // daima SABİT initialScale'den endScale'e
            float s = Mathf.Lerp(initialScale, endScale, k);
            transform.localScale = Vector3.one * s;

            var c = c0; c.a = Mathf.Lerp(c0.a, 0.65f, k);
            sr.color = c;

            yield return null;
        }

        if (!finished)
        {
            finished = true; playing = false;
            owner?.NotifyMiss(this); // süre doldu → miss → game over
            Destroy(gameObject);
        }
    }

    void OnMouseDown()
    {
        if (!playing || finished) return;
        finished = true; playing = false;

        RhythmClickGameManager.I.OnBubbleClicked(transform.position);
        owner?.NotifyHit(this);

        Destroy(gameObject);
    }
}
