using UnityEngine;
using UnityEngine.InputSystem;
using InputActions = GunQuest.Input.PlayerInput;

[RequireComponent(typeof(PlayerMotor))]
public class InputManager : MonoBehaviour
{
    private InputActions playerInput;
    private InputActions.OnFootActions onFoot;
    private PlayerMotor motor;
    private PlayerLook look;
    private PlayerInteract interact;
    private PlayerShoot shoot;

    public InputActions.OnFootActions OnFoot => onFoot;

    void Awake()
    {
        // Khởi tạo PlayerInput và tham chiếu đến Action Map "OnFoot" [1]
        playerInput = new InputActions();
        onFoot = playerInput.OnFoot;
        
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        interact = GetComponent<PlayerInteract>();
        shoot = GetComponent<PlayerShoot>();

        if (look == null)
        {
            look = GetComponentInChildren<PlayerLook>();
        }

        if (interact == null)
        {
            interact = GetComponentInChildren<PlayerInteract>();
        }

        if (shoot == null)
        {
            shoot = gameObject.AddComponent<PlayerShoot>();
        }

        // Đăng ký sự kiện Jump khi hành động được thực hiện [3]
        onFoot.Jump.performed += ctx =>
        {
            if (motor != null)
            {
                motor.Jump();
            }
        };

        onFoot.Crouch.performed += ctx =>
        {
            if (motor != null)
            {
                motor.Crouch();
            }
        };

        onFoot.Sprint.performed += ctx =>
        {
            if (motor != null)
            {
                motor.Sprint();
            }
        };

        onFoot.Interact.performed += ctx =>
        {
            if (interact != null)
            {
                interact.ProcessInteract();
            }
        };

        onFoot.Fire.performed += ctx =>
        {
            if (shoot != null)
            {
                shoot.Shoot();
            }
        };
    }

    void FixedUpdate()
    {
        // Yêu cầu PlayerMotor xử lý di chuyển bằng giá trị từ Movement Action [4]
        if (motor != null)
        {
            motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
        }
    }

    void LateUpdate()
    {
        // Yêu cầu PlayerLook xử lý nhìn xung quanh [2]
        if (look != null)
        {
            look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
        }
    }

    private void OnEnable()
    {
        onFoot.Enable(); // Kích hoạt Action Map [1]
    }

    private void OnDisable()
    {
        onFoot.Disable(); // Hủy kích hoạt Action Map [5]
    }
}
