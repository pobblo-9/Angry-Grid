using UnityEngine;

public class TicTacToeSquare : MonoBehaviour
{
    [Header("Visual Feedback")]
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
        if (squareRenderer == null) squareRenderer = GetComponentInChildren<Renderer>();
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

    // Called by the SlingShotController when the bird hits this square
    public void OnSquareHit(int player)
    {
        if (hasBeenClaimed) return;

        // Prefer using the board's ClaimSquare method (it handles validation, visuals, and notifications)
        if (parentBoard != null)
        {
            if (parentBoard.ClaimSquare(squareIndex, player))
            {
                hasBeenClaimed = true;
            }
        }
        else
        {
            // fallback if parentBoard wasn't assigned
            SetPlayer(player);
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnSquareClaimed(squareIndex, player);
            hasBeenClaimed = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bird") && !hasBeenClaimed && TurnManager.Instance != null)
        {
            int currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            OnSquareHit(currentPlayer);
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
                Destroy(child.gameObject);
            }
        }
    }

    public bool IsEmpty() { return ownerPlayer == 0; }
    public int GetOwner() { return ownerPlayer; }
}