using UnityEngine;

/// Sahneye boş bir GameObject’e ekle.
/// Inspector:
///  - Player = Player Transform (tag “Player” ise boş bırakabilirsin)
///  - Enemy Prefabs = 4 adet EnemyAgent prefab (Type0..Type3 sırasıyla)
///  - Stats Per Type = her tür için HP/Speed/Damage/XP
/// Çalışma:
///  - 0–300 sn arası aşamalı ağırlık ve hız artışı
///  - Ekran dışına (buffer) ve oyuncu etrafı halka içine spawn
public class EnemySpawnerSimple : MonoBehaviour
{
    [System.Serializable]
    public struct TypeStats
    {
        public int   maxHP;
        public float moveSpeed;
        public int   contactDamage;
        public float contactCooldown;
        public float knockbackDistance;
        public int   xpReward;
    }

    [Header("Refs")]
    [SerializeField] Transform player;
    [SerializeField] EnemyAgent[] enemyPrefabs = new EnemyAgent[4]; // index = EnemyType

    [Header("Stats Per Type (index=Type0..3)")]
    public TypeStats[] statsPerType = new TypeStats[4];

    [Header("Spawn Controls")]
    [SerializeField] int   maxAlive = 60;
    [SerializeField] float spawnIntervalStart = 1.25f;
    [SerializeField] float spawnIntervalEnd   = 0.50f;
    [SerializeField] float ringRadiusMin = 8f;
    [SerializeField] float ringRadiusMax = 14f;
    [SerializeField] float screenBuffer  = 3f; // ekran sınırından dışarı

    [Header("Game Timeline")]
    [SerializeField] float totalDuration = 300f; // 5 dk

    int alive;
    float timer;
    float elapsed;
    Camera cam;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        cam = Camera.main;

        // Eğer boşsa makul defaultları doldur (sen zaten inspector’dan girmişsin)
        EnsureDefaults();
    }

    void Start()
    {
        for (int i = 0; i < enemyPrefabs.Length; i++)
            if (!enemyPrefabs[i])
                Debug.LogWarning($"[Spawner] enemyPrefabs[{i}] atanmamış!");
    }


    void Update()
    {
        if (!player) return;

        elapsed += Time.deltaTime;
        float interval = Mathf.Lerp(spawnIntervalStart, spawnIntervalEnd, Mathf.Clamp01(elapsed / totalDuration));

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            TrySpawn();
        }
    }

    void TrySpawn()
    {
        if (alive >= maxAlive) return;

        var weights = GetWeightsForTime(elapsed);
        var type = RollType(weights);
        var prefab = enemyPrefabs[(int)type];
        if (!prefab) return;

        Vector2 pos = PickOffscreenPos();
        var e = Instantiate(prefab, pos, Quaternion.identity);
        e.owner = this;
        e.useSpawnerDefaults = false;
        e.Configure(type, GetStats(type));

        alive++;
    }

    Vector2 PickOffscreenPos()
    {
        Vector3 center = player.position;
        Vector2 dir;

        if (cam)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            int edge = Random.Range(0, 4); // 0=L,1=R,2=B,3=T
            float x = center.x, y = center.y;
            switch (edge)
            {
                case 0: x -= (halfW + screenBuffer); y += Random.Range(-halfH, halfH); break;
                case 1: x += (halfW + screenBuffer); y += Random.Range(-halfH, halfH); break;
                case 2: y -= (halfH + screenBuffer); x += Random.Range(-halfW, halfW); break;
                default: y += (halfH + screenBuffer); x += Random.Range(-halfW, halfW); break;
            }
            dir = (new Vector2(x, y) - (Vector2)center).normalized;
        }
        else
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }

        float dist = Random.Range(ringRadiusMin, ringRadiusMax);
        return (Vector2)center + dir * dist;
    }

    // --- Ağırlık zaman çizelgesi ---
    // 0–30: sadece T0 | 30–60: T0 | 60–90: T1 %25 | 90–120: %50 |
    // 120–150: T2 %20 | 150–180: %40 | 180–210: T3 %10 | 210–240: %20 | 240–270: %30 | 270–300: %40
    Vector4 GetWeightsForTime(float t)
    {
        if (t <  30f) return Normalize(new Vector4(1,0,0,0));
        if (t <  60f) return Normalize(new Vector4(1,0,0,0));
        if (t <  90f) return Normalize(new Vector4(0.75f,0.25f,0,0));
        if (t < 120f) return Normalize(new Vector4(0.5f,0.5f,0,0));
        if (t < 150f) return Normalize(new Vector4(0.3f,0.5f,0.2f,0));
        if (t < 180f) return Normalize(new Vector4(0.2f,0.4f,0.4f,0));
        if (t < 210f) return Normalize(new Vector4(0.15f,0.35f,0.4f,0.10f));
        if (t < 240f) return Normalize(new Vector4(0.10f,0.30f,0.40f,0.20f));
        if (t < 270f) return Normalize(new Vector4(0.05f,0.25f,0.40f,0.30f));
        return                Normalize(new Vector4(0.00f,0.20f,0.40f,0.40f));
    }
    Vector4 Normalize(Vector4 v)
    {
        float s = v.x+v.y+v.z+v.w; if (s <= 0f) return new Vector4(1,0,0,0);
        return v / s;
    }
    EnemyAgent.EnemyType RollType(Vector4 w)
    {
        float r = Random.value;
        if (r < w.x) return EnemyAgent.EnemyType.Type0; r -= w.x;
        if (r < w.y) return EnemyAgent.EnemyType.Type1; r -= w.y;
        if (r < w.z) return EnemyAgent.EnemyType.Type2;
        return EnemyAgent.EnemyType.Type3;
    }

    // --- Stat aktarımı ---
    public EnemyAgent.Stats GetStats(EnemyAgent.EnemyType type)
    {
        int i = (int)type;
        var t = (i >= 0 && i < statsPerType.Length) ? statsPerType[i] : DefaultType();
        return new EnemyAgent.Stats
        {
            maxHP = Mathf.Max(1, t.maxHP),
            moveSpeed = Mathf.Max(0.01f, t.moveSpeed),
            contactDamage = Mathf.Max(0, t.contactDamage),
            contactCooldown = Mathf.Max(0.05f, t.contactCooldown),
            knockbackDistance = Mathf.Max(0f, t.knockbackDistance),
            xpReward = Mathf.Max(0, t.xpReward)
        };
        TypeStats DefaultType() => new TypeStats { maxHP=10, moveSpeed=1.5f, contactDamage=5, contactCooldown=0.6f, knockbackDistance=0.5f, xpReward=1 };
    }

    // EnemyAgent ölümde çağırır
    public void NotifyEnemyDied(EnemyAgent e)
    {
        alive = Mathf.Max(0, alive - 1);
    }

    void EnsureDefaults()
    {
        if (statsPerType == null || statsPerType.Length < 4)
            statsPerType = new TypeStats[4];

        void PutDefault(int idx, int hp, float spd, int dmg, float cd, float kb, int xp)
        {
            if (idx >= statsPerType.Length) return;
            var s = statsPerType[idx];
            bool empty = s.maxHP == 0 && s.moveSpeed == 0 && s.contactDamage == 0 && s.contactCooldown == 0 && s.knockbackDistance == 0 && s.xpReward == 0;
            if (empty) { s.maxHP = hp; s.moveSpeed = spd; s.contactDamage = dmg; s.contactCooldown = cd; s.knockbackDistance = kb; s.xpReward = xp; statsPerType[idx] = s; }
        }

        // Senin ekran görüntündeki değerlerle aynı
        PutDefault(0, 100, 2.0f, 20, 1.0f, 0.5f, 1);
        PutDefault(1, 200, 2.2f, 40, 1.0f, 0.5f, 3);
        PutDefault(2, 500, 2.5f, 70, 1.0f, 0.5f, 10);
        PutDefault(3,1000, 2.5f,100, 1.0f, 0.5f, 25);
    }
}
