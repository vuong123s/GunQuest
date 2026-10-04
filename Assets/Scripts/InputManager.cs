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
    private PlayerShoot shoot;
    private PlayerMelee melee;
    private bool firing;
    private PlayerWeapons weapons;

    public InputActions.OnFootActions OnFoot => onFoot;

    private void Awake()
    {
        input = new InputActions();
        onFoot = input.OnFoot;
        motor = GetComponent<PlayerMotor>();
        look = GetComponentInChildren<PlayerLook>();
        interact = GetComponentInChildren<PlayerInteract>();
        shoot = GetComponent<PlayerShoot>();
        melee = GetComponent<PlayerMelee>();
        weapons = GetComponent<PlayerWeapons>();

        onFoot.Jump.performed += _ => motor.Jump();
        onFoot.Crouch.performed += _ => motor.Crouch();
        onFoot.Sprint.started += _ => motor.SetSprinting(true);
        onFoot.Sprint.canceled += _ => motor.SetSprinting(false);
        onFoot.Interact.performed += _ => interact?.ProcessInteract();
        onFoot.Melee.performed += _ => Melee();
        onFoot.Reload.performed += _ => { if (weapons == null || weapons.Current == null || !weapons.Current.melee) motor.Reload(); };
        onFoot.Fire.started += _ => SetFiring(true);
        onFoot.Fire.canceled += _ => SetFiring(false);
    }

    private void Update()
    {
        if (weapons != null)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                int slot = keyboard.digit1Key.wasPressedThisFrame ? 0 : keyboard.digit2Key.wasPressedThisFrame ? 1 :
                    keyboard.digit3Key.wasPressedThisFrame ? 2 : keyboard.digit4Key.wasPressedThisFrame ? 3 : -1;
                if (slot >= 0 && weapons.Equip(slot)) SetFiring(false);
            }
            if (firing && weapons.Current != null && weapons.Current.automatic) weapons.Attack();
            return;
        }
        if (!firing || shoot == null || !shoot.Shoot()) return;
        motor.FaceCameraDirection();
    }

    private void FixedUpdate()
    {
        motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

    private void LateUpdate()
    {
        look?.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    private void OnEnable() => onFoot.Enable();
    private void OnDisable()
    {
        firing = false;
        motor?.SetFiring(false);
        onFoot.Disable();
    }

    private void Melee()
    {
        if (melee != null && melee.Attack()) motor.PlayMeleeAnimation();
    }

    private void SetFiring(bool value)
    {
        firing = value;
        motor.SetFiring(value && (weapons == null || (weapons.Current != null && !weapons.Current.melee)));
        if (value && weapons != null) weapons.Attack();
    }
}
