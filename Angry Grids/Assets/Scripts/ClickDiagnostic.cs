// ClickDiagnostic.cs - Temporary script to test basic click detection
using UnityEngine;

public class ClickDiagnostic : MonoBehaviour
{
    [Header("Debug Info")]
    public bool logMouseClicks = true;
    public bool logColliderInfo = true;

    void Start()
    {
        if (logColliderInfo)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            Debug.Log($"GameObject {name} has {colliders.Length} colliders:");
            for (int i = 0; i < colliders.Length; i++)
            {
                Debug.Log($"  - Collider {i}: {colliders[i].name}, IsTrigger: {colliders[i].isTrigger}");
            }
        }
    }

    void Update()
    {
        if (logMouseClicks && Input.GetMouseButtonDown(0))
        {
            // Test basic ray casting
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Debug.Log($"[ClickDiagnostic] Mouse click hit: {hit.collider.name} on object: {hit.collider.gameObject.name}");
                    
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                    {
                        Debug.Log($"[ClickDiagnostic] ✅ THIS OBJECT WAS CLICKED! ({name})");
                        OnThisObjectClicked();
                    }
                    else
                    {
                        Debug.Log($"[ClickDiagnostic] Different object clicked: {hit.collider.gameObject.name}");
                    }
                }
                else
                {
                    Debug.Log($"[ClickDiagnostic] Mouse click missed all colliders");
                }
            }
            else
            {
                Debug.LogWarning($"[ClickDiagnostic] No camera found!");
            }
        }
    }

    void OnMouseDown()
    {
        Debug.Log($"[ClickDiagnostic] OnMouseDown triggered for {name}");
        OnThisObjectClicked();
    }

    void OnThisObjectClicked()
    {
        Debug.Log($"[ClickDiagnostic] 🎯 CONFIRMED CLICK ON {name}!");
        
        // Change color to show it was clicked
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
            // Change back after 1 second
            Invoke(nameof(ResetColor), 1f);
        }
    }

    void ResetColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }
    }
}