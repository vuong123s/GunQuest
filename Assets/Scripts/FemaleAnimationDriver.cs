using UnityEngine;

public class FemaleAnimationDriver : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

    [SerializeField] private Animator animator;
    private CharacterController controller;

    public void Configure(Animator target) => animator = target;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        Vector3 velocity = controller != null ? controller.velocity : Vector3.zero;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (HasParameter(SpeedHash)) animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
        if (HasParameter(IsGroundedHash)) animator.SetBool(IsGroundedHash, controller == null || controller.isGrounded);
        if (HasParameter(IsSprintingHash)) animator.SetBool(IsSprintingHash, speed > 6f);
    }

    private bool HasParameter(int hash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == hash) return true;
        return false;
    }
}
