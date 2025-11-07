using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Transform player;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Şeritler ve Konum")]
    [SerializeField] private float[] laneX = { -3f, 0f, 3f };
    [SerializeField] private Vector2 aheadZRange = new Vector2(80f, 100f);
    [SerializeField] private float yOffset = 0f;

    [Header("Mesafe Tabanlı Spawn")]
    [Tooltip("Her kaç metrede bir yeni engel denensin?")]
    [SerializeField] private float gapMeters = 25f;
    private float lastSpawnAtDistance; // GameManager.DistanceMeters bazlı

    [Header("Çakışmayı Önleme")]
    [SerializeField] private float laneMinGap = 15f;
    [SerializeField] private float laneXTolerance = 0.35f;

    [Header("Despawn")]
    [SerializeField] private float despawnBehindPlayer = -40f;

    private readonly List<GameObject> alive = new();

    void Start()
    {
        if (!player) Debug.LogWarning("[ObstacleSpawner] Player atanmadı (Inspector’dan atayın).");
        lastSpawnAtDistance = GameManager.Instance ? GameManager.Instance.DistanceMeters : 0f;
    }

    void Update()
    {
        // Mesafeye göre birikmiş spawnları telafi et
        if (GameManager.Instance && !GameManager.Instance.IsGameOver)
        {
            float dist = GameManager.Instance.DistanceMeters;
            int safety = 0;
            while (dist - lastSpawnAtDistance >= gapMeters && safety++ < 3)
            {
                TrySpawn();
                lastSpawnAtDistance += gapMeters;
            }
        }

        // oyuncunun arkasına düşenleri temizle
        if (player)
        {
            float cutZ = player.position.z + despawnBehindPlayer;
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                var go = alive[i];
                if (!go) { alive.RemoveAt(i); continue; }
                if (go.transform.position.z < cutZ)
                {
                    Destroy(go);
                    alive.RemoveAt(i);
                }
            }
        }
    }

    void TrySpawn()
    {
        if (!player || obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // 1) Spawn Z: oyuncunun önünde sabit menzil
        float minA = Mathf.Min(aheadZRange.x, aheadZRange.y);
        float maxA = Mathf.Max(aheadZRange.x, aheadZRange.y);
        float spawnZ = player.position.z + Random.Range(minA, maxA);

        // 2) Gap uygun şeritleri bul
        var candidates = new List<int>();
        for (int i = 0; i < laneX.Length; i++)
            if (LaneHasGap(i, spawnZ)) candidates.Add(i);

        if (candidates.Count == 0) return;

        // 3) Şerit seç + instantiate
        int laneIndex = candidates[Random.Range(0, candidates.Count)];
        float x = laneX[laneIndex];
        float y = player.position.y + yOffset;

        var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        var obj = Instantiate(prefab, new Vector3(x, y, spawnZ), Quaternion.identity);
        obj.transform.SetParent(null, true);
        obj.tag = "Obstacle";
        alive.Add(obj);

        // PrefabCar ise lane/hız senkronu
        var car = obj.GetComponent<PrefabCar>();
        if (car)
        {
            car.laneX = x;
            // Yol hızıyla birlikte aksın (trafik hızını yol hızına bağla)
            float roadV = GameManager.Instance ? GameManager.Instance.GetRoadSpeed() : 10f;
            car.SetBaseSpeed(roadV);
        }
    }

    bool LaneHasGap(int laneIndex, float spawnZ)
    {
        float lx = laneX[laneIndex];
        float nearestBelowZ = float.NegativeInfinity;
        bool found = false;

        foreach (var go in alive)
        {
            if (!go) continue;
            var p = go.transform.position;
            if (Mathf.Abs(p.x - lx) > laneXTolerance) continue;
            if (p.z <= spawnZ)
            {
                if (!found || p.z > nearestBelowZ)
                {
                    nearestBelowZ = p.z;
                    found = true;
                }
            }
        }
        if (!found) return true;
        return (spawnZ - nearestBelowZ) >= laneMinGap;
    }
}
