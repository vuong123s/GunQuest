using UnityEngine;
using UnityEngine.InputSystem;

namespace GunQuest.Input
{
    // C# wrapper for PlayerInput.inputactions, stored beside that asset as in the tutorial.
    public sealed class PlayerInput
    {
        public InputActionAsset Asset { get; }

        private readonly InputActionMap onFoot;
        private readonly InputAction movement;
        private readonly InputAction look;
        private readonly InputAction jump;
        private readonly InputAction crouch;
        private readonly InputAction sprint;
        private readonly InputAction interact;
        private readonly InputAction fire;
        private readonly InputAction idleGunMiddle;
        private readonly InputAction idleShoot;
        private readonly InputAction reload;
        private readonly InputAction runGunMiddle;
        private readonly InputAction shootAuto;
        private readonly InputAction shootBurst;
        private readonly InputAction shootSingle;
        private readonly InputAction die;

        public PlayerInput()
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            onFoot = Asset.AddActionMap("OnFoot");

            movement = onFoot.AddAction("Movement", InputActionType.Value, expectedControlLayout: "Vector2");
            movement.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            movement.AddBinding("<Gamepad>/leftStick");

            look = onFoot.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Pointer>/delta");
            look.AddBinding("<Gamepad>/rightStick");

            jump = onFoot.AddAction("Jump", InputActionType.Button, expectedControlLayout: "Button");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            crouch = onFoot.AddAction("Crouch", InputActionType.Button, expectedControlLayout: "Button");
            crouch.AddBinding("<Keyboard>/leftCtrl");
            crouch.AddBinding("<Gamepad>/buttonEast");

            sprint = onFoot.AddAction("Sprint", InputActionType.Button, expectedControlLayout: "Button");
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");

            interact = onFoot.AddAction("Interact", InputActionType.Button, expectedControlLayout: "Button");
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            fire = onFoot.AddAction("Fire", InputActionType.Button, expectedControlLayout: "Button");
            fire.AddBinding("<Mouse>/leftButton");
            fire.AddBinding("<Gamepad>/rightTrigger");

            idleGunMiddle = onFoot.AddAction("IdleGunMiddle", InputActionType.Button, expectedControlLayout: "Button");
            idleGunMiddle.AddBinding("<Keyboard>/digit1");

            idleShoot = onFoot.AddAction("IdleShoot", InputActionType.Button, expectedControlLayout: "Button");
            idleShoot.AddBinding("<Keyboard>/digit2");

            reload = onFoot.AddAction("Reload", InputActionType.Button, expectedControlLayout: "Button");
            reload.AddBinding("<Keyboard>/r");

            runGunMiddle = onFoot.AddAction("RunGunMiddle", InputActionType.Button, expectedControlLayout: "Button");
            runGunMiddle.AddBinding("<Keyboard>/digit3");

            shootAuto = onFoot.AddAction("ShootAuto", InputActionType.Button, expectedControlLayout: "Button");
            shootAuto.AddBinding("<Keyboard>/digit4");

            shootBurst = onFoot.AddAction("ShootBurst", InputActionType.Button, expectedControlLayout: "Button");
            shootBurst.AddBinding("<Keyboard>/digit5");

            shootSingle = onFoot.AddAction("ShootSingle", InputActionType.Button, expectedControlLayout: "Button");
            shootSingle.AddBinding("<Keyboard>/digit6");

            die = onFoot.AddAction("Die", InputActionType.Button, expectedControlLayout: "Button");
            die.AddBinding("<Keyboard>/k");
        }

        public OnFootActions OnFoot => new OnFootActions(this);

        public readonly struct OnFootActions
        {
            private readonly PlayerInput wrapper;
            public OnFootActions(PlayerInput wrapper) => this.wrapper = wrapper;
            public InputAction Movement => wrapper.movement;
            public InputAction Look => wrapper.look;
            public InputAction Jump => wrapper.jump;
            public InputAction Crouch => wrapper.crouch;
            public InputAction Sprint => wrapper.sprint;
            public InputAction Interact => wrapper.interact;
            public InputAction Fire => wrapper.fire;
            public InputAction IdleGunMiddle => wrapper.idleGunMiddle;
            public InputAction IdleShoot => wrapper.idleShoot;
            public InputAction Reload => wrapper.reload;
            public InputAction RunGunMiddle => wrapper.runGunMiddle;
            public InputAction ShootAuto => wrapper.shootAuto;
            public InputAction ShootBurst => wrapper.shootBurst;
            public InputAction ShootSingle => wrapper.shootSingle;
            public InputAction Die => wrapper.die;
            public void Enable() => wrapper.onFoot.Enable();
            public void Disable() => wrapper.onFoot.Disable();
        }
    }
}
