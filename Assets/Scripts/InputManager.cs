using UnityEngine;
using UnityEngine.InputSystem;
using InputActions = GunQuest.Input.PlayerInput;

[RequireComponent(typeof(PlayerMotor))]
public class InputManager : MonoBehaviour
{
    private static readonly Key[] SlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

    private InputActions input;
    private InputActions.OnFootActions onFoot;
    private PlayerMotor motor;
    private PlayerLook look;
    private PlayerMelee melee;
    private PlayerWeapons weapons;
    private PlayerHealth health;
    private bool firing;

    public InputActions.OnFootActions OnFoot => onFoot;
    private bool CanAct => ContractGame.ControlsAllowed && (health == null || !health.IsDead);

    private void Awake()
    {
        input = new InputActions();
        onFoot = input.OnFoot;

        for (int i = 0; i < onFoot.Melee.bindings.Count; i++)
            if (onFoot.Melee.bindings[i].path == "<Mouse>/rightButton")
                onFoot.Melee.ApplyBindingOverride(i, string.Empty);

        motor = GetComponent<PlayerMotor>();
        look = GetComponentInChildren<PlayerLook>();
        melee = GetComponent<PlayerMelee>();
        weapons = GetComponent<PlayerWeapons>();
        health = GetComponent<PlayerHealth>();

        onFoot.Jump.performed += _ => { if (CanAct) motor.Jump(); };
        onFoot.Crouch.performed += _ => { if (CanAct) motor.Crouch(); };
        onFoot.Sprint.started += _ => { if (CanAct) motor.SetSprinting(true); };
        onFoot.Sprint.canceled += _ => motor.SetSprinting(false);
        onFoot.Melee.performed += _ =>
        {
            if (!CanAct) return;
            firing = false;
            motor.SetFiring(false);
            if (weapons != null) weapons.QuickMelee();
            else if (melee != null && melee.Attack()) motor.PlayMeleeAnimation();
        };
        onFoot.Reload.performed += _ => { if (CanAct) { firing = false; weapons?.Reload(); } };
        onFoot.Fire.started += _ => SetFiring(true);
        onFoot.Fire.canceled += _ => SetFiring(false);
    }

    private void Update()
    {
        if (!CanAct || weapons == null) return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            for (int i = 0; i < SlotKeys.Length; i++)
                if (keyboard[SlotKeys[i]].wasPressedThisFrame && weapons.Equip(i)) { SetFiring(false); break; }
        }

        var mouse = Mouse.current;
        if (mouse != null && weapons.Count > 0)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.5f) weapons.Equip((weapons.SelectedIndex - 1 + weapons.Count) % weapons.Count);
            else if (scroll < -0.5f) weapons.Equip((weapons.SelectedIndex + 1) % weapons.Count);
        }

        if (firing && weapons.Current != null && weapons.Current.automatic) weapons.Attack();
    }

    private void FixedUpdate() { if (CanAct) motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>()); }
    private void LateUpdate() { if (CanAct) look?.ProcessLook(onFoot.Look.ReadValue<Vector2>()); }
    private void OnEnable() { if (input != null) onFoot.Enable(); }
    private void OnDisable()
    {
        firing = false;
        motor?.SetFiring(false);
        motor?.SetSprinting(false);
        if (input != null) onFoot.Disable();
    }
    private void OnDestroy() { if (input != null && input.Asset != null) Destroy(input.Asset); }

    private void SetFiring(bool value)
    {
        firing = value && CanAct;
        motor.SetFiring(firing && weapons != null && weapons.Current != null && !weapons.Current.melee && !weapons.IsMeleeAttacking);
        if (firing) weapons?.Attack();
    }
}
