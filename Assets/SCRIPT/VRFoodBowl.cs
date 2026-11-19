using UnityEngine;

public class VRFoodBowl : MonoBehaviour
{
    [Header("Referensi AI Anjing")]
    public DogRoute dogAI;

    [Header("Visual Makanan di Dalam Bowl")]
    [Tooltip("Objek visual tumpukan makanan di dalam bowl yang akan naik/membesar saat diisi")]
    public Transform foodVisual;
    public float emptyScaleY = 0.01f;
    public float fullScaleY = 0.6f;

    [Range(0f, 1f)]
    public float currentFill = 0f;

    public bool IsFull => currentFill >= 1f;

    private Vector3 baseScale;

    void Awake()
    {
        if (dogAI == null)
        {
            dogAI = FindFirstObjectByType<DogRoute>();
        }

        if (foodVisual != null)
        {
            baseScale = foodVisual.localScale;
        }

        UpdateFoodVisual();
    }

    public void AddFood(float amount)
    {
        if (IsFull) return;

        currentFill = Mathf.Clamp01(currentFill + amount);
        UpdateFoodVisual();

        if (dogAI != null)
        {
            // Anjing mendekat dan menunggu di pinggir bowl selama diisi
            dogAI.NotifyFoodPouring(transform, this);

            // Anjing baru mulai makan setelah bowl penuh 100%
            if (IsFull)
            {
                dogAI.NotifyFoodFull();
            }
        }
    }

    public void SetFillLevel(float normalizedFill)
    {
        currentFill = Mathf.Clamp01(normalizedFill);
        UpdateFoodVisual();
    }

    private void UpdateFoodVisual()
    {
        if (foodVisual == null) return;

        foodVisual.gameObject.SetActive(currentFill > 0.01f);
        float targetY = Mathf.Lerp(emptyScaleY, fullScaleY, currentFill);
        foodVisual.localScale = new Vector3(baseScale.x, targetY, baseScale.z);
    }
}