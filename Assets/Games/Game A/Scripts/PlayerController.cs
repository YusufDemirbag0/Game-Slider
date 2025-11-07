using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Lanes (X konumları)")]
    [SerializeField] private float[] laneX = { -3f, 0f, 3f };
    [SerializeField, Range(0, 2)] private int startLane = 1;
    [SerializeField] private float laneLerp = 12f;

    [Header("Input")]
    [SerializeField] private float swipePxThreshold = 60f;
    [SerializeField] private float inputCooldown = 0.18f;

    float fixedY, fixedZ;
    int lane;
    float targetX, cooldown;
    Vector2 touchStart;

    void Start()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;

        lane = Mathf.Clamp(startLane, 0, laneX.Length - 1);
        targetX = laneX[lane];
    }

    void Update()
    {
        if (GameManager.Instance && GameManager.Instance.IsGameOver) return;

        cooldown -= Time.deltaTime;
        HandleInput();

        Vector3 p = transform.position;
        p.x = Mathf.Lerp(p.x, targetX, laneLerp * Time.deltaTime);
        p.y = fixedY; p.z = fixedZ;
        transform.position = p;
    }

    void HandleInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) touchStart = Input.mousePosition;
        else if (Input.GetMouseButtonUp(0)) TrySwipe(Input.mousePosition.x - touchStart.x);

        if (cooldown <= 0f)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) MoveLane(+1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) MoveLane(-1);
        }
#else
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) touchStart = t.position;
            else if (t.phase == TouchPhase.Ended) TrySwipe(t.position.x - touchStart.x);
        }
#endif
    }

    void TrySwipe(float dx)
    {
        if (cooldown > 0f || Mathf.Abs(dx) < swipePxThreshold) return;
        MoveLane(dx > 0 ? +1 : -1);
    }

    void MoveLane(int dir)
    {
        lane = Mathf.Clamp(lane + dir, 0, laneX.Length - 1);
        targetX = laneX[lane];
        cooldown = inputCooldown;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
            GameManager.Instance?.GameOver();
    }
}
