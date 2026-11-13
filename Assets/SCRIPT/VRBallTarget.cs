using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRBallTarget : MonoBehaviour
{
    public DogRoute dogAI;

    [Header("Pengaman Batas Jatuh")]
    [Tooltip("Jika posisi Y bola turun di bawah angka ini, bola akan kembali ke posisi awal")]
    public float minHeightY = -2f;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Vector3 initialPosition;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;

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

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        if (dogAI != null)
        {
            dogAI.isHeldByPlayer = true;
            dogAI.targetObject = transform;
        }
    }

    private void OnBallReleased(SelectExitEventArgs args)
    {
        if (dogAI != null)
        {
            dogAI.isHeldByPlayer = false;
            dogAI.targetObject = transform;
        }
    }

    public void ResetBallPosition()
    {
        transform.SetParent(null);

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