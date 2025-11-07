using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class WeaponOrbit : MonoBehaviour
{
    public PlayerLoadout.WeaponType type = PlayerLoadout.WeaponType.Shovel;

    [Header("Yönelim")]
    [SerializeField] bool faceOutward = true; // uç kısmı dışarı baksın

    Transform center;                     // Player (parent)
    PlayerLoadout owner;                  // stat kaynağı
    PlayerLoadout.WeaponStats s;          // aktif stat seti
    float angle;                          // derece

    SpriteRenderer sr;
    Collider2D col;

    // Aynı overlap süresince tek vuruş
    readonly HashSet<Collider2D> insideSet = new HashSet<Collider2D>();

    void Awake()
    {
        center = transform.parent;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // weapon hitbox trigger
    }

    void Start()
    {
        owner = GetComponentInParent<PlayerLoadout>();
        RefreshFromLoadout(owner);
        UpdateImmediate();
    }

    void Update()
    {
        if (!center) { center = transform.parent; if (!center) return; }

        float dir = s.clockwise ? -1f : 1f;
        angle += dir * (s.rpm * 360f / 60f) * Time.deltaTime;

        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * s.radius;
        transform.position = center.position + offset;

        if (faceOutward) transform.right = offset.normalized;
    }

    void UpdateImmediate()
    {
        if (!center) return;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * s.radius;
        transform.position = center.position + offset;
        if (faceOutward) transform.right = offset.normalized;
    }

    // --- Vuruş: sadece girişte bir kez ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (insideSet.Contains(other)) return;
        insideSet.Add(other);

        if (other.TryGetComponent<EnemyAgent>(out var enemy))
        {
            enemy.ApplyDamage(s.damage, transform.position, s.enemyKnockback);
            return;
        }

        // Loot düşmanı için de hasar ver
        if (other.TryGetComponent<LootEnemyAgent>(out var loot))
        {
            loot.ApplyDamage(s.damage, transform.position, s.enemyKnockback);
            return;
        }
    }   


    void OnTriggerExit2D(Collider2D other)
    {
        if (insideSet.Contains(other))
            insideSet.Remove(other);
    }

    // --- EŞİT AÇILI DİZİLİM İÇİN DIŞARIDAN BAŞLANGIÇ AÇISI VERME ---
    public void SetAngleDeg(float deg)
    {
        angle = deg;
        UpdateImmediate();
    }

    // --- API ---
    public void SetType(PlayerLoadout.WeaponType newType) => type = newType;

    public void RefreshFromLoadout(PlayerLoadout source = null)
    {
        if (source == null) source = GetComponentInParent<PlayerLoadout>();
        owner = source;
        if (owner != null) s = owner.GetStats(type);
        UpdateImmediate();
    }
}
