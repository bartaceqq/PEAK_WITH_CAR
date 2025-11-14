using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    private int current_point = 0;
    public List<Transform> waypoints;
    public Animator enemy_animator;

    public int hp = 100;

    [Header("Player Settings")]
    public Transform player;
    public float detectionRange = 15f;
    public float shootingRange = 5f;
    public float chaseSpeed = 7f;      // running speed
    public float walkSpeed = 3f;       // walk speed for patrol

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

    private enum EnemyState
    {
        Walk,    // patrol
        Run,     // chase
        Shooting
    }

    private EnemyState currentState = EnemyState.Walk;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        SetState(EnemyState.Walk);
        UpdateAnimatorState();
        
    }

    void Update()
    {
        if (!player) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // ------- STATE SELECTION -------
        if (distanceToPlayer > detectionRange)
        {
            SetState(EnemyState.Walk);
        }
        else if (distanceToPlayer > shootingRange)
        {
            SetState(EnemyState.Run);
        }
        else
        {
            SetState(EnemyState.Shooting);
        }

        // ------- STATE BEHAVIOUR -------
        switch (currentState)
        {
            case EnemyState.Walk:
                PatrolRoute();
                break;

            case EnemyState.Run:
                ChasePlayer();
                break;

            case EnemyState.Shooting:
                moveDir = Vector3.zero;
                RotateTowards((player.position - transform.position).normalized);
                break;
        }

        ApplyGravity();
        // SAFETY FIX — enemy never gets stuck
        if (moveDir.sqrMagnitude < 0.1f && currentState != EnemyState.Shooting)
        {
            // Recalculate fallback movement
            if (currentState == EnemyState.Run)
                ChasePlayer();
            else if (currentState == EnemyState.Walk)
                PatrolRoute();
        }

        controller.Move((moveDir + velocity) * Time.deltaTime);
    }

    void SetState(EnemyState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        UpdateAnimatorState();
    }

    // -------------------------
    //  ANIMATIONS
    // -------------------------
    void UpdateAnimatorState()
    {
        if (!enemy_animator) return;

        // reset all three
        enemy_animator.SetBool("walking", false);
        enemy_animator.SetBool("moving", false);
        enemy_animator.SetBool("shooting", false);

        switch (currentState)
        {
           case EnemyState.Walk:
               enemy_animator.SetBool("walking", true);
           
               // Force restart walk animation
               enemy_animator.Play("forward", 0, 0f);
           
               break;


            case EnemyState.Run:
                // foreward -> Run01_Forward: walking = false, moving = true
                enemy_animator.SetBool("moving", true);
                // walking stays false
                break;

            case EnemyState.Shooting:
                enemy_animator.SetBool("shooting", true);
                break;
        }
    }

    // -------------------------
    //  PATROL (WALK)
    // -------------------------
    public void PatrolRoute()
    {
        if (waypoints.Count == 0) return;

        Transform wp = waypoints[current_point];

        float dist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(wp.position.x, 0, wp.position.z)
        );

        if (dist <= stopDistance)
        {
            current_point++;
            if (current_point >= waypoints.Count)
                current_point = 0;

            return;
        }

        Vector3 dir = (wp.position - transform.position).normalized;
        dir.y = 0;

        Vector3 adjustedDir = AvoidObstacles(dir);
        moveDir = adjustedDir * walkSpeed;   // walk speed

        RotateTowards(adjustedDir);
    }

    // -------------------------
    //  CHASE (RUN)
    // -------------------------
    void ChasePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;

        if (Vector3.Distance(transform.position, player.position) <= stopDistance)
        {
            moveDir = Vector3.zero;
            return;
        }

        Vector3 adjustedDir = AvoidObstacles(dirToPlayer);
        moveDir = adjustedDir * chaseSpeed;  // run speed
        RotateTowards(adjustedDir);
    }

    // -------------------------
    //  OBSTACLE AVOID
    // -------------------------
    Vector3 AvoidObstacles(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // forward ray
        if (Physics.Raycast(origin, transform.forward, out RaycastHit forwardHit, obstacleAvoidDistance, obstacleMask))
        {
            // Turn AWAY from the obstacle
            return (desiredDir + forwardHit.normal * 2f).normalized;
        }

        // left ray
        if (Physics.Raycast(origin, -transform.right, out RaycastHit leftHit, obstacleAvoidDistance * 0.7f, obstacleMask))
        {
            // turn right
            return (desiredDir + transform.right * 1.5f).normalized;
        }

        // right ray
        if (Physics.Raycast(origin, transform.right, out RaycastHit rightHit, obstacleAvoidDistance * 0.7f, obstacleMask))
        {
            // turn left
            return (desiredDir - transform.right * 1.5f).normalized;
        }

        return desiredDir.normalized;
    }


    // -------------------------
    //  ROTATION
    // -------------------------
    void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    }

    // -------------------------
    //  GRAVITY
    // -------------------------
    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
    }
}
