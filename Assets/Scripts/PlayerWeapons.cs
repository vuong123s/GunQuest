using System;
using System.Collections;
using UnityEngine;

public enum SlotKind
{
    Gun,
    Melee,
    Consumable,
    Throwable
}

[RequireComponent(typeof(PlayerShoot), typeof(PlayerMelee), typeof(PlayerMotor))]
public class PlayerWeapons : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public string label;
        public GameObject model;
        public Transform muzzle;
        public bool melee;
        public bool automatic;
        public SlotKind kind = SlotKind.Gun;
        public int count = 3;
        public int maxCount = 5;
        public GameObject itemPrefab;
        [Min(1f)] public float damage = 15f;
        [Min(0.05f)] public float cooldown = 0.25f;
        [Min(0.1f)] public float range = 100f;
    }

    [SerializeField] private Slot[] slots = new Slot[0];
    [SerializeField] private int startingSlot;
    private PlayerShoot shoot;
    private PlayerMelee melee;
    private PlayerMotor motor;
    private PlayerHealth health;
    private int[] magazines, reserves;
    private ContractProgress progress = new ContractProgress();
    private Coroutine reloadRoutine;
    private float lastUseTime = -999f;
    private static AudioClip healSound, throwSound;

    public int SelectedIndex { get; private set; } = -1;
    public int Count => slots.Length;
    public Slot Current => SelectedIndex >= 0 && SelectedIndex < slots.Length ? slots[SelectedIndex] : null;
    public SlotKind CurrentKind => Current != null ? GetKind(Current) : SlotKind.Gun;

    public int Ammo
    {
        get
        {
            if (Current == null) return 0;
            var k = GetKind(Current);
            if (k == SlotKind.Gun) return magazines != null && SelectedIndex < magazines.Length ? magazines[SelectedIndex] : 0;
            return (k == SlotKind.Consumable || k == SlotKind.Throwable) ? Current.count : 0;
        }
    }

    public int Reserve
    {
        get
        {
            if (Current == null) return 0;
            var k = GetKind(Current);
            if (k == SlotKind.Gun) return reserves != null && SelectedIndex < reserves.Length ? reserves[SelectedIndex] : 0;
            return (k == SlotKind.Consumable || k == SlotKind.Throwable) ? Current.maxCount : 0;
        }
    }

    public bool IsReloading { get; private set; }
    public bool IsMeleeAttacking => melee != null && melee.IsAttacking;
    public MeleeAttackTiming CurrentMeleeTiming => MeleeAttackTiming.Bat;
    public event Action Changed;
    public event Action Attacked;
    public event Action QuickMeleeStarted;
    public event Action<string, Color> StatusMessage;

    public static SlotKind GetKind(Slot s)
    {
        if (s == null) return SlotKind.Gun;
        if (s.kind != SlotKind.Gun) return s.kind;
        return s.melee ? SlotKind.Melee : SlotKind.Gun;
    }

    public SlotKind GetKind(int index) => (index >= 0 && index < slots.Length) ? GetKind(slots[index]) : SlotKind.Gun;
    public int GetCount(int index) => (index >= 0 && index < slots.Length) ? slots[index].count : 0;
    public string GetLabel(int index) => index >= 0 && index < slots.Length ? slots[index].label : "";

    private void Awake()
    {
        shoot = GetComponent<PlayerShoot>(); melee = GetComponent<PlayerMelee>();
        motor = GetComponent<PlayerMotor>(); health = GetComponent<PlayerHealth>();
        magazines = new int[slots.Length]; reserves = new int[slots.Length];
        ResetAmmo();
    }

    private void Start() { if (slots.Length > 0) Equip(Mathf.Clamp(startingSlot, 0, slots.Length - 1)); }

    public int Capacity(int index)
    {
        if (index < 0 || index >= slots.Length) return 0;
        var k = GetKind(slots[index]);
        if (k == SlotKind.Melee) return 0;
        if (k == SlotKind.Consumable || k == SlotKind.Throwable) return slots[index].count;
        return slots[index].automatic ? 30 + progress.magazineLevel * 5 : 12 + progress.magazineLevel * 2;
    }

    private void ResetAmmo()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var k = GetKind(slots[i]);
            if (k == SlotKind.Gun)
            {
                magazines[i] = Capacity(i);
                reserves[i] = progress != null ? (slots[i].automatic ? progress.rifleAmmoReserve : progress.pistolAmmoReserve) : (slots[i].automatic ? 120 : 60);
            }
            else if (k == SlotKind.Consumable) slots[i].count = progress != null ? progress.healthPotionCount : 3;
            else if (k == SlotKind.Throwable) slots[i].count = progress != null ? progress.grenadeCount : 3;
            else { magazines[i] = 0; reserves[i] = 0; }
        }
    }

    public void ApplyProgress(ContractProgress value)
    {
        progress = value;
        CancelReload();
        if (melee != null) melee.CancelAttack();
        ResetAmmo();
        if (Current != null) ConfigureCurrent();
        Changed?.Invoke();
    }

    public int PotionSlotIndex
    {
        get
        {
            for (int i = 0; slots != null && i < slots.Length; i++)
                if (slots[i] != null && slots[i].kind == SlotKind.Consumable) return i;
            return -1;
        }
    }

    public int GrenadeSlotIndex
    {
        get
        {
            for (int i = 0; slots != null && i < slots.Length; i++)
                if (slots[i] != null && slots[i].kind == SlotKind.Throwable) return i;
            return -1;
        }
    }

    public int PotionCount => PotionSlotIndex >= 0 ? slots[PotionSlotIndex].count : (progress != null ? progress.healthPotionCount : 0);
    public int GrenadeCount => GrenadeSlotIndex >= 0 ? slots[GrenadeSlotIndex].count : (progress != null ? progress.grenadeCount : 0);
    public int PistolReserve => (reserves != null && reserves.Length > 0) ? reserves[0] : (progress != null ? progress.pistolAmmoReserve : 0);
    public int RifleReserve => (reserves != null && reserves.Length > 1) ? reserves[1] : (progress != null ? progress.rifleAmmoReserve : 0);

    private void ConfigureCurrent()
    {
        Slot selected = Current;
        if (selected == null) return;
        bool isGun = GetKind(selected) == SlotKind.Gun;
        shoot.Configure(selected.muzzle, selected.damage * progress.DamageMultiplier, selected.cooldown, selected.range, isGun);
        melee.Configure((selected.melee ? selected.damage : 35f) * progress.DamageMultiplier,
            selected.melee ? selected.cooldown : 0.55f, selected.melee ? selected.range : 2f);
    }

    public bool Equip(int index)
    {
        if (index < 0 || index >= slots.Length || index == SelectedIndex || (health != null && health.IsDead) || slots[index].model == null) return false;
        CancelReload();
        if (melee != null) melee.CancelAttack();
        motor.SetFiring(false);
        for (int i = 0; i < slots.Length; i++) if (slots[i].model != null) slots[i].model.SetActive(i == index);
        SelectedIndex = index; ConfigureCurrent(); Changed?.Invoke(); return true;
    }

    public bool Attack()
    {
        if (!ContractGame.ControlsAllowed || Current == null || IsReloading || IsMeleeAttacking || (health != null && health.IsDead)) return false;
        motor.FaceCameraDirection();

        var k = GetKind(Current);
        if (k == SlotKind.Consumable) return UseConsumable();
        if (k == SlotKind.Throwable) return ThrowItem();
        if (k == SlotKind.Gun)
        {
            if (Ammo <= 0) { Reload(); return false; }
            if (!shoot.Shoot()) return false;
            magazines[SelectedIndex]--;
        }
        else
        {
            if (!melee.Attack(CurrentMeleeTiming)) return false;
            motor.PlayMeleeAnimation();
        }

        Attacked?.Invoke();
        return true;
    }

    private bool UseConsumable()
    {
        if (Current.count <= 0)
        {
            StatusMessage?.Invoke("Đã hết bình máu!", new Color(1f, 0.4f, 0.4f));
            return false;
        }
        if (Time.time - lastUseTime < Current.cooldown) return false;
        lastUseTime = Time.time;

        float healAmount = Current.damage > 0 ? Current.damage : 50f;
        if (health != null)
        {
            if (health.GetCurrentHealth() >= health.maxHealth)
            {
                StatusMessage?.Invoke("Máu đã đầy!", new Color(1f, 0.85f, 0.3f));
                return false;
            }
            health.RestoreHealth(healAmount);
        }

        Current.count--;
        if (progress != null) progress.healthPotionCount = Current.count;

        GameObject vfxPrefab = Current.itemPrefab != null ? Current.itemPrefab : Resources.Load<GameObject>("VFX/vfx_Heal_01");
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.position + Vector3.up * 0.1f, Quaternion.identity, transform);
            Destroy(vfx, 3.5f);
        }

        PlayHealSound();
        StatusMessage?.Invoke("+" + Mathf.CeilToInt(healAmount) + " HP", new Color(0.2f, 1f, 0.5f));
        motor.PlayMeleeAnimation();
        Changed?.Invoke();
        Attacked?.Invoke();
        return true;
    }

    private bool ThrowItem()
    {
        if (Current.count <= 0)
        {
            StatusMessage?.Invoke("Đã hết lựu đạn!", new Color(1f, 0.4f, 0.4f));
            return false;
        }
        if (Time.time - lastUseTime < Current.cooldown) return false;
        lastUseTime = Time.time;

        Current.count--;
        if (progress != null) progress.grenadeCount = Current.count;

        Camera cam = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : transform.position + Vector3.up * 1.5f;
        Vector3 camFwd = cam != null ? cam.transform.forward : transform.forward;
        Vector3 camRight = cam != null ? cam.transform.right : transform.right;

        Vector3 spawnPos = camPos + camFwd * 0.55f + camRight * 0.18f - Vector3.up * 0.1f;
        Vector3 throwVel = camFwd * 18f + Vector3.up * 3.5f + (motor != null ? motor.Velocity * 0.35f : Vector3.zero);

        if (Current.itemPrefab != null)
        {
            GameObject g = Instantiate(Current.itemPrefab, spawnPos, UnityEngine.Random.rotation);
            var rb = g.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = throwVel;
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 14f;
            }
            g.GetComponent<GrenadeProjectile>()?.Initialize(null, Current.damage > 0 ? Current.damage : 120f, Current.range > 0 ? Current.range : 6.5f);
        }

        PlayThrowSound();
        motor.PlayMeleeAnimation();
        Changed?.Invoke();
        Attacked?.Invoke();
        return true;
    }

    private static void PlayHealSound()
    {
        if (healSound == null) healSound = CreateHealSound();
        float vol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.9f;
        AudioSource.PlayClipAtPoint(healSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, vol);
    }

    private static AudioClip CreateHealSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.55f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float freq = Mathf.Lerp(440f, 880f, t / duration);
            float env = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.3f) * env * 0.6f;
        }
        AudioClip clip = AudioClip.Create("HealSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void PlayThrowSound()
    {
        if (throwSound == null) throwSound = CreateThrowSound();
        float vol = GameSettings.Instance != null ? GameSettings.Instance.sfxVolume : 0.85f;
        AudioSource.PlayClipAtPoint(throwSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, vol);
    }

    private static AudioClip CreateThrowSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.22f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        var rng = new System.Random(1337);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Sin(Mathf.PI * (t / duration));
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float whoosh = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 110f, t) * t);
            samples[i] = (noise * 0.35f + whoosh * 0.65f) * env * 0.6f;
        }
        AudioClip clip = AudioClip.Create("ThrowSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public bool QuickMelee()
    {
        if (Current != null && Current.melee) return Attack();
        if (!ContractGame.ControlsAllowed || Current == null || IsReloading || IsMeleeAttacking ||
            (health != null && health.IsDead) || !melee.Attack(MeleeAttackTiming.Quick)) return false;
        motor.SetFiring(false); motor.FaceCameraDirection(); motor.PlayMeleeAnimation();
        QuickMeleeStarted?.Invoke();
        return true;
    }

    public bool Reload()
    {
        if (!ContractGame.ControlsAllowed || Current == null || GetKind(Current) != SlotKind.Gun || IsReloading || IsMeleeAttacking ||
            (health != null && health.IsDead) || Ammo >= Capacity(SelectedIndex) || Reserve <= 0) return false;
        IsReloading = true; motor.SetFiring(false); motor.Reload();
        reloadRoutine = StartCoroutine(ReloadMagazine(SelectedIndex, Current.automatic ? 2f : 1.4f));
        return true;
    }

    private IEnumerator ReloadMagazine(int index, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (health == null || !health.IsDead)
        {
            int amount = Mathf.Min(Capacity(index) - magazines[index], reserves[index]);
            magazines[index] += amount; reserves[index] -= amount;
        }
        IsReloading = false; reloadRoutine = null;
    }

    private void CancelReload()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null; IsReloading = false;
    }

    private void OnDisable()
    {
        CancelReload();
        if (melee != null) melee.CancelAttack();
    }
}
