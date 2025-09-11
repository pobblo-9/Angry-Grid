using UnityEngine;

public class TicTacToeBoard : MonoBehaviour
{
    [Header("Board Setup")]
    public TicTacToeSquare[] squares = new TicTacToeSquare[9]; // 3x3 grid
    public GameObject player1Symbol; // X prefab
    public GameObject player2Symbol; // O prefab

    [Header("Board Layout")]
    public float squareSize = 2f;
    public float squareSpacing = 0.2f;

    private int[,] board = new int[3, 3]; // 0 = empty, 1 = player1, 2 = player2

    void Start() => SetupBoard();

    void SetupBoard()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int index = row * 3 + col;
                if (squares[index] != null)
                {
                    Vector3 position = new Vector3(
                    col * (squareSize + squareSpacing) - squareSize,
                    0,
                    row * (squareSize + squareSpacing) - squareSize
                    );
                    squares[index].transform.position = transform.position + position;
                    squares[index].Initialize(index, row, col, this);
                }
            }
        }
        ResetBoard();
    }

    public void ResetBoard()
    {
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) board[i, j] = 0;
        foreach (var square in squares) if (square != null) square.ClearSquare();
    }

    public bool ClaimSquare(int squareIndex, int player)
    {
        if (squareIndex < 0 || squareIndex >= 9) return false;

        int row = squareIndex / 3;
        int col = squareIndex % 3;

        if (board[row, col] != 0) return false;

        board[row, col] = player;
        squares[squareIndex].SetPlayer(player);
        SpawnSymbol(squareIndex, player);

        if (TurnManager.Instance != null) TurnManager.Instance.OnSquareClaimed(squareIndex, player);
        return true;
    }

    void SpawnSymbol(int squareIndex, int player)
    {
        GameObject symbolPrefab = (player == 1) ? player1Symbol : player2Symbol;
        if (symbolPrefab != null && squares[squareIndex] != null)
        {
            GameObject symbol = Instantiate(symbolPrefab, squares[squareIndex].transform);
            symbol.transform.localPosition = Vector3.up * 0.5f; // above the square
            symbol.transform.localRotation = Quaternion.identity;
            symbol.transform.localScale = Vector3.one * 0.8f;
        }
    }

    public bool CheckWin(int player)
    {
        for (int row = 0; row < 3; row++)
            if (board[row, 0] == player && board[row, 1] == player && board[row, 2] == player) return true;

        for (int col = 0; col < 3; col++)
            if (board[0, col] == player && board[1, col] == player && board[2, col] == player) return true;

        if (board[0, 0] == player && board[1, 1] == player && board[2, 2] == player) return true;
        if (board[0, 2] == player && board[1, 1] == player && board[2, 0] == player) return true;

        return false;
    }

    public bool IsBoardFull()
    {
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
                if (board[row, col] == 0) return false;
        return true;
    }

    public int[,] GetBoard() => board;
}