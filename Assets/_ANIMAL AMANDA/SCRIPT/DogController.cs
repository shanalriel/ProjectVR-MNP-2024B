using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DogController : MonoBehaviour
{
    [Header("NavMesh & Rute Waypoint")]
    public NavMeshAgent agent;
    public Transform[] waypoints;
    [Tooltip("ID animasi tiap titik (0 = Idle biasa, 1 = Animasi 1, 2 = Animasi 2, dst.)")]
    public int[] waypointIdleTypes;
    [Tooltip("Centang jika animasi ingin di-loop terus selama Wait Time; kosongkan jika ingin memakai jumlah putaran manual di bawah")]
    public bool[] waypointLoopIdle;
    [Tooltip("Jumlah putaran manual (1 = 1x, 2 = 2x, dst.) jika Waypoint Loop Idle tidak dicentang")]
    public int[] waypointLoopCounts;
    [Tooltip("Durasi diam (detik) tiap titik. Jika diisi 0, akan memakai nilai acak Min/Max Wait Time.")]
    public float[] waypointWaitTimes;
    public bool loopRoute = true;

    [Header("Pengaturan Tidur Setelah Loop Waypoint")]
    [Tooltip("Centang jika anjing akan tidur setelah menyelesaikan seluruh putaran waypoint")]
    public bool sleepAfterRouteLoop = true;
    [Tooltip("Berapa kali putaran penuh seluruh waypoint sebelum anjing tidur (1 = tidur setelah 1 putaran penuh)")]
    public int routeLoopsBeforeSleep = 1;
    [Tooltip("Durasi tidur (detik). Isi 0 jika ingin anjing tidur terus sampai dibangunkan (whistle/bola/elus/makan).")]
    public float sleepDuration = 15f;

    [Header("Interaksi Bola & Player VR")]
    public Transform targetObject;
    [Tooltip("Jarak minimal (meter) bola dari PLAYER agar dikejar; jika bola jatuh dekat player (< nilai ini), anjing tidak mengejar")]
    public float minDistanceFromPlayer = 2.5f;
    [Tooltip("Jarak maksimal (meter) anjing mendeteksi dan mengejar bola yang dilempar")]
    public float detectionRadius = 15f;
    public Transform mouthPoint;
    public Transform vrPlayer;
    [Tooltip("Jarak berhenti anjing di depan player untuk menyerahkan bola")]
    public float dropDistanceFromPlayer = 1.2f;
    [Tooltip("Jarak berhenti anjing di depan player saat player mengangkat bola dari tanah")]
    public float waitDistanceFromPlayer = 1.5f;
    [Tooltip("ID animasi saat anjing diam menyerahkan bola di mulutnya (1 = Duduk, 3 = Holding Ball)")]
    public int deliverBallIdleType = 3;
    [Tooltip("ID animasi saat anjing menghampiri & menunggu player yang mengangkat bola (2 = Standing)")]
    public int holdingBallIdleType = 2;
    [HideInInspector] public bool isHeldByPlayer = false;
    public bool IsCarryingBall => isCarryingBall;

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
    public int petIdleType = 1;
    [Tooltip("Waktu jeda diam (detik) setelah selesai dielus sebelum berjalan lagi")]
    public float postPetWaitTime = 1.5f;

    [Header("Interaksi Whistle (Panggil Anjing)")]
    [Tooltip("Jarak berhenti anjing di depan pemain saat dipanggil")]
    public float whistleStopDistance = 1.5f;
    [Tooltip("Durasi anjing diam di depan pemain setelah dipanggil (detik)")]
    public float whistleWaitDuration = 5f;
    [Tooltip("ID animasi saat anjing diam setelah dipanggil (0 = Idle biasa, 1 = Duduk, dst.)")]
    public int whistleIdleType = 1;

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
    private bool currentContinuousLoop = false;
    private int currentIdleLoopCount = 1;
    private bool enteredIdleAnimState = false;
    private float idleAnimSafetyTimer = 0f;

    private int completedRouteLoops = 0;
    private bool pendingSleepAfterWait = false;
    private bool isSleeping = false;
    private float sleepTimer = 0f;

    private bool isChasing = false;
    private bool isCarryingBall = false;
    private Transform carriedBall;
    private XRGrabInteractable carriedBallGrab;
    private float pickupCooldownTimer = 0f;
    private float throwDetectionTimer = 0f;

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

        if (agent != null && agent.isOnNavMesh && waypoints != null && waypoints.Length > 0)
        {
            MoveToCurrentWaypoint();
        }
    }

    public void SetBeingPetted(bool petted)
    {
        isBeingPetted = petted;
        if (petted && isSleeping)
        {
            WakeUpFromSleep();
        }
    }

    public void OnWhistleCalled()
    {
        if (isCarryingBall || isEating || isHeldByPlayer) return;

        WakeUpFromSleep();
        isCalledByWhistle = true;
        isWaitingAtWhistle = false;
        isWaiting = false;
        currentContinuousLoop = false;
        currentIdleLoopCount = 1;
        currentIdleType = 0;
    }

    public void OnBallGrabbedByPlayer(Transform ball = null)
    {
        WakeUpFromSleep();

        if (isCarryingBall)
        {
            isHeldByPlayer = false;
            targetObject = null;
            isChasing = false;
            ReleaseCarriedBallToPlayer();
            return;
        }

        isHeldByPlayer = true;
        isChasing = false;
        isCalledByWhistle = false;
        isWaitingAtWhistle = false;
        if (ball != null)
        {
            targetObject = ball;
        }
    }

    public void OnBallDropped()
    {
        isHeldByPlayer = false;
        isChasing = false;
        isWaiting = false;
        currentIdleType = 0;
        targetObject = null;

        if (!isSleeping && agent != null && agent.isOnNavMesh)
        {
            agent.speed = walkSpeed;
            MoveToCurrentWaypoint();
        }
    }

    public void OnBallThrown(Transform ball)
    {
        WakeUpFromSleep();

        if (isCarryingBall)
        {
            ReleaseCarriedBallToPlayer();
        }
        isHeldByPlayer = false;
        isChasing = false;
        targetObject = ball;
        pickupCooldownTimer = 0.8f;
        throwDetectionTimer = 3.5f;
    }

    public void NotifyFoodPouring(Transform bowlTransform, VRFoodBowl bowlScript)
    {
        if (foodBowl != null && bowlScript != null && bowlScript != foodBowl) return;
        if (foodBowl == null && bowlScript != null)
            foodBowl = bowlScript;

        if (!isHungry || isCarryingBall || isEating) return;

        WakeUpFromSleep();
        isApproachingBowl = true;
    }

    public void NotifyFoodFull()
    {
        if (!isHungry || isCarryingBall || isEating) return;

        WakeUpFromSleep();
        isApproachingBowl = true;
    }

    void WakeUpFromSleep()
    {
        if (!isSleeping) return;
        isSleeping = false;
        isWaiting = false;
        currentIdleType = 0;
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (!isHungry)
        {
            hungerTimer -= Time.deltaTime;
            if (hungerTimer <= 0f)
            {
                isHungry = true;

                if (foodBowl != null && foodBowl.currentFill > 0f)
                {
                    WakeUpFromSleep();
                    isApproachingBowl = true;
                }
            }
        }

        // Jika player mengambil bola langsung dari mulut anjing -> langsung kembali ke rute waypoint
        bool grabbedFromMouth = carriedBallGrab != null && carriedBallGrab.isSelected;
        if (isCarryingBall && (carriedBall == null || grabbedFromMouth))
        {
            isHeldByPlayer = false;
            ReleaseCarriedBallToPlayer();
        }

        if (targetObject != null)
        {
            WakeUpFromSleep();
            if (isEating) isEating = false;
            isCalledByWhistle = false;
            isWaitingAtWhistle = false;
        }

        // 1. MODE MEMBAWA BOLA KEMBALI KE PLAYER VR (BISA SEKALIGUS DI-PETTING DI DEPAN PLAYER)
        if (isCarryingBall && carriedBall != null)
        {
            Transform attachTarget = mouthPoint != null ? mouthPoint : transform;
            if (carriedBall.parent != attachTarget)
            {
                carriedBall.SetParent(attachTarget);
            }
            carriedBall.localPosition = Vector3.zero;

            if (vrPlayer != null)
            {
                Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 flatPlayerPos = new Vector3(vrPlayer.position.x, 0f, vrPlayer.position.z);
                float distToPlayer = Vector3.Distance(flatDogPos, flatPlayerPos);

                if (!isWaiting && distToPlayer > dropDistanceFromPlayer)
                {
                    agent.isStopped = false;
                    isChasing = true;
                    currentIdleType = 0;
                    agent.speed = runSpeed;
                    agent.SetDestination(vrPlayer.position);
                }
                else if (isWaiting && distToPlayer > dropDistanceFromPlayer + 0.6f)
                {
                    isWaiting = false;
                    agent.isStopped = false;
                    isChasing = true;
                    currentIdleType = 0;
                    agent.speed = runSpeed;
                    agent.SetDestination(vrPlayer.position);
                }
                else
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    isChasing = false;
                    isWaiting = true;
                    FaceTarget(vrPlayer.position);

                    if (isBeingPetted)
                    {
                        currentIdleType = petIdleType;
                        wasBeingPetted = true;
                    }
                    else
                    {
                        wasBeingPetted = false;
                        currentIdleType = deliverBallIdleType;
                    }
                }
            }
            else
            {
                DropBall();
            }

            UpdateAnimation();
            return;
        }

        // 2. MODE SAAT PLAYER MENGANGKAT BOLA DARI TANAH -> ANJING MENGHAMPIRI PLAYER
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
                FaceTarget(lookTarget);

                if (isBeingPetted)
                {
                    currentIdleType = petIdleType;
                    wasBeingPetted = true;
                }
                else
                {
                    wasBeingPetted = false;
                    currentIdleType = holdingBallIdleType;
                }
            }

            UpdateAnimation();
            return;
        }

        // 3. MODE MENGEJAR BOLA (HANYA JIKA DILEMPAR MENJAUHI PLAYER & MASUK JANGKAUAN ANJING)
        if (pickupCooldownTimer > 0f)
        {
            pickupCooldownTimer -= Time.deltaTime;
        }

        if (targetObject != null && !isHeldByPlayer)
        {
            float distanceDogToBall = Vector3.Distance(transform.position, targetObject.position);
            float distancePlayerToBall = vrPlayer != null
                ? Vector3.Distance(
                    new Vector3(vrPlayer.position.x, 0f, vrPlayer.position.z),
                    new Vector3(targetObject.position.x, 0f, targetObject.position.z))
                : minDistanceFromPlayer;

            bool farEnoughFromPlayer = distancePlayerToBall >= minDistanceFromPlayer;
            bool withinDogDetection = distanceDogToBall <= detectionRadius;

            if (!isChasing && farEnoughFromPlayer && withinDogDetection)
            {
                isChasing = true;
            }
            else if (isChasing && !withinDogDetection)
            {
                isChasing = false;
                targetObject = null;
                MoveToCurrentWaypoint();
            }

            if (isChasing && targetObject != null)
            {
                agent.isStopped = false;
                isWaiting = false;
                currentIdleType = 0;
                agent.speed = runSpeed;
                agent.SetDestination(targetObject.position);

                if (pickupCooldownTimer <= 0f && distanceDogToBall <= agent.stoppingDistance + 0.5f)
                {
                    PickUpBall(targetObject);
                }

                UpdateAnimation();
                return;
            }
            else if (targetObject != null)
            {
                throwDetectionTimer -= Time.deltaTime;
                if (throwDetectionTimer <= 0f)
                {
                    targetObject = null;
                    MoveToCurrentWaypoint();
                }
            }
        }

        // 4. MODE MENUJU TEMPAT MAKAN, MENUNGGU PENUH, & MAKAN (HANYA SAAT LAPAR)
        if (isHungry && foodBowl != null && (isApproachingBowl || isEating || foodBowl.IsFull))
        {
            WakeUpFromSleep();
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
            WakeUpFromSleep();
            isCalledByWhistle = false;
            isWaitingAtWhistle = false;

            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            isChasing = false;
            isWaiting = true;
            currentContinuousLoop = true;
            currentIdleType = petIdleType;
            wasBeingPetted = true;

            UpdateAnimation();
            return;
        }
        else if (wasBeingPetted)
        {
            wasBeingPetted = false;
            isWaiting = true;
            currentContinuousLoop = false;
            currentIdleType = 0;
            waitTimer = postPetWaitTime;
        }

        // 6. MODE DIPANGGIL WHISTLE (MENDEKAT LALU DIAM 5 DETIK)
        if (isCalledByWhistle && vrPlayer != null)
        {
            WakeUpFromSleep();
            Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 flatPlayerPos = new Vector3(vrPlayer.position.x, 0f, vrPlayer.position.z);
            float distToPlayer = Vector3.Distance(flatDogPos, flatPlayerPos);

            if (!isWaitingAtWhistle)
            {
                if (distToPlayer > whistleStopDistance)
                {
                    agent.isStopped = false;
                    isWaiting = false;
                    isChasing = true;
                    currentIdleType = 0;
                    agent.speed = runSpeed;
                    agent.SetDestination(vrPlayer.position);
                }
                else
                {
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
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                isChasing = false;
                isWaiting = true;
                currentIdleType = whistleIdleType;
                FaceTarget(vrPlayer.position);

                whistleTimer -= Time.deltaTime;
                if (whistleTimer <= 0f)
                {
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

        // 7. MODE TIDUR SETELAH SELESAI LOOP SELURUH WAYPOINT
        if (isSleeping)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            isChasing = false;
            isWaiting = true;
            currentIdleType = 0;

            if (sleepDuration > 0f)
            {
                sleepTimer -= Time.deltaTime;
                if (sleepTimer <= 0f)
                {
                    isSleeping = false;
                    isWaiting = false;
                    if (loopRoute)
                    {
                        MoveToCurrentWaypoint();
                    }
                }
            }

            UpdateAnimation();
            return;
        }

        // 8. MODE PATROLI RUTE WAYPOINT
        agent.isStopped = false;
        isChasing = false;
        agent.speed = walkSpeed;

        if (isWaiting)
        {
            if (currentIdleType > 0 && !currentContinuousLoop && animator != null)
            {
                idleAnimSafetyTimer -= Time.deltaTime;
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                if (!animator.IsInTransition(0) && !stateInfo.IsName("Dog_001_walk") && !stateInfo.IsName("Run"))
                {
                    enteredIdleAnimState = true;
                }

                float targetNormalizedTime = currentIdleLoopCount - 0.05f;
                if ((enteredIdleAnimState && stateInfo.normalizedTime >= targetNormalizedTime) || idleAnimSafetyTimer <= 0f)
                {
                    currentIdleType = 0;
                    enteredIdleAnimState = false;
                }

                UpdateAnimation();
                return;
            }

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                currentIdleType = 0;
                currentContinuousLoop = false;
                currentIdleLoopCount = 1;

                if (pendingSleepAfterWait)
                {
                    pendingSleepAfterWait = false;
                    isSleeping = true;
                    sleepTimer = sleepDuration;
                }
                else
                {
                    MoveToCurrentWaypoint();
                }
            }
            UpdateAnimation();
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
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
                currentContinuousLoop = GetContinuousLoopForWaypoint(currentWaypointIndex);

                int manualCount = GetIdleLoopCountForWaypoint(currentWaypointIndex);
                currentIdleLoopCount = manualCount > 0 ? manualCount : 1;

                enteredIdleAnimState = false;
                idleAnimSafetyTimer = currentContinuousLoop ? waitTimer + 2f : currentIdleLoopCount * 8f;
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

    bool GetContinuousLoopForWaypoint(int index)
    {
        if (waypointLoopIdle != null && index >= 0 && index < waypointLoopIdle.Length)
        {
            return waypointLoopIdle[index];
        }
        return false;
    }

    int GetIdleLoopCountForWaypoint(int index)
    {
        if (waypointLoopCounts != null && index >= 0 && index < waypointLoopCounts.Length)
        {
            return waypointLoopCounts[index];
        }
        return 1;
    }

    void PickUpBall(Transform ball)
    {
        carriedBall = ball;
        carriedBallGrab = carriedBall.GetComponent<XRGrabInteractable>();
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
            ballCol.isTrigger = false;
            SetIgnoreBallCollision(ballCol, true);
        }

        Transform attachTarget = mouthPoint != null ? mouthPoint : transform;
        carriedBall.SetParent(attachTarget);
        carriedBall.localPosition = Vector3.zero;
        carriedBall.localRotation = Quaternion.identity;
    }

    void ReleaseCarriedBallToPlayer()
    {
        if (carriedBall != null)
        {
            if (carriedBall.parent == mouthPoint || carriedBall.parent == transform)
            {
                carriedBall.SetParent(null);
            }

            Collider ballCol = carriedBall.GetComponent<Collider>();
            if (ballCol != null)
            {
                ballCol.isTrigger = false;
                SetIgnoreBallCollision(ballCol, false);
            }

            Rigidbody ballRb = carriedBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = false;
                ballRb.useGravity = true;
            }

            carriedBall = null;
        }

        carriedBallGrab = null;
        targetObject = null;
        isCarryingBall = false;
        isChasing = false;
        isWaiting = false;
        wasBeingPetted = false;
        currentIdleType = 0;
        agent.speed = walkSpeed;
        MoveToCurrentWaypoint();
    }

    void SetIgnoreBallCollision(Collider ballCol, bool ignore)
    {
        if (ballCol == null) return;

        Collider[] dogColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in dogColliders)
        {
            if (col != null && col != ballCol)
            {
                Physics.IgnoreCollision(ballCol, col, ignore);
            }
        }
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
                SetIgnoreBallCollision(ballCol, false);
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

        carriedBallGrab = null;
        isCarryingBall = false;
        isHeldByPlayer = false;
        agent.speed = walkSpeed;

        isWaiting = true;
        currentIdleType = 0;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void MoveToCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0 || waypoints[currentWaypointIndex] == null) return;
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    void AdvanceWaypointIndex()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            completedRouteLoops++;
            int targetLoops = routeLoopsBeforeSleep > 0 ? routeLoopsBeforeSleep : 1;

            if (sleepAfterRouteLoop && completedRouteLoops >= targetLoops)
            {
                pendingSleepAfterWait = true;
                completedRouteLoops = 0;
            }

            currentWaypointIndex = loopRoute ? 0 : waypoints.Length - 1;
        }
    }

    void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        animator.SetBool("IsEating", isEating);
        animator.SetBool("IsSleeping", isSleeping);

        float currentSpeed = agent.velocity.magnitude;

        if (isSleeping || isWaiting || currentSpeed < 0.1f)
        {
            animator.SetFloat("Vert", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("State", 0f, 0.1f, Time.deltaTime);
            animator.SetInteger("IdleType", (isWaiting && !isEating && !isSleeping) ? currentIdleType : 0);
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