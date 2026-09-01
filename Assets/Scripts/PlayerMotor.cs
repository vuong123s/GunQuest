using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    private HashSet<int> animatorParameters = new HashSet<int>();
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool lerpCrouch;
    private bool crouching;
    private bool sprinting;
    private bool animatorSprinting;
    private bool jumpAnimationActive;
    private bool leftGroundAfterJump;
    private float actionAnimationLockUntil;
    private float crouchTimer;
    private float currentMoveSpeed;

    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchDuration = 1f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;
    public Animator characterAnimator;
    public string idleStateName = "IdleOneWeapon";
    public string walkStateName = "WalkFWDOneWeapon_IP";
    public string walkBackStateName = "WalkBWDOneWeapon_IP";
    public string walkLeftStateName = "WalkLFTOneWeapon_IP";
    public string walkRightStateName = "WalkRGTOneWeapon_IP";
    public string runStateName = "RunFWDOneWeapon_IP";
    public string jumpStateName = "JumpFullOneWeapon_IP";
    public string shootStateName = "ShootSingleshotOneWeapon";
    public string meleeStateName = "MeleeVerticalOneWeapon";
    public string hitStateName = "GetHitOneWeapon";
    public string dieStateName = "DieOneWeapon";
    public float animationFadeDuration = 0.1f;
    public float animationDampTime = 0.1f;
    public float sprintAnimationSpeed = 1.35f;
    public float walkAnimationSpeed = 1f;
    public float rotationSharpness = 16f;
    public float shootAnimationLockDuration = 0.12f;
    public float meleeAnimationLockDuration = 0.55f;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        FitControllerToCharacter();
        animator = characterAnimator != null ? characterAnimator : GetComponentInChildren<Animator>();
        CacheAnimatorParameters();
    }

    void Update()
    {
        // Kiểm tra xem nhân vật có đang chạm đất không mỗi khung hình [7]
        isGrounded = controller.isGrounded;
        SetAnimatorBool(IsGroundedHash, isGrounded);
        SetAnimatorBool(IsCrouchingHash, crouching);
        SetAnimatorBool(IsSprintingHash, animatorSprinting);
        UpdateJumpAnimationLock();

        if (lerpCrouch)
        {
            crouchTimer += Time.deltaTime;
            float p = crouchTimer / Mathf.Max(crouchDuration, 0.01f);
            p *= p;

            float targetHeight = crouching ? crouchHeight : standingHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, p);
            controller.center = Vector3.Lerp(controller.center, GetControllerCenter(targetHeight), p);

            if (p > 1f)
            {
                lerpCrouch = false;
                crouchTimer = 0f;
                controller.height = targetHeight;
                controller.center = GetControllerCenter(targetHeight);
            }
        }
    }

    // Nhận đầu vào từ InputManager và áp dụng vào CharacterController [5, 8]
    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = GetCameraRelativeMoveDirection(input);

        // Di chuyển nhân vật dựa trên hướng nhìn [8]
        float currentSpeed = sprinting && !crouching ? sprintSpeed : speed;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
        currentMoveSpeed = moveDirection.magnitude * currentSpeed;
        UpdateMovementAnimator(input, currentSpeed);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }

        // Áp dụng trọng lực [6, 7]
        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Giữ nhân vật bám trên mặt đất
        }
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private Vector3 GetCameraRelativeMoveDirection(Vector2 input)
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Camera activeCamera = Camera.main;

        if (activeCamera != null)
        {
            forward = Vector3.ProjectOnPlane(activeCamera.transform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(activeCamera.transform.right, Vector3.up).normalized;
        }

        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }

    public void Jump()
    {
        if (isGrounded && !crouching)
        {
            // Công thức tính lực nhảy [3]
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
            SetAnimatorTrigger(JumpHash);
            if (!HasAnimatorParameter(JumpHash))
            {
                PlayAnimatorState(jumpStateName);
            }
            jumpAnimationActive = true;
            leftGroundAfterJump = false;
        }
    }

    public void Crouch()
    {
        crouching = !crouching;
        crouchTimer = 0f;
        lerpCrouch = true;
    }

    public void Sprint()
    {
        sprinting = !sprinting;
    }

    public void SetSprinting(bool isSprinting)
    {
        sprinting = isSprinting;
    }

    /// <summary>
    /// Plays any state created from the SciFiWarrior animation pack, for example
    /// "Reload", "Shoot_Autoshot_AR", "Die" or "Idle_gunMiddle_ar".
    /// </summary>
    public void PlayAnimation(string stateName, float fadeDuration = -1f)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        animator.CrossFadeInFixedTime(
            Animator.StringToHash(GetAnimatorStateName(stateName)),
            fadeDuration >= 0f ? fadeDuration : animationFadeDuration,
            0);
    }

    public void PlayShootAnimation()
    {
        PlayActionAnimation(shootStateName, shootAnimationLockDuration);
    }

    public void PlayMeleeAnimation()
    {
        PlayActionAnimation(meleeStateName, meleeAnimationLockDuration);
    }

    private void PlayActionAnimation(string stateName, float lockDuration)
    {
        actionAnimationLockUntil = Mathf.Max(actionAnimationLockUntil, Time.time + lockDuration);
        PlayAnimation(stateName);
    }

    private void CacheAnimatorParameters()
    {
        animatorParameters.Clear();

        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            animatorParameters.Add(parameter.nameHash);
        }
    }

    private void UpdateMovementAnimator(Vector2 input, float maxSpeed)
    {
        bool isMoving = input.sqrMagnitude > 0.01f;
        float animationSpeed = sprinting && isMoving ? sprintAnimationSpeed : walkAnimationSpeed;
        animatorSprinting = sprinting && isMoving && input.y > 0.5f && Mathf.Abs(input.x) < 0.5f;

        SetAnimatorFloat(MoveXHash, input.x, animationDampTime);
        SetAnimatorFloat(MoveYHash, input.y, animationDampTime);
        SetAnimatorFloat(SpeedHash, isMoving ? animationSpeed : 1f, animationDampTime);
        SetAnimatorBool(IsMovingHash, isMoving);
        SetAnimatorBool(IsSprintingHash, animatorSprinting);
        UpdateDirectAnimatorState(input);
    }

    private void SetAnimatorFloat(int parameterHash, float value, float dampTime = 0f)
    {
        if (!HasAnimatorParameter(parameterHash))
        {
            return;
        }

        if (dampTime > 0f)
        {
            animator.SetFloat(parameterHash, value, dampTime, Time.deltaTime);
        }
        else
        {
            animator.SetFloat(parameterHash, value);
        }
    }

    private void SetAnimatorBool(int parameterHash, bool value)
    {
        if (HasAnimatorParameter(parameterHash))
        {
            animator.SetBool(parameterHash, value);
        }
    }

    private void SetAnimatorTrigger(int parameterHash)
    {
        if (HasAnimatorParameter(parameterHash))
        {
            animator.SetTrigger(parameterHash);
        }
    }

    private bool HasAnimatorParameter(int parameterHash)
    {
        return animator != null && animatorParameters.Contains(parameterHash);
    }

    private void UpdateDirectAnimatorState(Vector2 input)
    {
        if (animator == null || animatorParameters.Count > 0 || jumpAnimationActive ||
            Time.time < actionAnimationLockUntil || !isGrounded)
        {
            return;
        }

        if (input.sqrMagnitude <= 0.01f)
        {
            PlayAnimatorState(idleStateName);
            return;
        }

        PlayAnimatorState(GetMovementStateName(input));
    }

    private void UpdateJumpAnimationLock()
    {
        if (!jumpAnimationActive)
        {
            return;
        }

        if (!isGrounded)
        {
            leftGroundAfterJump = true;
            return;
        }

        if (leftGroundAfterJump)
        {
            jumpAnimationActive = false;
            leftGroundAfterJump = false;
        }
    }

    private void PlayAnimatorState(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        int stateHash = Animator.StringToHash(GetAnimatorStateName(stateName));

        if (stateInfo.shortNameHash == stateHash && !animator.IsInTransition(0))
        {
            return;
        }

        if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == stateHash)
        {
            return;
        }

        animator.CrossFadeInFixedTime(stateHash, animationFadeDuration);
    }

    private string GetAnimatorStateName(string stateName)
    {
        switch (stateName)
        {
            case "Idle_Guard_AR":
            case "Idle_gunMiddle_AR":
            case "Idle_gunMiddle_ar":
            case "Idle_Shoot_Ar":
                return idleStateName;
            case "WalkFront_Shoot_AR":
                return walkStateName;
            case "WalkBack_Shoot_AR":
                return walkBackStateName;
            case "WalkLeft_Shoot_AR":
                return walkLeftStateName;
            case "WalkRight_Shoot_AR":
                return walkRightStateName;
            case "Run_guard_AR":
            case "Run_gunMiddle_AR":
                return runStateName;
            case "Jump":
                return jumpStateName;
            case "Shoot_AutoShot_AR":
            case "Shoot_Autoshot_AR":
            case "Shoot_BurstShot_AR":
            case "Shoot_SingleShot_AR":
                return shootStateName;
            case "Melee":
            case "Attack":
                return meleeStateName;
            case "GetHit":
                return hitStateName;
            case "Die":
                return dieStateName;
            default:
                return stateName;
        }
    }

    private string GetMovementStateName(Vector2 input)
    {
        if (sprinting && input.y > 0.5f && Mathf.Abs(input.x) < 0.5f)
        {
            return runStateName;
        }

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return input.x < 0f ? walkLeftStateName : walkRightStateName;
        }

        return input.y < 0f ? walkBackStateName : walkStateName;
    }

    private Vector3 GetControllerCenter(float height)
    {
        return new Vector3(0f, height * 0.5f, 0f);
    }

    private void FitControllerToCharacter()
    {
        if (controller == null)
        {
            return;
        }

        standingHeight = Mathf.Max(standingHeight, controller.height);
        controller.height = standingHeight;
        controller.center = GetControllerCenter(standingHeight);
    }
}
