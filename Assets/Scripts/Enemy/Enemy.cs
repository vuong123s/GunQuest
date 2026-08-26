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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stateMachine = GetComponent<StateMachine>();
    }

    void Start()
    {
        // Find player in scene if not assigned
        PlayerMotor playerMotor = Object.FindFirstObjectByType<PlayerMotor>();
        if (playerMotor != null)
        {
            player = playerMotor.gameObject;
        }
        else
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        stateMachine.Initialise();
    }

    void Update()
    {
        if (stateMachine != null && stateMachine.activeState != null)
        {
            currentState = stateMachine.activeState.ToString();
        }
    }

    public bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 eyePos = transform.position + (Vector3.up * eyeHeight);
        Vector3 targetPos = player.transform.position + Vector3.up; // Aim towards torso
        Vector3 directionToPlayer = targetPos - eyePos;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Check if within sight distance
        if (distanceToPlayer <= sightDistance)
        {
            // Check field of view angle
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            if (angleToPlayer <= fieldOfView * 0.5f)
            {
                // Raycast to check for line of sight blockage (walls, obstacles)
                Ray ray = new Ray(eyePos, directionToPlayer.normalized);
                if (Physics.Raycast(ray, out RaycastHit hit, sightDistance))
                {
                    if (hit.transform == player.transform || hit.transform.IsChildOf(player.transform))
                    {
                        Debug.DrawRay(ray.origin, ray.direction * distanceToPlayer, Color.red);
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
