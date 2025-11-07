using System.Collections.Generic;
using UnityEngine;

/// Arkasında yakın takip -> komşu şeride geçince puan.
/// PrefabCar şartı yok; Obstacle tag/layer ile çalışır.
[RequireComponent(typeof(Collider))]
public class NearMissScorer : MonoBehaviour
{
    [Header("Şeritler")]
    public float[] laneX = new float[] { -6f, -2f, 2f, 6f };
    [Tooltip("Oyuncunun bir şeritte sayılması için X toleransı")]
    public float laneSnapEpsilon = 1.2f;

    [Header("Yakın Takip (Ön kutu)")]
    [Tooltip("Ön tarafa koyulan kutunun Z uzunluğu")]
    public float nearAheadZ = 6.5f;
    [Tooltip("Kutunun yarım genişliği (X)")]
    public float halfBoxX = 1.2f;
    [Tooltip("Kutunun Y yüksekliği (yarısı)")]
    public float halfBoxY = 1.0f;
    [Tooltip("Kutunun merkezine verilecek Y ofseti")]
    public float boxCenterYOffset = 0.5f;
    [Tooltip("Sadece bu maskteki collider’ları say")]
    public LayerMask detectMask = ~0;
    [Tooltip("İsteğe bağlı: Yalnızca bu tag’li objeleri dikkate al (boş bırakılırsa tag filtrelenmez)")]
    public string obstacleTag = "Obstacle";

    [Header("Zaman/Puan")]
    [Tooltip("Yakın takip işaretinden sonra bu süre içinde şerit değişirse puan ver")]
    public float passWindow = 0.9f;
    [Tooltip("Komşu şeride değişim tetiklenince en geç bu kadar zamanda ödüllendir")]
    public float awardOnLaneChangeWithin = 0.4f;
    public int passScore = 100;
    public float globalCooldown = 0.6f;

    // durum
    int currentLane = -1, previousLane = -1;
    float lastLaneChangeTime = -999f;

    float lastCloseMarkTime = -999f;
    Collider lastCloseCollider;

    float lastAwardTime = -999f;

    void Start()
    {
        currentLane = NearestLaneIndex(transform.position.x);
        previousLane = currentLane;
    }

    void Update()
    {
        if (GameManager.Instance && GameManager.Instance.IsGameOver) return;

        float now = Time.time;

        // 1) Oyuncu şu an hangi şeritte?
        int newLane = NearestLaneIndex(transform.position.x);
        if (newLane != currentLane && Mathf.Abs(newLane - currentLane) == 1)
        {
            previousLane = currentLane;
            currentLane = newLane;
            lastLaneChangeTime = now;

            // Şerit değişti: yakın takip işareti tazeyse puan ver
            TryAward(now);
        }
        else if (newLane != currentLane)
        {
            // 2'den fazla şerit atlamaları sayma
            previousLane = currentLane;
            currentLane = newLane;
            lastLaneChangeTime = now;
        }

        // 2) Ön kutu: aynı şeritte önde yakın araç var mı?
        MarkCloseAhead(now);
    }

    void MarkCloseAhead(float now)
    {
        // Ön kutunun dünya merkezi/yarıçapları
        Vector3 p = transform.position;
        Vector3 center = new Vector3(p.x, p.y + boxCenterYOffset, p.z + nearAheadZ * 0.5f);
        Vector3 halfExtents = new Vector3(halfBoxX, halfBoxY, nearAheadZ * 0.5f);

        var cols = Physics.OverlapBox(center, halfExtents, Quaternion.identity, detectMask, QueryTriggerInteraction.Collide);
        if (cols == null || cols.Length == 0) return;

        // Aynı şeritte olan ilk "Obstacle"ı bul
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (!c || c.transform == transform) continue;

            if (!string.IsNullOrEmpty(obstacleTag) && !c.CompareTag(obstacleTag)) continue;

            // Aynı şerit onayı
            float cx = c.transform.position.x;
            if (Mathf.Abs(cx - laneX[currentLane]) > laneSnapEpsilon) continue;

            // Yakın takip işareti
            lastCloseMarkTime = now;
            lastCloseCollider = c;
            return;
        }
    }

    void TryAward(float now)
    {
        // Komşu şerit değilse veya pencere kaçtıysa atla
        if (now - lastLaneChangeTime > awardOnLaneChangeWithin) return;

        // Yakın takip işareti taze mi?
        if (now - lastCloseMarkTime > passWindow) return;

        // Küresel cooldown
        if (now - lastAwardTime < globalCooldown) return;

        // Puan!
        GameManager.Instance?.AddScore(passScore);
        GameManager.Instance?.AnimateBonus(passScore); // <<— BONUS SCALE EFECTİNİ TETİKLE
        lastAwardTime = now;

        // Aynı işaretten tekrar saymasın
        lastCloseCollider = null;
        lastCloseMarkTime = -999f;
    }

    int NearestLaneIndex(float x)
    {
        int best = 0; float bestD = float.MaxValue;
        for (int i = 0; i < laneX.Length; i++)
        {
            float d = Mathf.Abs(x - laneX[i]);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Ön kutuyu çiz (debug)
        Vector3 p = transform.position;
        Vector3 center = new Vector3(p.x, p.y + boxCenterYOffset, p.z + nearAheadZ * 0.5f);
        Vector3 size = new Vector3(halfBoxX * 2f, halfBoxY * 2f, nearAheadZ);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
