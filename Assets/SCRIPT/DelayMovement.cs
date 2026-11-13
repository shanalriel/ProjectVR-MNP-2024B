using UnityEngine;
using UnityEngine.AI;

public class DelayMovement : MonoBehaviour
{
    [Header("NavMesh & Area")]
    public NavMeshAgent agent;
    public Transform centrePoint;
    public float range = 10f;

    [Header("Deteksi Objek")]
    public Transform targetObject;
    public float detectionRadius = 8f;

    [Header("Pengaturan Kecepatan")]
    public float walkSpeed = 1.5f;   // Kecepatan saat berjalan santai
    public float runSpeed = 4.5f;    // Kecepatan saat mengejar bola

    [Header("Pengaturan Delay / Jeda")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    [Header("Komponen Animasi")]
    public Animator animator;

    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isChasing = false;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (agent != null && agent.isOnNavMesh)
        {
            MoveToRandomPoint();
        }
    }

    void Update()
    {
        if (centrePoint == null || agent == null || !agent.isOnNavMesh) return;

        // 1. Logika Mengejar Objek / Bola
        if (targetObject != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetObject.position);

            if (distanceToTarget <= detectionRadius)
            {
                isWaiting = false;
                isChasing = true;
                agent.speed = runSpeed; // Tingkatkan kecepatan NavMeshAgent ke kecepatan berlari
                agent.SetDestination(targetObject.position);

                // Jika anjing menyentuh / mengambil bola
                if (distanceToTarget <= agent.stoppingDistance + 0.5f)
                {
                    Destroy(targetObject.gameObject);
                    targetObject = null;
                    isChasing = false;
                    agent.speed = walkSpeed;

                    isWaiting = true;
                    waitTimer = Random.Range(minWaitTime, maxWaitTime);
                }

                UpdateAnimation();
                return;
            }
        }

        // 2. Jika tidak mengejar bola -> Kecepatan Jalan Santai
        isChasing = false;
        agent.speed = walkSpeed;

        // 3. Logika Jeda (Wait Timer) saat Wandering
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                MoveToRandomPoint();
            }
            UpdateAnimation();
            return;
        }

        // 4. Logika Sampai di Tujuan Acak
        if (!agent.pathPending && agent.hasPath)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        float currentSpeed = agent.velocity.magnitude;

        // Jika anjing diam / berhenti
        if (isWaiting || currentSpeed < 0.1f)
        {
            animator.SetFloat("Vert", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("State", 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            // Bergerak (Vert = 1 untuk mengaktifkan pergerakan)
            animator.SetFloat("Vert", 1f, 0.1f, Time.deltaTime);

            if (isChasing)
            {
                // Nilai State = 1 mengaktifkan animasi Run di Blend Tree
                animator.SetFloat("State", 1f, 0.15f, Time.deltaTime);
            }
            else
            {
                // Nilai State = 0 mengaktifkan animasi Walk di Blend Tree
                animator.SetFloat("State", 0f, 0.15f, Time.deltaTime);
            }
        }
    }

    void MoveToRandomPoint()
    {
        Vector3 point;
        if (RandomPoint(centrePoint.position, range, out point))
        {
            agent.SetDestination(point);
        }
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, range, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = Vector3.zero;
        return false;
    }
}