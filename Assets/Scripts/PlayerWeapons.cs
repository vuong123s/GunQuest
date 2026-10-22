using System;
using System.Collections;
using UnityEngine;

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
    public int SelectedIndex { get; private set; } = -1;
    public int Count => slots.Length;
    public Slot Current => SelectedIndex >= 0 && SelectedIndex < slots.Length ? slots[SelectedIndex] : null;
    public int Ammo => Current != null && !Current.melee ? magazines[SelectedIndex] : 0;
    public int Reserve => Current != null && !Current.melee ? reserves[SelectedIndex] : 0;
    public bool IsReloading { get; private set; }
    public event Action Changed;
    public event Action Attacked;

    private void Awake()
    {
        shoot = GetComponent<PlayerShoot>(); melee = GetComponent<PlayerMelee>();
        motor = GetComponent<PlayerMotor>(); health = GetComponent<PlayerHealth>();
        magazines = new int[slots.Length]; reserves = new int[slots.Length];
        ResetAmmo();
    }

    private void Start() { if (slots.Length > 0) Equip(Mathf.Clamp(startingSlot, 0, slots.Length - 1)); }
    public string GetLabel(int index) => index >= 0 && index < slots.Length ? slots[index].label : "";
    public int Capacity(int index) => slots[index].melee ? 0 :
        (slots[index].automatic ? 30 + progress.magazineLevel * 5 : 12 + progress.magazineLevel * 2);
    private void ResetAmmo()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            magazines[i] = Capacity(i);
            reserves[i] = slots[i].melee ? 0 : (slots[i].automatic ? 120 : 60);
        }
    }

    public void ApplyProgress(ContractProgress value)
    {
        progress = value;
        CancelReload();
        ResetAmmo();
        if (Current != null) ConfigureCurrent();
        Changed?.Invoke();
    }

    private void ConfigureCurrent()
    {
        Slot selected = Current;
        shoot.Configure(selected.muzzle, selected.damage * progress.DamageMultiplier, selected.cooldown, selected.range, !selected.melee);
        melee.Configure((selected.melee ? selected.damage : 35f) * progress.DamageMultiplier,
            selected.melee ? selected.cooldown : 0.55f, selected.melee ? selected.range : 2f);
    }

    public bool Equip(int index)
    {
        if (index < 0 || index >= slots.Length || index == SelectedIndex || (health != null && health.IsDead)) return false;
        if (slots[index].model == null) return false;
        CancelReload(); motor.SetFiring(false);
        for (int i = 0; i < slots.Length; i++) if (slots[i].model != null) slots[i].model.SetActive(i == index);
        SelectedIndex = index; ConfigureCurrent(); Changed?.Invoke(); return true;
    }

    public bool Attack()
    {
        if (!ContractGame.ControlsAllowed || Current == null || IsReloading || (health != null && health.IsDead)) return false;
        motor.FaceCameraDirection();
        if (!Current.melee)
        {
            if (Ammo <= 0) { Reload(); return false; }
            if (!shoot.Shoot()) return false;
            magazines[SelectedIndex]--;
        }
        else
        {
            if (!melee.Attack()) return false;
            motor.PlayMeleeAnimation();
        }
        Attacked?.Invoke(); return true;
    }

    public bool Reload()
    {
        if (!ContractGame.ControlsAllowed || Current == null || Current.melee || IsReloading ||
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
    private void OnDisable() => CancelReload();
}
