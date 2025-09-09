using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    [Header("Players")]
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
    private bool waitingForBird = false; // Track if we're waiting for a bird to settle

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
        waitingForBird = false;

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
        if (player1Slingshot != null) player1Slingshot.SetActive(player == 1);
        if (player2Slingshot != null) player2Slingshot.SetActive(player == 2);

        // Switch cameras
        if (player1Camera != null) player1Camera.gameObject.SetActive(player == 1);
        if (player2Camera != null) player2Camera.gameObject.SetActive(player == 2);

        // Update UI
        if (turnIndicator != null)
            turnIndicator.text = "Player " + player + "'s Turn";
    }

    public void OnBirdLaunched()
    {
        if (!gameActive) return;

        waitingForBird = true;
        // Start monitoring for when the bird stops moving
        StartCoroutine(WaitForBirdToSettle());
    }

    // Called when bird hits ground without hitting board
    public void OnBirdHitGround()
    {
        if (!gameActive || !waitingForBird) return;

        Debug.Log("Bird hit ground - ending turn immediately");

        // Stop waiting and immediately switch turns
        StopAllCoroutines();
        waitingForBird = false;

        // Reset the bird and switch turns
        SlingShotController currentSlingshot = (currentPlayer == 1) ? player1Slingshot : player2Slingshot;
        if (currentSlingshot != null)
        {
            StartCoroutine(ResetBirdAfterDelay(currentSlingshot, 1f));
        }
    }

    System.Collections.IEnumerator WaitForBirdToSettle()
    {
        SlingShotController currentSlingshot = (currentPlayer == 1) ? player1Slingshot : player2Slingshot;

        if (currentSlingshot == null)
        {
            Debug.LogError("Current slingshot is null!");
            waitingForBird = false;
            yield break;
        }

        // Get the bird's Rigidbody (the slingshot script is on the bird)
        GameObject bird = currentSlingshot.GetBird();
        if (bird == null)
        {
            Debug.LogError("Bird is null!");
            waitingForBird = false;
            yield break;
        }

        Rigidbody birdRb = bird.GetComponent<Rigidbody>();
        if (birdRb == null)
        {
            Debug.LogError("Bird has no Rigidbody!");
            waitingForBird = false;
            yield break;
        }

        // Wait for bird to stop moving
        yield return new WaitForSeconds(1f); // Initial delay

        // Wait until bird velocity is low enough
        while (birdRb.linearVelocity.magnitude > 0.1f || birdRb.angularVelocity.magnitude > 0.1f)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Additional delay before switching turns
        yield return new WaitForSeconds(turnSwitchDelay);

        // Reset bird position
        currentSlingshot.ResetBird();
        waitingForBird = false;

        // Switch to next player
        if (gameActive)
        {
            SwitchTurn();
        }
    }

    // Coroutine to reset bird after a short delay (for visual feedback)
    System.Collections.IEnumerator ResetBirdAfterDelay(SlingShotController slingshot, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (slingshot != null)
        {
            slingshot.ResetBird();
        }

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
        if (gameBoard != null && gameBoard.CheckWin(player))
        {
            EndGame(player);
        }
        else if (gameBoard != null && gameBoard.IsBoardFull())
        {
            EndGame(0); // Draw
        }
    }

    void EndGame(int winner)
    {
        gameActive = false;
        waitingForBird = false;

        // Disable both slingshots
        if (player1Slingshot != null) player1Slingshot.SetActive(false);
        if (player2Slingshot != null) player2Slingshot.SetActive(false);

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
        if (gameBoard != null)
            gameBoard.ResetBoard();

        // Reset both birds
        if (player1Slingshot != null) player1Slingshot.ResetBird();
        if (player2Slingshot != null) player2Slingshot.ResetBird();

        // Restart game
        SetupGame();
    }

    public int GetCurrentPlayer()
    {
        return currentPlayer;
    }
}