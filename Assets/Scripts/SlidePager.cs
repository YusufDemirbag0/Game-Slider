using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlidePager : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
	[SerializeField] ScrollRect scrollRect;
	[SerializeField] RectTransform content;
	[SerializeField] RectTransform viewport;
	[SerializeField, Range(0.05f, 0.4f)] float snapTime = 0.18f;
	[SerializeField] float flickVelocityThreshold = 500f; // Hızlı kaydırma hassasiyeti
	[SerializeField] float swipeThreshold = 0.5f; // %50 ekran eşiği

	int pageCount;
	float pageHeight;
	int currentIndex = -1;
	bool isSnapping;

	void Start()
	{
		if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
		if (!content) content = scrollRect.content;
		if (!viewport) viewport = scrollRect.viewport;

		pageCount = content.childCount;
		pageHeight = viewport.rect.height;

		SetPageImmediate(0);
	}

	public void OnBeginDrag(PointerEventData eventData) { }

	public void OnEndDrag(PointerEventData eventData)
    {
        if (isSnapping) return;

        var normalized = scrollRect.verticalNormalizedPosition;
        float pos = (1f - normalized) * (pageCount - 1);
        int nearest = Mathf.FloorToInt(pos + 0.5f);
        
        float vy = scrollRect.velocity.y;
        float dragDistance = Mathf.Abs(eventData.position.y - eventData.pressPosition.y);
        
        // Önce hızlı kaydırma (flick) durumunu kontrol et
        if (Mathf.Abs(vy) > flickVelocityThreshold)
        {
            // Yönü doğru şekilde belirle: vy pozitifse yukarı kaydırma -> sonraki sayfaya geç
            int dir = vy > 0f ? 1 : -1;
            int nextIndex = Mathf.Clamp(currentIndex + dir, 0, pageCount - 1);
            SnapTo(nextIndex);
        }
        // Sonra %50 ekran kuralını kontrol et
        else if (dragDistance > viewport.rect.height * swipeThreshold)
        {
            // Yönü doğru şekilde belirle: yukarı doğru çekme (-), aşağı doğru çekme (+)
            int dir = eventData.position.y > eventData.pressPosition.y ? -1 : 1;
            int nextIndex = Mathf.Clamp(currentIndex + dir, 0, pageCount - 1);
            SnapTo(nextIndex);
        }
        else
        {
            // Hiçbiri değilse, en yakın sayfaya yapış
            SnapTo(nearest);
        }
    }

	void SnapTo(int index)
	{
		// Sonsuz döngü için index'i ayarla
		int targetIndex = (index % pageCount + pageCount) % pageCount;
		
		StopAllCoroutines();
		StartCoroutine(SnapRoutine(targetIndex));
	}

	void Update()
	{
		if (!isSnapping && Input.touchCount > 0)
		{
			int visible = GetMostVisibleIndex();
			if (visible != currentIndex)
			{
				NotifyExit(currentIndex);
				currentIndex = visible;
				NotifyEnter(currentIndex);
			}
		}
	}
	
	int GetMostVisibleIndex()
	{
		float bestArea = -1f;
		int bestIdx = 0;
		for (int i = 0; i < pageCount; i++)
		{
			var child = content.GetChild(i) as RectTransform;
			if (!child) continue;

			Rect r = GetViewportSpaceRect(child);
			float w = Mathf.Min(r.xMax, viewport.rect.width) - Mathf.Max(r.xMin, 0f);
			float h = Mathf.Min(r.yMax, viewport.rect.height) - Mathf.Max(r.yMin, 0f);
			float area = Mathf.Max(0f, w) * Mathf.Max(0f, h);
			if (area > bestArea) { bestArea = area; bestIdx = i; }
		}
		return bestIdx;
	}

	Rect GetViewportSpaceRect(RectTransform child)
	{
		Vector3[] corners = new Vector3[4];
		child.GetWorldCorners(corners);
		Vector3[] vp = new Vector3[4];
		for (int i = 0; i < 4; i++) vp[i] = viewport.InverseTransformPoint(corners[i]);
		return new Rect(vp[0].x, vp[0].y, vp[2].x - vp[0].x, vp[2].y - vp[0].y);
	}

	IEnumerator SnapRoutine(int index)
	{
		isSnapping = true;

		float start = scrollRect.verticalNormalizedPosition;
		float target = 1f - (index / Mathf.Max(1f, (pageCount - 1)));
		float t = 0f;

		while (t < 1f)
		{
			t += Time.deltaTime / Mathf.Max(0.01f, snapTime);
			float v = Mathf.SmoothStep(0f, 1f, t);
			scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, v);
			yield return null;
		}

		scrollRect.verticalNormalizedPosition = target;
		isSnapping = false;

		if (index != currentIndex)
		{
			NotifyExit(currentIndex);
			currentIndex = index;
			NotifyEnter(currentIndex);
		}
	}

	void SetPageImmediate(int index)
	{
		int targetIndex = (index % pageCount + pageCount) % pageCount;
		float target = 1f - (targetIndex / Mathf.Max(1f, (pageCount - 1)));
		scrollRect.verticalNormalizedPosition = target;
		NotifyExit(currentIndex);
		currentIndex = targetIndex;
		NotifyEnter(currentIndex);
	}

	void NotifyEnter(int index)
	{
		var host = GetHost(index);
		host?.Enter();
	}

	void NotifyExit(int index)
	{
		var host = GetHost(index);
		host?.Exit();
	}

	SlideLifecycleHost GetHost(int index)
	{
		if (index < 0 || index >= pageCount) return null;
		var child = content.GetChild(index);
		return child ? child.GetComponent<SlideLifecycleHost>() : null;
	}
}