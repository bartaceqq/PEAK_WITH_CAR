using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;
    public float detectionRange = 10f;
    public float chaseSpeed = 5f;
    public float idleSpeed = 2f;

    [Header("Avoidance Settings")]
    public float obstacleAvoidDistance = 1.5f;   // how far to check for walls
    public float avoidTurnSpeed = 3f;            // how fast to steer around walls
    public LayerMask obstacleMask;               // which layers are obstacles

    [Header("Behaviour")]
    public float gravity = -9.81f;
    public float stopDistance = 1.5f;            // distance to stop from player

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveDir;
    private bool isChasing = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!player) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isChasing = distanceToPlayer < detectionRange;

        if (isChasing)
            ChasePlayer();
        else
            Wander();

        ApplyGravity();
        controller.Move((moveDir + velocity) * Time.deltaTime);
    }

    void ChasePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        // Stop if too close
        if (Vector3.Distance(transform.position, player.position) <= stopDistance)
        {
            moveDir = Vector3.zero;
            return;
        }

        // Avoid walls
        Vector3 adjustedDir = AvoidObstacles(dirToPlayer);

        // Move & rotate smoothly
        moveDir = adjustedDir * chaseSpeed;
        RotateTowards(adjustedDir);
    }

    void Wander()
    {
        // Simple idle rotation (could be replaced with patrol)
        moveDir = Vector3.zero;
    }

    Vector3 AvoidObstacles(Vector3 desiredDir)
    {
        Ray forwardRay = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);
        if (Physics.Raycast(forwardRay, out RaycastHit hit, obstacleAvoidDistance, obstacleMask))
        {
            // When hit a wall, try to steer around it by turning a bit to the side
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

    private void OnDrawGizmosSelected()
    {
        // Visualize detection radius
        Gizmos.color = isChasing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize obstacle check
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * obstacleAvoidDistance);
    }
}
    