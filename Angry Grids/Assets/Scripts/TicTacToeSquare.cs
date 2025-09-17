// NetworkTicTacToeSquare.cs - Fixed version
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkTicTacToeSquare : NetworkBehaviour
{
    [Header("Visual Feedback")]
    public Material defaultMaterial;
    public Material player1Material;
    public Material player2Material;

    private int squareIndex;
    private int row, col;
    private NetworkTicTacToeBoard parentBoard;
    private Renderer squareRenderer;

    // Fixed NetworkVariable declarations
    private NetworkVariable<int> ownerPlayer = new NetworkVariable<int>(0); // 0 = empty, 1 = player1, 2 = player2
    private NetworkVariable<bool> hasBeenClaimed = new NetworkVariable<bool>(false);

    void Awake()
    {
        squareRenderer = GetComponent<Renderer>();
        if (squareRenderer == null) squareRenderer = GetComponentInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        // Fixed event subscription
        ownerPlayer.OnValueChanged += OnOwnerPlayerChanged;
        hasBeenClaimed.OnValueChanged += OnClaimedStatusChanged;
    }

    public void Initialize(int index, int boardRow, int boardCol, NetworkTicTacToeBoard board)
    {
        squareIndex = index;
        row = boardRow;
        col = boardCol;
        parentBoard = board;

        if (squareRenderer != null && defaultMaterial != null)
            squareRenderer.material = defaultMaterial;
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnSquareHitServerRpc(int player, ulong clientId)
    {
        if (hasBeenClaimed.Value) return;

        if (parentBoard != null)
        {
            if (parentBoard.ClaimSquare(squareIndex, player))
            {
                hasBeenClaimed.Value = true;
            }
        }
        else
        {
            SetPlayerServerRpc(player);
            if (NetworkTurnManager.Instance != null)
                NetworkTurnManager.Instance.OnSquareClaimedServerRpc(squareIndex, player);
            hasBeenClaimed.Value = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bird") && !hasBeenClaimed.Value && NetworkTurnManager.Instance != null)
        {
            // Only the owner of the bird should trigger this
            NetworkSlingShotController birdController = other.GetComponent<NetworkSlingShotController>();
            if (birdController != null && birdController.IsOwner)
            {
                int currentPlayer = NetworkTurnManager.Instance.GetCurrentPlayer();
                OnSquareHitServerRpc(currentPlayer, NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerServerRpc(int player)
    {
        ownerPlayer.Value = player;
        hasBeenClaimed.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClearSquareServerRpc()
    {
        ownerPlayer.Value = 0;
        hasBeenClaimed.Value = false;
    }

    // Fixed callback signature
    void OnOwnerPlayerChanged(int previousValue, int newValue)
    {
        UpdateVisual(newValue);
    }

    // Fixed callback signature
    void OnClaimedStatusChanged(bool previousValue, bool newValue)
    {
        // Handle claimed status change if needed
    }

    void UpdateVisual(int player)
    {
        if (squareRenderer != null)
        {
            Material targetMaterial = defaultMaterial;

            if (player == 1 && player1Material != null)
                targetMaterial = player1Material;
            else if (player == 2 && player2Material != null)
                targetMaterial = player2Material;

            squareRenderer.material = targetMaterial;
        }
    }

    public void ClearSquare()
    {
        if (IsServer)
        {
            ClearSquareServerRpc();
        }

        // Clear visual locally
        if (squareRenderer != null && defaultMaterial != null)
            squareRenderer.material = defaultMaterial;

        // Destroy any symbol children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Symbol") || child.name.Contains("X") || child.name.Contains("O"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    public bool IsEmpty() { return ownerPlayer.Value == 0; }
    public int GetOwner() { return ownerPlayer.Value; }

    internal void ClaimSquare(int playerNumber)
    {
        throw new NotImplementedException();
    }
}