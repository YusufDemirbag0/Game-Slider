using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class MergeEvent : UnityEvent<int, Vector2> { } // (newLevel, pos)
[System.Serializable] public class BallSpawnEvent : UnityEvent<GameObject> { }

public class MergeManager : MonoBehaviour
{
    public static MergeManager I;

    [Header("Prefabs: 0 Golf, 1 Tenis, 2 Beyzbol, 3 Voleybol, 4 Basketbol, 5 Futbol, 6 Bowling")]
    public List<GameObject> ballPrefabs;
    public int MaxLevel => ballPrefabs.Count - 1;

    [Header("Drop Havuzu (Başlangıç)")]
    [Range(0,4)] public int startMaxDropLevel = 2; // 0..2 = ilk 3 top
    [HideInInspector] public int currentMaxDropLevel;

    [Header("İlerleme Eşikleri (Skor)")]
    public int unlock3_atScore = 300;   // Voleybol
    public int unlock4_atScore = 900;   // Basketbol

    [Header("Skor (iç kaynak opsiyonu)")]
    [Tooltip("Skoru MergeManager içinde tutmak istemiyorsan kapalı bırak (önerilen: BallGameManager yönetsin).")]
    public bool useInternalScore = false;
    public int scorePerMerge = 10;
    public int Score { get; private set; }

    [Header("Juice / FX (opsiyonel)")]
    public GameObject[] popFxByLevel;      // uzunluk: ballPrefabs.Count (boş bırakılabilir)
    public AudioClip[] mergeSfxByLevel;     // uzunluk: ballPrefabs.Count (boş bırakılabilir)
    public AudioSource sfx;                 // yoksa ses çalmaz
    public CameraKick cameraKick;           // varsa sarsar
    public float kickBaseAmp = 0.06f, kickPerLevel = 0.01f, kickDuration = 0.10f;

    [Header("Olaylar (event)")]
    public MergeEvent OnMerged;             // (newLevel, worldPos)
    public BallSpawnEvent OnBallSpawned;    // yeni top doğduğunda

    // Ağırlıklar: düşük seviye daha olası (drop için)
    readonly int[] weights = { 50, 35, 15, 8, 4 }; // 0..4

    void Awake()
    {
        I = this;
        currentMaxDropLevel = Mathf.Clamp(startMaxDropLevel, 0, 4);
    }

    // --- Skor & İlerleme (iç kaynak) ---
    public void AddScore(int s)
    {
        Score += s;
        if (Score >= unlock4_atScore) currentMaxDropLevel = Mathf.Max(currentMaxDropLevel, 4);
        else if (Score >= unlock3_atScore) currentMaxDropLevel = Mathf.Max(currentMaxDropLevel, 3);
    }

    // --- Skor & İlerleme (dış kaynaktan senkron) ---
    public void SyncProgressFromExternalScore(int totalScore)
    {
        if (totalScore >= unlock4_atScore) currentMaxDropLevel = Mathf.Max(currentMaxDropLevel, 4);
        else if (totalScore >= unlock3_atScore) currentMaxDropLevel = Mathf.Max(currentMaxDropLevel, 3);
    }

    // --- Drop seçimi ---
    public GameObject GetRandomDroppable()
    {
        int maxIdx = Mathf.Min(currentMaxDropLevel, 4);
        int total = 0;
        for (int i = 0; i <= maxIdx; i++) total += weights[i];

        int r = Random.Range(0, total), acc = 0;
        for (int i = 0; i <= maxIdx; i++)
        {
            acc += weights[i];
            if (r < acc) return ballPrefabs[i];
        }
        return ballPrefabs[0];
    }

    // Dışarıdan spawn edenler çağırabilir (Dropper kullanıyor)
    public GameObject SpawnBall(int level, Vector2 pos)
    {
        var go = Instantiate(ballPrefabs[level], pos, Quaternion.identity);
        OnBallSpawned?.Invoke(go);
        return go;
    }

    // --- Merge işlemi (Ball.OnCollisionEnter2D’den çağrılır) ---
    public void Merge(Ball a, Ball b, Vector2 contactPoint)
    {
        int next = Mathf.Min(a.level + 1, MaxLevel);

        // Ortak konum (ağırlıklı ortalama)
        Vector2 pos = (a.rb.position * a.rb.mass + b.rb.position * b.rb.mass) / (a.rb.mass + b.rb.mass);

        // Yeni top
        var go = Instantiate(ballPrefabs[next], pos, Quaternion.identity);
        var nb = go.GetComponent<Ball>();
        nb.level = next;

        // Momentumun bir kısmını taşı
        nb.rb.linearVelocity  = (a.rb.linearVelocity  + b.rb.linearVelocity)  * 0.35f;
        nb.rb.angularVelocity = (a.rb.angularVelocity + b.rb.angularVelocity) * 0.35f;

        // (Opsiyonel) İç skor
        if (useInternalScore)
            AddScore(scorePerMerge * (next + 1));

        // FX: Parçacık
        if (popFxByLevel != null && next < popFxByLevel.Length && popFxByLevel[next])
            Instantiate(popFxByLevel[next], pos, Quaternion.identity);

        // FX: Ses
        if (sfx && mergeSfxByLevel != null && next < mergeSfxByLevel.Length && mergeSfxByLevel[next])
            sfx.PlayOneShot(mergeSfxByLevel[next], 0.6f);

        // FX: Kamera sarsıntısı
        if (cameraKick) cameraKick.Kick(kickBaseAmp + kickPerLevel * next, kickDuration);

        // Skoru BallGameManager yönetsin (combo vs.)
        BallGameManager.Instance?.OnMerged(next);

        // Event (dış sistemlere pos ile haber ver)
        OnMerged?.Invoke(next, pos);

        Destroy(a.gameObject);
        Destroy(b.gameObject);
    }
}
