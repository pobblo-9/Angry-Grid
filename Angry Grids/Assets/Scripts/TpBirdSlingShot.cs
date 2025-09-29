// NetworkedBird.cs - Networked bird physics and collision detection
using UnityEngine;
using Unity.Netcode;

public class NetworkedBird : NetworkBehaviour
{
    [Header("Bird Physics")]
    public float mass = 1f;
    public float drag = 0.5f;
    public float angularDrag = 0.8f;
    public bool useGravity = true;

    [Header("Collision Settings")]
    public LayerMask groundLayer = -1;
    public LayerMask boardLayer = -1;
    
    [Header("Effects")]
    public ParticleSystem hitEffect;
    public AudioClip hitSound;
    public AudioClip launchSound;

    private Rigidbody rb;
    private AudioSource audioSource;
    private bool hasLaunched = false;
    private bool hasHitBoard = false;
    private int ownerPlayerNumber = 0;

    // Network variables to sync bird state
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();
    private NetworkVariable<bool> isLaunched = new NetworkVariable<bool>(false);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        SetupPhysics();
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to network variable changes
        networkPosition.OnValueChanged += OnPositionChanged;
        networkVelocity.OnValueChanged += OnVelocityChanged;
        isLaunched.OnValueChanged += OnLaunchedStateChanged;
        
        // Initialize position
        if (IsOwner)
        {
            networkPosition.Value = transform.position;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from network variable changes
        if (networkPosition != null) networkPosition.OnValueChanged -= OnPositionChanged;
        if (networkVelocity != null) networkVelocity.OnValueChanged -= OnVelocityChanged;
        if (isLaunched != null) isLaunched.OnValueChanged -= OnLaunchedStateChanged;
    }

    void SetupPhysics()
    {
        if (rb != null)
        {
            rb.mass = mass;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.useGravity = useGravity;
            rb.isKinematic = true; // Start kinematic, enable physics when launched
        }
    }

    public void InitializeBird(int playerNumber)
    {
        ownerPlayerNumber = playerNumber;
        gameObject.tag = "Bird";
        Debug.Log($"Bird initialized for Player {playerNumber}");
    }

    public void LaunchBird(Vector3 force)
    {
        if (!IsOwner || hasLaunched) return;

        Debug.Log($"Launching bird with force: {force}");
        
        hasLaunched = true;
        hasHitBoard = false;
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(force, ForceMode.Impulse);
        }

        // Update network state
        if (IsServer)
        {
            isLaunched.Value = true;
            networkVelocity.Value = rb.linearVelocity;
        }

        // Play launch sound
        if (launchSound != null && audioSource != null)
            audioSource.PlayOneShot(launchSound);

        LaunchBirdClientRpc(force);
    }

    [ClientRpc]
    void LaunchBirdClientRpc(Vector3 force)
    {
        if (!IsOwner) // Non-owners need to sync the launch
        {
            hasLaunched = true;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(force, ForceMode.Impulse);
            }

            if (launchSound != null && audioSource != null)
                audioSource.PlayOneShot(launchSound);
        }
    }

    public void ResetBird(Vector3 resetPosition, Quaternion resetRotation)
    {
        Debug.Log($"Resetting bird for Player {ownerPlayerNumber}");
        
        hasLaunched = false;
        hasHitBoard = false;
        
        transform.position = resetPosition;
        transform.rotation = resetRotation;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (IsServer)
        {
            isLaunched.Value = false;
            networkPosition.Value = resetPosition;
            networkVelocity.Value = Vector3.zero;
        }

        ResetBirdClientRpc(resetPosition, resetRotation);
    }

    [ClientRpc]
    void ResetBirdClientRpc(Vector3 resetPosition, Quaternion resetRotation)
    {
        if (!IsOwner)
        {
            hasLaunched = false;
            hasHitBoard = false;
            transform.position = resetPosition;
            transform.rotation = resetRotation;
            
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    void Update()
    {
        // Sync position for owner
        if (IsOwner && hasLaunched && rb != null)
        {
            if (IsServer)
            {
                networkPosition.Value = transform.position;
                networkVelocity.Value = rb.linearVelocity;
            }
        }
        
        // Check if bird is out of bounds
        if (hasLaunched && transform.position.y < -50f)
        {
            OnBirdOutOfBounds();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasLaunched || !IsOwner) return;

        // Check for tic-tac-toe square collision
        TicTacToeSquare square = collision.gameObject.GetComponent<TicTacToeSquare>();
        if (square != null && !hasHitBoard)
        {
            hasHitBoard = true;
            OnBoardHit(square);
            return;
        }

        // Check for ground collision
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            OnGroundHit();
            return;
        }

        // Play hit effects
        PlayHitEffects(collision.contacts[0].point);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasLaunched || !IsOwner) return;

        // Check for tic-tac-toe square trigger
        TicTacToeSquare square = other.GetComponent<TicTacToeSquare>();
        if (square != null && !hasHitBoard)
        {
            hasHitBoard = true;
            OnBoardHit(square);
        }
    }

    void OnBoardHit(TicTacToeSquare square)
    {
        Debug.Log($"Player {ownerPlayerNumber} bird hit the tic-tac-toe board!");
        
        if (TurnManager.Instance != null)
        {
            int currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            TicTacToeBoard board = FindFirstObjectByType<TicTacToeBoard>();
            if (board != null && board.IsSpawned && square != null)
            {
                int squareIndex = square.GetIndex();
                board.RequestClaimSquareServerRpc(squareIndex, currentPlayer);
            }
            
            if (TurnManager.Instance.IsSpawned)
            {
                TurnManager.Instance.OnBirdHitBoardServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        PlayHitEffects(transform.position);
    }

    void OnGroundHit()
    {
        if (!hasHitBoard)
        {
            Debug.Log($"Player {ownerPlayerNumber} bird hit the ground without hitting the board!");
            
            if (TurnManager.Instance != null && TurnManager.Instance.IsSpawned)
            {
                TurnManager.Instance.OnBirdHitGroundServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        PlayHitEffects(transform.position);
    }

    void OnBirdOutOfBounds()
    {
        Debug.Log($"Player {ownerPlayerNumber} bird went out of bounds!");
        
        if (TurnManager.Instance != null && TurnManager.Instance.IsSpawned)
        {
            TurnManager.Instance.OnBirdHitGroundServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    void PlayHitEffects(Vector3 hitPosition)
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect.gameObject, hitPosition, Quaternion.identity);
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);
    }

    // Network variable change callbacks
    void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
    {
        if (!IsOwner)
        {
            transform.position = newValue;
        }
    }

    void OnVelocityChanged(Vector3 previousValue, Vector3 newValue)
    {
        if (!IsOwner && rb != null)
        {
            rb.linearVelocity = newValue;
        }
    }

    void OnLaunchedStateChanged(bool previousValue, bool newValue)
    {
        hasLaunched = newValue;
        if (newValue && rb != null)
        {
            rb.isKinematic = false;
        }
    }

    // Public getters
    public bool HasLaunched() => hasLaunched;
    public bool HasHitBoard() => hasHitBoard;
    public int GetPlayerNumber() => ownerPlayerNumber;
    public Rigidbody GetRigidbody() => rb;
}
