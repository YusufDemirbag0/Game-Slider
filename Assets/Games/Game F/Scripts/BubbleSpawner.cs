using UnityEngine;
using System.Collections;

public class BubbleSpawner : MonoBehaviour
{
    [Header("Camera & Area")]
    [SerializeField] Camera cam;
    [SerializeField] float safeMarginWorld = 0.8f;
    [SerializeField] float worldZ = 0f;

    [Header("Spawn Aralığı (Tempo)")]
    [SerializeField] float startInterval  = 1.10f;
    [SerializeField] float minInterval    = 0.40f;
    [SerializeField] float intervalPerHit = -0.015f;

    [Header("Bubble Ömrü")]
    [SerializeField] float startLifeSeconds = 1.30f;
    [SerializeField] float minLifeSeconds   = 0.60f;
    [SerializeField] float lifePerHit       = -0.010f;

    [Header("Başlangıç Ölçek Aralığı")]
    [SerializeField] float startScaleMin = 2.8f;
    [SerializeField] float startScaleMax = 3.4f;

    [Header("Hit Başına Ölçek Azaltma")]
    [SerializeField] float scaleStepPerHit = 0.015f;
    [SerializeField] float floorScaleMin   = 1.1f;
    [SerializeField] float floorScaleMax   = 1.6f;

    [Header("Diğer")]
    [SerializeField] float startDelay = 0.35f;

    float currentInterval;
    float currentLife;
    float curScaleMin;
    float curScaleMax;

    bool running;
    public ClickBubble currentBubble { get; private set; }

    public float TempoMultiplier => startInterval / Mathf.Max(currentInterval, 0.0001f);

    void ResetProgression()
    {
        currentInterval = startInterval;
        currentLife     = startLifeSeconds;
        curScaleMin     = startScaleMin;
        curScaleMax     = startScaleMax;
    }

    public void Begin()
    {
        if (!cam) cam = Camera.main;
        ResetProgression();
        running = true;
        StartCoroutine(SpawnLoop(startDelay));
    }

    // Game Over sırasında çağrılır: sadece durdur, progresyonu ELLEME
    public void StopSpawning()
    {
        running = false;
        StopAllCoroutines();
        if (currentBubble) { Destroy(currentBubble.gameObject); currentBubble = null; }
    }

    public void ResetSpawner()
    {
        StopSpawning();
        ResetProgression();
    }

    // CONTINUE: progresyonu koru, kısa bir gecikmeden sonra devam et
    public void ContinueFromPause(float delay = 0.5f)
    {
        if (!cam) cam = Camera.main;
        running = true;
        StartCoroutine(SpawnLoop(Mathf.Max(0f, delay)));
    }

    public void OnSuccessfulHit()
    {
        currentInterval = Mathf.Max(minInterval, currentInterval + intervalPerHit);
        currentLife     = Mathf.Max(minLifeSeconds, currentLife + lifePerHit);
        curScaleMin = Mathf.Max(floorScaleMin, curScaleMin - scaleStepPerHit);
        curScaleMax = Mathf.Max(floorScaleMax, curScaleMax - scaleStepPerHit);
        if (curScaleMax < curScaleMin) curScaleMax = curScaleMin;
    }

    IEnumerator SpawnLoop(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        while (running)
        {
            // Spawn
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float x = Random.Range(-halfW + safeMarginWorld, halfW - safeMarginWorld);
            float y = Random.Range(-halfH + safeMarginWorld, halfH - safeMarginWorld);
            Vector3 pos = cam.transform.position + new Vector3(x, y, 0f);
            pos.z = worldZ;

            var bubble = Instantiate(RhythmClickGameManager.I.bubblePrefab, pos, Quaternion.identity);
            currentBubble = bubble;

            float startScale = Random.Range(curScaleMin, curScaleMax);
            bubble.Play(currentLife, this, startScale);

            // Bu bubble bitmeden yenisini basma
            yield return new WaitUntil(() => currentBubble == null || currentBubble.Equals(null));
            if (!running) yield break;

            yield return new WaitForSeconds(currentInterval);
        }
    }

    public void NotifyMiss(ClickBubble b)
    {
        if (currentBubble == b) currentBubble = null;
        RhythmClickGameManager.I.OnBubbleMissed();
    }

    public void NotifyHit(ClickBubble b)
    {
        if (currentBubble == b) currentBubble = null;
    }
}
