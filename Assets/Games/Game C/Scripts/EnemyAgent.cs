using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAgent : MonoBehaviour
{
    public enum EnemyType { Type0, Type1, Type2, Type3 }

    [System.Serializable]
    public struct Stats
    {
        public int   maxHP;
        public float moveSpeed;
        public int   contactDamage;
        public float contactCooldown;
        public float knockbackDistance;
        public int   xpReward;
    }

    [Header("Setup")]
    public EnemyType type;
    public bool useSpawnerDefaults = true;   // Spawner Configure edeceği için genelde true

    [Header("Visual")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite hitSprite;       // vurulunca 0.1 sn
    [SerializeField] Sprite deathSprite;     // ölünce 1 sn kalır
    [SerializeField] float hitFlashTime = 0.1f;
    [SerializeField] float deathShowTime = 1.0f;

    [Header("Tags")]
    [SerializeField] string playerTag = "Player";

    [HideInInspector] public EnemySpawnerSimple owner; // spawner set eder

    Rigidbody2D rb;
    Transform player;
    SpriteRenderer sr;
    Animator anim;

    Stats s;
    int hp;
    float lastContactTime = -999f;
    bool dying;
    bool facingRight = true;
    Vector2 lastDir = Vector2.right;

    Coroutine hitCo;

    [Header("Coin Drop (prefabs)")]
    [SerializeField] GameObject coinBronzePrefab; // 1 gold
    [SerializeField] GameObject coinSilverPrefab; // 5 gold
    [SerializeField] GameObject coinGoldPrefab;   // 10 gold

    // --- Public: Spawner çağırır ---
    public void Configure(EnemyType t, Stats stats)
    {
        type = t;
        s = stats;
        hp = Mathf.Max(1, s.maxHP);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        sr = spriteRenderer ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        // güvenlik: spawner yoksa kendi default’unu yükle
        if (useSpawnerDefaults && owner == null)
        {
            var sp = FindFirstObjectByType<EnemySpawnerSimple>();
            if (sp) { owner = sp; Configure(type, sp.GetStats(type)); }
            else    { Configure(type, GetFallbackStats()); }
        }
    }

    void FixedUpdate()
    {
        if (dying || !player) return;

        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        lastDir = dir;
        rb.MovePosition(rb.position + dir * s.moveSpeed * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (dying) return;

        // sadece X ekseninde sağ/sola bak
        if (sr && Mathf.Abs(lastDir.x) > 0.01f)
        {
            bool shouldFaceRight = lastDir.x > 0f;
            if (shouldFaceRight != facingRight)
            {
                sr.flipX = !shouldFaceRight;
                facingRight = shouldFaceRight;
            }
        }

        // olası hatalı döndürmeleri sıfırla
        if (transform.rotation != Quaternion.identity)
            transform.rotation = Quaternion.identity;
    }

    // --- Player ile çarpışma: hasar + hafif itme ---
    void OnCollisionStay2D(Collision2D collision)
    {
        if (dying || !collision.collider.CompareTag(playerTag)) return;

        if (Time.time - lastContactTime < s.contactCooldown) return;
        lastContactTime = Time.time;

        // hasar ver
        var ph = collision.collider.GetComponent<PlayerHealth>();
        if (ph) ph.TakeDamage(Mathf.Max(0, s.contactDamage));

        // oyuncuyu biraz geri it
        var prb = collision.collider.attachedRigidbody;
        if (prb)
        {
            Vector2 pushDir = (prb.position - rb.position).normalized;
            prb.MovePosition(prb.position + pushDir * Mathf.Max(0f, s.knockbackDistance));
        }
    }

    // --- Silahlar burayı çağırır ---
    public void ApplyDamage(int amount, Vector2 sourcePos, float extraKnockback = 0f)
    {
        if (dying) return;

        hp -= Mathf.Max(0, amount);

        // geri itme
        float kb = Mathf.Max(0f, s.knockbackDistance + extraKnockback);
        if (kb > 0f)
        {
            Vector2 dir = ((Vector2)transform.position - sourcePos).normalized;
            rb.MovePosition(rb.position + dir * kb);
        }

        // hit sprite kısa göster
        if (hitSprite && sr)
        {
            if (hitCo != null) StopCoroutine(hitCo);
            hitCo = StartCoroutine(HitFlashCo());
        }

        if (hp <= 0) StartCoroutine(DeathCo());
    }

    IEnumerator HitFlashCo()
    {
        if (dying || !sr) yield break;

        var prevAnim = anim ? anim.enabled : false;
        var prevSprite = sr.sprite;

        if (anim) anim.enabled = false;
        sr.sprite = hitSprite;

        yield return new WaitForSeconds(hitFlashTime);

        if (!dying && sr)
        {
            sr.sprite = prevSprite;
            if (anim) anim.enabled = prevAnim;
        }
        hitCo = null;
    }

    IEnumerator DeathCo()
    {
        dying = true;

        // çarpışmayı kapat
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        rb.linearVelocity = Vector2.zero; rb.simulated = false;

        // XP ver
        var pxp = GameObject.FindGameObjectWithTag(playerTag)?.GetComponent<PlayerXP>();
        if (pxp && s.xpReward > 0) pxp.AddXP(s.xpReward);

        // Coin drop
        TryDropCoin();

        // hit co durdur
        if (hitCo != null) { StopCoroutine(hitCo); hitCo = null; }

        // görseli ölüşe çek
        if (anim) anim.enabled = false;
        if (sr && deathSprite) sr.sprite = deathSprite;

        yield return new WaitForSeconds(deathShowTime);

        owner?.NotifyEnemyDied(this);
        Destroy(gameObject);
    }

        void TryDropCoin()
    {
        var prefab = RollCoinPrefabByType(type);
        if (!prefab) return;

        var go = Instantiate(prefab, transform.position, Quaternion.identity);
        // GoldPickup varsa mıknatıs/animasyon zaten çalışır
    }

    GameObject RollCoinPrefabByType(EnemyType t)
    {
        float r = Random.value;

        switch (t)
        {
            // Zombie 1 (Type0): %25 bronz
            case EnemyType.Type0:
                if (r < 0.25f) return coinBronzePrefab;
                return null;

            // Zombie 2 (Type1): %25 bronz, %10 gümüş, %1 altın
            case EnemyType.Type1:
                if (r < 0.25f) return coinBronzePrefab;   r -= 0.25f;
                if (r < 0.10f) return coinSilverPrefab;    r -= 0.10f;
                if (r < 0.01f) return coinGoldPrefab;
                return null;

            // Zombie 3 (Type2): %10 bronz, %30 gümüş, %5 altın
            case EnemyType.Type2:
                if (r < 0.10f) return coinBronzePrefab;    r -= 0.10f;
                if (r < 0.30f) return coinSilverPrefab;    r -= 0.30f;
                if (r < 0.05f) return coinGoldPrefab;
                return null;

            // Zombie 4 (Type3): %20 gümüş, %15 altın
            case EnemyType.Type3:
                if (r < 0.20f) return coinSilverPrefab;    r -= 0.20f;
                if (r < 0.15f) return coinGoldPrefab;
                return null;
        }
        return null;
    }


    // fallback stats (spawner yoksa)
    Stats GetFallbackStats() => new Stats {
        maxHP = 10, moveSpeed = 1.5f, contactDamage = 5, contactCooldown = 0.6f, knockbackDistance = 0.5f, xpReward = 1
    };
}
