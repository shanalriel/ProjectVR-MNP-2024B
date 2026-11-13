using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class VRBallTarget : MonoBehaviour
{
    public DogRoute dogAI;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (dogAI == null)
        {
            dogAI = FindFirstObjectByType<DogRoute>();
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

    // Saat pemain mengambil bola dengan tangan VR -> anjing mulai berlari mendekati pemain
    private void OnBallGrabbed(SelectEnterEventArgs args)
    {
        // Lepaskan dari mulut anjing jika sedang digigit
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

    // Saat pemain melempar/melepas bola -> anjing mengejar, menggigit, dan mengembalikannya ke ReturnPoint
    private void OnBallReleased(SelectExitEventArgs args)
    {
        if (dogAI != null)
        {
            dogAI.isHeldByPlayer = false;
            dogAI.targetObject = transform;
        }
    }
}