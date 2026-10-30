using UnityEngine;
using UnityEngine.InputSystem;
using InputActions = GunQuest.Input.PlayerInput;

[RequireComponent(typeof(PlayerMotor))]
public class InputManager : MonoBehaviour
{
    private InputActions input;
    private InputActions.OnFootActions onFoot;
    private PlayerMotor motor;
    private PlayerLook look;
    private PlayerInteract interact;
    private PlayerMelee melee;
    private PlayerWeapons weapons;
    private PlayerHealth health;
    private bool firing;
    public InputActions.OnFootActions OnFoot => onFoot;
    private bool CanAct => ContractGame.ControlsAllowed && (health == null || !health.IsDead);
    private void Awake()
    {
        input = new InputActions(); onFoot = input.OnFoot;
        // Right mouse belongs to ADS; retain F and the gamepad shoulder for quick melee.
        for (int i = 0; i < onFoot.Melee.bindings.Count; i++)
            if (onFoot.Melee.bindings[i].path == "<Mouse>/rightButton")
                onFoot.Melee.ApplyBindingOverride(i, string.Empty);
        motor = GetComponent<PlayerMotor>(); look = GetComponentInChildren<PlayerLook>();
        interact = GetComponentInChildren<PlayerInteract>(); melee = GetComponent<PlayerMelee>();
        weapons = GetComponent<PlayerWeapons>(); health = GetComponent<PlayerHealth>();
        onFoot.Jump.performed += _ => { if (CanAct) motor.Jump(); };
        onFoot.Crouch.performed += _ => { if (CanAct) motor.Crouch(); };
        onFoot.Sprint.started += _ => { if (CanAct) motor.SetSprinting(true); };
        onFoot.Sprint.canceled += _ => motor.SetSprinting(false);
        onFoot.Interact.performed += _ => { if (CanAct) interact?.ProcessInteract(); };
        onFoot.Melee.performed += _ =>
        {
            if (!CanAct) return;
            firing = false; motor.SetFiring(false);
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
            int slot = -1;
            if (keyboard.digit1Key.wasPressedThisFrame) slot = 0;
            else if (keyboard.digit2Key.wasPressedThisFrame) slot = 1;
            else if (keyboard.digit3Key.wasPressedThisFrame) slot = 2;
            else if (keyboard.digit4Key.wasPressedThisFrame) slot = 3;
            else if (keyboard.digit5Key.wasPressedThisFrame) slot = 4;
            else if (keyboard.digit6Key.wasPressedThisFrame) slot = 5;
            else if (keyboard.digit7Key.wasPressedThisFrame) slot = 6;
            else if (keyboard.digit8Key.wasPressedThisFrame) slot = 7;
            else if (keyboard.digit9Key.wasPressedThisFrame) slot = 8;

            if (slot >= 0 && weapons.Equip(slot)) SetFiring(false);
        }

        var mouse = Mouse.current;
        if (mouse != null && weapons.Count > 0)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.5f)
            {
                int next = (weapons.SelectedIndex - 1 + weapons.Count) % weapons.Count;
                weapons.Equip(next);
            }
            else if (scroll < -0.5f)
            {
                int next = (weapons.SelectedIndex + 1) % weapons.Count;
                weapons.Equip(next);
            }
        }

        if (firing && weapons.Current != null && weapons.Current.automatic) weapons.Attack();
    }
    private void FixedUpdate() { if (CanAct) motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>()); }
    private void LateUpdate() { if (CanAct) look?.ProcessLook(onFoot.Look.ReadValue<Vector2>()); }
    private void OnEnable() { if (input != null) onFoot.Enable(); }
    private void OnDisable()
    {
        firing = false; motor?.SetFiring(false); motor?.SetSprinting(false);
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
