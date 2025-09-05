using UnityEngine;

public class CameraFollowBird : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform bird;
    public float followSpeed = 2f;
    public float returnSpeed = 3f;
    public Vector3 offset = new Vector3(0, 2f, -10f);

    [Header("Follow Triggers")]
    public float minVelocityToFollow = 5f; // Start following when bird moves this fast
    public float maxStopTime = 2f; // Stop following after bird is slow for this long

    [Header("Boundaries")]
    public float maxFollowDistance = 50f; // Don't follow beyond this distance from slingshot
    public Transform slingshot;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isFollowing = false;
    private float stopTimer = 0f;
    private Vector3 targetPosition;
    private Rigidbody birdRb;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        if (bird != null)
            birdRb = bird.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (bird == null || birdRb == null) return;

        float birdSpeed = birdRb.linearVelocity.magnitude;
        float distanceFromSlingshot = Vector3.Distance(bird.position, slingshot.position);

        // Start following if bird is moving fast enough
        if (!isFollowing && birdSpeed > minVelocityToFollow)
        {
            isFollowing = true;
            stopTimer = 0f;
        }

        // Stop following if bird is too slow for too long or too far away
        if (isFollowing)
        {
            if (birdSpeed < minVelocityToFollow || distanceFromSlingshot > maxFollowDistance)
            {
                stopTimer += Time.deltaTime;
                if (stopTimer >= maxStopTime)
                {
                    isFollowing = false;
                }
            }
            else
            {
                stopTimer = 0f;
            }
        }

        // Calculate target position
        if (isFollowing)
        {
            // Follow bird with offset and some prediction based on velocity
            Vector3 prediction = birdRb.linearVelocity.normalized * 3f;
            targetPosition = bird.position + offset + prediction;

            // Smoothly move to target
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            // Return to original position
            transform.position = Vector3.Lerp(transform.position, originalPosition, returnSpeed * Time.deltaTime);
        }

        // Always look at bird when following, or look forward when returning
        if (isFollowing)
        {
            // Calculate direction to bird
            Vector3 directionToBird = (bird.position - transform.position).normalized;

            // For 2D games, you might want to keep the camera looking forward instead of at the bird
            // Comment out the next two lines if you want the camera to just follow without rotating
            Quaternion targetRotation = Quaternion.LookRotation(directionToBird, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
        }
        else
        {
            // Gradually return to original rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, returnSpeed * Time.deltaTime);
        }
    }

    // Call this from SlingShotController when bird is launched
    public void OnBirdLaunched()
    {
        stopTimer = 0f;
        // Reset following state to let velocity trigger it naturally
        isFollowing = false;
    }

    // Call this to manually return camera (e.g., when resetting bird)
    public void ReturnToOriginal()
    {
        isFollowing = false;
        stopTimer = maxStopTime; // Force stop
    }
}
