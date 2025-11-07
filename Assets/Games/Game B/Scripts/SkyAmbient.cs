using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyAmbient : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Bulut sprite'ından yaptığın prefab")]
    public GameObject cloudPrefab;
    [Tooltip("Kuş sprite'ından yaptığın prefab")]
    public GameObject birdPrefab;

    [Header("Spawn Sıklığı (saniye)")]
    [Tooltip("Bulut: her seferinde 1 adet, 15–25 sn arası rastgele")]
    public Vector2 cloudIntervalRange = new Vector2(15f, 25f);
    [Tooltip("Kuş: her seferinde 1 adet, 25–35 sn arası rastgele")]
    public Vector2 birdIntervalRange  = new Vector2(25f, 35f);

    [Header("Hız (soldan → sağa, dünya birimi/sn)")]
    public Vector2 cloudSpeedRange = new Vector2(0.4f, 0.8f);
    public Vector2 birdSpeedRange  = new Vector2(1.0f, 1.6f);

    [Header("Ölçek (uniform)")]
    public Vector2 cloudScaleRange = new Vector2(0.6f, 0.95f);
    public Vector2 birdScaleRange  = new Vector2(0.65f, 0.95f);

    [Header("Dikey dağılım (Viewport Y)")]
    [Tooltip("0=alt, 1=üst. Örn: 0.15–0.9 arasında rastgele")]
    public Vector2 viewportYRangeCloud = new Vector2(0.15f, 0.9f);
    public Vector2 viewportYRangeBird  = new Vector2(0.2f,  0.9f);

    [Header("Kenar ofsetleri (Viewport X)")]
    [Tooltip("Sol kenarın bu kadar SOLUNDAN başlasın (negatif tarafa). Örn: 0.05")]
    public float leftSpawnPadding = 0.05f;   // 0 → tam çizgi, 0.05 → çizginin biraz solu
    [Tooltip("Sağda şu kadar DIŞARI çıkınca yok et (pozitif). Örn: 0.05")]
    public float rightKillPadding  = 0.05f;

    [Header("Limitler (güvenlik)")]
    public int maxActiveClouds = 6;
    public int maxActiveBirds  = 2;

    // ---- Dahili ----
    Camera cam;

    class Moving {
        public Transform t;
        public float speed;
        public float baseY;
        // Kuş yalpalama
        public bool wobble;
        public float ampY, ampX, freq, acc;
    }

    readonly List<Moving> clouds = new();
    readonly List<Moving> birds  = new();

    Coroutine cloudRoutine, birdRoutine;

    void Awake()
    {
        cam = Camera.main;
    }

    void OnEnable()
    {
        cloudRoutine = StartCoroutine(CloudLoop());
        birdRoutine  = StartCoroutine(BirdLoop());
    }

    void OnDisable()
    {
        if (cloudRoutine != null) StopCoroutine(cloudRoutine);
        if (birdRoutine  != null) StopCoroutine(birdRoutine);
        clouds.Clear(); birds.Clear();
    }

    void Update()
    {
        // Sağ kill çizgisi (viewport → world)
        float rightKillX = cam.ViewportToWorldPoint(new Vector3(1f + rightKillPadding, 0f, 0f)).x;

        // Bulutlar: düz sağa
        for (int i = clouds.Count - 1; i >= 0; i--)
        {
            var m = clouds[i];
            var p = m.t.position;
            p.x += m.speed * Time.deltaTime;
            m.t.position = p;

            if (p.x > rightKillX) { Destroy(m.t.gameObject); clouds.RemoveAt(i); }
        }

        // Kuşlar: yalpalı
        for (int i = birds.Count - 1; i >= 0; i--)
        {
            var m = birds[i];
            var p = m.t.position;
            p.x += m.speed * Time.deltaTime;

            m.acc += Time.deltaTime * m.freq;
            p.y = m.baseY + Mathf.Sin(m.acc) * m.ampY;
            p.x += Mathf.Sin(m.acc * 0.7f) * m.ampX * Time.deltaTime;

            m.t.position = p;

            if (p.x > rightKillX) { Destroy(m.t.gameObject); birds.RemoveAt(i); }
        }
    }

    // ----------------- Coroutines -----------------
    IEnumerator CloudLoop()
    {
        while (true)
        {
            float wait = RandomRangeSafe(cloudIntervalRange);
            yield return new WaitForSeconds(wait);

            if (cloudPrefab == null || clouds.Count >= maxActiveClouds) continue;
            SpawnCloud();
        }
    }

    IEnumerator BirdLoop()
    {
        while (true)
        {
            float wait = RandomRangeSafe(birdIntervalRange);
            yield return new WaitForSeconds(wait);

            if (birdPrefab == null || birds.Count >= maxActiveBirds) continue;
            SpawnBird();
        }
    }

    // ----------------- Spawns -----------------
    void SpawnCloud()
    {
        // Y: kameranın içinde rastgele (viewport)
        float vY = RandomRangeSafe(viewportYRangeCloud);
        float y  = cam.ViewportToWorldPoint(new Vector3(0f, vY, 0f)).y;

        // X: sol çizginin biraz SOLUNDAN başla (ekran dışında)
        float xLeftOff = cam.ViewportToWorldPoint(new Vector3(0f - Mathf.Abs(leftSpawnPadding), 0f, 0f)).x;

        var go = Instantiate(cloudPrefab, new Vector3(xLeftOff, y, 0f), Quaternion.identity);

        float s = RandomRangeSafe(cloudScaleRange);
        go.transform.localScale = Vector3.one * s;

        clouds.Add(new Moving
        {
            t = go.transform,
            speed = RandomRangeSafe(cloudSpeedRange),
            baseY = y,
            wobble = false
        });
    }

    void SpawnBird()
    {
        float vY = RandomRangeSafe(viewportYRangeBird);
        float y  = cam.ViewportToWorldPoint(new Vector3(0f, vY, 0f)).y;
        float xLeftOff = cam.ViewportToWorldPoint(new Vector3(0f - Mathf.Abs(leftSpawnPadding), 0f, 0f)).x;

        var go = Instantiate(birdPrefab, new Vector3(xLeftOff, y, 0f), Quaternion.identity);

        float s = RandomRangeSafe(birdScaleRange);
        go.transform.localScale = Vector3.one * s;

        birds.Add(new Moving
        {
            t = go.transform,
            speed = RandomRangeSafe(birdSpeedRange),
            baseY = y,
            wobble = true,
            ampY = Random.Range(0.10f, 0.20f),   // yukarı-aşağı
            ampX = Random.Range(0.03f, 0.07f),   // hafif sağ-sol
            freq = Random.Range(1.5f, 2.5f),
            acc = 0f
        });
    }

    // ----------------- Helpers -----------------
    static float RandomRangeSafe(Vector2 r)
    {
        float a = Mathf.Min(r.x, r.y);
        float b = Mathf.Max(r.x, r.y);
        return Random.Range(a, b);
    }
}
