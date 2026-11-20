using UnityEngine;
using UnityEngine.AI;

public class CatRoute : MonoBehaviour
{
    [Header("NavMesh & Rute Waypoint")]
    public NavMeshAgent agent;
    public Transform[] waypoints;
    [Tooltip("ID animasi tiap titik (0 = Idle biasa, 1 = Animasi 1, 2 = Animasi 2, dst.)")]
    public int[] waypointIdleTypes;
    [Tooltip("Durasi diam (detik) tiap titik. Jika diisi 0, akan memakai nilai acak Min/Max Wait Time.")]
    public float[] waypointWaitTimes;
    public bool loopRoute = true;

    [Header("Interaksi Tempat Makan & Sistem Lapar")]
    public VRFoodBowl foodBowl;
    [Tooltip("Jarak berhenti kucing di pinggir tempat makan")]
    public float eatStopDistance = 0.85f;
    [Tooltip("Durasi minimum kucing menghabiskan makanan (detik)")]
    public float minEatDuration = 5f;
    [Tooltip("Durasi maksimum kucing menghabiskan makanan (detik)")]
    public float maxEatDuration = 10f;
    [Tooltip("ID animasi saat kucing menunggu bowl diisi penuh (0 = Idle biasa, 1 = Duduk, dst.)")]
    public int waitingFoodIdleType = 0;
    [Tooltip("Jeda waktu minimum (detik) sebelum kucing lapar kembali (180 = 3 menit)")]
    public float minHungerCooldown = 180f;
    [Tooltip("Jeda waktu maksimum (detik) sebelum kucing lapar kembali (300 = 5 menit)")]
    public float maxHungerCooldown = 300f;

    [Header("Interaksi Elus (Petting)")]
    [Tooltip("ID animasi saat kucing dielus (0 = Idle biasa, 1 = Duduk, dst.)")]
    public int petIdleType = 0;
    [Tooltip("Waktu jeda diam (detik) setelah selesai dielus sebelum berjalan lagi")]
    public float postPetWaitTime = 1.5f;

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

    private bool isHungry = true;
    private float hungerTimer = 0f;
    private bool isApproachingBowl = false;
    private bool isEating = false;
    private float eatTimer = 0f;
    private float currentEatDuration = 5f;

    private bool isBeingPetted = false;
    private bool wasBeingPetted = false;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

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

    public void NotifyFoodPouring(Transform bowlTransform, VRFoodBowl bowlScript)
    {
        if (foodBowl == null && bowlScript != null)
            foodBowl = bowlScript;

        if (!isHungry || isEating) return;

        isApproachingBowl = true;
    }

    public void NotifyFoodFull()
    {
        if (!isHungry || isEating) return;

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

        // 1. MODE MENUJU TEMPAT MAKAN, MENUNGGU PENUH, & MAKAN (HANYA SAAT LAPAR)
        if (isHungry && foodBowl != null && (isApproachingBowl || isEating || foodBowl.IsFull))
        {
            Vector3 bowlPos = foodBowl.transform.position;
            Vector3 flatCatPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 flatBowlPos = new Vector3(bowlPos.x, 0f, bowlPos.z);
            float distToBowl = Vector3.Distance(flatCatPos, flatBowlPos);

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

        // 2. MODE SEDANG DIELUS (PETTING)
        if (isBeingPetted)
        {
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

        // 3. MODE PATROLI RUTE WAYPOINT
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
            animator.SetFloat("State", isChasing ? 1f : 0f, 0.15f, Time.deltaTime);
        }
    }
}