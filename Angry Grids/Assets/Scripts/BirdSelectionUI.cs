using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BirdSelectionUI : MonoBehaviour
{
    [Header("References")]
    public BirdSelectionManager selectionManager;
    public BirdSelectionApplier applier;

    [Header("UI Elements")]
    public Dropdown player1Dropdown;
    public Dropdown player2Dropdown;
    public Button applyBothButton;
    public Button applyP1Button;
    public Button applyP2Button;

    void Start()
    {
        if (selectionManager == null)
        {
            Debug.LogWarning("BirdSelectionUI: No BirdSelectionManager assigned.");
            return;
        }

        PopulateDropdown(player1Dropdown);
        PopulateDropdown(player2Dropdown);

        // Initialize dropdown values to current selections
        if (player1Dropdown != null) player1Dropdown.value = Mathf.Clamp(selectionManager.selectedIndexP1, 0, GetCount()-1);
        if (player2Dropdown != null) player2Dropdown.value = Mathf.Clamp(selectionManager.selectedIndexP2, 0, GetCount()-1);

        // Wire value change events
        if (player1Dropdown != null)
            player1Dropdown.onValueChanged.AddListener((value) => selectionManager.SelectIndexForPlayer(1, value));

        if (player2Dropdown != null)
            player2Dropdown.onValueChanged.AddListener((value) => selectionManager.SelectIndexForPlayer(2, value));

        // Wire buttons
        if (applyBothButton != null)
            applyBothButton.onClick.AddListener(() => { if (applier != null) { applier.applyToBothPlayers = true; applier.ApplySelection(); } });

        if (applyP1Button != null)
            applyP1Button.onClick.AddListener(() => { if (applier != null) { applier.applyToBothPlayers = false; applier.ApplyToPlayer(1); } });

        if (applyP2Button != null)
            applyP2Button.onClick.AddListener(() => { if (applier != null) { applier.applyToBothPlayers = false; applier.ApplyToPlayer(2); } });
    }

    void PopulateDropdown(Dropdown dropdown)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        int count = GetCount();
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = selectionManager.birdPrefabs[i];
            options.Add(prefab != null ? prefab.name : $"Bird {i}");
        }

        dropdown.AddOptions(options);
    }

    int GetCount()
    {
        return (selectionManager.birdPrefabs != null) ? selectionManager.birdPrefabs.Length : 0;
    }
}