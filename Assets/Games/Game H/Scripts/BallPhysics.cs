// BallPhysics.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BallPhysics : MonoBehaviour
{
    [Header("Ground Layer Mask")]
    public LayerMask groundMask;

    [Header("Drag / Limits")]
    public float airDrag = 0.05f;
    public float groundDrag = 3.5f;
    public float groundAngularDrag = 7f;
    public float maxSpeed = 6f;
    public float maxAngularVel = 120f;

    [Header("Damping")]
    public float impactDamp = 0.35f;
    public float sleepVelSqr = 0.0025f; // v^2
    public float sleepAng = 5f;
    public int stableFramesToSleep = 10; // üst üste şu kadar kare stabilse uyu

    CircleCollider2D col;
    Rigidbody2D rb;
    bool grounded;
    int stableCounter;
    float spawnTime;

    void Awake(){ rb = GetComponent<Rigidbody2D>(); col = GetComponent<CircleCollider2D>(); }
    void OnEnable(){ spawnTime = Time.time; stableCounter = 0; rb.WakeUp(); }

    void FixedUpdate()
    {
        // Zemin tespiti (yarıçap + küçük pay)
        float r = col.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        Vector2 p = (Vector2)transform.position + Vector2.down * (r + 0.02f);
        grounded = Physics2D.OverlapCircle(p, 0.04f, groundMask);

        // Drag
        rb.linearDamping  = grounded ? groundDrag       : airDrag;
        rb.angularDamping = grounded ? groundAngularDrag: 2.5f;

        // Limitler
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxAngularVel, maxAngularVel);

        // Uyku mantığı: spawn'dan biraz süre geçmeden asla uyuma
        if (Time.time - spawnTime < 0.4f) return;

        bool verySlow = rb.linearVelocity.sqrMagnitude < sleepVelSqr && Mathf.Abs(rb.angularVelocity) < sleepAng;
        if (grounded && verySlow)
        {
            stableCounter++;
            if (stableCounter >= stableFramesToSleep) rb.Sleep();
        }
        else
        {
            stableCounter = 0;
            if (rb.IsSleeping()) rb.WakeUp();
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        rb.linearVelocity  *= (1f - impactDamp);
        rb.angularVelocity *= (1f - impactDamp);
    }
}
