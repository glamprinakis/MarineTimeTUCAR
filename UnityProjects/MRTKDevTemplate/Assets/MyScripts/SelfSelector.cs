using UnityEngine;

public class SelfSelector : MonoBehaviour
{
    public void SelectSelf()
    {
        // Find the ObjectSpawner GameObject
        GameObject objectSpawner = GameObject.Find("ObjectSpawner");
        if (objectSpawner == null)
        {
            Debug.LogError("ObjectSpawner not found.");
            return;
        }

        // Get the GenerateObjectInFront component
        GenerateObjectInFront generateObjectInFront = objectSpawner.GetComponent<GenerateObjectInFront>();
        if (generateObjectInFront == null)
        {
            Debug.LogError("GenerateObjectInFront component not found on ObjectSpawner.");
            return;
        }

        // Set this GameObject as selected, which will deselect all others
        generateObjectInFront.SetObjectSelected(gameObject, true);
    }
}
