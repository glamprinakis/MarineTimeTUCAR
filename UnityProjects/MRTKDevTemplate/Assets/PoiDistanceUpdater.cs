using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PoiDistanceUpdater : MonoBehaviour
{
    private Transform _userTransform;
    public TextMeshProUGUI poiDistanceText;
    public float refresh_rate = 1;
    // Start is called before the first frame update
    void Start()
    {
        _userTransform = Camera.main.transform;
        try
        {
            StartCoroutine(UpdateDistance());
        }
        catch (System.Exception)
        {
            Debug.LogWarning("InfoPanelController not found");
            throw;
        }
    }

    IEnumerator UpdateDistance()
    {
        while (true)
        {
            Vector3 objectPositionXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 userPositionXZ = new Vector3(_userTransform.position.x, 0, _userTransform.position.z);
            float distance = Vector3.Distance(objectPositionXZ, userPositionXZ);
            if (poiDistanceText != null)
                poiDistanceText.text = $"{distance:F2} m";
            yield return new WaitForSeconds(refresh_rate); // Wait for 1 second
        }
    }
}
