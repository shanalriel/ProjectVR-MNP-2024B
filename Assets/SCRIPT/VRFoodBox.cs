using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class VRFoodBox : MonoBehaviour
{
    [Header("Referensi Tempat Makan")]
    public VRFoodBowl targetBowl;
    [Tooltip("Titik ujung tempat keluarnya makanan (opsional, default pusat box)")]
    public Transform pourOrigin;

    [Header("Pengaturan Menuang")]
    [Tooltip("Sudut kemiringan minimum (derajat) agar makanan mulai tumpah")]
    public float minTiltAngle = 65f;
    [Tooltip("Jarak horizontal maksimal dari atas Food Bowl untuk menuang")]
    public float maxPourHorizontalDistance = 0.8f;
    [Tooltip("Kecepatan pengisian bowl per detik (0.35 = penuh dalam ~3 detik)")]
    public float fillSpeed = 0.35f;

    [Header("Efek Visual Tumpahan (Opsional)")]
    public ParticleSystem pourParticles;

    private XRGrabInteractable grabInteractable;
    private bool isPouring = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (targetBowl == null)
        {
            targetBowl = FindFirstObjectByType<VRFoodBowl>();
        }
    }

    void Update()
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        bool isTiltedEnough = tiltAngle >= minTiltAngle;

        Transform origin = pourOrigin != null ? pourOrigin : transform;
        bool isOverBowl = false;

        if (targetBowl != null)
        {
            Vector2 flatOrigin = new Vector2(origin.position.x, origin.position.z);
            Vector2 flatBowl = new Vector2(targetBowl.transform.position.x, targetBowl.transform.position.z);
            float horizontalDist = Vector2.Distance(flatOrigin, flatBowl);
            bool isAboveBowl = origin.position.y >= targetBowl.transform.position.y - 0.1f;

            isOverBowl = isAboveBowl && (horizontalDist <= maxPourHorizontalDistance);
        }

        if (grabInteractable.isSelected && isTiltedEnough && isOverBowl && !targetBowl.IsFull)
        {
            StartPouring();
            targetBowl.AddFood(fillSpeed * Time.deltaTime);
        }
        else
        {
            StopPouring();
        }
    }

    void StartPouring()
    {
        if (!isPouring)
        {
            isPouring = true;
            if (pourParticles != null && !pourParticles.isPlaying)
                pourParticles.Play();
        }
    }

    void StopPouring()
    {
        if (isPouring)
        {
            isPouring = false;
            if (pourParticles != null && pourParticles.isPlaying)
                pourParticles.Stop();
        }
    }
}