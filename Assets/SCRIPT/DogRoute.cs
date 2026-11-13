using UnityEngine;
using UnityEngine.AI;

public class DogRoute : MonoBehaviour
{
    [Header("NavMesh & Rute Waypoint")]
    public NavMeshAgent agent;
    public Transform[] waypoints;
    public bool loopRoute = true;

    [Header("Interaksi Bola & Player VR")]
    public Transform targetObject;
    public float detectionRadius = 15f;
    public Transform mouthPoint;              // Titik mulut anjing
    public Transform vrPlayer;                // Isi dengan Main Camera dari XR Origin
    public float dropDistanceFromPlayer = 1.2f; // Jarak berhenti di depan pemain saat mengembalikan bola
    [HideInInspector] public bool isHeldByPlayer = false;

    [Header("Pengaturan Kecepatan")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4.5f;

    [Header("Pengaturan Delay / Jeda")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 4f;

    [Header("Komponen Animasi")]
    public Animator animator;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isChasing = false;
    private bool isCarryingBall = false;
    private Transform carriedBall;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Otomatis mencari Main Camera (kepala pemain VR) jika belum diisi di Inspector
        if (vrPlayer == null && Camera.main != null)
            vrPlayer = Camera.main.transform;

        if (agent != null && agent.isOnNavMesh && waypoints.Length > 0)
        {
            MoveToCurrentWaypoint();
        }
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Jika pemain VR langsung mengambil bola dari mulut anjing saat sedang dibawa
        if (isCarryingBall && (carriedBall == null || isHeldByPlayer))
        {
            isCarryingBall = false;
            carriedBall = null;
        }

        // 1. MODE MEMBAWA BOLA KEMBALI KE PLAYER VR
        if (isCarryingBall && carriedBall != null)
        {
            isWaiting = false;
            agent.speed = runSpeed;

            if (vrPlayer != null)
            {
                agent.SetDestination(vrPlayer.position);

                // Hitung jarak mendatar (sumbu X dan Z) antara anjing dan pemain VR
                Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 flatPlayerPos = new Vector3(vrPlayer.position.x, 0f, vrPlayer.position.z);
                float distToPlayer = Vector3.Distance(flatDogPos, flatPlayerPos);

                // Jatuhkan bola tepat di depan pemain VR
                if (distToPlayer <= dropDistanceFromPlayer)
                {
                    DropBall();
                }
            }
            else
            {
                DropBall();
            }

            UpdateAnimation();
            return;
        }

        // 2. MODE MENGEJAR BOLA
        if (targetObject != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetObject.position);

            if (distanceToTarget <= detectionRadius)
            {
                isWaiting = false;
                isChasing = true;
                agent.speed = runSpeed;
                agent.SetDestination(targetObject.position);

                // Gigit bola jika sudah dilepas/dilempar pemain VR dan jaraknya sudah dekat
                if (!isHeldByPlayer && distanceToTarget <= agent.stoppingDistance + 0.5f)
                {
                    PickUpBall(targetObject);
                }

                UpdateAnimation();
                return;
            }
        }

        // 3. MODE PATROLI RUTE WAYPOINT
        isChasing = false;
        agent.speed = walkSpeed;

        if (waypoints == null || waypoints.Length == 0)
        {
            UpdateAnimation();
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                MoveToCurrentWaypoint();
            }
            UpdateAnimation();
            return;
        }

        if (!agent.pathPending && agent.hasPath)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
                AdvanceWaypointIndex();
            }
        }

        UpdateAnimation();
    }

    void PickUpBall(Transform ball)
    {
        carriedBall = ball;
        targetObject = null;
        isChasing = false;
        isCarryingBall = true;

        Rigidbody ballRb = carriedBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.isKinematic = true;
            ballRb.useGravity = false;
        }

        // Ubah menjadi Trigger sementara agar tidak mendorong anjing, namun tetap bisa diambil tangan VR
        Collider ballCol = carriedBall.GetComponent<Collider>();
        if (ballCol != null)
        {
            ballCol.isTrigger = true;
        }

        Transform attachTarget = mouthPoint != null ? mouthPoint : transform;
        carriedBall.SetParent(attachTarget);
        carriedBall.localPosition = Vector3.zero;
    }

    void DropBall()
    {
        if (carriedBall != null)
        {
            carriedBall.SetParent(null);

            Collider ballCol = carriedBall.GetComponent<Collider>();
            if (ballCol != null)
            {
                ballCol.isTrigger = false;
            }

            Rigidbody ballRb = carriedBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = false;
                ballRb.useGravity = true;
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }

            carriedBall = null;
        }

        isCarryingBall = false;
        isHeldByPlayer = false;
        agent.speed = walkSpeed;

        // Diam sejenak di depan pemain sebelum kembali ke rute patroli
        isWaiting = true;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void MoveToCurrentWaypoint()
    {
        if (waypoints.Length == 0 || waypoints[currentWaypointIndex] == null) return;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    void AdvanceWaypointIndex()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = loopRoute ? 0 : waypoints.Length - 1;
        }
    }

    void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        float currentSpeed = agent.velocity.magnitude;

        if (isWaiting || currentSpeed < 0.1f)
        {
            animator.SetFloat("Vert", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("State", 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Vert", 1f, 0.1f, Time.deltaTime);
            bool isRunning = isChasing || isCarryingBall;
            animator.SetFloat("State", isRunning ? 1f : 0f, 0.15f, Time.deltaTime);
        }
    }
}