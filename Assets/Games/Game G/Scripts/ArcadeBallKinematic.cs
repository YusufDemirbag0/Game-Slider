using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ArcadeBallKinematic : MonoBehaviour
{
    [Header("Çarpışma")]
    [SerializeField] LayerMask collisionMask = ~0;
    [SerializeField] float radius = 0.25f;
    [SerializeField] float skin = 0.005f;
    [SerializeField] int maxBouncesPerStep = 4;

    [Header("Hareket")]
    [SerializeField] float speed = 13f;
    [SerializeField] Vector2 initialDir = new(0.45f, 1f);
    [SerializeField] float deathLineY = -10f;

    [Header("Açı Kısıtları (Arcade)")]
    [SerializeField] float minAngleFromHorizontal = 12f;
    [SerializeField] float maxAngleFromVertical = 80f;
    [SerializeField] float minVy = 0.10f;

    [Header("Paddle English")]
    [SerializeField] Transform paddle;
    [SerializeField] float paddleEnglishStrength = 2.6f;
    [SerializeField] float nudgeUpAfterPaddle = 0.25f;

    // ====== Tier / Sprites ======
    [Header("Tier Sistemi")]
    [Tooltip("1=Metal (iz yok), 2=Ilık (1 iz), 3=Sıcak (2 iz)")]
    [SerializeField, Range(1, 3)] int currentTier = 1;
    [SerializeField] SpriteRenderer sourceRenderer;
    [SerializeField] Sprite[] tierSprites = new Sprite[3]; // 0..2
    [SerializeField] int bouncesPerUpgrade = 10;
    int bounceCount = 0;

    // ====== Afterimage ======
    [Header("Afterimage")]
    [SerializeField] int poolSize = 24;
    [SerializeField] float tier2SpawnInterval = 0.035f;
    [SerializeField] float tier2Lifetime = 0.18f;
    [SerializeField, Range(0f, 1f)] float tier2StartAlpha = 0.38f;
    [SerializeField] float tier3SpawnInterval = 0.022f;
    [SerializeField] float tier3Lifetime = 0.26f;
    [SerializeField, Range(0f, 1f)] float tier3StartAlpha = 0.55f;
    [SerializeField] float minSpeedForTrail = 4f;
    [SerializeField] float scaleDownOverLife = 0.12f;

    // ====== Eventler ======
    public Action onPaddleBounce;
    public Action onDeath;

    // Raycast (trigger ignore)
    readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
    ContactFilter2D _cf;

    class Ghost
    {
        public Transform t;
        public SpriteRenderer sr;
        public float life, maxLife;
        public bool active;
        public Vector3 baseScale;
    }

    List<Ghost> pool;
    Transform ghostRoot;
    float spawnTimer;
    Vector2 dir, prevPos;
    bool launched;

    // ====== Yeni: Hızlanma Sistemi ======
    float baseSpeed;
    [Header("Speed Visuals")]
    [SerializeField] ParticleSystem speedBoostEffect;

    void Awake()
    {
        if (!sourceRenderer) sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        BuildPool();

        _cf = new ContactFilter2D();
        _cf.SetLayerMask(collisionMask);
        _cf.useLayerMask = true;
        _cf.useTriggers = false; // pickuplar etkilenmez
    }

    void Start()
    {
        baseSpeed = speed;
        dir = EnforceAngles(initialDir.sqrMagnitude < 1e-6f ? Vector2.up : initialDir.normalized);
        prevPos = transform.position;
        ApplyTierVisuals(currentTier);
    }

    void Update()
    {
        if (transform.position.y < deathLineY)
        {
            launched = false;
            onDeath?.Invoke();
        }
        UpdateGhosts();
    }

    void LateUpdate() => prevPos = transform.position;

    void FixedUpdate()
    {
        if (!launched) return;

        float remaining = speed * Time.deltaTime;
        Vector2 pos = (Vector2)transform.position;
        int safety = maxBouncesPerStep;

        while (remaining > 0f && safety-- > 0)
        {
            int hitCount = Physics2D.CircleCast(pos, radius, dir, _cf, _hits, remaining);
            if (hitCount == 0)
            {
                pos += dir * remaining;
                remaining = 0f;
                break;
            }
            var hit = _hits.Take(hitCount).OrderBy(h => h.distance).First();

            float travel = Mathf.Max(0f, hit.distance - skin);
            pos += dir * travel;
            remaining -= travel;

            if (paddle && hit.collider.transform == paddle)
            {
                onPaddleBounce?.Invoke();
                bounceCount++;
                if (bouncesPerUpgrade > 0 && bounceCount % bouncesPerUpgrade == 0 && currentTier < 3)
                    SetTier(currentTier + 1);

                float pCenterX = hit.collider.bounds.center.x;
                float pHalf = Mathf.Max(0.0001f, hit.collider.bounds.extents.x);
                float offsetNorm = Mathf.Clamp((pos.x - pCenterX) / pHalf, -1f, 1f);

                Vector2 refl = Vector2.Reflect(dir, hit.normal);
                refl.x += offsetNorm * paddleEnglishStrength;
                if (refl.y <= 0f) refl.y = nudgeUpAfterPaddle;
                dir = EnforceAngles(refl.normalized);
            }
            else
            {
                dir = EnforceAngles(Vector2.Reflect(dir, hit.normal).normalized);
            }

            pos += hit.normal * skin;
        }

        transform.position = pos;
        TrySpawnAfterimage(Time.deltaTime);
    }

    // ---------- PUBLIC API ----------
    public void ResetFromSpawnPoint(Vector2 spawnPos)
    {
        transform.position = spawnPos;
        prevPos = transform.position;
        launched = true;
        dir = EnforceAngles(initialDir.sqrMagnitude < 1e-6f ? Vector2.up : initialDir.normalized);
    }

    public void ContinueFromSpawnPoint(Vector2 spawnPos)
    {
        transform.position = spawnPos;
        prevPos = transform.position;
        launched = true;
        Vector2 baseDir = initialDir.sqrMagnitude < 1e-6f ? Vector2.up : initialDir;
        baseDir.x += UnityEngine.Random.Range(-0.1f, 0.1f);
        dir = EnforceAngles(baseDir.normalized);
    }

    public void ResetTierAndBounce()
    {
        bounceCount = 0;
        SetTier(1);
        ApplySpeedTier(0);
    }

    public void SetTier(int tier)
    {
        currentTier = Mathf.Clamp(tier, 1, 3);
        ApplyTierVisuals(currentTier);
    }

    // ---------- Hızlanma ve Görsel Efekt ----------
    public void ApplySpeedTier(int tier)
    {
        float newSpeed = baseSpeed;
        switch (tier)
        {
            case 1: newSpeed = baseSpeed * 1.2f; break;  // 20 puan
            case 2: newSpeed = baseSpeed * 1.4f; break;  // 40 puan
            case 3: newSpeed = baseSpeed * 1.6f; break;  // 60 puan
            default: newSpeed = baseSpeed; break;
        }

        speed = newSpeed;
        TriggerSpeedVisual();
    }

    void TriggerSpeedVisual()
    {
        // Parlama efekti (renk geçişi)
        if (sourceRenderer)
        {
            sourceRenderer.color = Color.Lerp(Color.white, Color.yellow, 0.45f);
            CancelInvoke(nameof(ResetVisual));
            Invoke(nameof(ResetVisual), 0.25f);
        }

        // Particle efekti varsa
        if (speedBoostEffect)
            speedBoostEffect.Play();
    }

    void ResetVisual()
    {
        if (sourceRenderer)
            sourceRenderer.color = Color.white;
    }

    // ---------- Afterimage ----------
    void BuildPool()
    {
        ghostRoot = new GameObject("Afterimages").transform;
        ghostRoot.SetParent(transform.parent, false);
        pool = new List<Ghost>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("ghost_" + i);
            go.transform.SetParent(ghostRoot, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.enabled = false;
            pool.Add(new Ghost { t = go.transform, sr = sr, active = false });
        }
    }

    void UpdateGhosts()
    {
        if (pool == null) return;
        foreach (var g in pool)
        {
            if (!g.active) continue;
            g.life -= Time.deltaTime;
            float k = Mathf.InverseLerp(0f, g.maxLife, g.life);
            var c = g.sr.color; c.a = k * c.a; g.sr.color = c;
            if (scaleDownOverLife > 0f) g.t.localScale = g.baseScale * (1f - (1f - k) * scaleDownOverLife);
            if (g.life <= 0f) { g.active = false; g.sr.enabled = false; }
        }
    }

    void TrySpawnAfterimage(float dt)
    {
        if (currentTier <= 1 || sourceRenderer == null) return;
        float instSpeed = (((Vector2)transform.position - prevPos).magnitude) / dt;
        if (instSpeed < minSpeedForTrail) return;

        float interval = (currentTier == 2) ? tier2SpawnInterval : tier3SpawnInterval;
        float life = (currentTier == 2) ? tier2Lifetime : tier3Lifetime;
        float alpha = (currentTier == 2) ? tier2StartAlpha : tier3StartAlpha;

        spawnTimer += dt;
        if (spawnTimer < interval) return; spawnTimer = 0f;

        var g = GetFreeGhost(); g.active = true; g.maxLife = g.life = life;
        g.sr.sprite = sourceRenderer.sprite;
        g.sr.sharedMaterial = sourceRenderer.sharedMaterial;
        g.sr.sortingLayerID = sourceRenderer.sortingLayerID;
        g.sr.sortingOrder = sourceRenderer.sortingOrder - 1;
        var col = sourceRenderer.color; col.a = alpha; g.sr.color = col;
        g.t.position = sourceRenderer.transform.position;
        g.t.rotation = sourceRenderer.transform.rotation;
        g.t.localScale = sourceRenderer.transform.lossyScale;
        g.baseScale = g.t.localScale;
        g.sr.enabled = true;
    }

    Ghost GetFreeGhost()
    {
        foreach (var g in pool) if (!g.active) return g;
        return pool[0];
    }

    // ---------- Yardımcı ----------
    Vector2 EnforceAngles(Vector2 d)
    {
        if (Mathf.Abs(d.y) < minVy) d.y = Mathf.Sign(d.y == 0 ? 1 : d.y) * minVy;

        float angFromH = Mathf.Abs(Mathf.Rad2Deg * Mathf.Atan2(d.y, d.x));
        if (angFromH < minAngleFromHorizontal)
        {
            float sx = Mathf.Sign(d.x == 0 ? 1 : d.x);
            float sy = Mathf.Sign(d.y == 0 ? 1 : d.y);
            float t = minAngleFromHorizontal * Mathf.Deg2Rad;
            d = new Vector2(Mathf.Cos(t) * sx, Mathf.Sin(t) * sy).normalized;
        }
        float angFromV = Mathf.Abs(Mathf.Rad2Deg * Mathf.Atan2(d.x, d.y));
        if (angFromV > maxAngleFromVertical)
        {
            float sx = Mathf.Sign(d.x == 0 ? 1 : d.x);
            float sy = Mathf.Sign(d.y == 0 ? 1 : d.y);
            float t = (90f - maxAngleFromVertical) * Mathf.Deg2Rad;
            d = new Vector2(Mathf.Sin(t) * sx, Mathf.Cos(t) * sy).normalized;
        }
        if (Mathf.Abs(d.y) < 1e-4f) d.y = 0.1f * Mathf.Sign(d.y == 0 ? 1 : d.y);
        return d.normalized;
    }

    void ApplyTierVisuals(int tier)
    {
        if (sourceRenderer && tierSprites != null && tierSprites.Length >= tier && tierSprites[tier - 1])
            sourceRenderer.sprite = tierSprites[tier - 1];
    }
}
