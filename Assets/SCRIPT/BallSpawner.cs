using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem; // Hapus baris ini jika menggunakan Old Input System

public class BallSpawner : MonoBehaviour
{
    [Header("Referensi Objek")]
    public GameObject ballPrefab;
    public DelayMovement dogAI;
    public Transform centrePoint;
    public float spawnRange = 8f;

    [Header("Ketinggian Spawn")]
    public float spawnHeight = 5f; // Ketinggian bola saat pertama kali spawn (5 unit di atas lantai)

    private GameObject currentBall;

    void Update()
    {
        // Tombol Spacebar untuk spawn bola
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        if (centrePoint == null || ballPrefab == null || dogAI == null) return;

        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        Vector3 randomPosition = centrePoint.position + Random.insideUnitSphere * spawnRange;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPosition, out hit, spawnRange, NavMesh.AllAreas))
        {
            // Tambahkan spawnHeight pada sumbu Y agar bola muncul di udara
            Vector3 spawnPos = hit.position + new Vector3(0, spawnHeight, 0);
            currentBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

            // Berikan target bola ke script anjing
            dogAI.targetObject = currentBall.transform;
        }
    }
}