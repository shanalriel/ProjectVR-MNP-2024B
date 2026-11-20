using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class VRCatPetting : MonoBehaviour
{
    [Header("Referensi AI, Player & Efek Partikel")]
    public CatController catAI;
    public Transform vrPlayer;
    public ParticleSystem heartParticles;

    [Header("Batasan Jarak & Posisi Petting")]
    [Tooltip("Jarak maksimal (meter) antara player dan kucing agar bisa di-pet")]
    public float maxPetDistance = 1.8f;
    [Tooltip("Wajibkan kucing berada di depan arah hadap player")]
    public bool requireInFrontOfPlayer = true;
    [Tooltip("Sudut maksimal (derajat) di depan player agar kucing dianggap berada tepat di depan")]
    [Range(15f, 180f)]
    public float maxFrontAngle = 65f;

    [Header("Pengaturan Elus (Petting)")]
    [Tooltip("Jeda waktu (detik) sebelum efek hati berhenti setelah tangan dilepas")]
    public float stopPetDelay = 0.4f;
    [Tooltip("Kecepatan gerakan tangan minimum saat mengelus fisik (0 = cukup disentuh)")]
    public float minRubSpeed = 0.05f;

    private XRSimpleInteractable simpleInteractable;
    private bool isSelectingCat = false;
    private bool isTouchingCat = false;
    private float petGraceTimer = 0f;
    private Vector3 lastHandPosition;
    private bool hasLastHandPos = false;

    void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();

        if (catAI == null)
        {
            catAI = GetComponentInParent<CatController>();
            if (catAI == null)
                catAI = FindFirstObjectByType<CatController>();
        }

        if (vrPlayer == null && Camera.main != null)
        {
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
        Vector3 flatCatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        float distance = Vector3.Distance(flatPlayerPos, flatCatPos);

        // Tolak jika kucing terlalu jauh dari player
        if (distance > maxPetDistance)
            return false;

        // Tolak jika kucing berada di samping jauh atau di belakang player
        if (requireInFrontOfPlayer)
        {
            Vector3 flatForward = new Vector3(vrPlayer.forward.x, 0f, vrPlayer.forward.z).normalized;
            Vector3 dirToCat = (flatCatPos - flatPlayerPos).normalized;

            if (flatForward.sqrMagnitude > 0.001f && dirToCat.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(flatForward, dirToCat);
                if (angle > maxFrontAngle)
                    return false;
            }
        }

        return true;
    }

    private void OnPetStart(SelectEnterEventArgs args)
    {
        if (!IsWithinPettingRange()) return;

        isSelectingCat = true;
        petGraceTimer = stopPetDelay;
    }

    private void OnPetEnd(SelectExitEventArgs args)
    {
        isSelectingCat = false;
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
                isTouchingCat = true;
                petGraceTimer = stopPetDelay;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>() != null)
        {
            isTouchingCat = false;
            hasLastHandPos = false;
        }
    }

    void Update()
    {
        // Langsung hentikan petting jika jarak menjauh atau player berpaling
        if (!IsWithinPettingRange())
        {
            isSelectingCat = false;
            isTouchingCat = false;
            petGraceTimer = 0f;
        }
        else if (isSelectingCat || isTouchingCat)
        {
            petGraceTimer = stopPetDelay;
        }
        else if (petGraceTimer > 0f)
        {
            petGraceTimer -= Time.deltaTime;
        }

        bool activelyPetted = petGraceTimer > 0f;

        if (catAI != null)
        {
            catAI.SetBeingPetted(activelyPetted);
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