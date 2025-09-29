// NetworkTicTacToeBoard.cs
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;

public class TicTacToeBoard : NetworkBehaviour
{
    public TicTacToeSquare[] squares = new TicTacToeSquare[9];
    public GameObject player1Symbol;
    public GameObject player2Symbol;

[Header("Board Layout")]
    public float squareSize = 2f;
    public float squareSpacing = 0.2f;

    private int[,] board = new int[3, 3];

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
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                board[i, j] = 0;

        foreach (var square in squares)
            if (square != null)
                square.ClearSquare();
    }

    public bool ClaimSquare(int squareIndex, int player)
    {
        if (squareIndex < 0 || squareIndex >= 9) return false;

        int row = squareIndex / 3;
        int col = squareIndex % 3;

        if (board[row, col] != 0) return false;

        board[row, col] = player;

        if (IsServer)
        {
            // Update square ownership on server and all clients
            if (squares[squareIndex] != null)
                squares[squareIndex].SetPlayer(player);
            UpdateSquareOwnerClientRpc(squareIndex, player);
            SpawnSymbolClientRpc(squareIndex, player);
        }

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnSquareClaimedServerRpc(squareIndex, player);

        return true;
    }

    [ClientRpc]
    void SpawnSymbolClientRpc(int squareIndex, int player)
    {
        GameObject symbolPrefab = (player == 1) ? player1Symbol : player2Symbol;
        if (symbolPrefab != null && squares[squareIndex] != null)
        {
            GameObject symbol = Instantiate(symbolPrefab, squares[squareIndex].transform);
            symbol.transform.localPosition = Vector3.up * 0.5f;
            symbol.transform.localRotation = Quaternion.identity;
            symbol.transform.localScale = Vector3.one * 0.8f;
        }
    }

    [ClientRpc]
    void UpdateSquareOwnerClientRpc(int squareIndex, int player)
    {
        if (squares != null && squareIndex >= 0 && squareIndex < squares.Length && squares[squareIndex] != null)
        {
            squares[squareIndex].SetPlayer(player);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestClaimSquareServerRpc(int squareIndex, int player)
    {
        ClaimSquare(squareIndex, player);
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