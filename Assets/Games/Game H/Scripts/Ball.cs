using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    [Header("Seviye (0=Golf ... 6=Bowling)")]
    public int level;
    [HideInInspector] public Rigidbody2D rb;

    bool merging; // Çifte birleşmeyi engelle

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (merging) return;
        var other = col.collider.GetComponent<Ball>();
        if (other == null || other.merging) return;

        // Aynı seviye ise birleş
        if (other.level == level && level < MergeManager.I.MaxLevel)
        {
            merging = other.merging = true;
            MergeManager.I.Merge(this, other, col.GetContact(0).point);
        }
    }
}
