using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoldPickup : MonoBehaviour
{
    [Header("Değer")]
    public int value = 1;

    [Header("Mıknatıs")]
    public float attractRadius = 3f;
    public float attractSpeed  = 10f;   // temel hız
    public float extraSpeedNear = 15f;  // yaklaştıkça ek hız

    [Header("Drop Animasyonu")]
    public float hopHeight = 0.6f;
    public float hopTime   = 0.25f;

    [Header("Tags")]
    public string playerTag = "Player";

    Transform player;
    Vector3   basePos;
    float     hopTimer;
    bool      hopping;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        player = p ? p.transform : null;

        basePos = transform.position;
        hopTimer = 0f;
        hopping = true; // kısa hop animasyonu
    }

    void Update()
    {
        // Küçük bir yukarı-aşağı hop: parabol (frame bağımsız)
        if (hopping)
        {
            hopTimer += Time.deltaTime;
            float t = Mathf.Clamp01(hopTimer / hopTime);                 // 0..1
            float y = 4f * hopHeight * t * (1f - t);                     // tepe ortada
            transform.position = new Vector3(basePos.x, basePos.y + y, basePos.z);
            if (t >= 1f) { hopping = false; basePos = transform.position; }
            return; // hop biterken mıknatısa geç
        }

        // Mıknatıs: oyuncu 3f içinde ise çek
        if (player)
        {
            Vector3 toP = player.position - transform.position;
            float d = toP.magnitude;
            if (d <= attractRadius)
            {
                float speed = attractSpeed + extraSpeedNear * Mathf.Clamp01(1f - d / attractRadius);
                transform.position += toP.normalized * speed * Time.deltaTime;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        var pxp = other.GetComponent<PlayerXP>();
        if (!pxp) pxp = other.GetComponentInParent<PlayerXP>();
        if (pxp) pxp.AddGold(Mathf.Max(0, value));

        Destroy(gameObject);
    }
}
