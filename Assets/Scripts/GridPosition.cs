using UnityEngine;

public class GridPosition : MonoBehaviour
{
    [SerializeField] private int x, y;

    /// <summary>
    /// called when the user presses the left mouse button while over the Collider
    /// Note: This function is not called on objects that belong to Ignore Raycast layer
    /// </summary>
    private void OnMouseDown()
    {
        Debug.Log($"CLICKED ON {x}, {y}!");
    }
}