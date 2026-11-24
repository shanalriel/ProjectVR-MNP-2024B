using System.Collections.Generic;
using UnityEngine;

public class VRCameraCapture : MonoBehaviour
{
    [Header("Komponen Kamera")]
    public Camera photoCamera;
    public RenderTexture renderTexture;

    [Header("Pengaturan Deteksi Hewan")]
    public float maxPhotoDistance = 30f;   // Jarak maksimal hewan bisa terdeteksi
    public LayerMask animalLayer;          // Pilih layer "Animal" di Inspector
    public LayerMask obstacleLayer;        // Pilih layer penghalang (Default, Environment, Terrain, dll)

    // Panggil fungsi ini di dalam TakePhoto()
    public List<string> DetectAnimalsInFrame()
    {
        List<string> detectedAnimals = new List<string>();

        // 1. Ambil batas bidang pandang (frustum) dari kamera foto
        Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(photoCamera);

        // 2. Cari semua hewan di sekitar kamera dalam radius maxPhotoDistance
        Collider[] nearbyColliders = Physics.OverlapSphere(photoCamera.transform.position, maxPhotoDistance, animalLayer);

        foreach (Collider col in nearbyColliders)
        {
            // 3. Cek apakah collider hewan masuk di dalam frame kamera
            if (GeometryUtility.TestPlanesAABB(cameraPlanes, col.bounds))
            {
                Vector3 cameraPos = photoCamera.transform.position;
                Vector3 targetCenter = col.bounds.center;

                // 4. Cek Linecast: pastikan tidak ada tembok/pohon yang menghalangi pandangan ke titik tengah hewan
                if (!Physics.Linecast(cameraPos, targetCenter, obstacleLayer))
                {
                    AnimalSubject animal = col.GetComponentInParent<AnimalSubject>();
                    if (animal != null && !detectedAnimals.Contains(animal.animalName))
                    {
                        detectedAnimals.Add(animal.animalName);
                    }
                }
            }
        }

        return detectedAnimals;
    }
}   