// TicTacToeSquare.cs - Fixed version (no longer NetworkBehaviour)
using UnityEngine;
using Unity.Netcode;
using System;

public class TicTacToeSquare : MonoBehaviour
{
    [Header("Visual Feedback")]
    public Material defaultMaterial;
    public Material player1Material;
    public Material player2Material;

    private int squareIndex;
    private int row, col;
    private TicTacToeBoard parentBoard;
    private Renderer squareRenderer;

    // Regular variables (no longer networked)
    private int ownerPlayer = 0; // 0 = empty, 1 = player1, 2 = player2
    private bool hasBeenClaimed = false;

    void Awake()
    {
        squareRenderer = GetComponent<Renderer>();
        if (squareRenderer == null) squareRenderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        // Initialize square visual
        if (squareRenderer != null && defaultMaterial != null)
            squareRenderer.material = defaultMaterial;
    }

    public void Initialize(int index, int boardRow, int boardCol, TicTacToeBoard board)
    {
        squareIndex = index;
        row = boardRow;
        col = boardCol;
        parentBoard = board;

        if (squareRenderer != null && defaultMaterial != null)
            squareRenderer.material = defaultMaterial;
    }

    public void OnSquareHit(int player)
    {
        if (hasBeenClaimed) return;

        if (parentBoard != null)
        {
            if (parentBoard.ClaimSquare(squareIndex, player))
            {
                hasBeenClaimed = true;
            }
        }
        else
        {
            SetPlayer(player);
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnSquareClaimedServerRpc(squareIndex, player);
            hasBeenClaimed = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bird") && !hasBeenClaimed && TurnManager.Instance != null)
        {
            // Only the owner of the bird should trigger this
            SlingShotController birdController = other.GetComponent<SlingShotController>();
            if (birdController != null && birdController.IsOwner)
            {
                int currentPlayer = TurnManager.Instance.GetCurrentPlayer();
                OnSquareHit(currentPlayer);
            }
        }
    }

    public void SetPlayer(int player)
    {
        ownerPlayer = player;
        hasBeenClaimed = true;
        UpdateVisual(player);
    }

    public void ClearSquareInternal()
    {
        ownerPlayer = 0;
        hasBeenClaimed = false;
        UpdateVisual(0);
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
        ClearSquareInternal();

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

    public bool IsEmpty() { return ownerPlayer == 0; }
    public int GetOwner() { return ownerPlayer; }
    public int GetIndex() { return squareIndex; }

}
