using System;
using UnityEngine;

[Serializable]
public class ContractProgress
{
    public int credits;
    public int damageLevel;
    public int magazineLevel;
    public int completedContracts;
    public const int MaxLevel = 5;
    private const string SaveKey = "GunQuest.ContractProgress.v1";
    public float DamageMultiplier => 1f + damageLevel * 0.1f;
    public int UpgradeCost(bool damage) => 100 * ((damage ? damageLevel : magazineLevel) + 1);
    public bool CanBuy(bool damage) => (damage ? damageLevel : magazineLevel) < MaxLevel && credits >= UpgradeCost(damage);

    public bool TryBuy(bool damage)
    {
        if (!CanBuy(damage)) return false;
        credits -= UpgradeCost(damage);
        if (damage) damageLevel++; else magazineLevel++;
        return true;
    }

    public static ContractProgress Load()
    {
        ContractProgress progress = null;
        try { progress = JsonUtility.FromJson<ContractProgress>(PlayerPrefs.GetString(SaveKey, "{}")); }
        catch (ArgumentException) { }
        if (progress == null) progress = new ContractProgress();
        progress.credits = Mathf.Max(0, progress.credits);
        progress.damageLevel = Mathf.Clamp(progress.damageLevel, 0, MaxLevel);
        progress.magazineLevel = Mathf.Clamp(progress.magazineLevel, 0, MaxLevel);
        progress.completedContracts = Mathf.Max(0, progress.completedContracts);
        return progress;
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }
}
