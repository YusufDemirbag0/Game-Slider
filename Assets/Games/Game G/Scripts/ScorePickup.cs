using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ScorePickup : MonoBehaviour
{
    [SerializeField] int points = 5;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        gameObject.tag = "Pickup";
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<ArcadeBallKinematic>())
        {
            FireBallManager.Instance?.AddScorePoint(points);
            Destroy(gameObject);
        }
    }
}
