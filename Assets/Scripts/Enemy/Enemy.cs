using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(StateMachine))]
public class Enemy : MonoBehaviour
{
    private StateMachine stateMachine;
    private NavMeshAgent agent;
    private GameObject player;

    public NavMeshAgent Agent => agent;
    public GameObject Player => player;
    public StateMachine StateMachine => stateMachine;

    [Header("Patrol Settings")]
    public Path path;

    [Header("Sight Settings")]
    public float sightDistance = 20f;
    public float fieldOfView = 85f;
    public float eyeHeight = 1.6f;

    [Header("Weapon & Combat")]
    public Transform gunBarrel;
    public GameObject bulletPrefab;
    [Range(0.1f, 10f)]
    public float fireRate = 1.2f;

    [Header("Debug")]
    [SerializeField]
    private string currentState;

    public Vector3 LastKnownPosition { get; set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stateMachine = GetComponent<StateMachine>();
        if (GetComponent<EnemyHealth>() == null) gameObject.AddComponent<EnemyHealth>();
    }

    private void Start()
    {
        PlayerMotor playerMotor = Object.FindFirstObjectByType<PlayerMotor>();
        player = playerMotor != null ? playerMotor.gameObject : GameObject.FindGameObjectWithTag("Player");
        stateMachine.Initialise();
    }

    private void Update()
    {
        if (stateMachine != null && stateMachine.activeState != null) currentState = stateMachine.activeState.ToString();
    }

    public bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 eyePos = transform.position + (Vector3.up * eyeHeight);
        Vector3 targetPos = player.transform.position + Vector3.up;
        Vector3 directionToPlayer = targetPos - eyePos;
        float distanceToPlayer = directionToPlayer.magnitude;
        if (distanceToPlayer > sightDistance || Vector3.Angle(transform.forward, directionToPlayer) > fieldOfView * 0.5f) return false;
        return Physics.Raycast(eyePos, directionToPlayer.normalized, out RaycastHit hit, distanceToPlayer) &&
               (hit.transform == player.transform || hit.transform.IsChildOf(player.transform));
    }
}
