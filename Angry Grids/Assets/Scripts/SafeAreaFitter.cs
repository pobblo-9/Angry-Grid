using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafe;
    Vector2 lastScreen;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // Re-apply on resolution/orientation changes in editor and runtime
        if (lastSafe != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
            ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        lastSafe = safe;
        lastScreen = new Vector2(Screen.width, Screen.height);

        // Convert safe area rectangle (pixels) to normalized anchors
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}