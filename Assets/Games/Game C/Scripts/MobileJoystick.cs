using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum Mode { Fixed, Floating }

    [Header("Refs")]
    [SerializeField] RectTransform bg;          // sabit daire
    [SerializeField] RectTransform handle;      // küçük daire
    [SerializeField] PlayerMovement target;     // otomatik besleme

    [Header("Ayarlar")]
    [SerializeField] Mode mode = Mode.Fixed;    // SABİT varsayılan
    [SerializeField] float radius = 110f;       // px
    [SerializeField] float deadZone = 0.12f;    // 0..1
    [SerializeField] float returnSpeed = 18f;   // bırakınca merkeze dönüş
    [SerializeField] float inputSmoothing = 0.0f;

    [Header("Görsel")]
    [SerializeField] float pressScale = 1.04f;     // basılıyken bg scale
    [SerializeField] float handleMaxOffset = 1.0f; // 0..1

    Canvas canvas;
    Camera uiCam;
    Vector2 centerScreenPos;  // bg'nin ekran koordinatındaki merkezi
    Vector2 rawDir, smoothDir;
    int pointerId = -1;
    bool pressed;

    public Vector2 Direction => (inputSmoothing > 0f) ? smoothDir : rawDir;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        uiCam = canvas && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;

        if (!bg || !handle)
            Debug.LogWarning("[MobileJoystick] bg/handle atanmadı.");

        if (bg) centerScreenPos = RectTransformUtility.WorldToScreenPoint(uiCam, bg.position);
    }

    void Update()
    {
        // Bırakınca merkeze yumuşak dönüş
        if (!pressed)
        {
            if (handle) handle.anchoredPosition = Vector2.Lerp(handle.anchoredPosition, Vector2.zero, Time.deltaTime * returnSpeed);
            rawDir = Vector2.Lerp(rawDir, Vector2.zero, Time.deltaTime * returnSpeed);
        }

        // Input smoothing
        if (inputSmoothing > 0f)
            smoothDir = Vector2.Lerp(smoothDir, rawDir, 1f - Mathf.Exp(-inputSmoothing * Time.deltaTime));

        // PlayerMovement’e gönder
        if (target) target.SetExternalInput(Direction);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId != -1) return;
        pointerId = eventData.pointerId;
        pressed = true;

        // SABİT mod: bg yerinde kalır. (Floating ise sadece o modda parmağa gider)
        if (mode == Mode.Floating && bg)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bg.parent as RectTransform, eventData.position, uiCam, out Vector2 localPos);
            bg.anchoredPosition = localPos;
        }

        if (bg) centerScreenPos = RectTransformUtility.WorldToScreenPoint(uiCam, bg.position);

        if (bg) bg.localScale = Vector3.one * pressScale;

        ProcessDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;
        ProcessDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != pointerId) return;

        pointerId = -1;
        pressed = false;
        if (bg) bg.localScale = Vector3.one;
    }

    void ProcessDrag(PointerEventData eventData)
    {
        if (!bg || !handle) return;

        // EKRAN uzayında parmak - bg merkezi
        Vector2 delta = eventData.position - centerScreenPos;

        // Canvas ölçeğini dikkate alarak sınırla
        float maxPx = radius * handleMaxOffset;
        Vector2 clampedPx = Vector2.ClampMagnitude(delta, maxPx);

        // Handle local pozisyon (anchoredPosition piksel cinsinden, canvas ölçeğine böl)
        handle.anchoredPosition = clampedPx / Mathf.Max(1f, canvas ? canvas.scaleFactor : 1f);

        // normalize (0..1) + deadZone
        Vector2 norm = clampedPx / Mathf.Max(1f, radius);
        float mag = norm.magnitude;

        if (mag < deadZone) rawDir = Vector2.zero;
        else rawDir = norm.normalized * Mathf.InverseLerp(deadZone, 1f, mag);
    }

    // Koddan ayarlamak istersen:
    public void SetMode(Mode m) => mode = m;
    public void SetTarget(PlayerMovement t) => target = t;
}
