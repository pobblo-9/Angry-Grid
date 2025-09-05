using UnityEngine;

public class SlingShotController : MonoBehaviour
{
    public Transform leftPost;
    public Transform rightPost;
    public LineRenderer leftBand;
    public LineRenderer rightBand;
    public LineRenderer trajectoryLine;
    public float forceMultiplier = 100f;
    public float maxStretch = 5f;
    public float minLaunch = 0.5f;
    public int trajectoryPoints = 30;
    public float timeStep = 0.1f;

private Rigidbody rb;
    private bool isDragging = false;
    private Vector3 startPos;

    // Two-stage aiming variables
    private enum AimingStage { None, Vertical, Horizontal }
    private AimingStage currentStage = AimingStage.None;
    private float verticalOffset = 0f;
    private float initialMouseY;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;

        // setup bands
        leftBand.positionCount = 3;
        rightBand.positionCount = 3;
        leftBand.enabled = false;
        rightBand.enabled = false;

        // setup trajectory
        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.enabled = false;
    }

    void Update()
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
            // For vertical stage, we just wait for the next click
        }
    }

    void StartVerticalAiming()
    {
        currentStage = AimingStage.Vertical;
        isDragging = true;
        rb.isKinematic = true;
        leftBand.enabled = true;
        rightBand.enabled = true;
        trajectoryLine.enabled = true;

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
        // No need to store initial mouse position since we're using world space raycast
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

            // Tell camera to start following the bird
            Object.FindFirstObjectByType<CameraFollowBird>()?.OnBirdLaunched();

            // Reset offsets for next shot
            verticalOffset = 0f;
        }

        leftBand.enabled = false;
        rightBand.enabled = false;
        trajectoryLine.enabled = false;
    }

    void UpdateBands()
    {
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

        // make it dotted by tiling the texture
        trajectoryLine.material.mainTextureScale = new Vector2(trajectoryPoints / 2f, 1);
    }
}