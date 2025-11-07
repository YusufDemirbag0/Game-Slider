using UnityEngine;

public class UpgradeOption : MonoBehaviour
{
    public enum Type { Speed, MaxHPAdd, AmmoAdd, GoldAdd }
    public Type type;
    public float value = 1f;
    public bool isPercent = false;
}
