using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class VRFoodBox : MonoBehaviour
{
    [Header("Referensi Tempat Makan")]
    [Tooltip("Daftar bowl yang bisa diisi oleh box ini (jika kosong, otomatis mendeteksi semua VRFoodBowl di scene)")]
    public VRFoodBowl[] targetBowls;
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

        if (targetBowls == null || targetBowls.Length == 0)
        {
            targetBowls = FindObjectsByType<VRFoodBowl>(FindObjectsSortMode.None);
        }
    }

    void Update()
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        bool isTiltedEnough = tiltAngle >= minTiltAngle;

        Transform origin = pourOrigin != null ? pourOrigin : transform;
        VRFoodBowl activeBowl = GetBowlUnderOrigin(origin);

        if (grabInteractable.isSelected && isTiltedEnough && activeBowl != null && !activeBowl.IsFull)
        {
            StartPouring();
            activeBowl.AddFood(fillSpeed * Time.deltaTime);
        }
        else
        {
            StopPouring();
        }
    }

    VRFoodBowl GetBowlUnderOrigin(Transform origin)
    {
        if (targetBowls == null || targetBowls.Length == 0) return null;

        VRFoodBowl closestBowl = null;
        float closestDist = maxPourHorizontalDistance;
        Vector2 flatOrigin = new Vector2(origin.position.x, origin.position.z);

        foreach (VRFoodBowl bowl in targetBowls)
        {
            if (bowl == null) continue;

            bool isAboveBowl = origin.position.y >= bowl.transform.position.y - 0.1f;
            if (!isAboveBowl) continue;

            Vector2 flatBowl = new Vector2(bowl.transform.position.x, bowl.transform.position.z);
            float horizontalDist = Vector2.Distance(flatOrigin, flatBowl);

            if (horizontalDist <= closestDist)
            {
                closestDist = horizontalDist;
                closestBowl = bowl;
            }
        }

        return closestBowl;
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