// NetworkTurnManager.cs - Fixed version with proper player assignment and turn logic
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class NetworkTurnManager : NetworkBehaviour
{
    [Header("Players")]
    public NetworkSlingShotController player1Slingshot;
    public NetworkSlingShotController player2Slingshot;
    public Camera player1Camera;
    public Camera player2Camera;

[Header("UI")]
    public Text turnIndicator;
    public GameObject gameOverPanel;
    public Text gameOverText;
    public Text connectionStatus;
    public Text playerRole;

    [Header("Game Settings")]
    public float turnSwitchDelay = 3f;

    // NetworkVariables with proper initialization
    private NetworkVariable<int> currentPlayer = new NetworkVariable<int>(1);
    private NetworkVariable<bool> gameActive = new NetworkVariable<bool>(true);
    private NetworkVariable<bool> waitingForBird = new NetworkVariable<bool>(false);

    private NetworkTicTacToeBoard gameBoard;
    private int myPlayerNumber = 0;
    private bool isInitialized = false;
    private bool gameStarted = false;

    public static NetworkTurnManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"NetworkTurnManager spawned - IsHost: {IsHost}, ClientId: {NetworkManager.Singleton.LocalClientId}");

        gameBoard = FindFirstObjectByType<NetworkTicTacToeBoard>();

        // Fixed player number assignment
        if (NetworkManager.Singleton.IsHost)
        {
            myPlayerNumber = 1;
            Debug.Log("I am Player 1 (Host)");
        }
        else
        {
            myPlayerNumber = 2;
            Debug.Log("I am Player 2 (Client)");
        }

        // Subscribe to network variable changes AFTER spawn
        currentPlayer.OnValueChanged += OnCurrentPlayerChanged;
        gameActive.OnValueChanged += OnGameActiveChanged;
        waitingForBird.OnValueChanged += OnWaitingForBirdChanged;

        // Initialize game state
        if (IsServer)
        {
            StartCoroutine(InitializeGameWhenReady());
        }
        else
        {
            // For clients, wait a bit then update UI
            StartCoroutine(ClientInitializationDelay());
        }

        isInitialized = true;
    }

    IEnumerator ClientInitializationDelay()
    {
        yield return new WaitForSeconds(1f);
        UpdateUI();
        SetPlayerTurn();
    }

    public override void OnNetworkDespawn()
    {
        if (currentPlayer != null) currentPlayer.OnValueChanged -= OnCurrentPlayerChanged;
        if (gameActive != null) gameActive.OnValueChanged -= OnGameActiveChanged;
        if (waitingForBird != null) waitingForBird.OnValueChanged -= OnWaitingForBirdChanged;

        isInitialized = false;
    }

    IEnumerator InitializeGameWhenReady()
    {
        // Wait for both players to connect
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Wait a bit more for all objects to spawn
        yield return new WaitForSeconds(1f);

        SetupGame();
    }

    void SetupGame()
    {
        if (!IsServer || !isInitialized) return;

        Debug.Log("Setting up game...");

        currentPlayer.Value = 1; // Player 1 (Host) starts
        gameActive.Value = true;
        waitingForBird.Value = false;
        gameStarted = true;

        SetPlayerTurn();
        UpdateUI();
    }

    public void StartGameFromLauncher()
    {
        if (!IsServer) return;

        Debug.Log("Game started from launcher");
        gameStarted = true;
        SetupGame();
    }

    void SetPlayerTurn()
    {
        if (!isInitialized || !gameStarted) return;

        Debug.Log($"Setting turn - Current Player: {currentPlayer.Value}, My Player: {myPlayerNumber}");

        // Enable/disable slingshots based on whose turn it is and ownership
        bool isMyTurn = currentPlayer.Value == myPlayerNumber;

        if (myPlayerNumber == 1 && player1Slingshot != null)
        {
            player1Slingshot.SetActive(isMyTurn);
            Debug.Log($"Player 1 slingshot active: {isMyTurn}");
        }

        if (myPlayerNumber == 2 && player2Slingshot != null)
        {
            player2Slingshot.SetActive(isMyTurn);
            Debug.Log($"Player 2 slingshot active: {isMyTurn}");
        }

        // Switch cameras based on current player (all clients see the same camera)
        if (player1Camera != null)
        {
            player1Camera.gameObject.SetActive(currentPlayer.Value == 1);
            Debug.Log($"Player 1 camera active: {currentPlayer.Value == 1}");
        }
        if (player2Camera != null)
        {
            player2Camera.gameObject.SetActive(currentPlayer.Value == 2);
            Debug.Log($"Player 2 camera active: {currentPlayer.Value == 2}");
        }
    }

    void UpdateUI()
    {
        if (!isInitialized) return;

        if (turnIndicator != null)
        {
            string turnText = $"Player {currentPlayer.Value}'s Turn";
            if (currentPlayer.Value == myPlayerNumber)
                turnText += " (Your Turn!)";
            turnIndicator.text = turnText;
        }

        if (playerRole != null)
            playerRole.text = $"You are Player {myPlayerNumber}";

        if (connectionStatus != null && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
                connectionStatus.text = "Host (Player 1)";
            else if (NetworkManager.Singleton.IsClient)
                connectionStatus.text = "Client (Player 2)";
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(!gameActive.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnBirdLaunchedServerRpc(ulong clientId)
    {
        if (!gameActive.Value || !IsServer)
        {
            Debug.Log("Ignoring bird launch - game not active or not server");
            return;
        }

        Debug.Log($"Bird launched by client {clientId}, current player: {currentPlayer.Value}");

        // Check if it's the correct player's turn
        bool isValidTurn = false;
        if (currentPlayer.Value == 1 && clientId == 0) // Host is Player 1, clientId 0
            isValidTurn = true;
        else if (currentPlayer.Value == 2 && clientId != 0) // Client is Player 2, clientId != 0
            isValidTurn = true;

        if (!isValidTurn)
        {
            Debug.Log($"Wrong player tried to launch! Current player: {currentPlayer.Value}, ClientId: {clientId}");
            return;
        }

        waitingForBird.Value = true;
        StartCoroutine(WaitForBirdToSettleCoroutine());
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnBirdHitGroundServerRpc(ulong clientId)
    {
        if (!gameActive.Value || !waitingForBird.Value || !IsServer) return;

        Debug.Log("Bird hit ground - ending turn immediately");
        StopAllCoroutines();
        waitingForBird.Value = false;

        ResetCurrentBirdClientRpc();

        if (gameActive.Value)
        {
            SwitchTurn();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnBirdHitBoardServerRpc(ulong clientId)
    {
        if (!gameActive.Value || !waitingForBird.Value || !IsServer) return;

        Debug.Log("Bird hit board - ending turn immediately");
        StopAllCoroutines();
        waitingForBird.Value = false;

        ResetCurrentBirdClientRpc();

        if (gameActive.Value)
        {
            SwitchTurn();
        }
    }

    [ClientRpc]
    void ResetCurrentBirdClientRpc()
    {
        NetworkSlingShotController currentSlingshot = (currentPlayer.Value == 1) ? player1Slingshot : player2Slingshot;
        if (currentSlingshot != null)
        {
            currentSlingshot.ResetBird();
        }
    }

    IEnumerator WaitForBirdToSettleCoroutine()
    {
        yield return new WaitForSeconds(1f);

        NetworkSlingShotController currentSlingshot = (currentPlayer.Value == 1) ? player1Slingshot : player2Slingshot;
        if (currentSlingshot == null)
        {
            waitingForBird.Value = false;
            yield break;
        }

        GameObject bird = currentSlingshot.GetBird();
        if (bird == null)
        {
            waitingForBird.Value = false;
            yield break;
        }

        Rigidbody birdRb = bird.GetComponent<Rigidbody>();
        if (birdRb == null)
        {
            waitingForBird.Value = false;
            yield break;
        }

        // Wait for bird to settle
        while (birdRb.linearVelocity.magnitude > 0.1f || birdRb.angularVelocity.magnitude > 0.1f)
        {
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(turnSwitchDelay);

        ResetCurrentBirdClientRpc();
        waitingForBird.Value = false;

        if (gameActive.Value)
        {
            SwitchTurn();
        }
    }

    void SwitchTurn()
    {
        if (!IsServer) return;

        currentPlayer.Value = (currentPlayer.Value == 1) ? 2 : 1;
        Debug.Log($"Switched to Player {currentPlayer.Value}'s turn");
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnSquareClaimedServerRpc(int squareIndex, int player)
    {
        if (!IsServer) return;

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
        if (!IsServer) return;

        gameActive.Value = false;
        waitingForBird.Value = false;
        EndGameClientRpc(winner);
    }

    [ClientRpc]
    void EndGameClientRpc(int winner)
    {
        if (player1Slingshot != null) player1Slingshot.SetActive(false);
        if (player2Slingshot != null) player2Slingshot.SetActive(false);

        if (gameOverPanel != null && gameOverText != null)
        {
            gameOverPanel.SetActive(true);
            if (winner == 0)
                gameOverText.text = "It's a Draw!";
            else
                gameOverText.text = $"Player {winner} Wins!";
        }
    }

    public void RestartGame()
    {
        if (!IsServer) return;

        RestartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void RestartGameServerRpc()
    {
        if (gameBoard != null)
            gameBoard.ResetBoard();

        RestartGameClientRpc();
        SetupGame();
    }

    [ClientRpc]
    void RestartGameClientRpc()
    {
        if (player1Slingshot != null) player1Slingshot.ResetBird();
        if (player2Slingshot != null) player2Slingshot.ResetBird();
    }

    // Public getters with safety checks
    public int GetCurrentPlayer()
    {
        return isInitialized ? currentPlayer.Value : 1;
    }

    public int GetMyPlayerNumber()
    {
        return myPlayerNumber;
    }

    public bool IsMyTurn()
    {
        bool result = isInitialized && gameStarted && currentPlayer.Value == myPlayerNumber;
        Debug.Log($"IsMyTurn check - Initialized: {isInitialized}, GameStarted: {gameStarted}, CurrentPlayer: {currentPlayer.Value}, MyPlayer: {myPlayerNumber}, Result: {result}");
        return result;
    }

    public bool IsGameActive()
    {
        return isInitialized && gameActive.Value && gameStarted;
    }

    // Network variable change callbacks
    void OnCurrentPlayerChanged(int previousValue, int newValue)
    {
        Debug.Log($"Player turn changed from {previousValue} to {newValue}");
        SetPlayerTurn();
        UpdateUI();
    }

    void OnGameActiveChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"Game active changed to {newValue}");
        UpdateUI();
    }

    void OnWaitingForBirdChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"Waiting for bird changed to {newValue}");
    }

}