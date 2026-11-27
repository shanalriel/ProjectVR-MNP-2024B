using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRBallTarget : MonoBehaviour
{
    public DogController dogAI;
    [Tooltip("Pengali kekuatan lemparan bola")]
    public float throwMultiplier = 1.5f;
    [Tooltip("Kecepatan minimal (m/s) agar dianggap lemparan; jika lebih pelan dari ini dianggap dijatuhkan (drop)")]
    public float minThrowSpeed = 1.5f;

    [Header("Pengaturan Respawn Bola")]
    [Tooltip("Titik respawn khusus (kosongkan jika ingin kembali ke posisi awal bola di scene)")]
    public Transform customRespawnPoint;
    [Tooltip("Batas ketinggian Y; jika bola jatuh di bawah angka ini, bola otomatis respawn")]
    public float fallThresholdY = -3f;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Transform currentHandTransform;
    private Vector3 lastHandPos;
    private Vector3 trackedHandVelocity;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (dogAI == null)
            dogAI = FindFirstObjectByType<DogController>();
    }

    void Start()
    {
        initialPosition = customRespawnPoint != null ? customRespawnPoint.position : transform.position;
        initialRotation = customRespawnPoint != null ? customRespawnPoint.rotation : transform.rotation;
    }

    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnBallGrabbed);
        grabInteractable.selectExited.AddListener(OnBallReleased);
    }

    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnBallGrabbed);
        grabInteractable.selectExited.RemoveListener(OnBallReleased);
    }

    void Update()
    {
        if (grabInteractable.isSelected && currentHandTransform != null && Time.deltaTime > 0f)
        {
            Vector3 instantVel = (currentHandTransform.position - lastHandPos) / Time.deltaTime;
            trackedHandVelocity = Vector3.Lerp(trackedHandVelocity, instantVel, Time.deltaTime * 15f);
            lastHandPos = currentHandTransform.position;
        }

        bool isCarriedByDog = dogAI != null && dogAI.IsCarryingBall;
        if (!grabInteractable.isSelected && !isCarriedByDog && transform.position.y < fallThresholdY)
        {
            RespawnBall();
        }
    }

    public void RespawnBall()
    {
        transform.SetParent(null);
        transform.position = customRespawnPoint != null ? customRespawnPoint.position : initialPosition;
        transform.rotation = customRespawnPoint != null ? customRespawnPoint.rotation : initialRotation;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (dogAI != null)
        {
            dogAI.OnBallDropped();
        }
    }

    private void OnBallGrabbed(SelectEnterEventArgs args)
    {
        transform.SetParent(null);
        currentHandTransform = args.interactorObject.transform;
        lastHandPos = currentHandTransform.position;
        trackedHandVelocity = Vector3.zero;

        if (dogAI != null)
        {
            dogAI.OnBallGrabbedByPlayer(transform);
        }
    }

    private void OnBallReleased(SelectExitEventArgs args)
    {
        transform.SetParent(null);
        float releaseSpeed = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            if (rb.linearVelocity.sqrMagnitude < 0.05f && trackedHandVelocity.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity = trackedHandVelocity * throwMultiplier;
            }

            releaseSpeed = rb.linearVelocity.magnitude;
        }

        currentHandTransform = null;

        if (dogAI != null)
        {
            // Jika dilepas tanpa ayunan/kecepatan (sekadar di-drop), anjing tidak akan mengejar
            if (releaseSpeed >= minThrowSpeed)
            {
                dogAI.OnBallThrown(transform);
            }
            else
            {
                dogAI.OnBallDropped();
            }
        }
    }
}