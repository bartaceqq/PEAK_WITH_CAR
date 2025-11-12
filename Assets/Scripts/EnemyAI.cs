using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public Animator enemy_animator;
    public int hp = 100;

    [Header("Player Settings")]
    public Transform player;
    public float detectionRange = 15f;
    public float shootingRange = 5f;
    public float chaseSpeed = 5f;

    [Header("Avoidance Settings")]
    public float obstacleAvoidDistance = 1.5f;
    public float avoidTurnSpeed = 3f;
    public LayerMask obstacleMask;

    [Header("Behaviour Settings")]
    public float gravity = -9.81f;
    public float stopDistance = 1.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDir;

    private enum EnemyState { Idle, Chasing, Shooting }
    private EnemyState currentState = EnemyState.Idle;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        SetState(EnemyState.Idle);
    }

    void Update()
    {
        if (!player) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- STATE DECISION ---
        if (distanceToPlayer > detectionRange)
            SetState(EnemyState.Idle);
        else if (distanceToPlayer > shootingRange)
            SetState(EnemyState.Chasing);
        else
            SetState(EnemyState.Shooting);

        // --- STATE ACTIONS ---
        switch (currentState)
        {
            case EnemyState.Idle:
                moveDir = Vector3.zero;
                break;

            case EnemyState.Chasing:
                ChasePlayer();
                break;

            case EnemyState.Shooting:
                moveDir = Vector3.zero;
                RotateTowards((player.position - transform.position).normalized);
                break;
        }

        ApplyGravity();
        controller.Move((moveDir + velocity) * Time.deltaTime);
    }

    // ======================
    // == STATE MANAGEMENT ==
    // ======================
    void SetState(EnemyState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        UpdateAnimatorState();
    }

    void UpdateAnimatorState()
    {
        if (!enemy_animator) return;

        // Reset all
        enemy_animator.SetBool("moving", false);
        enemy_animator.SetBool("shooting", false);

        switch (currentState)
        {
            case EnemyState.Chasing:
                enemy_animator.SetBool("moving", true);
                break;
            case EnemyState.Shooting:
                enemy_animator.SetBool("shooting", true);
                break;
        }
    }

    // ======================
    // == MOVEMENT LOGIC ====
    // ======================
    void ChasePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        if (Vector3.Distance(transform.position, player.position) <= stopDistance)
        {
            moveDir = Vector3.zero;
            return;
        }

        Vector3 adjustedDir = AvoidObstacles(dirToPlayer);
        moveDir = adjustedDir * chaseSpeed;
        RotateTowards(adjustedDir);
    }

    Vector3 AvoidObstacles(Vector3 desiredDir)
    {
        Ray forwardRay = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);
        if (Physics.Raycast(forwardRay, out RaycastHit hit, obstacleAvoidDistance, obstacleMask))
        {
            Vector3 avoidDir = Vector3.Cross(hit.normal, Vector3.up);
            desiredDir = Vector3.Lerp(desiredDir, avoidDir, Time.deltaTime * avoidTurnSpeed);
        }
        return desiredDir.normalized;
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
    }

    // ======================
    // == DEBUG GIZMOS ======
    // ======================
    private void OnDrawGizmosSelected()
    {
        if (currentState == EnemyState.Chasing) Gizmos.color = Color.red;
        else if (currentState == EnemyState.Shooting) Gizmos.color = Color.green;
        else Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
}
