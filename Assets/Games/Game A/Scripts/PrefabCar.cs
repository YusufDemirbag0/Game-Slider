using UnityEngine;

/// Kendi taban hızında akar; öndeki aynı şeritteyse takip mesafesinde hız eşitler.
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PrefabCar : MonoBehaviour
{
    [Header("Hız")]
    [SerializeField] float baseSpeed = 12f;
    [SerializeField] float accel = 8f;
    [SerializeField] float maxDeltaBrake = 20f;

    [Header("Takip / Görüş")]
    [SerializeField] float followDistance = 9f;
    [SerializeField] float hardBrakeDistance = 4.5f;
    [SerializeField] float laneSnapEpsilon = 0.35f;
    [SerializeField] float raycastWidth = 0.8f;
    [SerializeField] LayerMask obstacleMask = ~0;

    [Header("Şerit")]
    public float laneX; // Spawner set eder

    float currentSpeed;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        currentSpeed = baseSpeed;
    }

    public void SetBaseSpeed(float v)
    {
        baseSpeed = Mathf.Max(0f, v);
        if (currentSpeed < 0.01f) currentSpeed = baseSpeed;
    }

    void Update()
    {
        if (GameManager.Instance && GameManager.Instance.IsGameOver) return;

        // Yol hızına senkron tut (cihaz yavaşsa bile göreli hız bozulmaz)
        float roadV = GameManager.Instance ? GameManager.Instance.GetRoadSpeed() : baseSpeed;
        float targetSpeed = roadV; // temel akış hızı

        // Önde aynı şeritte araç var mı?
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = Vector3.back;
        if (Physics.SphereCast(origin, raycastWidth, dir, out RaycastHit hit, followDistance, obstacleMask, QueryTriggerInteraction.Collide))
        {
            var other = hit.collider.GetComponentInParent<PrefabCar>();
            if (other && Mathf.Abs(other.laneX - laneX) <= laneSnapEpsilon)
            {
                float dist = hit.distance;
                if (dist <= hardBrakeDistance)
                    targetSpeed = Mathf.Min(targetSpeed, Mathf.Max(0f, other.currentSpeed - maxDeltaBrake));
                else
                    targetSpeed = Mathf.Min(targetSpeed, other.currentSpeed);
            }
        }

        // Hedef hıza yumuşak yaklaş ve ilerlet
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);
        transform.position += Vector3.back * currentSpeed * Time.deltaTime;
    }
}
