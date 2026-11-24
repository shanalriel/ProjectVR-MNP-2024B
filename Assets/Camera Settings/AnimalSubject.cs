using UnityEngine;

public class AnimalSubject : MonoBehaviour
{
    public string animalName = "Rubah"; // Ganti sesuai nama hewannya di Inspector
    public Collider animalCollider;     // Drag collider utama hewan ke sini

    private void Reset()
    {
        animalCollider = GetComponent<Collider>();
    }
}