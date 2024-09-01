using Mapbox.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum POIType
{
    CollisionPoint,
    Ship,
    OtherShip,
    MyRoutePoint,
    OtherRoutePoint,
    General
}

[System.Serializable]
public class GPSCoordinate
{
    [Tooltip("Input format: 'Latitude,Longitude' e.g., '34.0522,-118.2437'")]
    public string coordinates;
    public POIType POIType;

    public double Latitude
    {
        get
        {
            return double.Parse(coordinates.Split(',')[0].Trim());
        }
    }

    public double Longitude
    {
        get
        {
            return double.Parse(coordinates.Split(',')[1].Trim());
        }
    }
}

public class GPSMulti : MonoBehaviour
{
    [Header("User Coordinates")]
    public double myLatitude = 0.0;
    public double myLongitude = 0.0;

    [Header("POIs Coordinates")]
    [Tooltip("Add all object coordinates here")]
    public List<GPSCoordinate> staticCoordinates = new List<GPSCoordinate>();

    [Header("Route Coordinates")]
    [Tooltip("Add all object coordinates here")]
    public List<GPSCoordinate> RoutecCoordinates = new List<GPSCoordinate>();

    [Header("Area Coordinates")]
    public List<string> areaCoordinates;

    [Header("POI Prefabs")]
    public GameObject CollisionPointPrefab;
    public GameObject ShipPrefab;
    public GameObject MyRoutePointPrefab;
    public GameObject OtherRoutePointPrefab;
    public GameObject GeneralPrefab;

    [Header("Map Prefabs")]
    public GameObject CollisionPointMapPrefab;
    public GameObject MyShipMapPrefab;
    public GameObject OtherShipMapPrefab;
    public GameObject MyRoutePointMapPreafab;
    public GameObject OtherRoutePointMapPrefab;
    public GameObject generalPointMapPrefab;



    public double referenceLatitude = 0.0;
    public double referenceLongitude = 0.0;
    public bool referenceCoordinatesSet = false;  // Add this line
    public GameObject mapGameObject;

    private Dictionary<POIType, GameObject> poiPrefabs;

    private Dictionary<POIType, GameObject> mapPrefabs;

    public Dictionary<POIType, List<GameObject>> spawnedObjects = new Dictionary<POIType, List<GameObject>>();

    private void Awake()
    {
        poiPrefabs = new Dictionary<POIType, GameObject>
        {
            { POIType.CollisionPoint, CollisionPointPrefab },
            { POIType.Ship, ShipPrefab },
            { POIType.OtherShip, ShipPrefab },
            { POIType.MyRoutePoint, MyRoutePointPrefab },
            { POIType.OtherRoutePoint, OtherRoutePointPrefab },
            { POIType.General, GeneralPrefab }
        };

        mapPrefabs = new Dictionary<POIType, GameObject>
        {
            { POIType.CollisionPoint, CollisionPointMapPrefab },
            { POIType.Ship, MyShipMapPrefab },
            { POIType.OtherShip, OtherShipMapPrefab },
            { POIType.MyRoutePoint, MyRoutePointMapPreafab },
            { POIType.OtherRoutePoint, OtherRoutePointMapPrefab },
            { POIType.General, generalPointMapPrefab }
    };
    }



    [Header("Line Renderer")]
    public LineRenderer myRouteLineRenderer;   // Assign this via the Inspector
    public LineRenderer otherRouteLineRenderer; // Assign this via the Inspector

    public LineRenderer myRouteUncertainLineRenderer;
    public LineRenderer otherRouteUncertainLineRenderer;

    // Public variables for uncertain line renderers
    public float myRouteUncertainStartWidth = 0.5f;
    public float myRouteUncertainEndWidth = 10f;
    public float otherRouteUncertainStartWidth = 0.5f;
    public float otherRouteUncertainEndWidth = 10f;

    // Public variables for offset
    public float myRouteUncertainYOffset = -0.1f;
    public float otherRouteUncertainYOffset = -0.1f;





    private void Start()
    {
        StartCoroutine(InitializeGPSMulti());
        foreach (string area in areaCoordinates)
        {
            List<GPSCoordinate> myAreaCoordinates = ParseCoordinates(area);
            CreateArea(myAreaCoordinates);  // Pass the list to CreateArea
        }
    }

    private IEnumerator InitializeGPSMulti()
    {
        // Wait until the end of the frame to ensure DataDeserialization has finished initializing
        yield return new WaitForEndOfFrame();

        // Create LineRenderers for the uncertain routes if not assigned
        if (myRouteUncertainLineRenderer == null)
        {
            // Create a LineRenderer for MyRouteUncertain if not assigned
            GameObject myRouteUncertainLineObj = new GameObject("MyRouteUncertainLineObject");
            myRouteUncertainLineRenderer = myRouteUncertainLineObj.AddComponent<LineRenderer>();
            myRouteUncertainLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        if (otherRouteUncertainLineRenderer == null)
        {
            // Create a LineRenderer for OtherRouteUncertain if not assigned
            GameObject otherRouteUncertainLineObj = new GameObject("OtherRouteUncertainLineObject");
            otherRouteUncertainLineRenderer = otherRouteUncertainLineObj.AddComponent<LineRenderer>();
            otherRouteUncertainLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Create new LineRenderers for the routes (without width change)
        if (myRouteLineRenderer == null)
        {
            // Create a LineRenderer for MyRoute if not assigned
            GameObject myRouteLineObj = new GameObject("MyRouteLineObject");
            myRouteLineRenderer = myRouteLineObj.AddComponent<LineRenderer>();
            myRouteLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        if (otherRouteLineRenderer == null)
        {
            // Create a LineRenderer for OtherRoute if not assigned
            GameObject otherRouteLineObj = new GameObject("OtherRouteLineObject");
            otherRouteLineRenderer = otherRouteLineObj.AddComponent<LineRenderer>();
            otherRouteLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Spawn static objects
        foreach (var coord in staticCoordinates)
        {
            GameObject pointObject = SpawnObjectAtLocation(coord);
            spawnedObjects[coord.POIType].Add(pointObject); // Store the GameObject
        }
        Debug.Log(" Static" + spawnedObjects[POIType.General].Count);

        // Spawn route objects
        foreach (var coord in RoutecCoordinates)
        {
            GameObject pointObject = SpawnObjectAtLocation(coord);
            spawnedObjects[coord.POIType].Add(pointObject);
        }

        if (DataDeserialization.Instance != null)
        {
            ShipData shipData = DataDeserialization.Instance.ShipData;

            if (shipData.ship1_traj_lon != null && shipData.ship1_traj_lat != null)
            {
                for (int i = 0; i < shipData.ship1_traj_lon.Length; i++)
                {
                    GPSCoordinate coordinates = new GPSCoordinate
                    {
                        coordinates = shipData.ship1_traj_lat[i] + "," + shipData.ship1_traj_lon[i],
                        POIType = POIType.MyRoutePoint
                    };
                    GameObject pointObject = SpawnObjectAtLocation(coordinates);
                    spawnedObjects[coordinates.POIType].Add(pointObject);
                    Debug.Log("Ship 1111111: " + shipData.ship1_traj_lat[i] + shipData.ship1_traj_lon[i]);
                }

                // Initialize the MyRouteLineRenderer position count
                myRouteLineRenderer.positionCount = spawnedObjects[POIType.MyRoutePoint].Count;
                myRouteUncertainLineRenderer.positionCount = spawnedObjects[POIType.MyRoutePoint].Count;
            }

            if (shipData.ship2_traj_lon != null && shipData.ship2_traj_lat != null)
            {
                for (int i = 0; i < shipData.ship2_traj_lon.Length; i++)
                {
                    GPSCoordinate coordinates = new GPSCoordinate
                    {
                        coordinates = shipData.ship2_traj_lat[i] + "," + shipData.ship2_traj_lon[i],
                        POIType = POIType.OtherRoutePoint
                    };
                    GameObject pointObject = SpawnObjectAtLocation(coordinates);
                    spawnedObjects[coordinates.POIType].Add(pointObject);
                    Debug.Log("Ship 222222: " + shipData.ship2_traj_lat[i] + shipData.ship2_traj_lon[i]);
                }

                // Initialize the OtherRouteLineRenderer position count
                otherRouteLineRenderer.positionCount = spawnedObjects[POIType.OtherRoutePoint].Count;
                otherRouteUncertainLineRenderer.positionCount = spawnedObjects[POIType.OtherRoutePoint].Count;
            }
        }
        else
        {
            Debug.LogError("DataDeserialization instance is not available.");
        }
    }

    private void Update()
    {
        int myRoutePointCount = spawnedObjects[POIType.MyRoutePoint].Count;
        int otherRoutePointCount = spawnedObjects[POIType.OtherRoutePoint].Count;

        if (myRoutePointCount > 0)
        {
            // Update certain line renderer positions (no width change)
            myRouteLineRenderer.positionCount = myRoutePointCount;
            for (int i = 0; i < myRoutePointCount; i++)
            {
                if (spawnedObjects[POIType.MyRoutePoint][i] != null)
                {
                    myRouteLineRenderer.SetPosition(i, spawnedObjects[POIType.MyRoutePoint][i].transform.position);
                }
            }

            // Update uncertain line renderer positions (with progressive width change)
            myRouteUncertainLineRenderer.positionCount = myRoutePointCount;
            SetLineRendererWidthProgressive(myRouteUncertainLineRenderer, myRouteUncertainStartWidth, myRouteUncertainEndWidth);
            for (int i = 0; i < myRoutePointCount; i++)
            {
                if (spawnedObjects[POIType.MyRoutePoint][i] != null)
                {
                    Vector3 offsetPosition = spawnedObjects[POIType.MyRoutePoint][i].transform.position;
                    offsetPosition.y += myRouteUncertainYOffset;
                    myRouteUncertainLineRenderer.SetPosition(i, offsetPosition);
                }
            }
        }

        if (otherRoutePointCount > 0)
        {
            // Update certain line renderer positions (no width change)
            otherRouteLineRenderer.positionCount = otherRoutePointCount;
            for (int i = 0; i < otherRoutePointCount; i++)
            {
                if (spawnedObjects[POIType.OtherRoutePoint][i] != null)
                {
                    otherRouteLineRenderer.SetPosition(i, spawnedObjects[POIType.OtherRoutePoint][i].transform.position);
                }
            }

            // Update uncertain line renderer positions (with progressive width change)
            otherRouteUncertainLineRenderer.positionCount = otherRoutePointCount;
            SetLineRendererWidthProgressive(otherRouteUncertainLineRenderer, otherRouteUncertainStartWidth, otherRouteUncertainEndWidth);
            for (int i = 0; i < otherRoutePointCount; i++)
            {
                if (spawnedObjects[POIType.OtherRoutePoint][i] != null)
                {
                    Vector3 offsetPosition = spawnedObjects[POIType.OtherRoutePoint][i].transform.position;
                    offsetPosition.y += otherRouteUncertainYOffset;
                    otherRouteUncertainLineRenderer.SetPosition(i, offsetPosition);
                }
            }
        }

        AdjustPointDirections();
    }


    private void SetLineRendererWidth(LineRenderer lineRenderer, float width)
    {
        // Set the start and end width of the LineRenderer to be constant
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        // Reset the width curve to a straight line
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, width);
        widthCurve.AddKey(1.0f, width);

        // Apply the width curve to the LineRenderer
        lineRenderer.widthCurve = widthCurve;
    }

    private void SetLineRendererWidthProgressive(LineRenderer lineRenderer, float startWidth, float endWidth)
    {
        // Set the start and end width of the LineRenderer to different values for a progressive effect
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;

        // Create a width curve to make the line progressively wider
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0.0f, startWidth);
        widthCurve.AddKey(1.0f, endWidth);

        // Apply the width curve to the LineRenderer
        lineRenderer.widthCurve = widthCurve;
    }









    public void UpdateReferenceCoordinates(double latitude, double longitude)
    {
        referenceLatitude = latitude;
        referenceLongitude = longitude;
        referenceCoordinatesSet = true;
    }

    public void SpawnPOIForMap(GPSCoordinate coord)
    {
        if (referenceCoordinatesSet)
        {
            try
            {
                GameObject prefab = mapPrefabs[coord.POIType];
                //spawn the object as child of mapGameObject
                GameObject createdObj = Instantiate(prefab, mapGameObject.transform);
                // Set created object's position to 0,0.1,0
                //createdObj.transform.localPosition = new Vector3(0, 0, 0);
                // Add to mapObjects list
                mapGameObject.GetComponentInParent<MapAndPlayerManager>().spawnedPOIs.Add(createdObj, new Vector2d(coord.Latitude, coord.Longitude));
                mapGameObject.GetComponentInParent<MapAndPlayerManager>().UpdateSpawnedPOIs();
                // Track instances for MyRoutePointMapPrefab and OtherRoutePointMapPrefab
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to spawn map point object: " + ex.ToString());
            }
        }
        else
        {
            Debug.LogError("Reference coordinates not set");
        }


    }


    public GameObject SpawnObjectAtLocation(GPSCoordinate coord)
    {
        Vector3 position = CalculateObjectLocalPosition(coord.Latitude, coord.Longitude);
        GameObject preafab = poiPrefabs[coord.POIType];
        GameObject createdObj = Instantiate(preafab, position, Quaternion.identity);
        if (!spawnedObjects.ContainsKey(coord.POIType))
        {
            spawnedObjects[coord.POIType] = new List<GameObject>();
        }
        spawnedObjects[coord.POIType].Add(createdObj);
        SpawnPOIForMap(coord);
        return createdObj;
    }

    Vector3 CalculateObjectLocalPosition(double latitude, double longitude)
    {
        double latOffset = (latitude - myLatitude) * 111000.0;  // meters per latitude degree
        double lonOffset = (longitude - myLongitude) * (111000.0 * Mathf.Cos((float)(myLatitude * Mathf.PI / 180.0)));
        return new Vector3((float)lonOffset, 0, (float)latOffset); // Changed Y to 0 for visualization on a flat plane
    }

    void AdjustPointDirections()
    {
        AdjustDirectionsForPoints(POIType.MyRoutePoint);
        AdjustDirectionsForPoints(POIType.OtherRoutePoint);
    }

    void AdjustDirectionsForPoints(POIType pointType)
    {
        var pointsList = spawnedObjects[pointType];

        for (int i = 0; i < pointsList.Count - 1; i++)
        {
            Transform currentPoint = pointsList[i].transform;
            Transform nextPoint = pointsList[i + 1].transform;

            Vector3 direction = nextPoint.position - currentPoint.position;
            if (direction != Vector3.zero)
            {
                currentPoint.LookAt(nextPoint);
            }
        }

        // Handle the last object separately
        if (pointsList.Count > 1)
        {
            Transform lastPoint = pointsList[pointsList.Count - 1].transform;
            Transform secondLastPoint = pointsList[pointsList.Count - 2].transform;

            Vector3 lastDirection = secondLastPoint.position - lastPoint.position;
            if (lastDirection != Vector3.zero)
            {
                lastPoint.rotation = secondLastPoint.rotation;
            }
        }
    }

    public void CreateArea(List<GPSCoordinate> areaCoordinates)
    {
        GameObject areaGameObject = new GameObject("DynamicArea");
        MeshFilter meshFilter = areaGameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = areaGameObject.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = areaGameObject.AddComponent<MeshCollider>();
        meshCollider.convex = false;
        meshCollider.isTrigger = false;

        meshRenderer.material = new Material(Shader.Find("Standard"));
        meshRenderer.material.color = Color.white;

        AreaScript areaScript = areaGameObject.AddComponent<AreaScript>();

        // Convert GPS coordinates to local positions and create vertices
        Vector3[] vertices = new Vector3[areaCoordinates.Count];
        for (int i = 0; i < areaCoordinates.Count; i++)
        {
            vertices[i] = CalculateObjectLocalPosition(areaCoordinates[i].Latitude, areaCoordinates[i].Longitude);
        }

        // Assuming the area is flat on the ground, you only need to define vertices in the XZ plane
        int[] triangles = Triangulate(vertices.Length); // You need to implement this based on your vertices

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    // Simple triangulation function for convex shapes. For complex shapes, use a proper triangulation library.
    private int[] Triangulate(int vertexCount)
    {
        List<int> triangles = new List<int>();
        for (int i = 1; i < vertexCount - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }
        return triangles.ToArray();
    }

    public List<GPSCoordinate> ParseCoordinates(string coordinatesString)
    {
        List<GPSCoordinate> coordinatesList = new List<GPSCoordinate>();
        string[] coordinatePairs = coordinatesString.Split(new char[] { ' ', '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < coordinatePairs.Length; i += 2)
        {
            double latitude = double.Parse(coordinatePairs[i], CultureInfo.InvariantCulture);
            double longitude = double.Parse(coordinatePairs[i + 1], CultureInfo.InvariantCulture);

            // Add the parsed coordinates to the list
            coordinatesList.Add(new GPSCoordinate
            {
                coordinates = $"{latitude},{longitude}",
                POIType = POIType.General
            });
        }

        return coordinatesList;
    }
}
