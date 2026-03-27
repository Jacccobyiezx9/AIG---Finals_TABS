using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class TabsAI : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Transform[] patrolPoints;

    [Header("Status")]
    public float health = 100f;
    public bool isDead = false;
    public Team myTeam;
    public Team targetTeam;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float loseRange = 14f;

    [Header("Patrol")]
    public float waypointTolerance = 1.0f;
    public float idleAtWaypointSeconds = 1.0f;

    [Header("Combat")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime = -Mathf.Infinity;

    private Node root;
    private NavMeshAgent agent;
    private Animator anim;
    private Collider col;
    private float idleTimer;
    private bool isChasing;

    private enum NodeState { Success, Failure, Running }
    public enum Team { Human, Monster }

    private abstract class Node { public abstract NodeState Tick(); }

    private class Selector : Node
    {
        private readonly List<Node> children;
        public Selector(List<Node> children) => this.children = children;
        public override NodeState Tick()
        {
            foreach (var child in children)
            {
                var state = child.Tick();
                if (state == NodeState.Success) return NodeState.Success;
                if (state == NodeState.Running) return NodeState.Running;
            }
            return NodeState.Failure;
        }
    }

    private class Sequence : Node
    {
        private readonly List<Node> children;
        public Sequence(List<Node> children) => this.children = children;
        public override NodeState Tick()
        {
            foreach (var child in children)
            {
                var state = child.Tick();
                if (state == NodeState.Failure) return NodeState.Failure;
                if (state == NodeState.Running) return NodeState.Running;
            }
            return NodeState.Success;
        }
    }

    private class ActionNode : Node
    {
        private readonly Func<NodeState> action;
        public ActionNode(Func<NodeState> action) => this.action = action;
        public override NodeState Tick() => action();
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider>();
    }

    private void Start()
    {
        // Root: Chase -> Patrol -> Idle
        var combatSequence = new Sequence(new List<Node>
        {
            new ActionNode(DetectTarget),
            new ActionNode(ChaseTarget),
            new ActionNode(AttackTarget)
        });

        var patrolSequence = new Sequence(new List<Node>
        {
            new ActionNode(HasPatrolPoints),
            new ActionNode(Patrol)
        });

        var idleAction = new ActionNode(Idle);
        var deathAction = new ActionNode(Die);


        root = new Selector(new List<Node>
        {
            deathAction,
            combatSequence,
            patrolSequence,
            idleAction,
        });
    }

    private void Update()
    {
        if (isDead) return;
        root.Tick();
    }

    // Starts chase when inside detectionRange, keeps chasing until beyond loseRange
    private NodeState DetectTarget()
    {
        if (target != null)
        {
            var targetAI = target.GetComponent<TabsAI>();
            if (targetAI != null && !targetAI.isDead && Vector3.Distance(transform.position, target.position) <= loseRange)
                return NodeState.Success;

            target = null;
            isChasing = false;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            var ai = hit.GetComponent<TabsAI>();
            if (ai == null || ai.isDead || ai.myTeam != targetTeam) continue;

            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < closestDistance)
            {
                closestDistance = d;
                closest = hit.transform;
            }
        }

        if (closest != null)
        {
            target = closest;
            return NodeState.Success;
        }

        return NodeState.Failure;
    }

    private NodeState ChaseTarget()
    {
        if (target == null) return NodeState.Failure;

        float d = Vector3.Distance(transform.position, target.position);

        if (d <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            anim.SetBool("Running", false);
            return NodeState.Success;
        }

        agent.isStopped = false;
        agent.SetDestination(target.position);
        anim.SetBool("Running", true);
        return NodeState.Running;
    }

    private NodeState AttackTarget()
    {
        if (target == null) return NodeState.Failure;

        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.forward = dir;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            anim.SetTrigger("Attack");
            lastAttackTime = Time.time;
            return NodeState.Success;
        }

        return NodeState.Running;
    }
    private NodeState HasPatrolPoints()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return NodeState.Failure;
        return NodeState.Success;
    }

    // Moves through patrol points and pauses briefly at each one
    private NodeState Patrol()
    {
        if (target != null) return NodeState.Failure;

        Transform current = patrolPoints[0];

        if (idleTimer > 0f)
        {
            agent.isStopped = true;
            anim.SetBool("Running", false);
            idleTimer -= Time.deltaTime;
            return NodeState.Running;
        }

        agent.isStopped = false;
        anim.SetBool("Running", true);
        agent.SetDestination(current.position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            anim.SetBool("Running", false);
            idleTimer = idleAtWaypointSeconds;
        }
        return NodeState.Running;
    }

    private NodeState Idle()
    {
        agent.isStopped = true;
        anim.SetBool("Running", false);
        return NodeState.Running;
    }

    private NodeState Die()
    {
        if (health > 0) return NodeState.Failure;

        if (!isDead)
        {
            isDead = true;
            anim.SetTrigger("Dead");

            if (col != null) col.enabled = false;
            agent.isStopped = true;
            agent.enabled = false;
            target = null;

        }
        return NodeState.Success;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        health -= damage;
    }

    // Visualize detection and lose ranges in the Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}