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
    public string idleStateName = "Idle_Guard_AR";
    public string walkStateName = "WalkFront_Shoot_AR";
    public string runStateName = "Run_guard_AR";
    public string jumpStateName = "Jump";
    public float animationFadeDuration = 0.1f;
    public float animationDampTime = 0.1f;
    public float sprintAnimationSpeed = 1.35f;
    public float walkAnimationSpeed = 1f;

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
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // Di chuyển nhân vật dựa trên hướng nhìn [8]
        float currentSpeed = sprinting && !crouching ? sprintSpeed : speed;
        controller.Move(transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime);
        currentMoveSpeed = moveDirection.magnitude * currentSpeed;
        UpdateMovementAnimator(input, currentSpeed);

        // Áp dụng trọng lực [6, 7]
        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Giữ nhân vật bám trên mặt đất
        }
        controller.Move(playerVelocity * Time.deltaTime);
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
        float speed01 = maxSpeed <= 0f ? 0f : currentMoveSpeed / maxSpeed;
        bool isMoving = input.sqrMagnitude > 0.01f;
        float animationSpeed = sprinting && isMoving ? sprintAnimationSpeed : walkAnimationSpeed;
        animatorSprinting = sprinting && isMoving && input.y > 0.5f && Mathf.Abs(input.x) < 0.5f;

        SetAnimatorFloat(MoveXHash, input.x, animationDampTime);
        SetAnimatorFloat(MoveYHash, input.y, animationDampTime);
        SetAnimatorFloat(SpeedHash, isMoving ? animationSpeed : 1f, animationDampTime);
        SetAnimatorBool(IsMovingHash, isMoving);
        SetAnimatorBool(IsSprintingHash, animatorSprinting);
        UpdateDirectAnimatorState(speed01);
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

    private void UpdateDirectAnimatorState(float speed01)
    {
        if (animator == null || animatorParameters.Count > 0 || jumpAnimationActive || !isGrounded)
        {
            return;
        }

        if (speed01 <= 0.05f)
        {
            PlayAnimatorState(idleStateName);
            return;
        }

        PlayAnimatorState(sprinting ? runStateName : walkStateName);
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
        return stateName == "Shoot_AutoShot_AR" ? "Shoot_Autoshot_AR" : stateName;
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
