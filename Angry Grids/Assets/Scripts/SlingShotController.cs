using UnityEngine;

public class SlingShotController : MonoBehaviour
{
    [Header("Slingshot Components")]
    public Transform leftPost;
    public Transform rightPost;
    public LineRenderer leftBand;
    public LineRenderer rightBand;
    public LineRenderer trajectoryLine;

    [Header("Launch Settings")]
    public float forceMultiplier = 100f;
    public float maxStretch = 5f;
    public float minLaunch = 0.5f;

    [Header("Trajectory Preview")]
    public int trajectoryPoints = 30;
    public float timeStep = 0.1f;

    private Rigidbody rb;
    private bool isDragging = false;
    private Vector3 startPos;
    private bool isLaunched = false;
    private bool isActive = true;
    private bool hasHitBoard = false;

    // Store original position for reset
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Two-stage aiming variables
    private enum AimingStage { None, Vertical, Horizontal }
    private AimingStage currentStage = AimingStage.None;
    private float verticalOffset = 0f;
    private float initialMouseY;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;

        // Store original position and rotation for reset
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Setup bands
        if (leftBand != null)
        {
            leftBand.positionCount = 3;
            leftBand.enabled = false;
        }

        if (rightBand != null)
        {
            rightBand.positionCount = 3;
            rightBand.enabled = false;
        }

        // Setup trajectory
        if (trajectoryLine != null)
        {
            trajectoryLine.positionCount = trajectoryPoints;
            trajectoryLine.enabled = false;
        }
    }

    void Update()
    {
        // Don't respond to input if not active
        if (!isActive) return;

        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currentStage == AimingStage.None)
            {
                // First click - check if we're clicking on the bird
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
                {
                    StartVerticalAiming();
                }
            }
            else if (currentStage == AimingStage.Vertical)
            {
                // Second click - start horizontal aiming
                StartHorizontalAiming();
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (currentStage == AimingStage.Vertical)
            {
                UpdateVerticalAiming();
            }
            else if (currentStage == AimingStage.Horizontal)
            {
                UpdateHorizontalAiming();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentStage == AimingStage.Horizontal)
            {
                // Second release - launch the bird
                LaunchBird();
            }
        }
    }

    void StartVerticalAiming()
    {
        currentStage = AimingStage.Vertical;
        isDragging = true;
        rb.isKinematic = true;

        if (leftBand != null) leftBand.enabled = true;
        if (rightBand != null) rightBand.enabled = true;
        if (trajectoryLine != null) trajectoryLine.enabled = true;

        initialMouseY = Input.mousePosition.y;
        verticalOffset = 0f;
    }

    void UpdateVerticalAiming()
    {
        // Calculate vertical offset based on mouse Y movement
        float mouseYDelta = (Input.mousePosition.y - initialMouseY) / Screen.height;
        verticalOffset = Mathf.Clamp(mouseYDelta * maxStretch * 2f, -maxStretch, maxStretch);

        // Update bird position (only vertical for now)
        Vector3 newPos = startPos + new Vector3(0, verticalOffset, 0);
        transform.position = newPos;

        UpdateBands();
        UpdateTrajectory();
    }

    void StartHorizontalAiming()
    {
        currentStage = AimingStage.Horizontal;
    }

    void UpdateHorizontalAiming()
    {
        // Cast a ray from camera through mouse position onto a horizontal plane
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Create a plane at the slingshot's Y level facing up
        Plane dragPlane = new Plane(Vector3.up, startPos);

        Vector3 worldMouse = startPos; // Default fallback

        // Find where the mouse ray intersects the plane
        if (dragPlane.Raycast(mouseRay, out float distance))
        {
            worldMouse = mouseRay.GetPoint(distance);
        }

        // Calculate horizontal drag - direct movement (not inverted)
        Vector3 horizontalDrag = worldMouse - startPos;
        horizontalDrag.y = 0; // Keep it purely horizontal

        // Clamp the horizontal drag
        horizontalDrag = Vector3.ClampMagnitude(horizontalDrag, maxStretch);

        // Combine with vertical offset from first stage
        Vector3 totalDrag = horizontalDrag + new Vector3(0, verticalOffset, 0);
        totalDrag = Vector3.ClampMagnitude(totalDrag, maxStretch);

        transform.position = startPos + totalDrag;

        UpdateBands();
        UpdateTrajectory();
    }

    void LaunchBird()
    {
        isDragging = false;
        isLaunched = true;
        hasHitBoard = false; // Reset board hit flag
        rb.isKinematic = false;
        currentStage = AimingStage.None;

        Vector3 pullVector = startPos - transform.position;

        if (pullVector.magnitude < minLaunch)
        {
            // Reset for next shot
            verticalOffset = 0f;
        }
        else
        {
            // Launch the bird
            rb.AddForce(pullVector * forceMultiplier, ForceMode.Impulse);

            // Tell camera to start following the bird (if it exists)
            CameraFollowBird cameraFollow = FindFirstObjectByType<CameraFollowBird>();
            if (cameraFollow != null)
            {
                cameraFollow.OnBirdLaunched();
            }

            // Notify turn manager that bird was launched
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnBirdLaunched();
            }

            // Reset offsets for next shot
            verticalOffset = 0f;
        }

        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    void UpdateBands()
    {
        if (leftBand == null || rightBand == null || leftPost == null || rightPost == null)
            return;

        Vector3 midLeft = (leftPost.position + transform.position) / 2;
        midLeft.y -= 0.5f;
        Vector3 midRight = (rightPost.position + transform.position) / 2;
        midRight.y -= 0.5f;

        leftBand.SetPosition(0, leftPost.position);
        leftBand.SetPosition(1, midLeft);
        leftBand.SetPosition(2, transform.position);

        rightBand.SetPosition(0, rightPost.position);
        rightBand.SetPosition(1, midRight);
        rightBand.SetPosition(2, transform.position);
    }

    void UpdateTrajectory()
    {
        if (trajectoryLine == null || rb == null) return;

        Vector3 pullVector = startPos - transform.position;
        Vector3 launchVelocity = pullVector * forceMultiplier / rb.mass;

        Vector3 pos = transform.position;
        Vector3 vel = launchVelocity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            trajectoryLine.SetPosition(i, pos);
            vel += Physics.gravity * timeStep;
            pos += vel * timeStep;
        }

        // Make it dotted by tiling the texture
        if (trajectoryLine.material != null)
        {
            trajectoryLine.material.mainTextureScale = new Vector2(trajectoryPoints / 2f, 1);
        }
    }

    // Collision detection for ground and board
    void OnCollisionEnter(Collision collision)
    {
        // Only process collisions if the bird has been launched
        if (!isLaunched) return;

        // Check if bird hit the tic-tac-toe board
        if (collision.gameObject.CompareTag("TicTacToeSquare") ||
        collision.gameObject.GetComponent<TicTacToeSquare>() != null)
        {
            hasHitBoard = true;
            Debug.Log("Bird hit the tic-tac-toe board!");
            return;
        }

        // Check if bird hit the ground (and hasn't hit the board yet)
        if (collision.gameObject.CompareTag("Ground") && !hasHitBoard)
        {
            Debug.Log("Bird hit the ground without hitting the board - resetting!");

            // Notify turn manager to reset immediately
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnBirdHitGround();
            }
        }
    }

    // Trigger detection as backup (in case you use trigger colliders)
    void OnTriggerEnter(Collider other)
    {
        // Only process triggers if the bird has been launched
        if (!isLaunched) return;

        // Check if bird hit the tic-tac-toe board
        if (other.CompareTag("TicTacToeSquare") ||
        other.GetComponent<TicTacToeSquare>() != null)
        {
            hasHitBoard = true;
            Debug.Log("Bird triggered the tic-tac-toe board!");
            return;
        }

        // Check if bird hit the ground (and hasn't hit the board yet)
        if (other.CompareTag("Ground") && !hasHitBoard)
        {
            Debug.Log("Bird triggered the ground without hitting the board - resetting!");

            // Notify turn manager to reset immediately
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnBirdHitGround();
            }
        }
    }

    // Methods required by TurnManager
    public void SetActive(bool active)
    {
        isActive = active;

        // If deactivating, cancel any current aiming
        if (!active && isDragging)
        {
            CancelAiming();
        }
    }

    public void ResetBird()
    {
        // Reset position and rotation
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        startPos = originalPosition;

        // Reset physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        // Reset state
        isDragging = false;
        isLaunched = false;
        hasHitBoard = false;
        currentStage = AimingStage.None;
        verticalOffset = 0f;

        // Hide visual elements
        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    public GameObject GetBird()
    {
        return gameObject; // This script is attached to the bird itself
    }

    public bool IsLaunched()
    {
        return isLaunched;
    }

    public bool HasHitBoard()
    {
        return hasHitBoard;
    }

    private void CancelAiming()
    {
        isDragging = false;
        currentStage = AimingStage.None;
        verticalOffset = 0f;

        // Reset position
        transform.position = startPos;

        // Hide visual elements
        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }
}