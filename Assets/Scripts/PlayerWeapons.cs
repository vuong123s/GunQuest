using System;
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

    public int SelectedIndex { get; private set; } = -1;
    public int Count => slots.Length;
    public Slot Current => SelectedIndex >= 0 ? slots[SelectedIndex] : null;
    public event Action Changed;

    private void Awake()
    {
        shoot = GetComponent<PlayerShoot>();
        melee = GetComponent<PlayerMelee>();
        motor = GetComponent<PlayerMotor>();
        health = GetComponent<PlayerHealth>();
    }

    private void Start() => Equip(Mathf.Clamp(startingSlot, 0, slots.Length - 1));

    public string GetLabel(int index) => index >= 0 && index < slots.Length ? slots[index].label : "";

    public bool Equip(int index)
    {
        if (index < 0 || index >= slots.Length || index == SelectedIndex || (SelectedIndex >= 0 && health != null && health.IsDead)) return false;
        Slot selected = slots[index];
        if (selected.model == null) return false;

        motor.SetFiring(false);
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].model != null) slots[i].model.SetActive(i == index);

        SelectedIndex = index;
        shoot.Configure(selected.muzzle, selected.damage, selected.cooldown, selected.range, !selected.melee);
        melee.Configure(selected.melee ? selected.damage : 35f, selected.melee ? selected.cooldown : 0.55f,
            selected.melee ? selected.range : 2f);
        Changed?.Invoke();
        return true;
    }

    public bool Attack()
    {
        if (Current == null || (health != null && health.IsDead)) return false;
        motor.FaceCameraDirection();
        if (!Current.melee) return shoot.Shoot();
        if (!melee.Attack()) return false;
        motor.PlayMeleeAnimation();
        return true;
    }
}
