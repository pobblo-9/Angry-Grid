using UnityEngine;
using System.Collections;
public class TicTacToeSquare : MonoBehaviour
{
    public Material defaultMaterial;
    public Material player1Material; // Red material
    public Material player2Material; // Blue material

private int squareIndex;
    private int row, col;
    private TicTacToeBoard parentBoard;
    private Renderer squareRenderer;
    private int ownerPlayer = 0; // 0 = empty, 1 = player1, 2 = player2
    private bool hasBeenClaimed = false;

    void Awake()
    {
        squareRenderer = GetComponent<Renderer>();
        if (squareRenderer == null)
            squareRenderer = GetComponentInChildren<Renderer>();
    }

    public void Initialize(int index, int boardRow, int boardCol, TicTacToeBoard board)
    {
        squareIndex = index;
        row = boardRow;
        col = boardCol;
        parentBoard = board;

        // Set default material
        if (squareRenderer != null && defaultMaterial != null)
            squareRenderer.material = defaultMaterial;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if a bird (projectile) entered this square
        if (other.CompareTag("Bird") && !hasBeenClaimed)
        {
            // Get current player from turn manager
            int currentPlayer = TurnManager.Instance.GetCurrentPlayer();

            // Try to claim this square
            if (parentBoard.ClaimSquare(squareIndex, currentPlayer))
            {
                Debug.Log($"Player {currentPlayer} claimed square {squareIndex}");
            }
        }
    }

    public void SetPlayer(int player)
    {
        ownerPlayer = player;
        hasBeenClaimed = true;

        // Update visual
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
        ownerPlayer = 0;
        hasBeenClaimed = false;

        // Reset visual
        if (squareRenderer != null && defaultMaterial != null)
            squareRenderer.material = defaultMaterial;

        // Destroy any symbol children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Symbol") || child.name.Contains("X") || child.name.Contains("O"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    public bool IsEmpty()
    {
        return ownerPlayer == 0;
    }

    public int GetOwner()
    {
        return ownerPlayer;
    }
}