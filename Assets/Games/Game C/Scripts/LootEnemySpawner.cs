using UnityEngine;

public class LootEnemySpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform player;
    [SerializeField] LootEnemyAgent lootEnemyPrefab;

    [Header("Alan")]
    [SerializeField] float areaWidth  = 100f;
    [SerializeField] float areaHeight = 100f;
    [SerializeField] float minSpawnDistanceFromPlayer = 6f;
    [SerializeField] int   maxTries = 20;
    [SerializeField] float spawnDelayOnEnterArea = 0.1f;

    Rect currentArea;
    Vector2 areaCenter;
    LootEnemyAgent aliveLoot;
    float delay;

    // Bu alanda spawn yapıldı mı? (Bir alanda sadece 1 kere)
    bool spawnedThisArea = false;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        RecenterArea();
        delay = spawnDelayOnEnterArea;
    }

    void Update()
    {
        if (!player) return;

        // Oyuncu alan dışına çıktıysa - alanı yenile ve hakkı sıfırla
        if (!Contains(currentArea, player.position))
        {
            RecenterArea();
            if (aliveLoot) { Destroy(aliveLoot.gameObject); aliveLoot = null; }
            delay = spawnDelayOnEnterArea;
            spawnedThisArea = false;
        }

        // Alanda hala spawn yapmadıysan ve sahnede aktif loot yoksa dene
        if (!spawnedThisArea && !aliveLoot)
        {
            if (delay > 0f) { delay -= Time.deltaTime; return; }
            TrySpawn();
        }
    }

    void RecenterArea()
    {
        areaCenter = player ? (Vector2)player.position : Vector2.zero;
        currentArea = new Rect(
            areaCenter.x - areaWidth  * 0.5f,
            areaCenter.y - areaHeight * 0.5f,
            areaWidth, areaHeight
        );
    }

    bool Contains(Rect r, Vector2 p)
    {
        return p.x >= r.xMin && p.x <= r.xMax && p.y >= r.yMin && p.y <= r.yMax;
    }

    void TrySpawn()
    {
        if (!lootEnemyPrefab) return;

        for (int i = 0; i < maxTries; i++)
        {
            float x = Random.Range(currentArea.xMin, currentArea.xMax);
            float y = Random.Range(currentArea.yMin, currentArea.yMax);
            Vector2 pos = new Vector2(x, y);

            if (Vector2.Distance(pos, player.position) < minSpawnDistanceFromPlayer) continue;

            aliveLoot = Instantiate(lootEnemyPrefab, pos, Quaternion.identity);
            spawnedThisArea = true;      // Bu alan için hakkı kullandık
            return;
        }
    }
}
