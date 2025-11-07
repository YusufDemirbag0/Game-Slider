using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] float moveSpeed = 4.5f;     // ü/s (taban hız)
    [SerializeField] bool useSmoothing = true;   // fizik yumuşatma (anim bundan etkilenmez)
    [SerializeField] float acceleration = 18f;   // sadece useSmoothing=true iken
    [SerializeField] float deceleration = 22f;   // sadece useSmoothing=true iken

    [Header("Görsel")]
    [SerializeField] bool flipHorizontally = true; // sola giderken flipX

    [Header("Ölüm")]
    [SerializeField] bool freezeOnDie = true;

    // --- YENİ: Hız çarpanı (upgrade için) ---
    [Header("Stat")]
    [SerializeField] float speed = 1f; // <-- BAŞLANGIÇ 1 (çarpan)  // NEW

    // Animator parametreleri (minimal)
    static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    static readonly int HashDie      = Animator.StringToHash("Die");

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    Vector2 inputKeyboard, inputExternal;
    Vector2 desiredVel, currentVel, velRef;
    bool hasExternalInput, isDead;

    // Eşikler
    const float INPUT_MOVE_THRESHOLD = 0.01f;    // anim karar eşiği (input)
    const float VELOCITY_ZERO_EPS    = 0.0004f;  // fizik dead-zone

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // frame uyumu
    }

    void Update()
    {
        if (isDead) { anim.SetBool(HashIsMoving, false); return; }

        // 1) INPUT — anında anim kararı için ham input'u al
        inputKeyboard.x = Input.GetAxisRaw("Horizontal");
        inputKeyboard.y = Input.GetAxisRaw("Vertical");

        Vector2 input = hasExternalInput ? inputExternal : inputKeyboard;
        hasExternalInput = false;
        if (input.magnitude > 1f) input.Normalize();

        // 2) ANIM — gecikmesiz: IsMoving'i direkt inputa göre set et
        bool movingInstant = input.sqrMagnitude > INPUT_MOVE_THRESHOLD * INPUT_MOVE_THRESHOLD;
        anim.SetBool(HashIsMoving, movingInstant);

        // 3) FİZİK HEDEF HIZ
        // --- YENİ: efektif hız = taban * speed çarpanı ---
        float effectiveSpeed = moveSpeed * Mathf.Max(0.0f, speed);   // NEW
        desiredVel = input * effectiveSpeed;                         // NEW

        if (useSmoothing)
        {
            float smoothTime = movingInstant
                ? Mathf.Max(0.0001f, 1f / acceleration)
                : Mathf.Max(0.0001f, 1f / deceleration);

            currentVel = Vector2.SmoothDamp(currentVel, desiredVel, ref velRef, smoothTime);
        }
        else
        {
            currentVel = desiredVel; // anında hız değişimi
        }

        // 4) Dead-zone: çok küçük hızları sıfırla (kayma hissini keser)
        if (currentVel.sqrMagnitude < VELOCITY_ZERO_EPS) currentVel = Vector2.zero;

        // 5) Opsiyonel flip
        if (flipHorizontally && sr != null && Mathf.Abs(currentVel.x) > 0.02f)
            sr.flipX = currentVel.x < 0f;
    }

    void FixedUpdate()
    {
        if (isDead && freezeOnDie) return;
        rb.MovePosition(rb.position + currentVel * Time.fixedDeltaTime);
    }

    /// Mobil joystick için her frame çağır.
    public void SetExternalInput(Vector2 joystickDir)
    {
        inputExternal = joystickDir;
        hasExternalInput = true;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        desiredVel = currentVel = velRef = Vector2.zero;
        anim.ResetTrigger(HashDie);
        anim.SetTrigger(HashDie);
        anim.SetBool(HashIsMoving, false);
    }

    public void Revive(Vector2 atPosition)
    {
        isDead = false;
        rb.position = atPosition;
        desiredVel = currentVel = velRef = Vector2.zero;
        anim.SetBool(HashIsMoving, false);
    }

    // --- YENİ: Speed API (upgrade için) ---
    public float GetSpeed() => speed;                              // NEW
    public void  AddSpeed(float amount) { speed = Mathf.Max(0f, speed + amount); }    // NEW
    public void  AddSpeedPercent(float percent) { speed = Mathf.Max(0f, speed * (1f + percent)); } // NEW
}
