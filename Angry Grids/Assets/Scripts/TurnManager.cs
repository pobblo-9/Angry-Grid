using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [Header(“Players”)]
    public SlingShotController player1Slingshot;
    public SlingShotController player2Slingshot;
    public Camera player1Camera;
    public Camera player2Camera;

[Header("UI")]
    public Text turnIndicator;
    public GameObject gameOverPanel;
    public Text gameOverText;

    [Header("Game Settings")]
    public float turnSwitchDelay = 3f; // Time to wait after bird stops moving

    private int currentPlayer = 1; // 1 or 2
    private TicTacToeBoard gameBoard;
    private bool gameActive = true;

    public static TurnManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gameBoard = FindFirstObjectByType<TicTacToeBoard>();
        SetupGame();
    }

    void SetupGame()
    {
        currentPlayer = 1;
        gameActive = true;

        // Set initial turn
        SetPlayerTurn(currentPlayer);

        // Setup UI
        if (turnIndicator != null)
            turnIndicator.text = "Player " + currentPlayer + "'s Turn";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void SetPlayerTurn(int player)
    {
        // Enable/disable slingshots
        player1Slingshot.SetActive(player == 1);
        player2Slingshot.SetActive(player == 2);

        // Switch cameras
        player1Camera.gameObject.SetActive(player == 1);
        player2Camera.gameObject.SetActive(player == 2);

        // Update UI
        if (turnIndicator != null)
            turnIndicator.text = "Player " + player + "'s Turn";
    }

    public void OnBirdLaunched()
    {
        if (!gameActive) return;

        // Start monitoring for when the bird stops moving
        StartCoroutine(WaitForBirdToSettle());
    }

    System.Collections.IEnumerator WaitForBirdToSettle()
    {
        SlingShotController currentSlingshot = (currentPlayer == 1) ? player1Slingshot : player2Slingshot;
        Rigidbody birdRb = currentSlingshot.GetComponent<Rigidbody>();

        // Wait for bird to stop moving
        yield return new WaitForSeconds(1f); // Initial delay

        while (birdRb.linearVelocity.magnitude > 0.1f)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Additional delay before switching turns
        yield return new WaitForSeconds(turnSwitchDelay);

        // Reset bird position
        currentSlingshot.ResetBird();

        // Switch to next player
        if (gameActive)
        {
            SwitchTurn();
        }
    }

    void SwitchTurn()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        SetPlayerTurn(currentPlayer);
    }

    public void OnSquareClaimed(int squareIndex, int player)
    {
        // Check for win condition
        if (gameBoard.CheckWin(player))
        {
            EndGame(player);
        }
        else if (gameBoard.IsBoardFull())
        {
            EndGame(0); // Draw
        }
    }

    void EndGame(int winner)
    {
        gameActive = false;

        // Disable both slingshots
        player1Slingshot.SetActive(false);
        player2Slingshot.SetActive(false);

        // Show game over UI
        if (gameOverPanel != null && gameOverText != null)
        {
            gameOverPanel.SetActive(true);

            if (winner == 0)
                gameOverText.text = "It's a Draw!";
            else
                gameOverText.text = "Player " + winner + " Wins!";
        }
    }

    public void RestartGame()
    {
        // Reset the board
        gameBoard.ResetBoard();

        // Reset both birds
        player1Slingshot.ResetBird();
        player2Slingshot.ResetBird();

        // Restart game
        SetupGame();
    }

    public int GetCurrentPlayer()
    {
        return currentPlayer;
    }
}