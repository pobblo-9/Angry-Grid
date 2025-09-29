using UnityEngine;

public class BirdSelectionManager : MonoBehaviour
{
    [Header("Available Bird Prefabs")]
    public GameObject[] birdPrefabs;

    [Header("Per-Player Selection")]
    [Tooltip("Index of the selected bird for Player 1")] public int selectedIndexP1 = 0;
    [Tooltip("Index of the selected bird for Player 2")] public int selectedIndexP2 = 0;

    [Header("Legacy (optional)")]
    [Tooltip("Legacy single selection (kept for compatibility). Not used by applier when per-player selection is present.")]
    public int selectedIndex = 0;

    // Legacy single selection getter
    public GameObject GetSelectedPrefab()
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return null;
        if (selectedIndex < 0 || selectedIndex >= birdPrefabs.Length) selectedIndex = 0;
        return birdPrefabs[selectedIndex];
    }

    // Per-player getters
    public GameObject GetSelectedPrefabForPlayer(int player)
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return null;
        int idx = (player == 2) ? selectedIndexP2 : selectedIndexP1;
        if (idx < 0 || idx >= birdPrefabs.Length) idx = 0;
        return birdPrefabs[idx];
    }

    // Legacy setters (single selection)
    public void SelectIndex(int index)
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return;
        selectedIndex = Mathf.Clamp(index, 0, birdPrefabs.Length - 1);
    }

    public void SelectNext()
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return;
        selectedIndex = (selectedIndex + 1) % birdPrefabs.Length;
    }

    public void SelectPrevious()
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return;
        selectedIndex = (selectedIndex - 1 + birdPrefabs.Length) % birdPrefabs.Length;
    }

    // Per-player setters
    public void SelectIndexForPlayer(int player, int index)
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return;
        int clamped = Mathf.Clamp(index, 0, birdPrefabs.Length - 1);
        if (player == 2) selectedIndexP2 = clamped; else selectedIndexP1 = clamped;
    }

    public void SelectNextForPlayer(int player)
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return;
        if (player == 2)
            selectedIndexP2 = (selectedIndexP2 + 1) % birdPrefabs.Length;
        else
            selectedIndexP1 = (selectedIndexP1 + 1) % birdPrefabs.Length;
    }

    public void SelectPreviousForPlayer(int player)
    {
        if (birdPrefabs == null || birdPrefabs.Length == 0) return;
        if (player == 2)
            selectedIndexP2 = (selectedIndexP2 - 1 + birdPrefabs.Length) % birdPrefabs.Length;
        else
            selectedIndexP1 = (selectedIndexP1 - 1 + birdPrefabs.Length) % birdPrefabs.Length;
    }
}
