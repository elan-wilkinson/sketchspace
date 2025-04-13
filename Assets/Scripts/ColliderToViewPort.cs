using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ColliderToViewport : MonoBehaviour
{
    public Camera cam; // Assign your camera (usually main)

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        Collider2D col = GetComponent<Collider2D>();
        Bounds bounds = col.bounds; // Includes scale!

        // Get corners of the bounds
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // Convert to viewport coordinates
        Vector3 viewportMin = cam.WorldToViewportPoint(min);
        Vector3 viewportMax = cam.WorldToViewportPoint(max);

        // Viewport rectangle (x, y, width, height), all from 0 to 1
        Rect viewportRect = new Rect(
            viewportMin.x,
            viewportMin.y,
            viewportMax.x - viewportMin.x,
            viewportMax.y - viewportMin.y
        );

        Debug.Log("Collider viewport rect: " + viewportRect);
    }
}
