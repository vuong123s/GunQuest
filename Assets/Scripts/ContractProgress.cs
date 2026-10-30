using System;
using UnityEngine;

[Serializable]
public class ContractProgress
{
    public int credits;
    public int damageLevel;
    public int magazineLevel;
    public int completedContracts;

    // Consumables & Supplies stocked
    public int pistolAmmoReserve = 60;
    public int rifleAmmoReserve = 120;
    public int healthPotionCount = 3;
    public int grenadeCount = 3;

    public const int MaxLevel = 5;
    public const int MaxPotions = 5;
    public const int MaxGrenades = 5;
    public const int MaxPistolReserve = 180;
    public const int MaxRifleReserve = 360;

    public const int CostPistolAmmo = 30;   // +30 viên đạn súng lục
    public const int CostRifleAmmo = 50;    // +60 viên đạn rifle
    public const int CostPotion = 40;       // +1 bình máu
    public const int CostGrenade = 60;      // +1 quả bom

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

    public bool CanBuyPistolAmmo() => credits >= CostPistolAmmo && pistolAmmoReserve < MaxPistolReserve;
    public bool TryBuyPistolAmmo()
    {
        if (!CanBuyPistolAmmo()) return false;
        credits -= CostPistolAmmo;
        pistolAmmoReserve = Mathf.Min(MaxPistolReserve, pistolAmmoReserve + 30);
        return true;
    }

    public bool CanBuyRifleAmmo() => credits >= CostRifleAmmo && rifleAmmoReserve < MaxRifleReserve;
    public bool TryBuyRifleAmmo()
    {
        if (!CanBuyRifleAmmo()) return false;
        credits -= CostRifleAmmo;
        rifleAmmoReserve = Mathf.Min(MaxRifleReserve, rifleAmmoReserve + 60);
        return true;
    }

    public bool CanBuyPotion() => credits >= CostPotion && healthPotionCount < MaxPotions;
    public bool TryBuyPotion()
    {
        if (!CanBuyPotion()) return false;
        credits -= CostPotion;
        healthPotionCount = Mathf.Min(MaxPotions, healthPotionCount + 1);
        return true;
    }

    public bool CanBuyGrenade() => credits >= CostGrenade && grenadeCount < MaxGrenades;
    public bool TryBuyGrenade()
    {
        if (!CanBuyGrenade()) return false;
        credits -= CostGrenade;
        grenadeCount = Mathf.Min(MaxGrenades, grenadeCount + 1);
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
        if (progress.pistolAmmoReserve <= 0) progress.pistolAmmoReserve = 60;
        if (progress.rifleAmmoReserve <= 0) progress.rifleAmmoReserve = 120;
        if (progress.healthPotionCount <= 0) progress.healthPotionCount = 3;
        if (progress.grenadeCount <= 0) progress.grenadeCount = 3;
        return progress;
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }
}
