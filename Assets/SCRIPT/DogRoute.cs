using UnityEngine;
using UnityEngine.AI;

public class DogRoute : MonoBehaviour
{
    [Header("NavMesh & Rute Waypoint")]
    public NavMeshAgent agent;
    public Transform[] waypoints;
    [Tooltip("ID animasi tiap titik (0 = Idle biasa, 1 = Animasi 1, 2 = Animasi 2, dst.)")]
    public int[] waypointIdleTypes;
    [Tooltip("Durasi diam (detik) tiap titik. Jika diisi 0, akan memakai nilai acak Min/Max Wait Time.")]
    public float[] waypointWaitTimes;
    public bool loopRoute = true;

    [Header("Interaksi Bola & Player VR")]
    public Transform targetObject;
    public float detectionRadius = 15f;
    public Transform mouthPoint;
    public Transform vrPlayer;
    public float dropDistanceFromPlayer = 1.2f;
    public float waitDistanceFromPlayer = 1.5f;
    public int holdingBallIdleType = 0;
    [HideInInspector] public bool isHeldByPlayer = false;

    [Header("Interaksi Tempat Makan & Sistem Lapar")]
    public VRFoodBowl foodBowl;
    [Tooltip("Jarak berhenti anjing di pinggir tempat makan")]
    public float eatStopDistance = 0.85f;
    [Tooltip("Durasi minimum anjing menghabiskan makanan (detik)")]
    public float minEatDuration = 5f;
    [Tooltip("Durasi maksimum anjing menghabiskan makanan (detik)")]
    public float maxEatDuration = 10f;
    [Tooltip("ID animasi saat anjing menunggu bowl diisi penuh (0 = Idle biasa, 1 = Duduk, dst.)")]
    public int waitingFoodIdleType = 0;
    [Tooltip("Jeda waktu minimum (detik) sebelum anjing lapar kembali (180 = 3 menit)")]
    public float minHungerCooldown = 180f;
    [Tooltip("Jeda waktu maksimum (detik) sebelum anjing lapar kembali (300 = 5 menit)")]
    public float maxHungerCooldown = 300f;

    [Header("Interaksi Elus (Petting)")]
    [Tooltip("ID animasi saat anjing dielus (0 = Idle biasa, 1 = Duduk, dst.)")]
    public int petIdleType = 0;
    [Tooltip("Waktu jeda diam (detik) setelah selesai dielus sebelum berjalan lagi")]
    public float postPetWaitTime = 1.5f;

    [Header("Interaksi Whistle (Panggil Anjing)")]
    [Tooltip("Jarak berhenti anjing di depan pemain saat dipanggil")]
    public float whistleStopDistance = 1.5f;
    [Tooltip("Durasi anjing diam di depan pemain setelah dipanggil (detik)")]
    public float whistleWaitDuration = 5f;
    [Tooltip("ID animasi saat anjing diam setelah dipanggil (0 = Idle biasa, 1 = Duduk, dst.)")]
    public int whistleIdleType = 0;

    [Header("Pengaturan Kecepatan")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 4.5f;

    [Header("Pengaturan Delay Cadangan (Jika Wait Time Titik = 0)")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 4f;

    [Header("Komponen Animasi")]
    public Animator animator;

    private int currentWaypointIndex = 0;
    private int currentIdleType = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isChasing = false;
    private bool isCarryingBall = false;
    private Transform carriedBall;

    private bool isHungry = true;
    private float hungerTimer = 0f;
    private bool isApproachingBowl = false;
    private bool isEating = false;
    private float eatTimer = 0f;
    private float currentEatDuration = 5f;

    private bool isBeingPetted = false;
    private bool wasBeingPetted = false;

    private bool isCalledByWhistle = false;
    private bool isWaitingAtWhistle = false;
    private float whistleTimer = 0f;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (vrPlayer == null && Camera.main != null)
            vrPlayer = Camera.main.transform;

        if (foodBowl == null)
            foodBowl = FindFirstObjectByType<VRFoodBowl>();

        isHungry = true;

        if (agent != null && agent.isOnNavMesh && waypoints.Length > 0)
        {
            MoveToCurrentWaypoint();
        }
    }

    public void SetBeingPetted(bool petted)
    {
        isBeingPetted = petted;
    }

    public void OnWhistleCalled()
    {
        if (isCarryingBall || isEating || isHeldByPlayer) return;

        isCalledByWhistle = true;
        isWaitingAtWhistle = false;
        isWaiting = false;
        currentIdleType = 0;
    }

    public void NotifyFoodPouring(Transform bowlTransform, VRFoodBowl bowlScript)
    {
        if (foodBowl == null && bowlScript != null)
            foodBowl = bowlScript;

        if (!isHungry || isCarryingBall || isEating) return;

        isApproachingBowl = true;
    }

    public void NotifyFoodFull()
    {
        if (!isHungry || isCarryingBall || isEating) return;

        isApproachingBowl = true;
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Hitung mundur jeda lapar (3-5 menit setelah selesai makan)
        if (!isHungry)
        {
            hungerTimer -= Time.deltaTime;
            if (hungerTimer <= 0f)
            {
                isHungry = true;

                if (foodBowl != null && foodBowl.currentFill > 0f)
                {
                    isApproachingBowl = true;
                }
            }
        }

        if (isCarryingBall && (carriedBall == null || isHeldByPlayer))
        {
            isCarryingBall = false;
            carriedBall = null;
        }

        if (targetObject != null)
        {
            if (isEating) isEating = false;
            isCalledByWhistle = false;
            isWaitingAtWhistle = false;
        }

        // 1. MODE MEMBAWA BOLA KEMBALI KE PLAYER VR
        if (isCarryingBall && carriedBall != null)
        {
            agent.isStopped = false;
            isWaiting = false;
            currentIdleType = 0;
            agent.speed = runSpeed;

            if (vrPlayer != null)
            {
                agent.SetDestination(vrPlayer.position);

                Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 flatPlayerPos = new Vector3(vrPlayer.position.x, 0f, vrPlayer.position.z);
                float distToPlayer = Vector3.Distance(flatDogPos, flatPlayerPos);

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

        // 2. MODE SAAT BOLA SEDANG DIPEGANG PLAYER VR
        if (targetObject != null && isHeldByPlayer)
        {
            Vector3 lookTarget = vrPlayer != null ? vrPlayer.position : targetObject.position;
            Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 flatTargetPos = new Vector3(lookTarget.x, 0f, lookTarget.z);
            float flatDistance = Vector3.Distance(flatDogPos, flatTargetPos);

            if (flatDistance > waitDistanceFromPlayer)
            {
                agent.isStopped = false;
                isWaiting = false;
                isChasing = true;
                currentIdleType = 0;
                agent.speed = runSpeed;
                agent.SetDestination(lookTarget);
            }
            else
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                isChasing = false;
                isWaiting = true;
                currentIdleType = holdingBallIdleType;
                FaceTarget(lookTarget);
            }

            UpdateAnimation();
            return;
        }

        // 3. MODE MENGEJAR BOLA
        if (targetObject != null && !isHeldByPlayer)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetObject.position);

            if (distanceToTarget <= detectionRadius)
            {
                agent.isStopped = false;
                isWaiting = false;
                currentIdleType = 0;
                isChasing = true;
                agent.speed = runSpeed;
                agent.SetDestination(targetObject.position);

                if (distanceToTarget <= agent.stoppingDistance + 0.5f)
                {
                    PickUpBall(targetObject);
                }

                UpdateAnimation();
                return;
            }
        }

        // 4. MODE MENUJU TEMPAT MAKAN, MENUNGGU PENUH, & MAKAN (HANYA SAAT LAPAR)
        if (isHungry && foodBowl != null && (isApproachingBowl || isEating || foodBowl.IsFull))
        {
            isCalledByWhistle = false;
            isWaitingAtWhistle = false;

            Vector3 bowlPos = foodBowl.transform.position;
            Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 flatBowlPos = new Vector3(bowlPos.x, 0f, bowlPos.z);
            float distToBowl = Vector3.Distance(flatDogPos, flatBowlPos);

            if (!isEating && distToBowl > eatStopDistance)
            {
                agent.isStopped = false;
                isWaiting = false;
                isChasing = true;
                currentIdleType = 0;
                agent.speed = runSpeed;
                agent.SetDestination(bowlPos);
            }
            else if (!isEating && !foodBowl.IsFull)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                isChasing = false;
                isWaiting = true;
                currentIdleType = waitingFoodIdleType;
                FaceTarget(bowlPos);
            }
            else
            {
                if (!isEating)
                {
                    isEating = true;
                    isApproachingBowl = false;
                    currentEatDuration = Random.Range(minEatDuration, maxEatDuration);
                    eatTimer = currentEatDuration;
                }

                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                isChasing = false;
                isWaiting = true;
                FaceTarget(bowlPos);

                eatTimer -= Time.deltaTime;
                if (currentEatDuration > 0f)
                {
                    foodBowl.SetFillLevel(eatTimer / currentEatDuration);
                }

                if (eatTimer <= 0f)
                {
                    foodBowl.SetFillLevel(0f);
                    isEating = false;
                    isApproachingBowl = false;
                    isHungry = false;
                    hungerTimer = Random.Range(minHungerCooldown, maxHungerCooldown);

                    isWaiting = false;
                    agent.isStopped = false;
                    MoveToCurrentWaypoint();
                }
            }

            UpdateAnimation();
            return;
        }

        // 5. MODE SEDANG DIELUS (PETTING)
        if (isBeingPetted)
        {
            isCalledByWhistle = false;
            isWaitingAtWhistle = false;

            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            isChasing = false;
            isWaiting = true;
            currentIdleType = petIdleType;
            wasBeingPetted = true;

            UpdateAnimation();
            return;
        }
        else if (wasBeingPetted)
        {
            wasBeingPetted = false;
            isWaiting = true;
            waitTimer = postPetWaitTime;
        }

        // 6. MODE DIPANGGIL WHISTLE (MENDEKAT LALU DIAM 5 DETIK)
        if (isCalledByWhistle && vrPlayer != null)
        {
            Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 flatPlayerPos = new Vector3(vrPlayer.position.x, 0f, vrPlayer.position.z);
            float distToPlayer = Vector3.Distance(flatDogPos, flatPlayerPos);

            if (!isWaitingAtWhistle)
            {
                if (distToPlayer > whistleStopDistance)
                {
                    // Berlari menghampiri player
                    agent.isStopped = false;
                    isWaiting = false;
                    isChasing = true;
                    currentIdleType = 0;
                    agent.speed = runSpeed;
                    agent.SetDestination(vrPlayer.position);
                }
                else
                {
                    // Sampai di depan player -> mulai diam selama 5 detik
                    isWaitingAtWhistle = true;
                    whistleTimer = whistleWaitDuration;
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    isChasing = false;
                    isWaiting = true;
                    currentIdleType = whistleIdleType;
                    FaceTarget(vrPlayer.position);
                }
            }
            else
            {
                // Diam selama 5 detik menghadap player
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                isChasing = false;
                isWaiting = true;
                currentIdleType = whistleIdleType;
                FaceTarget(vrPlayer.position);

                whistleTimer -= Time.deltaTime;
                if (whistleTimer <= 0f)
                {
                    // Selesai 5 detik -> kembali ke rute patroli semula
                    isCalledByWhistle = false;
                    isWaitingAtWhistle = false;
                    isWaiting = false;
                    currentIdleType = 0;
                    agent.isStopped = false;
                    MoveToCurrentWaypoint();
                }
            }

            UpdateAnimation();
            return;
        }

        // 7. MODE PATROLI RUTE WAYPOINT
        agent.isStopped = false;
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
                currentIdleType = 0;
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
                waitTimer = GetWaitTimeForWaypoint(currentWaypointIndex);
                currentIdleType = GetIdleTypeForWaypoint(currentWaypointIndex);
                AdvanceWaypointIndex();
            }
        }

        UpdateAnimation();
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
        }
    }

    float GetWaitTimeForWaypoint(int index)
    {
        if (waypointWaitTimes != null && index >= 0 && index < waypointWaitTimes.Length && waypointWaitTimes[index] > 0f)
        {
            return waypointWaitTimes[index];
        }
        return Random.Range(minWaitTime, maxWaitTime);
    }

    int GetIdleTypeForWaypoint(int index)
    {
        if (waypointIdleTypes != null && index >= 0 && index < waypointIdleTypes.Length)
        {
            return waypointIdleTypes[index];
        }
        return 0;
    }

    void PickUpBall(Transform ball)
    {
        carriedBall = ball;
        targetObject = null;
        isChasing = false;
        isCarryingBall = true;
        currentIdleType = 0;

        Rigidbody ballRb = carriedBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.isKinematic = true;
            ballRb.useGravity = false;
        }

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
            carriedBall.position = transform.position + transform.forward * 0.6f + Vector3.up * 0.25f;

            Collider ballCol = carriedBall.GetComponent<Collider>();
            if (ballCol != null)
            {
                ballCol.isTrigger = false;
                ballCol.material = null;
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

        isWaiting = true;
        currentIdleType = 0;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void MoveToCurrentWaypoint()
    {
        if (waypoints.Length == 0 || waypoints[currentWaypointIndex] == null) return;
        agent.isStopped = false;
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

        animator.SetBool("IsEating", isEating);

        float currentSpeed = agent.velocity.magnitude;

        if (isWaiting || currentSpeed < 0.1f)
        {
            animator.SetFloat("Vert", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("State", 0f, 0.1f, Time.deltaTime);
            animator.SetInteger("IdleType", (isWaiting && !isEating) ? currentIdleType : 0);
        }
        else
        {
            animator.SetInteger("IdleType", 0);
            animator.SetFloat("Vert", 1f, 0.1f, Time.deltaTime);
            bool isRunning = isChasing || isCarryingBall;
            animator.SetFloat("State", isRunning ? 1f : 0f, 0.15f, Time.deltaTime);
        }
    }
}