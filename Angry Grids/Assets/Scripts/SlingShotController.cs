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
        if (!isActive) return;
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currentStage == AimingStage.None)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
                {
                    StartVerticalAiming();
                }
            }
            else if (currentStage == AimingStage.Vertical)
            {
                StartHorizontalAiming();
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (currentStage == AimingStage.Vertical) UpdateVerticalAiming();
            else if (currentStage == AimingStage.Horizontal) UpdateHorizontalAiming();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentStage == AimingStage.Horizontal) LaunchBird();
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
        float mouseYDelta = (Input.mousePosition.y - initialMouseY) / Screen.height;
        verticalOffset = Mathf.Clamp(mouseYDelta * maxStretch * 2f, -maxStretch, maxStretch);

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
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.up, startPos);
        Vector3 worldMouse = startPos;

        if (dragPlane.Raycast(mouseRay, out float distance))
        {
            worldMouse = mouseRay.GetPoint(distance);
        }

        Vector3 horizontalDrag = worldMouse - startPos;
        horizontalDrag.y = 0;
        horizontalDrag = Vector3.ClampMagnitude(horizontalDrag, maxStretch);

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
        hasHitBoard = false;
        rb.isKinematic = false;
        currentStage = AimingStage.None;

        Vector3 pullVector = startPos - transform.position;

        if (pullVector.magnitude >= minLaunch)
        {
            rb.AddForce(pullVector * forceMultiplier, ForceMode.Impulse);

            CameraFollowBird cameraFollow = FindFirstObjectByType<CameraFollowBird>();
            if (cameraFollow != null) cameraFollow.OnBirdLaunched();

            if (TurnManager.Instance != null) TurnManager.Instance.OnBirdLaunched();
        }

        verticalOffset = 0f;

        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    void UpdateBands()
    {
        if (leftBand == null || rightBand == null || leftPost == null || rightPost == null) return;

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

        if (trajectoryLine.material != null)
        {
            trajectoryLine.material.mainTextureScale = new Vector2(trajectoryPoints / 2f, 1);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLaunched) return;

        TicTacToeSquare square = collision.gameObject.GetComponent<TicTacToeSquare>();
        if (square != null)
        {
            hasHitBoard = true;
            if (TurnManager.Instance != null) TurnManager.Instance.OnBirdHitBoard();
            return;
        }

        if (collision.gameObject.CompareTag("TicTacToeSquare"))
        {
            hasHitBoard = true;
            if (TurnManager.Instance != null) TurnManager.Instance.OnBirdHitBoard();
            return;
        }

        if (collision.gameObject.CompareTag("Ground") && !hasHitBoard)
        {
            if (TurnManager.Instance != null) TurnManager.Instance.OnBirdHitGround();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isLaunched) return;

        TicTacToeSquare square = other.GetComponent<TicTacToeSquare>();
        if (square != null)
        {
            hasHitBoard = true;
            if (TurnManager.Instance != null) TurnManager.Instance.OnBirdHitBoard();
            return;
        }

        if (other.CompareTag("TicTacToeSquare"))
        {
            hasHitBoard = true;
            if (TurnManager.Instance != null) TurnManager.Instance.OnBirdHitBoard();
            return;
        }

        if (other.CompareTag("Ground") && !hasHitBoard)
        {
            if (TurnManager.Instance != null) TurnManager.Instance.OnBirdHitGround();
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active && isDragging) CancelAiming();
    }

    public void ResetBird()
    {
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        startPos = originalPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // stay frozen until launched
        }

        isDragging = false;
        isLaunched = false;
        hasHitBoard = false;
        currentStage = AimingStage.None;
        verticalOffset = 0f;

        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    public GameObject GetBird() => gameObject;
    public bool IsLaunched() => isLaunched;
    public bool HasHitBoard() => hasHitBoard;

    private void CancelAiming()
    {
        isDragging = false;
        currentStage = AimingStage.None;
        verticalOffset = 0f;
        transform.position = startPos;

        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }
}
