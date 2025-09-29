using UnityEngine;

[DisallowMultipleComponent]
public class BirdInfo : MonoBehaviour
{
    [Header("Display")]
    public string displayName;
    public Sprite icon;

    [Header("Stats (0-10)")]
    [Range(0f, 10f)] public float speed = 5f;
    [Range(0f, 10f)] public float weight = 5f;
    [Range(0f, 10f)] public float power = 5f;

    [Header("Ability")]
    public string abilityName;
    [TextArea]
    public string description;
}