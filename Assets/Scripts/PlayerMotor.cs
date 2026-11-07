using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchDuration = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private float animationDampTime = 0.1f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
    private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
    private static readonly int IsAiming = Animator.StringToHash("IsAiming");
    private static readonly int IsFiring = Animator.StringToHash("IsFiring");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int MeleeTrigger = Animator.StringToHash("Melee");
    private static readonly int HitTrigger = Animator.StringToHash("Hit");
    private static readonly int ReloadTrigger = Animator.StringToHash("Reload");
    private static readonly int DieTrigger = Animator.StringToHash("Die");

    private readonly HashSet<int> animatorParameters = new HashSet<int>();
    private CharacterController controller;
    private Animator animator;
    private float verticalSpeed;
    private bool crouching;
    private bool sprinting;
    private bool aiming;
    private bool dead;

    public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = characterAnimator != null ? characterAnimator : GetComponentInChildren<Animator>();
        standingHeight = Mathf.Max(standingHeight, controller.height);

        if (animator == null) return;
        animator.applyRootMotion = false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            animatorParameters.Add(parameter.nameHash);
    }

    private void Update()
    {
        float targetHeight = crouching ? crouchHeight : standingHeight;
        float heightSpeed = standingHeight / Mathf.Max(crouchDuration, 0.01f);
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, heightSpeed * Time.deltaTime);
        controller.center = Vector3.up * controller.height * 0.5f;
    }

    public void ProcessMove(Vector2 input)
    {
        if (dead) return;

        Vector3 move = GetMoveDirection(input);
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;

        float moveSpeed = sprinting && !crouching ? sprintSpeed : speed;
        controller.Move((move * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
        UpdateAnimator(input);
    }

    public void Jump()
    {
        if (dead || !controller.isGrounded || crouching) return;
        verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
        SetTrigger(JumpTrigger);
    }

    public void Crouch()
    {
        if (dead) return;
        crouching = !crouching;
        SetBool(IsCrouching, crouching);
    }

    public void SetSprinting(bool value)
    {
        sprinting = value && !dead;
        SetBool(IsSprinting, sprinting);
    }

    public void SetAiming(bool value)
    {
        aiming = value && !dead;
        SetBool(IsAiming, aiming);
    }

    public void SetFiring(bool value)
    {
        bool firing = value && !dead;
        SetAiming(firing);
        SetBool(IsFiring, firing);
    }

    public void FaceCameraDirection()
    {
        if (dead || Camera.main == null) return;
        Vector3 direction = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up);
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
    }

    public void Reload() { if (!dead) SetTrigger(ReloadTrigger); }
    public void PlayMeleeAnimation() => SetTrigger(MeleeTrigger);
    public void PlayHitAnimation() => SetTrigger(HitTrigger);

    public void PlayDieAnimation()
    {
        dead = true;
        sprinting = aiming = false;
        SetBool(IsMoving, false);
        SetBool(IsSprinting, false);
        SetBool(IsAiming, false);
        SetBool(IsFiring, false);
        SetTrigger(DieTrigger);
    }

    private static Vector3 GetMoveDirection(Vector2 input)
    {
        if (Camera.main == null) return new Vector3(input.x, 0f, input.y);
        Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }

    private void UpdateAnimator(Vector2 input)
    {
        if (animator == null) return;
        bool moving = input.sqrMagnitude > 0.01f;
        SetFloat(MoveX, input.x, animationDampTime);
        SetFloat(MoveY, input.y, animationDampTime);
        SetFloat(Speed, input.magnitude, animationDampTime);
        SetBool(IsMoving, moving);
        SetBool(IsGrounded, controller.isGrounded);
        SetBool(IsCrouching, crouching);
        SetBool(IsSprinting, sprinting && moving && !crouching);
    }

    private void SetFloat(int parameter, float value, float dampTime)
    {
        if (animator != null && animatorParameters.Contains(parameter))
            animator.SetFloat(parameter, value, dampTime, Time.deltaTime);
    }

    private void SetBool(int parameter, bool value)
    {
        if (animator != null && animatorParameters.Contains(parameter)) animator.SetBool(parameter, value);
    }

    private void SetTrigger(int parameter)
    {
        if (animator != null && animatorParameters.Contains(parameter)) animator.SetTrigger(parameter);
    }
}
