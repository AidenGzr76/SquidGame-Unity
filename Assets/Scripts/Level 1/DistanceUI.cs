using UnityEngine;
using UnityEngine.UI;

public class DistanceUI : MonoBehaviour
{
    // Make player private, we'll find it via code
    private Transform playerTransform; // Changed name for clarity

    public Transform startLine;
    public Transform finishLine;
    public Slider distanceSlider;

    private float totalDistance;
    private bool playerFound = false; // Flag to check if we've found the player

    void Start()
    {
        // Calculate total distance (stays the same)
        if (startLine != null && finishLine != null)
        {
            totalDistance = Vector3.Distance(startLine.position, finishLine.position);
        }
        else
        {
            Debug.LogError("StartLine or FinishLine is not assigned in DistanceUI!", this);
            totalDistance = 1f; // Prevent division by zero
        }


        // Set slider range (stays the same)
        if (distanceSlider != null)
        {
            distanceSlider.minValue = 0;
            distanceSlider.maxValue = 1;
            distanceSlider.value = 0; // Start at 0
        }
        else
        {
            Debug.LogError("DistanceSlider is not assigned in DistanceUI!", this);
        }

        // Try to find the player immediately at the start
        FindPlayer();
    }

    void Update()
    {
        // If we haven't found the player yet, keep trying each frame
        if (!playerFound)
        {
            FindPlayer();
            // If still not found after trying, exit Update for this frame
            if (!playerFound) return;
        }

        // If player and slider are valid, update the slider
        if (playerTransform != null && distanceSlider != null && totalDistance > 0)
        {
            // Calculate distance only along the main axis of movement (usually Y)
            // This prevents sideways movement from affecting the slider too much
            float startY = startLine.position.y;
            float finishY = finishLine.position.y;
            float playerY = playerTransform.position.y;

            // Calculate progress based on Y position relative to start and finish
            // Clamp01 ensures the value stays between 0 and 1
            float progress = Mathf.Clamp01((playerY - startY) / (finishY - startY));

            distanceSlider.value = progress;
        }
    }

    /// <summary>
    /// Tries to find the GameObject tagged as "Player" and assign its transform.
    /// </summary>
    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerFound = true;
            Debug.Log("Player found and assigned to DistanceUI.");
        }
        // No need for an error here, Update will keep trying
    }
}












// using UnityEngine;
// using UnityEngine.UI;

// public class DistanceUI : MonoBehaviour
// {
//     public Transform player;
//     public Transform startLine;
//     public Transform finishLine;
//     public Slider distanceSlider;

//     private float totalDistance;

//     void Start()
//     {
//         // محاسبه کل مسیر از خط شروع تا پایان
//         totalDistance = Vector3.Distance(startLine.position, finishLine.position);

//         // مقدار اسلایدر بین 0 و 1
//         distanceSlider.minValue = 0;
//         distanceSlider.maxValue = 1;
//     }

//     void Update()
//     {
//         // فاصله فعلی تا خط پایان
//         float remainingDistance = Vector3.Distance(player.position, finishLine.position);

//         // مقدار اسلایدر (هرچه به خط پایان نزدیک‌تر شود، مقدار به 1 نزدیک‌تر می‌شود)
//         distanceSlider.value = 1 - (remainingDistance / totalDistance);
//     }
// }
