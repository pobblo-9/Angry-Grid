// NetworkGameLauncher.cs - Fixed version with proper null checks and disconnect handling
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

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

    [Header("Scenes")]
    [Tooltip("Optional: name of your Title scene to return to")] public string titleSceneName = "";

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
        if (gameplayObjects != null) gameplayObjects.SetActive(true);
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

        // Notify TurnManager immediately on server
        if (IsServer && TurnManager.Instance != null)
        {
            TurnManager.Instance.StartGameFromLauncher();
        }
    }
    
    void SpawnGameNetworkObjects()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        Debug.Log("Spawning network game objects...");
        
        // Spawn ALL NetworkObjects that aren't already spawned (include inactive so gameplay objects hidden in UI are found)
        NetworkObject[] allNetworkObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"Total NetworkObjects in scene (incl. inactive): {allNetworkObjects.Length}");
        
        for (int i = 0; i < allNetworkObjects.Length; i++)
        {
            NetworkObject netObj = allNetworkObjects[i];
            Debug.Log($"NetworkObject {i}: {netObj.gameObject.name}, Active: {netObj.gameObject.activeInHierarchy}, IsSpawned: {netObj.IsSpawned}");
            
            if (!netObj.IsSpawned)
            {
                try
                {
                    if (!netObj.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"Activating {netObj.gameObject.name} to allow network spawn");
                        netObj.gameObject.SetActive(true);
                    }
                    Debug.Log($"Spawning NetworkObject: {netObj.gameObject.name}");
                    // For scene objects, regular Spawn is fine once active
                    netObj.Spawn(destroyWithScene: true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not spawn {netObj.gameObject.name}: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"NetworkObject {netObj.gameObject.name} already spawned");
            }
        }
        
        // Find and spawn TurnManager if not already spawned
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null)
        {
            NetworkObject turnManagerNetObj = turnManager.GetComponent<NetworkObject>();
            if (turnManagerNetObj != null && !turnManagerNetObj.IsSpawned)
            {
                if (!turnManagerNetObj.gameObject.activeInHierarchy)
                    turnManagerNetObj.gameObject.SetActive(true);
                Debug.Log("Spawning TurnManager");
                turnManagerNetObj.Spawn();
            }
        }
        
        // Find and spawn all SlingShotControllers (include inactive so hidden ones are found)
        Debug.Log("Looking for SlingShotControllers in scene...");
        SlingShotController[] slingshots = FindObjectsByType<SlingShotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"Found {slingshots.Length} SlingShotController(s) in scene (incl. inactive)");
        
        // If no SlingShotControllers found, let's search more broadly (including inactive)
        if (slingshots.Length == 0)
        {
            Debug.Log("No SlingShotControllers found! Searching for any GameObject with that script...");
            
            // Search all GameObjects for SlingShotController scripts
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int foundScripts = 0;
            foreach (GameObject obj in allObjects)
            {
                SlingShotController script = obj.GetComponent<SlingShotController>();
                if (script != null)
                {
                    foundScripts++;
                    Debug.Log($"Found SlingShotController script on: {obj.name}, Player: {script.GetPlayerNumber()}, HasNetworkObject: {obj.GetComponent<NetworkObject>() != null}");
                }
            }
            Debug.Log($"Total SlingShotController scripts found: {foundScripts}");
        }
        
        for (int i = 0; i < slingshots.Length; i++)
        {
            SlingShotController slingshot = slingshots[i];
            Debug.Log($"Checking SlingShotController {i}: {slingshot.name}, Player: {slingshot.GetPlayerNumber()}");
            
            NetworkObject slingshotNetObj = slingshot.GetComponent<NetworkObject>();
            if (slingshotNetObj != null)
            {
                Debug.Log($"SlingShotController {slingshot.name} has NetworkObject, Active: {slingshotNetObj.gameObject.activeInHierarchy}, IsSpawned: {slingshotNetObj.IsSpawned}");
                if (!slingshotNetObj.IsSpawned)
                {
                    Debug.Log($"Attempting to spawn SlingShotController for Player {slingshot.GetPlayerNumber()}");
                    try
                    {
                        if (!slingshotNetObj.gameObject.activeInHierarchy)
                        {
                            Debug.Log($"Activating slingshot {slingshot.name} to allow network spawn");
                            slingshotNetObj.gameObject.SetActive(true);
                        }
                        slingshotNetObj.Spawn();
                        Debug.Log($"Successfully spawned SlingShotController for Player {slingshot.GetPlayerNumber()}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to spawn SlingShotController: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log($"SlingShotController {slingshot.name} already spawned");
                }
            }
            else
            {
                Debug.LogWarning($"SlingShotController {slingshot.name} has no NetworkObject component!");
            }
        }
        
        // Find and spawn TicTacToeBoard (include inactive)
        TicTacToeBoard board = null;
        var boards = FindObjectsByType<TicTacToeBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (boards != null && boards.Length > 0)
            board = boards[0];
        if (board != null)
        {
            NetworkObject boardNetObj = board.GetComponent<NetworkObject>();
            if (boardNetObj != null && !boardNetObj.IsSpawned)
            {
                if (!boardNetObj.gameObject.activeInHierarchy)
                    boardNetObj.gameObject.SetActive(true);
                Debug.Log("Spawning TicTacToeBoard");
                boardNetObj.Spawn();
            }
        }
        
        // Find and spawn all TicTacToeSquares (include inactive, if any are networked)
        TicTacToeSquare[] squares = FindObjectsByType<TicTacToeSquare>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TicTacToeSquare square in squares)
        {
            NetworkObject squareNetObj = square.GetComponent<NetworkObject>();
            if (squareNetObj != null && !squareNetObj.IsSpawned)
            {
                if (!squareNetObj.gameObject.activeInHierarchy)
                    squareNetObj.gameObject.SetActive(true);
                Debug.Log($"Spawning TicTacToeSquare: {square.name}");
                squareNetObj.Spawn();
            }
        }
        
        // Wait a frame then notify TurnManager to start
        StartCoroutine(NotifyTurnManagerAfterSpawn());
    }
    
    System.Collections.IEnumerator NotifyTurnManagerAfterSpawn()
    {
        yield return null; // Wait one frame for spawning to complete
        
        // Notify the TurnManager to start the game
        if (TurnManager.Instance != null)
        {
            Debug.Log("Notifying TurnManager to start game");
            TurnManager.Instance.StartGameFromLauncher();
        }
        else
        {
            Debug.LogWarning("TurnManager.Instance is null after spawning!");
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

        // If a title scene name is provided and menu is not present, allow loading it
        if (!string.IsNullOrEmpty(titleSceneName) && menuPanel == null)
        {
            SceneManager.LoadScene(titleSceneName);
            yield break;
        }

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

    // Disconnect (if connected) and then load title scene if provided
    public void DisconnectAndLoadTitle()
    {
        StartCoroutine(DisconnectThenLoadTitleCoroutine());
    }

    IEnumerator DisconnectThenLoadTitleCoroutine()
    {
        Debug.Log("Disconnecting and loading title scene...");
        gameStarted = false;
        isConnecting = false;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        // Give time for shutdown
        yield return new WaitForSeconds(0.5f);

        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
        else
        {
            // Fallback to showing local menu in current scene
            if (menuPanel != null) menuPanel.SetActive(true);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (gameplayObjects != null) gameplayObjects.SetActive(false);
            if (startGameButton != null) startGameButton.gameObject.SetActive(false);
            if (disconnectButton != null) disconnectButton.gameObject.SetActive(false);
            if (hostButton != null) hostButton.interactable = true;
            if (joinButton != null) joinButton.interactable = true;
            UpdateStatusText("Disconnected. Ready to connect...");
        }
    }

}
