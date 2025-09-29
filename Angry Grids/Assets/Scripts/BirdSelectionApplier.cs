using UnityEngine;

public class BirdSelectionApplier : MonoBehaviour
{
    [Header("References")]
    public BirdSelectionManager selection;
    public TurnManager turnManager;

    [Header("Behavior")]
    public bool applyOnStart = true;
    public bool applyToBothPlayers = true;

    void Start()
    {
        if (applyOnStart)
        {
            ApplySelection();
        }
    }

    public void ApplySelection()
    {
        if (selection == null)
        {
            Debug.LogWarning("BirdSelectionApplier: No BirdSelectionManager assigned.");
            return;
        }

        if (turnManager == null)
        {
            turnManager = TurnManager.Instance;
        }

        if (turnManager == null)
        {
            DebugErrorMissingTurnManager();
            return;
        }

        if (applyToBothPlayers)
        {
            ApplyToPlayer(1);
            ApplyToPlayer(2);
        }
        else
        {
            ApplyToPlayer(turnManager.GetCurrentPlayer());
        }
    }

    public void ApplyToPlayer(int player)
    {
        if (selection == null) return;
        GameObject prefab = selection.GetSelectedPrefabForPlayer(player);
        if (prefab == null)
        {
            Debug.LogWarning($"BirdSelectionApplier: Selected prefab for Player {player} is null.");
            return;
        }

        SlingShotController oldCtrl = (player == 1) ? turnManager.player1Slingshot : turnManager.player2Slingshot;
        if (oldCtrl == null)
        {
            Debug.LogWarning($"BirdSelectionApplier: Player {player} slingshot is not assigned.");
            return;
        }

        // If mid-aim or launched, cancel/reset before swapping
        oldCtrl.ResetBird();

        Vector3 pos = oldCtrl.transform.position;
        Quaternion rot = oldCtrl.transform.rotation;

        GameObject newObj = Instantiate(prefab, pos, rot);
        SlingShotController newCtrl = newObj.GetComponent<SlingShotController>();
        if (newCtrl == null)
        {
            newCtrl = newObj.AddComponent<SlingShotController>();
            Debug.LogWarning("Selected bird prefab had no SlingShotController; one was added automatically.");
        }

        // Copy slingshot wiring and tuning values
        newCtrl.leftPost = oldCtrl.leftPost;
        newCtrl.rightPost = oldCtrl.rightPost;
        newCtrl.leftBand = oldCtrl.leftBand;
        newCtrl.rightBand = oldCtrl.rightBand;
        newCtrl.trajectoryLine = oldCtrl.trajectoryLine;

        newCtrl.forceMultiplier = oldCtrl.forceMultiplier;
        newCtrl.maxStretch = oldCtrl.maxStretch;
        newCtrl.minLaunch = oldCtrl.minLaunch;
        newCtrl.trajectoryPoints = oldCtrl.trajectoryPoints;
        newCtrl.timeStep = oldCtrl.timeStep;

        // Update TurnManager reference
        if (player == 1) turnManager.player1Slingshot = newCtrl; else turnManager.player2Slingshot = newCtrl;

        // Update camera follow to track the new bird
        Camera playerCam = (player == 1) ? turnManager.player1Camera : turnManager.player2Camera;
        if (playerCam != null)
        {
            CameraFollowBird follow = playerCam.GetComponent<CameraFollowBird>();
            if (follow != null)
            {
                follow.SetBird(newObj.transform);
            }
        }

        // Remove old bird
        Destroy(oldCtrl.gameObject);

        Debug.Log($"BirdSelectionApplier: Applied selected bird to Player {player}.");
    }

    private void DebugErrorMissingTurnManager()
    {
        Debug.LogError("BirdSelectionApplier: TurnManager not found.");
    }
}
