using UnityEngine;
using UnityEngine.InputSystem;

public class VRWhistle : MonoBehaviour
{
    [Header("Referensi AI Anjing")]
    public DogController dogAI;

    [Header("Input Tombol VR & Keyboard")]
    [Tooltip("Opsional: Hubungkan ke tombol controller VR (misal XRI Right Interaction/Activate atau Primary Button)")]
    public InputActionProperty whistleAction;
    [Tooltip("Tombol keyboard untuk langsung memanggil anjing di Simulator")]
    public Key keyboardShortcut = Key.V;

    [Header("Efek Suara Siulan (Opsional)")]
    public AudioSource audioSource;
    public AudioClip whistleClip;
    [Tooltip("Jeda minimum (detik) antar siulan")]
    public float whistleCooldown = 1.5f;

    private float cooldownTimer = 0f;

    void Awake()
    {
        if (dogAI == null)
        {
            dogAI = FindFirstObjectByType<DogController>();
        }
    }

    void OnEnable()
    {
        if (whistleAction.action != null)
        {
            whistleAction.action.Enable();
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        bool vrButtonPressed = whistleAction.action != null && whistleAction.action.WasPressedThisFrame();
        bool keyPressed = Keyboard.current != null && keyboardShortcut != Key.None && Keyboard.current[keyboardShortcut].wasPressedThisFrame;

        if ((vrButtonPressed || keyPressed) && cooldownTimer <= 0f)
        {
            TriggerWhistle();
        }
    }

    public void TriggerWhistle()
    {
        cooldownTimer = whistleCooldown;

        if (audioSource != null && whistleClip != null)
        {
            audioSource.PlayOneShot(whistleClip);
        }

        if (dogAI != null)
        {
            dogAI.OnWhistleCalled();
        }
    }
}