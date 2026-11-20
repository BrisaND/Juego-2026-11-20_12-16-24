using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Stunned }

    [Header("Estado Actual")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;

    [Header("Patrullaje por Todo el Mapa")]
    [Tooltip("Si está activo, usa automáticamente el tamaño completo del Terrain de la escena")]
    [SerializeField] private bool useTerrainBounds = true;
    [Tooltip("Usado solo si no hay un Terrain activo en la escena")]
    [SerializeField] private Transform mapCenter;
    [SerializeField] private float fallbackMapRadius = 100f;

    [Header("Búsqueda Activa del Jugador")]
    [Range(0f, 1f)]
    [SerializeField] private float searchPlayerChance = 0.45f;
    [SerializeField] private float searchSectorRadius = 30f;

    [Header("Velocidades y Tiempos")]
    [SerializeField] private float patrolSpeed = 2.8f;
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float waitTimeAtPatrolPoint = 1.5f;

    [Header("Detección y Captura del Jugador")]
    [SerializeField] private float detectionRadius = 14f;
    [SerializeField] private float losePlayerRadius = 20f;
    [SerializeField] private float catchDistance = 1.6f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Configuración de Stun (Linterna)")]
    [SerializeField] private Light flashlight;
    [SerializeField] private float maxFlashlightDistance = 20f;
    [SerializeField] private float stunDuration = 3.5f;
    [SerializeField] private float stunCooldown = 2f;
    [SerializeField] private float targetHeightOffset = 1.2f;

    [Header("Audio 3D del Enemigo")]
    [SerializeField] private AudioSource enemyAudioSource;
    [SerializeField] private AudioClip[] walkFootstepClips;
    [SerializeField] private AudioClip[] chaseFootstepClips;
    [SerializeField] private AudioClip[] stunPainClips;

    [Header("Ritmo de Pisadas Pesadas")]
    [SerializeField] private float walkStepInterval = 0.75f;
    [SerializeField] private float chaseStepInterval = 0.40f;
    [SerializeField] private float minStepPitch = 0.82f;
    [SerializeField] private float maxStepPitch = 0.96f;

    private Transform player;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private Collider monsterCollider;

    private float waitTimer;
    private float stuckTimer;
    private float cooldownTimer;
    private float stepTimer;
    private bool isStunned;
    private bool hasCaughtPlayer;

    private static readonly int StateHash = Animator.StringToHash("State");
    private static readonly int StunTriggerHash = Animator.StringToHash("StunTrigger");

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        monsterCollider = GetComponent<Collider>();

        if (enemyAudioSource == null)
        {
            enemyAudioSource = GetComponent<AudioSource>();
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            if (flashlight == null)
            {
                flashlight = p.GetComponentInChildren<Light>();
            }
        }
    }

    private void Start()
    {
        SetState(EnemyState.Patrol);
        GetNextPatrolPoint();
    }

    private void Update()
    {
        if (player == null || hasCaughtPlayer) return;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (!isStunned && cooldownTimer <= 0 && IsLitByFlashlight())
        {
            StartCoroutine(StunRoutine());
            return;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrolState();
                HandleFootsteps(false);
                break;

            case EnemyState.Chase:
                HandleChaseState();
                HandleFootsteps(true);
                break;

            case EnemyState.Stunned:
                break;
        }
    }

    private void HandlePatrolState()
    {
        navMeshAgent.speed = patrolSpeed;
        navMeshAgent.isStopped = false;

        if (CanSeePlayer())
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.3f)
        {
            stuckTimer = 0f;
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPatrolPoint)
            {
                GetNextPatrolPoint();
                waitTimer = 0f;
            }
        }
        else if (!navMeshAgent.pathPending && navMeshAgent.velocity.sqrMagnitude < 0.05f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= 2f)
            {
                GetNextPatrolPoint();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void HandleChaseState()
    {
        navMeshAgent.speed = chaseSpeed;
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(player.position);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!isStunned && distanceToPlayer <= catchDistance)
        {
            hasCaughtPlayer = true;
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            return;
        }

        if (distanceToPlayer > losePlayerRadius)
        {
            SetState(EnemyState.Patrol);
            GetNextPatrolPoint();
        }
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        SetState(EnemyState.Stunned);

        navMeshAgent.isStopped = true;
        navMeshAgent.velocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetTrigger(StunTriggerHash);
        }

        PlayStunPainSound();

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
        cooldownTimer = stunCooldown;

        if (CanSeePlayer())
        {
            SetState(EnemyState.Chase);
        }
        else
        {
            SetState(EnemyState.Patrol);
            GetNextPatrolPoint();
        }
    }

    private void HandleFootsteps(bool isChasing)
    {
        if (enemyAudioSource == null || navMeshAgent == null) return;

        // Solo reproduce pasos si el monstruo está desplazándose realmente
        if (navMeshAgent.velocity.sqrMagnitude < 0.15f)
        {
            stepTimer = 0.15f;
            return;
        }

        float interval = isChasing ? chaseStepInterval : walkStepInterval;
        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            AudioClip[] clips = isChasing ? chaseFootstepClips : walkFootstepClips;
            if (clips != null && clips.Length > 0)
            {
                AudioClip chosenClip = clips[Random.Range(0, clips.Length)];
                if (chosenClip != null)
                {
                    // Tono levemente más grave para reforzar el peso del monstruo
                    enemyAudioSource.pitch = Random.Range(minStepPitch, maxStepPitch);
                    enemyAudioSource.PlayOneShot(chosenClip);
                }
            }

            stepTimer = interval;
        }
    }

    private void PlayStunPainSound()
    {
        if (enemyAudioSource == null || stunPainClips == null || stunPainClips.Length == 0) return;

        AudioClip chosenClip = stunPainClips[Random.Range(0, stunPainClips.Length)];
        if (chosenClip != null)
        {
            enemyAudioSource.pitch = Random.Range(0.95f, 1.05f);
            enemyAudioSource.PlayOneShot(chosenClip);
        }
    }

    private void SetState(EnemyState newState)
    {
        currentState = newState;
        if (animator != null)
        {
            animator.SetInteger(StateHash, (int)newState);
        }
    }

    private void GetNextPatrolPoint()
    {
        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < 12; i++)
        {
            Vector3 candidatePoint = GetRandomForestPoint();

            if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit navHit, 8f, NavMesh.AllAreas))
            {
                if (navMeshAgent.CalculatePath(navHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    navMeshAgent.SetDestination(navHit.position);
                    return;
                }
            }
        }
    }

    private Vector3 GetRandomForestPoint()
    {
        if (player != null && Random.value < searchPlayerChance)
        {
            Vector2 randomCircle = Random.insideUnitCircle * searchSectorRadius;
            return new Vector3(player.position.x + randomCircle.x, player.position.y, player.position.z + randomCircle.y);
        }

        Terrain terrain = Terrain.activeTerrain;
        if (useTerrainBounds && terrain != null)
        {
            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            float margin = 5f;

            float randomX = Random.Range(terrainPos.x + margin, terrainPos.x + terrainSize.x - margin);
            float randomZ = Random.Range(terrainPos.z + margin, terrainPos.z + terrainSize.z - margin);
            float y = terrain.SampleHeight(new Vector3(randomX, 0f, randomZ)) + terrainPos.y;

            return new Vector3(randomX, y, randomZ);
        }

        Vector3 center = (mapCenter != null) ? mapCenter.position : Vector3.zero;
        Vector2 fallbackCircle = Random.insideUnitCircle * fallbackMapRadius;
        return new Vector3(center.x + fallbackCircle.x, center.y, center.z + fallbackCircle.y);
    }

    private bool CanSeePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRadius) return false;

        Vector3 rayOrigin = transform.position + (Vector3.up * targetHeightOffset);
        Vector3 rayDirection = (player.position - rayOrigin).normalized;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, detectionRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.CompareTag("Player") || hit.transform.IsChildOf(player))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsLitByFlashlight()
    {
        if (flashlight == null || !flashlight.enabled || !flashlight.gameObject.activeInHierarchy)
            return false;

        Vector3 targetPosition = (monsterCollider != null)
            ? monsterCollider.bounds.center
            : transform.position + (Vector3.up * targetHeightOffset);

        float effectiveRange = maxFlashlightDistance > 0 ? maxFlashlightDistance : flashlight.range;
        float distanceToMonster = Vector3.Distance(flashlight.transform.position, targetPosition);

        if (distanceToMonster > effectiveRange)
            return false;

        Vector3 directionToEnemy = (targetPosition - flashlight.transform.position).normalized;
        float dotProduct = Vector3.Dot(flashlight.transform.forward, directionToEnemy);

        float angleThreshold = (flashlight.type == LightType.Spot)
            ? Mathf.Cos((flashlight.spotAngle * 0.5f) * Mathf.Deg2Rad)
            : 0.5f;

        if (dotProduct > angleThreshold)
        {
            Vector3 rayOrigin = flashlight.transform.position;
            Vector3 rayDirection = targetPosition - rayOrigin;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, effectiveRange, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}