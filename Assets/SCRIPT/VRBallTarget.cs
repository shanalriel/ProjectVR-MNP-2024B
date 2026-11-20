using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRBallTarget : MonoBehaviour
{
    public DogRoute dogAI;

    [Header("Pengaturan Pantulan")]
    [Tooltip("Masukkan Physics Material BallBounce agar aktif saat dilempar player")]
    public PhysicsMaterial bounceMaterial;

    [Header("Pengaman Batas Jatuh")]
    public float minHeightY = -2f;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Collider ballCollider;
    private Vector3 initialPosition;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
        initialPosition = transform.position;

        if (ballCollider != null && bounceMaterial == null)
        {
            bounceMaterial = ballCollider.sharedMaterial;
        }

        if (dogAI == null)
        {
            dogAI = FindFirstObjectByType<DogRoute>();
        }
    }

    void Update()
    {
        if (transform.position.y < minHeightY)
        {
            ResetBallPosition();
        }
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

    private void OnBallGrabbed(SelectEnterEventArgs args)
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        if (ballCollider != null)
        {
            ballCollider.isTrigger = false;
            ballCollider.material = bounceMaterial;
        }

        if (dogAI != null)
        {
            dogAI.isHeldByPlayer = true;
            dogAI.targetObject = transform;
        }
    }

    private void OnBallReleased(SelectExitEventArgs args)
    {
        if (ballCollider != null)
        {
            ballCollider.material = bounceMaterial;
        }

        if (dogAI != null)
        {
            dogAI.isHeldByPlayer = false;
            dogAI.targetObject = transform;
        }
    }

    public void ResetBallPosition()
    {
        transform.SetParent(null);

        if (ballCollider != null)
        {
            ballCollider.isTrigger = false;
            ballCollider.material = bounceMaterial;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = initialPosition;

        if (dogAI != null)
        {
            dogAI.isHeldByPlayer = false;
            if (dogAI.targetObject == transform)
            {
                dogAI.targetObject = null;
            }
        }
    }
}