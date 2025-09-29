using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BirdSelectionTMPUI : MonoBehaviour
{
    [Header("References")]
    public BirdSelectionManager selectionManager;
    public BirdSelectionApplier applier;

    [Header("TMP Dropdowns")]
    public TMP_Dropdown player1Dropdown;
    public TMP_Dropdown player2Dropdown;

    [Header("Player 1 UI")] 
    public TMP_Text p1NameText;
    public TMP_Text p1StatsText;
    public Image p1IconImage;
    public BirdPreviewController p1Preview;

    [Header("Player 2 UI")] 
    public TMP_Text p2NameText;
    public TMP_Text p2StatsText;
    public Image p2IconImage;
    public BirdPreviewController p2Preview;

    [Header("Buttons")]
    public Button applyBothButton;
    public Button applyP1Button;
    public Button applyP2Button;

    void Start()
    {
        if (selectionManager == null)
        {
            Debug.LogWarning("BirdSelectionTMPUI: No BirdSelectionManager assigned.");
            return;
        }

        PopulateDropdown(player1Dropdown);
        PopulateDropdown(player2Dropdown);

        // Initialize dropdown values
        if (player1Dropdown != null) player1Dropdown.value = ClampIndex(selectionManager.selectedIndexP1);
        if (player2Dropdown != null) player2Dropdown.value = ClampIndex(selectionManager.selectedIndexP2);

        // Wire dropdown changes
        if (player1Dropdown != null)
        {
            player1Dropdown.onValueChanged.AddListener((val) =>
            {
                selectionManager.SelectIndexForPlayer(1, val);
                UpdatePlayerUI(1);
            });
        }

        if (player2Dropdown != null)
        {
            player2Dropdown.onValueChanged.AddListener((val) =>
            {
                selectionManager.SelectIndexForPlayer(2, val);
                UpdatePlayerUI(2);
            });
        }

        // Buttons
        if (applyBothButton != null)
            applyBothButton.onClick.AddListener(() => { if (applier != null) { applier.applyToBothPlayers = true; applier.ApplySelection(); } });

        if (applyP1Button != null)
            applyP1Button.onClick.AddListener(() => { if (applier != null) { applier.applyToBothPlayers = false; applier.ApplyToPlayer(1); } });

        if (applyP2Button != null)
            applyP2Button.onClick.AddListener(() => { if (applier != null) { applier.applyToBothPlayers = false; applier.ApplyToPlayer(2); } });

        // Initial UI refresh
        UpdatePlayerUI(1);
        UpdatePlayerUI(2);
    }

    private void PopulateDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        int count = GetCount();
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = selectionManager.birdPrefabs[i];
            var info = GetInfo(prefab);
            string label = info != null && !string.IsNullOrEmpty(info.displayName) ? info.displayName : (prefab != null ? prefab.name : $"Bird {i}");
            Sprite icon = info != null ? info.icon : null;

            var option = new TMP_Dropdown.OptionData(label, icon, Color.white);
            options.Add(option);
        }

        dropdown.AddOptions(options);
    }

    private void UpdatePlayerUI(int player)
    {
        GameObject prefab = selectionManager.GetSelectedPrefabForPlayer(player);
        var info = GetInfo(prefab);

        if (player == 1)
        {
            if (p1NameText != null) p1NameText.text = GetDisplayName(prefab, info);
            if (p1StatsText != null) p1StatsText.text = FormatStats(info);
            if (p1IconImage != null) p1IconImage.sprite = info != null ? info.icon : null;
            if (p1IconImage != null) p1IconImage.enabled = p1IconImage.sprite != null;
            if (p1Preview != null) p1Preview.RefreshPreview();
        }
        else
        {
            if (p2NameText != null) p2NameText.text = GetDisplayName(prefab, info);
            if (p2StatsText != null) p2StatsText.text = FormatStats(info);
            if (p2IconImage != null) p2IconImage.sprite = info != null ? info.icon : null;
            if (p2IconImage != null) p2IconImage.enabled = p2IconImage.sprite != null;
            if (p2Preview != null) p2Preview.RefreshPreview();
        }
    }

    private BirdInfo GetInfo(GameObject prefab)
    {
        if (prefab == null) return null;
        return prefab.GetComponent<BirdInfo>();
    }

    private string GetDisplayName(GameObject prefab, BirdInfo info)
    {
        if (info != null && !string.IsNullOrEmpty(info.displayName)) return info.displayName;
        return prefab != null ? prefab.name : "Unknown Bird";
    }

    private string FormatStats(BirdInfo info)
    {
        if (info == null)
        {
            return "Speed: -  Weight: -  Power: -\nAbility: -";
        }

        string ability = string.IsNullOrEmpty(info.abilityName) ? "-" : info.abilityName;
        return $"Speed: {info.speed:0}  Weight: {info.weight:0}  Power: {info.power:0}\nAbility: {ability}";
    }

    private int GetCount()
    {
        return (selectionManager != null && selectionManager.birdPrefabs != null) ? selectionManager.birdPrefabs.Length : 0;
    }

    private int ClampIndex(int idx)
    {
        int count = GetCount();
        if (count <= 0) return 0;
        return Mathf.Clamp(idx, 0, count - 1);
    }
}