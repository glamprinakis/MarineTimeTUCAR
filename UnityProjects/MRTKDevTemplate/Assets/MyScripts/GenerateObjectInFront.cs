using UnityEngine;
using System.Collections.Generic;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit;

public class GenerateObjectInFront : MonoBehaviour
{
    public GameObject objectPrefab;  // The prefab for the object to be generated
    public GameObject panelPrefab; // The GameObject to enable/disable
    public List<SelectableGameObject> generatedObjects = new List<SelectableGameObject>(); // List to store generated objects with their selected status

    // This function generates a GameObject 1 unit in front of the user
    public void GenerateObject()
    {
        if (objectPrefab != null)
        {
            // Get the camera's transform
            Transform cameraTransform = Camera.main.transform;

            // Calculate the position 1 unit in front of the camera
            Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * 1.0f;

            // Instantiate the object at the calculated position with the same rotation as the camera
            GameObject newObject = Instantiate(objectPrefab, spawnPosition, cameraTransform.rotation);

            // Create a new SelectableGameObject and add it to the list
            SelectableGameObject selectableObject = new SelectableGameObject(newObject, false);
            generatedObjects.Add(selectableObject);

            // Add StatefulInteractable component if not already present
            if (newObject.GetComponent<StatefulInteractable>() == null)
            {
                newObject.AddComponent<StatefulInteractable>();
            }

            // Get the StatefulInteractable component and set up the onClick event
            StatefulInteractable interactable = newObject.GetComponent<StatefulInteractable>();
            interactable.OnClicked.AddListener(() => newObject.GetComponent<SelfSelector>().SelectSelf());
        }
        else
        {
            Debug.LogError("Object prefab is not assigned.");
        }
    }

    // Public function to set the selected state of a specific object and deselect others
    public void SetObjectSelected(GameObject obj, bool isSelected)
    {
        // Deselect all other objects
        foreach (SelectableGameObject selectable in generatedObjects)
        {
            if (selectable.gameObject == obj)
            {
                selectable.selected = isSelected;
            }
            else
            {
                selectable.selected = false;
            }
        }
    }

    // Function to disable manipulation and selection for all objects and disable the additional GameObject
    public void DisableManipulationAndSelection()
    {
        foreach (SelectableGameObject selectable in generatedObjects)
        {
            // Disable ObjectManipulator if it exists
            ObjectManipulator manipulator = selectable.gameObject.GetComponent<ObjectManipulator>();
            if (manipulator != null)
            {
                manipulator.enabled = false;
            }

            // Disable the StatefulInteractable to prevent selection
            StatefulInteractable interactable = selectable.gameObject.GetComponent<StatefulInteractable>();
            if (interactable != null)
            {
                interactable.enabled = false;
            }
        }

        // Disable the additional GameObject
        if (panelPrefab != null)
        {
            panelPrefab.SetActive(false);
        }
    }

    // Function to enable manipulation and selection for all objects and enable the additional GameObject
    public void EnableManipulationAndSelection()
    {
        foreach (SelectableGameObject selectable in generatedObjects)
        {
            // Enable ObjectManipulator if it exists
            ObjectManipulator manipulator = selectable.gameObject.GetComponent<ObjectManipulator>();
            if (manipulator != null)
            {
                manipulator.enabled = true;
            }

            // Enable the StatefulInteractable to allow selection
            StatefulInteractable interactable = selectable.gameObject.GetComponent<StatefulInteractable>();
            if (interactable != null)
            {
                interactable.enabled = true;
            }
        }

        // Enable the additional GameObject
        if (panelPrefab != null)
        {
            panelPrefab.SetActive(true);
        }
    }
}
