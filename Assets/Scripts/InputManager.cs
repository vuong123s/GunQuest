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
        motor = GetComponent<PlayerMotor>(); look = GetComponentInChildren<PlayerLook>();
        interact = GetComponentInChildren<PlayerInteract>(); melee = GetComponent<PlayerMelee>();
        weapons = GetComponent<PlayerWeapons>(); health = GetComponent<PlayerHealth>();
        onFoot.Jump.performed += _ => { if (CanAct) motor.Jump(); };
        onFoot.Crouch.performed += _ => { if (CanAct) motor.Crouch(); };
        onFoot.Sprint.started += _ => { if (CanAct) motor.SetSprinting(true); };
        onFoot.Sprint.canceled += _ => motor.SetSprinting(false);
        onFoot.Interact.performed += _ => { if (CanAct) interact?.ProcessInteract(); };
        onFoot.Melee.performed += _ => { if (CanAct && melee != null && melee.Attack()) motor.PlayMeleeAnimation(); };
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
            int slot = keyboard.digit1Key.wasPressedThisFrame ? 0 : keyboard.digit2Key.wasPressedThisFrame ? 1 :
                keyboard.digit3Key.wasPressedThisFrame ? 2 : keyboard.digit4Key.wasPressedThisFrame ? 3 : -1;
            if (slot >= 0 && weapons.Equip(slot)) SetFiring(false);
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
        motor.SetFiring(firing && weapons != null && weapons.Current != null && !weapons.Current.melee);
        if (firing) weapons?.Attack();
    }
}
