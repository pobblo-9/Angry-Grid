using UnityEngine;

public class BirdPreviewController : MonoBehaviour
{
    [Header("Selection Source")]
    public BirdSelectionManager selectionManager;
    [Tooltip("Player index to preview (1 or 2)")]
    public int player = 1;

    [Header("Preview Setup")]
    public Transform previewParent; // Empty GameObject in the scene to hold the preview instance
    public bool autoSpin = true;
    public float spinSpeed = 45f;
    public Vector3 spinAxis = Vector3.up;

    [Header("Instance Controls")]
    public Vector3 offset = Vector3.zero;
    public float uniformScale = 1f;

    private GameObject currentInstance;

    void Start()
    {
        RefreshPreview();
    }

    void Update()
    {
        if (autoSpin && currentInstance != null)
        {
            currentInstance.transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);
        }
    }

    public void RefreshPreview()
    {
        if (selectionManager == null || previewParent == null) return;

        ClearPreview();

        GameObject prefab = selectionManager.GetSelectedPrefabForPlayer(player);
        if (prefab == null) return;

        currentInstance = Instantiate(prefab, previewParent);
        currentInstance.transform.localPosition = offset;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one * Mathf.Max(0.001f, uniformScale);

        PrepareAsPreview(currentInstance);
    }

    public void ClearPreview()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }

        // Also clear any stray children under the parent
        if (previewParent != null)
        {
            for (int i = previewParent.childCount - 1; i >= 0; i--)
            {
                Destroy(previewParent.GetChild(i).gameObject);
            }
        }
    }

    private void PrepareAsPreview(GameObject instance)
    {
        if (instance == null) return;

        // Disable gameplay scripts that might cause errors in preview
        var sling = instance.GetComponentInChildren<SlingShotController>(true);
        if (sling != null) sling.enabled = false;

        // Stop physics interactions
        var rb = instance.GetComponentInChildren<Rigidbody>(true);
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var colliders = instance.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = false;

        // Optionally keep Animator if present for idle anims
        // Nothing else needed; the autoSpin will rotate the instance
    }
}