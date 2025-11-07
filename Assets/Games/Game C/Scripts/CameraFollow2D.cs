using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = Vector2.zero;

    [Header("Smooth (0 = anında)")]
    [SerializeField] float smoothTime = 0.15f;
    Vector3 _vel;

    [Header("Bounds (opsiyonel)")]
    [SerializeField] bool useManualBounds = false;
    [SerializeField] Rect manualBounds;

    [Header("Pixel Snap (opsiyonel)")]
    [SerializeField] bool pixelSnapCamera = false;
    [SerializeField] int  pixelsPerUnit  = 32; // sprite PPU ile aynı olmalı

    Camera _cam;
    float _zFixed;
    Vector3 _lastTargetPos;
    Rigidbody2D _targetRb;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _zFixed = transform.position.z;
    }

    void Start()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        _targetRb = target ? target.GetComponent<Rigidbody2D>() : null;
        _lastTargetPos = target ? target.position : transform.position;
        InstantSnap();
    }

    void LateUpdate()
    {
        if (!target) return;

        // hedef pozisyon
        Vector3 desired = new Vector3(target.position.x + offset.x,
                                      target.position.y + offset.y,
                                      _zFixed);

        // yumuşak takip
        Vector3 next = (smoothTime > 0f)
            ? Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime)
            : desired;

        // pixel snap (opsiyonel)
        if (pixelSnapCamera && pixelsPerUnit > 0)
        {
            float p = 1f / pixelsPerUnit;
            next.x = Mathf.Round(next.x / p) * p;
            next.y = Mathf.Round(next.y / p) * p;
        }

        // clamp (opsiyonel)
        if (useManualBounds)
            next = ClampCameraToBounds(next, manualBounds);

        transform.position = next;
        _lastTargetPos = target.position;
    }

    Vector3 ClampCameraToBounds(Vector3 camPos, Rect bounds)
    {
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        float minX = bounds.xMin + halfW;
        float maxX = bounds.xMax - halfW;
        float minY = bounds.yMin + halfH;
        float maxY = bounds.yMax - halfH;

        // harita kamera görünümünden küçükse merkezle
        if (minX > maxX) camPos.x = bounds.center.x;
        else camPos.x = Mathf.Clamp(camPos.x, minX, maxX);

        if (minY > maxY) camPos.y = bounds.center.y;
        else camPos.y = Mathf.Clamp(camPos.y, minY, maxY);

        camPos.z = _zFixed;
        return camPos;
    }

    // ---- Basit API ----
    public void SetTarget(Transform t)
    {
        target = t;
        _targetRb = t ? t.GetComponent<Rigidbody2D>() : null;
        InstantSnap();
    }

    public void SetManualBounds(Rect r, bool enable = true)
    {
        manualBounds = r;
        useManualBounds = enable;
    }

    public void InstantSnap()
    {
        if (!target) return;
        Vector3 p = new Vector3(target.position.x + offset.x, target.position.y + offset.y, _zFixed);

        if (pixelSnapCamera && pixelsPerUnit > 0)
        {
            float px = 1f / pixelsPerUnit;
            p.x = Mathf.Round(p.x / px) * px;
            p.y = Mathf.Round(p.y / px) * px;
        }

        transform.position = p;
        _vel = Vector3.zero;
        _lastTargetPos = target.position;
    }
}
