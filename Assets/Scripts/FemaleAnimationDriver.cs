using UnityEngine;

// Bridges existing gameplay to the Female Humanoid controller without changing weapon logic.
[DefaultExecutionOrder(100)]
public class FemaleAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    private PlayerWeapons weapons;
    private PlayerHealth health;
    private PlayerLook look;
    public Animator CharacterAnimator => characterAnimator;

    private void Start()
    {
        weapons = GetComponent<PlayerWeapons>();
        health = GetComponent<PlayerHealth>();
        look = GetComponent<PlayerLook>();
        if (weapons != null) { weapons.Changed += RefreshWeapon; weapons.Attacked += OnAttack; }
        RefreshWeapon();
    }

    private void RefreshWeapon()
    {
        if (characterAnimator == null || weapons == null || weapons.Current == null) return;
        if (characterAnimator.isInitialized && characterAnimator.layerCount > 1)
        {
            characterAnimator.ResetTrigger("Reload");
            characterAnimator.ResetTrigger("Shoot");
            characterAnimator.Play("Ready", 1, 0f);
        }
        characterAnimator.SetBool("IsRifle", weapons.Current.automatic);
        characterAnimator.SetBool("IsMeleeWeapon", weapons.Current.melee);
    }

    private void OnAttack()
    {
        if (characterAnimator != null && weapons.Current != null && !weapons.Current.melee)
            characterAnimator.SetTrigger("Shoot");
    }

    private void Update()
    {
        if (characterAnimator == null) return;
        bool dead = health != null && health.IsDead; characterAnimator.SetFloat("MoveRate", characterAnimator.GetBool("IsCrouching") ? 0.5f : characterAnimator.GetBool("IsSprinting") ? 1.35f : 0.85f);
        characterAnimator.SetBool("IsDead", dead);
        // Whole-body actions must not be overwritten by upper-body gun animation.
        var state = characterAnimator.GetCurrentAnimatorStateInfo(0);
        bool action = state.IsTag("FullBody");
        if (characterAnimator.layerCount > 1)
            characterAnimator.SetLayerWeight(1, dead || action || (weapons != null && weapons.Current != null && weapons.Current.melee) ? 0f : 1f);
        if (look != null) characterAnimator.SetFloat("AimPitch", -look.Pitch / 85f);
    }

    // Optional animation-only actions; dash/roll movement and knockdown gameplay are not invented here.
    public void PlayAction(string trigger)
    {
        if (characterAnimator == null || (health != null && health.IsDead)) return;
        foreach (var p in characterAnimator.parameters)
            if (p.name == trigger && p.type == AnimatorControllerParameterType.Trigger)
            { characterAnimator.SetTrigger(trigger); return; }
        Debug.LogWarning("Unknown Female animation trigger: " + trigger, this);
    }

    private void OnDestroy()
    {
        if (weapons != null) { weapons.Changed -= RefreshWeapon; weapons.Attacked -= OnAttack; }
    }
}
