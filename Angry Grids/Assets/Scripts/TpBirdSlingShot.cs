using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TeleportManager : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private GameObject selectedObject;
    [SerializeField] private Vector3 targetCoordinates = new Vector3(0, 0, 0);

    [Header("UI References")]
    [SerializeField] private Button teleportButton;

    [Header("Optional Settings")]
    [SerializeField] private bool useRandomOffset = false;
    [SerializeField] private float randomOffsetRange = 1f;
    [SerializeField] private bool playTeleportEffect = false;
    [SerializeField] private ParticleSystem teleportEffect;
    [SerializeField] private AudioClip teleportSound;

    private AudioSource audioSource;
    private bool teleportRequested = false;

    void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && teleportSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Subscribe to button click event
        if (teleportButton != null)
        {
            teleportButton.onClick.AddListener(RequestTeleport);
        }
        else
        {
            Debug.LogWarning("Teleport button is not assigned!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from button event to prevent memory leaks
        if (teleportButton != null)
        {
            teleportButton.onClick.RemoveListener(RequestTeleport);
        }
    }

    // This method is called by the UI button ONLY
    public void RequestTeleport()
    {
        teleportRequested = true;
        Debug.Log("Teleport requested via UI button");
        TeleportObject();
    }

    // This is the actual teleport method - only call this from RequestTeleport()
    private void TeleportObject()
    {
        // Safety check - only teleport if it was requested via the UI button
        if (!teleportRequested)
        {
            Debug.Log("Teleport blocked: Not requested via UI button");
            return;
        }

        if (selectedObject == null)
        {
            Debug.LogWarning("No object selected for teleportation!");
            teleportRequested = false;
            return;
        }

        Vector3 finalPosition = targetCoordinates;

        // Add random offset if enabled
        if (useRandomOffset)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(-randomOffsetRange, randomOffsetRange)
            );
            finalPosition += randomOffset;
        }

        // Play teleport effect at current position (before teleporting)
        if (playTeleportEffect && teleportEffect != null)
        {
            PlayTeleportEffectAt(selectedObject.transform.position);
        }

        Vector3 oldPosition = selectedObject.transform.position;

        // Teleport the object
        selectedObject.transform.position = finalPosition;

        // Play teleport effect at destination
        if (playTeleportEffect && teleportEffect != null)
        {
            PlayTeleportEffectAt(finalPosition);
        }

        // Play teleport sound
        if (teleportSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        Debug.Log($"Successfully teleported {selectedObject.name} from {oldPosition} to {finalPosition}");

        // Reset the request flag
        teleportRequested = false;
    }

    private void PlayTeleportEffectAt(Vector3 position)
    {
        if (teleportEffect != null)
        {
            // Create temporary effect at position
            ParticleSystem effect = Instantiate(teleportEffect, position, Quaternion.identity);
            effect.Play();

            // Destroy effect after it finishes
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
        }
    }

    // Method to set selected object from script
    public void SetSelectedObject(GameObject obj)
    {
        selectedObject = obj;
        Debug.Log($"Selected object set to: {obj.name}");
    }

    // Method to set target coordinates from script
    public void SetTargetCoordinates(Vector3 coordinates)
    {
        targetCoordinates = coordinates;
        Debug.Log($"Target coordinates set to: {coordinates}");
    }

    // Method to set target coordinates with individual values
    public void SetTargetCoordinates(float x, float y, float z)
    {
        SetTargetCoordinates(new Vector3(x, y, z));
    }

    // Emergency method to force teleport (bypasses all checks)
    public void ForceTeleport()
    {
        teleportRequested = true;
        TeleportObject();
    }

    // Check if something else is trying to call TeleportObject directly
    void Update()
    {
        // If you see this message in the console, something else is calling TeleportObject()
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Manual teleport test - this should work");
            RequestTeleport();
        }
    }
}