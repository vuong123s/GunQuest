using UnityEngine;

/// <summary>Shared strike timing for gameplay damage and the first-person motion.</summary>
public readonly struct MeleeAttackTiming
{
    public readonly float ImpactTime;
    public readonly float Duration;

    public MeleeAttackTiming(float impactTime, float duration)
    {
        Duration = Mathf.Max(0.05f, duration);
        ImpactTime = Mathf.Clamp(impactTime, 0f, Duration);
    }

    public static readonly MeleeAttackTiming Bat = new MeleeAttackTiming(0.22f, 0.65f);
    public static readonly MeleeAttackTiming Shovel = new MeleeAttackTiming(0.33f, 0.85f);
    public static readonly MeleeAttackTiming Quick = new MeleeAttackTiming(0.12f, 0.4f);
    public static MeleeAttackTiming ForWeapon(bool shovel) => shovel ? Shovel : Bat;
}
