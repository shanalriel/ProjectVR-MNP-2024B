using UnityEngine;
using UnityEngine.InputSystem;

public class VRCameraToggle : MonoBehaviour
{
    [Header("Referensi Kamera")]
    public GameObject cameraObject; // Drag object Cam ke sini

    [Header("Tombol Controller VR")]
    public InputActionProperty toggleButton; // Pilih tombol controller (misal tombol A / PrimaryButton)

    private void OnEnable()
    {
        toggleButton.action.Enable();
        toggleButton.action.performed += OnButtonPressed;
    }

    private void OnDisable()
    {
        toggleButton.action.performed -= OnButtonPressed;
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        if (cameraObject != null)
        {
            // Membalik status aktif: kalau mati jadi nyala, kalau nyala jadi mati
            bool isActive = cameraObject.activeSelf;
            cameraObject.SetActive(!isActive);
        }
    }
}