using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;

public class ScalingScript : MonoBehaviour
{
    private List<SelectableGameObject> generatedObjects; // Reference to the list in the GenerateObjectInFront component

    public string objectSpawnerName = "ObjectSpawner"; // Name of the ObjectSpawner GameObject in the hierarchy
    public string magicWindowName = "UX.Slate.MagicWindow"; // Name of the child GameObject to act on
    public string scaleUpXButtonName = "Scale Up X"; // Name of the Scale X button
    public string scaleUpYButtonName = "Scale Up Y"; // Name of the Scale Y button
    public string scaleDownXButtonName = "Scale Down X"; // Name of the Scale Down X button
    public string scaleDownYButtonName = "Scale Down Y"; // Name of the Scale Down Y button
    public string toggleFreezeButtonName = "PushToSet"; // Name of the Toggle Freeze button

    public float scaleSpeed = 0.1f; // Adjust the speed of scaling
    private bool isScalingX = false;
    private bool isScalingY = false;
    private bool isScalingDownX = false;
    private bool isScalingDownY = false;
    private bool isFrozen = false;
    private ObjectManipulator objectManipulator;

    void Start()
    {
        // Find the ObjectSpawner GameObject in the hierarchy
        GameObject objectSpawner = GameObject.Find(objectSpawnerName);
        if (objectSpawner != null)
        {
            GenerateObjectInFront generateObjectInFront = objectSpawner.GetComponent<GenerateObjectInFront>();
            if (generateObjectInFront != null)
            {
                generatedObjects = generateObjectInFront.generatedObjects;
            }
            else
            {
                Debug.LogError("GenerateObjectInFront component not found on ObjectSpawner GameObject.");
            }
        }
        else
        {
            Debug.LogError("ObjectSpawner GameObject not found in the hierarchy.");
        }

        // Find and assign buttons dynamically
        PressableButton scaleUpXButton = FindButtonByName(transform, scaleUpXButtonName);
        PressableButton scaleUpYButton = FindButtonByName(transform, scaleUpYButtonName);
        PressableButton scaleDownXButton = FindButtonByName(transform, scaleDownXButtonName);
        PressableButton scaleDownYButton = FindButtonByName(transform, scaleDownYButtonName);
        PressableButton toggleFreezeButton = FindButtonByName(transform, toggleFreezeButtonName);

        if (scaleUpXButton != null)
        {
            scaleUpXButton.OnClicked.AddListener(() => ToggleScaleX());
            Debug.Log("Scale Up X Button assigned.");
        }
        else
        {
            Debug.LogError("Scale Up X Button not found.");
        }

        if (scaleUpYButton != null)
        {
            scaleUpYButton.OnClicked.AddListener(() => ToggleScaleY());
            Debug.Log("Scale Up Y Button assigned.");
        }
        else
        {
            Debug.LogError("Scale Up Y Button not found.");
        }

        if (scaleDownXButton != null)
        {
            scaleDownXButton.OnClicked.AddListener(() => ToggleScaleDownX());
            Debug.Log("Scale Down X Button assigned.");
        }
        else
        {
            Debug.LogError("Scale Down X Button not found.");
        }

        if (scaleDownYButton != null)
        {
            scaleDownYButton.OnClicked.AddListener(() => ToggleScaleDownY());
            Debug.Log("Scale Down Y Button assigned.");
        }
        else
        {
            Debug.LogError("Scale Down Y Button not found.");
        }

        if (toggleFreezeButton != null)
        {
            //toggleFreezeButton.OnClicked.AddListener(() => ToggleFreeze());
            Debug.Log("Toggle Freeze Button assigned.");
        }
        else
        {
            Debug.LogError("Toggle Freeze Button not found.");
        }

        UpdateSelectedObjectManipulator();
    }

    void Update()
    {
        if (isScalingX)
        {
            ScaleUpX();
        }

        if (isScalingY)
        {
            ScaleUpY();
        }

        if (isScalingDownX)
        {
            ScaleDownX();
        }

        if (isScalingDownY)
        {
            ScaleDownY();
        }
    }

    private PressableButton FindButtonByName(Transform panelTransform, string buttonName)
    {
        Transform buttonTransform = panelTransform.Find(buttonName);
        if (buttonTransform != null)
        {
            return buttonTransform.GetComponent<PressableButton>();
        }
        return null;
    }

    private void UpdateSelectedObjectManipulator()
    {
        if (generatedObjects == null)
        {
            Debug.LogError("Generated objects list is null.");
            return;
        }

        foreach (var item in generatedObjects)
        {
            if (item.selected)
            {
                if (item.gameObject != null)
                {
                    Transform magicWindowTransform = item.gameObject.transform.Find(magicWindowName);
                    if (magicWindowTransform != null)
                    {
                        objectManipulator = magicWindowTransform.GetComponent<ObjectManipulator>();
                        if (objectManipulator == null)
                        {
                            Debug.LogError("ObjectManipulator component not found on selected object.");
                        }
                        else
                        {
                            Debug.Log("ObjectManipulator component found on selected object.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Child GameObject '{magicWindowName}' not found on selected object.");
                    }
                }
                return;
            }
        }
        Debug.LogError("No selected object found.");
    }

    public void ToggleScaleX()
    {
        if (isFrozen)
        {
            Debug.Log("Panel is frozen. Scaling is disabled.");
            return;
        }

        isScalingX = !isScalingX;
        if (isScalingX)
        {
            isScalingY = false; // Ensure only one scaling operation is active
            isScalingDownX = false;
            isScalingDownY = false;
            Debug.Log("Started scaling X.");
        }
        else
        {
            Debug.Log("Stopped scaling X.");
        }
    }

    public void ToggleScaleY()
    {
        if (isFrozen)
        {
            Debug.Log("Panel is frozen. Scaling is disabled.");
            return;
        }

        isScalingY = !isScalingY;
        if (isScalingY)
        {
            isScalingX = false; // Ensure only one scaling operation is active
            isScalingDownX = false;
            isScalingDownY = false;
            Debug.Log("Started scaling Y.");
        }
        else
        {
            Debug.Log("Stopped scaling Y.");
        }
    }

    public void ToggleScaleDownX()
    {
        if (isFrozen)
        {
            Debug.Log("Panel is frozen. Scaling is disabled.");
            return;
        }

        isScalingDownX = !isScalingDownX;
        if (isScalingDownX)
        {
            isScalingX = false; // Ensure only one scaling operation is active
            isScalingY = false;
            isScalingDownY = false;
            Debug.Log("Started scaling down X.");
        }
        else
        {
            Debug.Log("Stopped scaling down X.");
        }
    }

    public void ToggleScaleDownY()
    {
        if (isFrozen)
        {
            Debug.Log("Panel is frozen. Scaling is disabled.");
            return;
        }

        isScalingDownY = !isScalingDownY;
        if (isScalingDownY)
        {
            isScalingX = false; // Ensure only one scaling operation is active
            isScalingY = false;
            isScalingDownX = false;
            Debug.Log("Started scaling down Y.");
        }
        else
        {
            Debug.Log("Stopped scaling down Y.");
        }
    }

    public void ToggleFreeze()
    {
        isFrozen = !isFrozen;
        if (isFrozen)
        {
            // Disable manipulation and stop any ongoing scaling
            if (objectManipulator != null)
            {
                objectManipulator.enabled = false;
            }
            isScalingX = false;
            isScalingY = false;
            isScalingDownX = false;
            isScalingDownY = false;
            Debug.Log("Panel frozen.");
        }
        else
        {
            // Enable manipulation
            if (objectManipulator != null)
            {
                objectManipulator.enabled = true;
            }
            Debug.Log("Panel unfrozen.");
        }
    }

    public void ScaleUpX()
    {
        foreach (var item in generatedObjects)
        {
            if (item.selected && item.gameObject != null)
            {
                Transform magicWindowTransform = item.gameObject.transform.Find(magicWindowName);
                if (magicWindowTransform != null)
                {
                    float scaleFactor = scaleSpeed * Time.deltaTime;
                    Vector3 scaleChange = new Vector3(scaleFactor, 0, 0);
                    magicWindowTransform.localScale += scaleChange;
                    Debug.Log("Scaling X by " + scaleFactor);
                }
                return;
            }
        }
    }

    public void ScaleUpY()
    {
        foreach (var item in generatedObjects)
        {
            if (item.selected && item.gameObject != null)
            {
                Transform magicWindowTransform = item.gameObject.transform.Find(magicWindowName);
                if (magicWindowTransform != null)
                {
                    float scaleFactor = scaleSpeed * Time.deltaTime;
                    Vector3 scaleChange = new Vector3(0, scaleFactor, 0);
                    magicWindowTransform.localScale += scaleChange;
                    Debug.Log("Scaling Y by " + scaleFactor);
                }
                return;
            }
        }
    }

    public void ScaleDownX()
    {
        foreach (var item in generatedObjects)
        {
            if (item.selected && item.gameObject != null)
            {
                Transform magicWindowTransform = item.gameObject.transform.Find(magicWindowName);
                if (magicWindowTransform != null)
                {
                    float scaleFactor = scaleSpeed * Time.deltaTime;
                    Vector3 scaleChange = new Vector3(-scaleFactor, 0, 0);
                    magicWindowTransform.localScale += scaleChange;
                    Debug.Log("Scaling down X by " + scaleFactor);
                }
                return;
            }
        }
    }

    public void ScaleDownY()
    {
        foreach (var item in generatedObjects)
        {
            if (item.selected && item.gameObject != null)
            {
                Transform magicWindowTransform = item.gameObject.transform.Find(magicWindowName);
                if (magicWindowTransform != null)
                {
                    float scaleFactor = scaleSpeed * Time.deltaTime;
                    Vector3 scaleChange = new Vector3(0, -scaleFactor, 0);
                    magicWindowTransform.localScale += scaleChange;
                    Debug.Log("Scaling down Y by " + scaleFactor);
                }
                return;
            }
        }
    }
}
