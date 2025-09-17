// NetworkGameLauncher.cs - Fixed version with proper null checks and disconnect handling
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class NetworkGameLauncher : NetworkBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button joinButton;
    public Button startGameButton;
    public Button disconnectButton; // Add this if you have a disconnect button
    public Text statusText;
    public GameObject menuPanel;
    public GameObject lobbyPanel;
    public Text playersConnectedText;

[Header("Game Objects")]
    public GameObject gameplayObjects;

    private bool gameStarted = false;
    private bool isConnecting = false;

    void Start()
    {
        // Setup button listeners with null checks
        if (hostButton != null) hostButton.onClick.AddListener(StartHost);
        if (joinButton != null) joinButton.onClick.AddListener(StartClient);
        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(DisconnectAndReturnToMenu);

        // Initialize UI
        if (menuPanel != null) menuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (gameplayObjects != null) gameplayObjects.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(false);

        UpdateStatusText("Ready to connect...");
    }

    void Update()
    {
        // Add null check for NetworkManager
        if (NetworkManager.Singleton == null) return;

        // Update connection status
        if (NetworkManager.Singleton.IsListening)
        {
            UpdatePlayersConnectedText();

            // Show disconnect button when connected
            if (disconnectButton != null)
                disconnectButton.gameObject.SetActive(true);

            // Show start button only for host when 2 players connected
            if (NetworkManager.Singleton.IsHost && startGameButton != null && !gameStarted)
            {
                bool canStart = NetworkManager.Singleton.ConnectedClientsList.Count >= 2;
                startGameButton.gameObject.SetActive(canStart);
            }

            // Auto-show game for clients when game starts
            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost && gameStarted)
            {
                ShowGameplay();
            }
        }
        else
        {
            // Hide disconnect button when not connected
            if (disconnectButton != null)
                disconnectButton.gameObject.SetActive(false);
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            UpdateStatusText("NetworkManager not found!");
            return;
        }

        if (isConnecting) return;
        isConnecting = true;

        // Disable buttons to prevent double-clicking
        if (hostButton != null) hostButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;

        StartCoroutine(StartHostCoroutine());
    }

    IEnumerator StartHostCoroutine()
    {
        UpdateStatusText("Starting host...");

        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
            // Wait a frame for network to initialize
            yield return null;

            UpdateStatusText("Hosting game... Waiting for players");
            ShowLobby();

            // Subscribe to connection events with null checks
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
        else
        {
            UpdateStatusText("Failed to start host!");
            // Re-enable buttons
            if (hostButton != null) hostButton.interactable = true;
            if (joinButton != null) joinButton.interactable = true;
        }

        isConnecting = false;
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            UpdateStatusText("NetworkManager not found!");
            return;
        }

        if (isConnecting) return;
        isConnecting = true;

        // Disable buttons to prevent double-clicking
        if (hostButton != null) hostButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;

        StartCoroutine(StartClientCoroutine());
    }

    IEnumerator StartClientCoroutine()
    {
        UpdateStatusText("Connecting to host...");

        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            // Wait for connection to establish
            float timeoutTimer = 0f;
            const float connectionTimeout = 10f;

            while (!NetworkManager.Singleton.IsConnectedClient && timeoutTimer < connectionTimeout)
            {
                timeoutTimer += Time.deltaTime;
                yield return null;
            }

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                UpdateStatusText("Connected to host!");
                ShowLobby();

                // Subscribe to connection events with null checks
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                }
            }
            else
            {
                UpdateStatusText("Connection timeout!");
                // Re-enable buttons
                if (hostButton != null) hostButton.interactable = true;
                if (joinButton != null) joinButton.interactable = true;
            }
        }
        else
        {
            UpdateStatusText("Failed to connect!");
            // Re-enable buttons
            if (hostButton != null) hostButton.interactable = true;
            if (joinButton != null) joinButton.interactable = true;
        }

        isConnecting = false;
    }

    public void StartGame()
    {
        if (NetworkManager.Singleton == null)
        {
            UpdateStatusText("NetworkManager not available!");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            UpdateStatusText("Only host can start the game!");
            return;
        }

        if (gameStarted)
        {
            UpdateStatusText("Game already started!");
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
        {
            UpdateStatusText("Need 2 players to start!");
            return;
        }

        gameStarted = true;
        UpdateStatusText("Starting game...");

        // Notify the NetworkTurnManager to start the game
        if (NetworkTurnManager.Instance != null)
        {
            NetworkTurnManager.Instance.StartGameFromLauncher();
        }

        // Start the game for all clients
        if (IsSpawned)
        {
            StartGameClientRpc();
        }
        else
        {
            // If not spawned as NetworkBehaviour, handle locally
            ShowGameplay();
        }
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        gameStarted = true;
        ShowGameplay();
        UpdateStatusText("Game started!");
    }

    void ShowLobby()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (gameplayObjects != null) gameplayObjects.SetActive(false);
    }

    void ShowGameplay()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (gameplayObjects != null) gameplayObjects.SetActive(true);
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");

        if (NetworkManager.Singleton != null)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            UpdateStatusText($"Player {clientId} joined! ({playerCount}/2 players)");
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected!");
        UpdateStatusText($"Player {clientId} left the game");

        // Reset game state when someone disconnects
        gameStarted = false;

        // If we're still connected, go back to lobby
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            ShowLobby();
        }
    }

    void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"Status: {message}");
    }

    void UpdatePlayersConnectedText()
    {
        if (playersConnectedText != null && NetworkManager.Singleton != null)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
            playersConnectedText.text = $"Players Connected: {playerCount}/2";
        }
    }

    void OnDestroy()
    {
        // Cleanup event subscriptions with null checks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void DisconnectAndReturnToMenu()
    {
        Debug.Log("Disconnecting and returning to menu...");

        gameStarted = false;
        isConnecting = false;

        // Unsubscribe from events before shutdown
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            // Proper shutdown based on role
            if (NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    Debug.Log("Shutting down as host...");
                    NetworkManager.Singleton.Shutdown();
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("Disconnecting as client...");
                    NetworkManager.Singleton.Shutdown();
                }
            }
        }

        // Wait a frame before resetting UI to ensure network cleanup
        StartCoroutine(ResetUIAfterDisconnect());
    }

    IEnumerator ResetUIAfterDisconnect()
    {
        yield return new WaitForSeconds(0.5f); // Give time for network cleanup

        // Reset UI
        if (menuPanel != null) menuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (gameplayObjects != null) gameplayObjects.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        if (disconnectButton != null) disconnectButton.gameObject.SetActive(false);

        // Re-enable connection buttons
        if (hostButton != null) hostButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;

        UpdateStatusText("Disconnected. Ready to connect...");
    }

}