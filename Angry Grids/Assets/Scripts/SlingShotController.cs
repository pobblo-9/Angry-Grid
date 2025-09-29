// NetworkSlingShotController.cs - Fixed version with better turn validation
using UnityEngine;
using Unity.Netcode;

public class SlingShotController : NetworkBehaviour
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

    [Header("Player Assignment")]
    public int playerNumber = 1; // Set this in inspector: 1 for player1, 2 for player2

    private Rigidbody rb;
    private bool isDragging = false;
    private Vector3 startPos;
    private bool isLaunched = false;
    private bool isActive = true;
    private bool hasHitBoard = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private enum AimingStage { None, Vertical, Horizontal }
    private AimingStage currentStage = AimingStage.None;
    private float verticalOffset = 0f;
    private float initialMouseY;

    // Safety flags
    private bool isNetworkSpawned = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if (leftBand != null) { leftBand.positionCount = 3; leftBand.enabled = false; }
        if (rightBand != null) { rightBand.positionCount = 3; rightBand.enabled = false; }
        if (trajectoryLine != null) { trajectoryLine.positionCount = trajectoryPoints; trajectoryLine.enabled = false; }

        if (rb != null) rb.isKinematic = true;

        Debug.Log($"Slingshot initialized for Player {playerNumber}");
    }

    public override void OnNetworkSpawn()
    {
        isNetworkSpawned = true;
        Debug.Log($"SlingShotController Player {playerNumber} NETWORK SPAWNED! IsOwner: {IsOwner}, ClientId: {OwnerClientId}, GameObject: {gameObject.name}");
        Debug.Log($"Player {playerNumber} slingshot is now network ready!");
    }

    public override void OnNetworkDespawn()
    {
        isNetworkSpawned = false;
    }

    void Update()
    {
        if (!isActive || !isNetworkSpawned) return;

        // Check if it's this player's turn and if we should handle input
        bool canHandleInput = CanHandleInput();

        if (canHandleInput)
        {
            HandleInput();
        }
    }
    

    bool CanHandleInput()
    {
        // Simplified for debugging - let's see if the basic mechanics work first
        Debug.Log($"CanHandleInput check for Player {playerNumber}: NetworkSpawned={isNetworkSpawned}, Active={isActive}");
        
        // Basic checks first
        if (!isActive)
        {
            Debug.Log($"Slingshot {playerNumber} is not active");
            return false;
        }
        
        // Ensure this slingshot has been network spawned
        if (!isNetworkSpawned)
        {
            Debug.Log($"Slingshot {playerNumber} is not network spawned");
            return false;
        }
        
        // Check if TurnManager exists
        if (TurnManager.Instance == null)
        {
            Debug.Log($"TurnManager.Instance is null for Player {playerNumber}");
            return false;
        }
        
        if (!TurnManager.Instance.IsSpawned)
        {
            Debug.Log($"TurnManager not spawned for Player {playerNumber}");
            return false;
        }

        // Check if the game is active
        if (!TurnManager.Instance.IsGameActive())
        {
            Debug.Log($"Game not active for Player {playerNumber}");
            return false;
        }

        // Get current player from turn manager
        int currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        int myPlayerNumber = TurnManager.Instance.GetMyPlayerNumber();
        
        // Simplified logic: just check if it's my turn
        bool isMyTurn = (currentPlayer == playerNumber && myPlayerNumber == playerNumber);
        
        Debug.Log($"Turn check for Player {playerNumber}: Current={currentPlayer}, My={myPlayerNumber}, IsMyTurn={isMyTurn}");
        
        return isMyTurn;
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"Mouse clicked! Player {playerNumber}, Stage: {currentStage}");
            
            if (currentStage == AimingStage.None)
            {
                Camera currentCamera = Camera.main;
                if (currentCamera == null) currentCamera = FindFirstObjectByType<Camera>();
                
                if (currentCamera != null)
                {
                    Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
                    Debug.Log($"Casting ray from camera for Player {playerNumber}. Camera: {currentCamera.name}");
                    
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        Debug.Log($"Raycast hit: {hit.collider.gameObject.name}, Expected root: {gameObject.name}");
                        // Accept hit if it is this object or any of its children
                        bool hitThisBird = hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
                        if (hitThisBird)
                        {
                            Debug.Log($"Player {playerNumber} bird clicked (hit accepted)!");
                            StartVerticalAiming();
                        }
                        else
                        {
                            Debug.Log($"Hit different object: {hit.collider.gameObject.name}");
                        }
                    }
                    else
                    {
                        Debug.Log($"Raycast missed everything for Player {playerNumber}");
                    }
                }
                else
                {
                    Debug.LogWarning($"No camera found for Player {playerNumber} input!");
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

        if (Input.GetMouseButtonUp(0) && currentStage == AimingStage.Horizontal)
        {
            LaunchBird();
        }
    }

    void StartVerticalAiming()
    {
        Debug.Log($"Starting vertical aiming for Player {playerNumber}");
        currentStage = AimingStage.Vertical;
        isDragging = true;
        if (rb != null) rb.isKinematic = true;

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
        Debug.Log($"Starting horizontal aiming for Player {playerNumber}");
        currentStage = AimingStage.Horizontal;
    }

    void UpdateHorizontalAiming()
    {
        Camera currentCamera = Camera.main;
        if (currentCamera == null) currentCamera = FindFirstObjectByType<Camera>();
        
        if (currentCamera == null) return;
        
        Ray mouseRay = currentCamera.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.up, startPos);
        Vector3 worldMouse = startPos;

        if (dragPlane.Raycast(mouseRay, out float distance))
            worldMouse = mouseRay.GetPoint(distance);

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
        Vector3 pullVector = startPos - transform.position;

        if (pullVector.magnitude >= minLaunch)
        {
            Debug.Log($"Launching bird for Player {playerNumber} with force: {pullVector.magnitude}");

            // Safety check before sending RPC
            if (IsSpawned && isNetworkSpawned && NetworkManager.Singleton != null)
            {
                try
                {
                    LaunchBirdServerRpc(pullVector, NetworkManager.Singleton.LocalClientId);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to send LaunchBirdServerRpc: {e.Message}");
                    ExecuteLaunchLocal(pullVector);
                }
            }
        }
        else
        {
            CancelAiming();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void LaunchBirdServerRpc(Vector3 pullVector, ulong clientId, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Server received launch from client {clientId} for Player {playerNumber}");

        // Execute launch on all clients
        ExecuteLaunchClientRpc(pullVector);

        // Notify turn manager
        if (TurnManager.Instance != null && TurnManager.Instance.IsSpawned)
        {
            try
            {
                TurnManager.Instance.OnBirdLaunchedServerRpc(clientId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to notify turn manager: {e.Message}");
            }
        }
    }

    [ClientRpc]
    void ExecuteLaunchClientRpc(Vector3 pullVector)
    {
        ExecuteLaunchLocal(pullVector);
    }

    void ExecuteLaunchLocal(Vector3 pullVector)
    {
        Debug.Log($"Executing launch locally for Player {playerNumber}");

        isDragging = false;
        isLaunched = true;
        hasHitBoard = false;
        if (rb != null) rb.isKinematic = false;
        currentStage = AimingStage.None;

        if (rb != null) rb.AddForce(pullVector * forceMultiplier, ForceMode.Impulse);

        CameraFollowBird cameraFollow = FindFirstObjectByType<CameraFollowBird>();
        if (cameraFollow != null) cameraFollow.OnBirdLaunched();

        verticalOffset = 0f;

        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    void UpdateBands()
    {
        if (leftBand == null || rightBand == null || leftPost == null || rightPost == null) return;

        Vector3 midLeft = (leftPost.position + transform.position) / 2; midLeft.y -= 0.5f;
        Vector3 midRight = (rightPost.position + transform.position) / 2; midRight.y -= 0.5f;

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
            trajectoryLine.material.mainTextureScale = new Vector2(trajectoryPoints / 2f, 1);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLaunched || !isNetworkSpawned) return;

        TicTacToeSquare square = collision.gameObject.GetComponent<TicTacToeSquare>();
        if (square != null)
        {
            hasHitBoard = true;
            Debug.Log($"Player {playerNumber} bird hit tic-tac-toe square!");
            HandleBoardHit(square);
            return;
        }

        if (collision.gameObject.CompareTag("TicTacToeSquare"))
        {
            hasHitBoard = true;
            Debug.Log($"Player {playerNumber} bird hit tic-tac-toe board (by tag)!");
            TicTacToeSquare parentSquare = collision.gameObject.GetComponentInParent<TicTacToeSquare>();
            if (parentSquare != null)
            {
                HandleBoardHit(parentSquare);
            }
            return;
        }

        if (collision.gameObject.CompareTag("Ground") && !hasHitBoard)
        {
            Debug.Log($"Player {playerNumber} bird hit the ground without hitting the board - resetting!");
            HandleGroundHit();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isLaunched || !isNetworkSpawned) return;

        TicTacToeSquare square = other.GetComponent<TicTacToeSquare>();
        if (square != null)
        {
            hasHitBoard = true;
            Debug.Log($"Player {playerNumber} bird triggered tic-tac-toe square!");
            HandleBoardHit(square);
            return;
        }

        if (other.CompareTag("TicTacToeSquare"))
        {
            hasHitBoard = true;
            Debug.Log($"Player {playerNumber} bird triggered tic-tac-toe board (by tag)!");
            TicTacToeSquare parentSquare = other.GetComponentInParent<TicTacToeSquare>();
            if (parentSquare != null)
            {
                HandleBoardHit(parentSquare);
            }
            return;
        }

        if (other.CompareTag("Ground") && !hasHitBoard)
        {
            Debug.Log($"Player {playerNumber} bird triggered the ground without hitting the board - resetting!");
            HandleGroundHit();
        }
    }

    void HandleBoardHit(TicTacToeSquare square)
    {
        try
        {
            if (TurnManager.Instance != null)
            {
                int currentPlayer = TurnManager.Instance.GetCurrentPlayer();

                // Prefer server-authoritative claim via board ServerRpc
                TicTacToeBoard board = FindFirstObjectByType<TicTacToeBoard>();
                if (board != null && board.IsSpawned && square != null)
                {
                    int index = square.GetIndex();
                    board.RequestClaimSquareServerRpc(index, currentPlayer);
                }
                else if (square != null)
                {
                    // Fallback: local update
                    square.OnSquareHit(currentPlayer);
                }

                if (TurnManager.Instance.IsSpawned)
                {
                    TurnManager.Instance.OnBirdHitBoardServerRpc(NetworkManager.Singleton.LocalClientId);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to handle board hit: {e.Message}");
        }
    }

    void HandleGroundHit()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.IsSpawned)
        {
            try
            {
                TurnManager.Instance.OnBirdHitGroundServerRpc(NetworkManager.Singleton.LocalClientId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to handle ground hit: {e.Message}");
            }
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        Debug.Log($"Player {playerNumber} slingshot active: {active}");
        if (!active && isDragging) CancelAiming();
    }

    public void ResetBird()
    {
        Debug.Log($"Resetting Player {playerNumber} bird");

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        startPos = originalPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
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
    public int GetPlayerNumber() => playerNumber;

    private void CancelAiming()
    {
        Debug.Log($"Canceling aiming for Player {playerNumber}");
        isDragging = false;
        currentStage = AimingStage.None;
        verticalOffset = 0f;
        transform.position = startPos;

        if (leftBand != null) leftBand.enabled = false;
        if (rightBand != null) rightBand.enabled = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

}