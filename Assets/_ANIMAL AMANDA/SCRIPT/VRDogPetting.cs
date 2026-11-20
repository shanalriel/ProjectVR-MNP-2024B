using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class VRDogPetting : MonoBehaviour
{
    [Header("Referensi AI, Player & Efek Partikel")]
    public DogController dogAI;
    public Transform vrPlayer;
    public ParticleSystem heartParticles;

    [Header("Batasan Jarak & Posisi Petting")]
    [Tooltip("Jarak maksimal (meter) antara player dan anjing agar bisa di-pet")]
    public float maxPetDistance = 1.8f;
    [Tooltip("Wajibkan anjing berada di depan arah hadap player")]
    public bool requireInFrontOfPlayer = true;
    [Tooltip("Sudut maksimal (derajat) di depan player agar anjing dianggap berada tepat di depan")]
    [Range(15f, 180f)]
    public float maxFrontAngle = 65f;

    [Header("Pengaturan Elus (Petting)")]
    [Tooltip("Jeda waktu (detik) sebelum efek hati berhenti setelah tangan dilepas")]
    public float stopPetDelay = 0.4f;
    [Tooltip("Kecepatan gerakan tangan minimum saat mengelus fisik (0 = cukup disentuh)")]
    public float minRubSpeed = 0.05f;

    private XRSimpleInteractable simpleInteractable;
    private bool isSelectingDog = false;
    private bool isTouchingDog = false;
    private float petGraceTimer = 0f;
    private Vector3 lastHandPosition;
    private bool hasLastHandPos = false;

    void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();

        if (dogAI == null)
        {
            dogAI = GetComponentInParent<DogController>();
            if (dogAI == null)
                dogAI = FindFirstObjectByType<DogController>();
        }

        if (vrPlayer == null)
        {
            if (dogAI != null && dogAI.vrPlayer != null)
                vrPlayer = dogAI.vrPlayer;
            else if (Camera.main != null)
                vrPlayer = Camera.main.transform;
        }

        if (heartParticles != null)
        {
            heartParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void OnEnable()
    {
        simpleInteractable.selectEntered.AddListener(OnPetStart);
        simpleInteractable.selectExited.AddListener(OnPetEnd);
    }

    void OnDisable()
    {
        simpleInteractable.selectEntered.RemoveListener(OnPetStart);
        simpleInteractable.selectExited.RemoveListener(OnPetEnd);
    }

    private bool IsWithinPettingRange()
    {
        if (vrPlayer == null) return true;

        Vector3 flatPlayerPos = new Vector3(vrPlayer.position.x, 0f, vrPlayer.position.z);
        Vector3 flatDogPos = new Vector3(transform.position.x, 0f, transform.position.z);
        float distance = Vector3.Distance(flatPlayerPos, flatDogPos);

        // Tolak jika anjing terlalu jauh dari player
        if (distance > maxPetDistance)
            return false;

        // Tolak jika anjing berada di samping jauh atau di belakang player
        if (requireInFrontOfPlayer)
        {
            Vector3 flatForward = new Vector3(vrPlayer.forward.x, 0f, vrPlayer.forward.z).normalized;
            Vector3 dirToDog = (flatDogPos - flatPlayerPos).normalized;

            if (flatForward.sqrMagnitude > 0.001f && dirToDog.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(flatForward, dirToDog);
                if (angle > maxFrontAngle)
                    return false;
            }
        }

        return true;
    }

    private void OnPetStart(SelectEnterEventArgs args)
    {
        if (!IsWithinPettingRange()) return;

        isSelectingDog = true;
        petGraceTimer = stopPetDelay;
    }

    private void OnPetEnd(SelectExitEventArgs args)
    {
        isSelectingDog = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsWithinPettingRange()) return;

        if (other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>() != null)
        {
            Vector3 currentPos = other.transform.position;
            if (!hasLastHandPos)
            {
                lastHandPosition = currentPos;
                hasLastHandPos = true;
            }

            float handSpeed = (currentPos - lastHandPosition).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            lastHandPosition = currentPos;

            if (handSpeed >= minRubSpeed)
            {
                isTouchingDog = true;
                petGraceTimer = stopPetDelay;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>() != null)
        {
            isTouchingDog = false;
            hasLastHandPos = false;
        }
    }

    void Update()
    {
        // Langsung hentikan petting jika jarak menjauh atau player berpaling
        if (!IsWithinPettingRange())
        {
            isSelectingDog = false;
            isTouchingDog = false;
            petGraceTimer = 0f;
        }
        else if (isSelectingDog || isTouchingDog)
        {
            petGraceTimer = stopPetDelay;
        }
        else if (petGraceTimer > 0f)
        {
            petGraceTimer -= Time.deltaTime;
        }

        bool activelyPetted = petGraceTimer > 0f;

        if (dogAI != null)
        {
            dogAI.SetBeingPetted(activelyPetted);
        }

        if (heartParticles != null)
        {
            if (activelyPetted && !heartParticles.isPlaying)
            {
                heartParticles.Play();
            }
            else if (!activelyPetted && heartParticles.isPlaying)
            {
                heartParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}