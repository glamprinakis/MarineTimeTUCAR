using System.Collections;
using WebSocketSharp;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;
using System.Linq;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;

public class WebSocketConnection : MonoBehaviour
{
    WebSocket ws;
    public TMP_InputField inputField;
    public Button sendButton, ChiefButton, Operator1Button, Operator2Button, Operator3Button, saveButton, loadButton;
    public string ip;
    public bool isApplicationQuitting = false;
    public int attempt = 0;
    public string uniqueID;
    NetworkReachability previousReachability;
    //serialized field game object to pass the game object to the script
    [SerializeField] private GameObject stick;
    [SerializeField] private AbstractMap mapGameObject;
    LocationArrayEditorLocationProvider locationProvider;

    public GPSMulti spawner;
    void Start()
    {
        spawner = GetComponent<GPSMulti>();
        //locationProvider = stick.GetComponent<LocationArrayEditorLocationProvider>();

        previousReachability = Application.internetReachability;
        uniqueID = SystemInfo.deviceUniqueIdentifier;

        // Attach the SendInputMessage method to the button's click event.
        // sendButton.onClick.AddListener(SendInputMessage);

        // ChiefButton.onClick.AddListener(() => LocationManager.ChangeStickPosition(stick, "35.512083, 24.019154"));
        // Operator1Button.onClick.AddListener(() => SendMessageToServer("2"));
        // Operator2Button.onClick.AddListener(() => SendMessageToServer("3"));
        // Operator3Button.onClick.AddListener(() => SendMessageToServer("4"));

        // saveButton.onClick.AddListener(SaveLastMessageInfo);
        // loadButton.onClick.AddListener(LoadLastMessageInfo);

        // Connect to the WebSocket server.
        ConnectToServer();
    }

    void SaveLastMessageInfo()
    {
        SaveLoadManager.SaveLastMessages();
        Debug.Log("Saved last messages");
    }

    void LoadLastMessageInfo()
    {
        Debug.Log("Loading...");
        string lastState = SaveLoadManager.LoadLastMessages();
        string[] lines = lastState.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            // Split the line into parts and remove the last part (the message)
            string[] parts = line.Split(',');
            string messageToSend = parts[0] + "," + parts[1] + ",seek";
            //Debug.Log("\n\nMessage to send: " + messageToSend + "\n\n");
            // Send the message to the server
            SendMessageToServer(messageToSend);
        }
    }

    void Update()
    {
        // Check if the down arrow key is pressed.
        // if (Input.GetKeyDown(KeyCode.DownArrow))
        // {
        //     // for (int i = 0; i < 30; i++)
        //     // {
        //     //     SendMessageToServer(i + ",0,seek");
        //     // }
        //     Debug.Log("Last data: " + SaveLoadManager.GetLastData());
        // }
        // if (Input.GetKeyDown(KeyCode.LeftArrow))
        // {
        //     Debug.Log("Cleared Save File\n");
        //     SaveLoadManager.ClearSaveFile();
        // }
        if (Application.internetReachability != previousReachability)
        {
            previousReachability = Application.internetReachability;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("Lost WiFi connection.");
            }
            else if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                Debug.Log("WiFi connection restored.");
                if (ws.ReadyState != WebSocketState.Open)
                {
                    Debug.Log("Reconnecting to server...");
                }
            }
        }
    }

    // This method is responsible for connecting to the WebSocket server.
    void ConnectToServer()
    {
        // Initialize the WebSocket connection to the specified URL.
        ws = new WebSocket("ws://" + ip + ":8080");
        Debug.Log("Connecting to server...");
        // Subscribe to the OnOpen event. This event is triggered when the connection is opened.
        ws.OnOpen += OnConnectionOpened;

        // Subscribe to the OnMessage event. This event is triggered when a message is received from the server.
        ws.OnMessage += OnMessageReceived;

        // Subscribe to the OnClose event. This event is triggered when the connection is closed.
        ws.OnClose += OnConnectionClosed;

        // Connect to the server asynchronously.
        ws.ConnectAsync();
    }

    // This method is called when the connection is opened.
    void OnConnectionOpened(object sender, EventArgs e)
    {
        if (attempt > 0)
        {
            Debug.Log("Reconnected to the server.");
            SendMessageToServer("Recome");
        }
        else
        {
            Debug.Log("Connected to the server");
            SendMessageToServer("Come");
        }
        SendMessageToServer(uniqueID + ",ID");
        attempt = 0;
    }

    // This method is called when a message is received from the server.
    void OnMessageReceived(object sender, MessageEventArgs e)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            HandleMessage(e.Data);
        });
    }

    void HandleMessage(string message)
    {

        //format the message
        string rawMessage;
        try
        {
            rawMessage = RawMessage(message);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("An error occurred when formatting the message: " + ex.Message);
            rawMessage = message;
        }
        Debug.Log(rawMessage.Contains("spawn"));

        if (message == "Readys")
        {
            Debug.Log("Ready!");
        }

        if (message.Contains("info"))
        {
            if (rawMessage.Contains("spawn"))
            {
                Debug.Log("Spawn section handling: " + rawMessage);
                // This assumes that the latitude and longitude are part of the 'raw_message'
                string[] spawnParts = rawMessage.Split(',');
                double latitude = double.Parse(spawnParts[0]);
                double longitude = double.Parse(spawnParts[1]);
                //print spawn parts
                Debug.Log("Latitude: " + latitude + " Longitude: " + longitude);
                if (spawner != null)
                {
                    Debug.Log("Spawning object at: " + latitude + ", " + longitude);
                    //spawner.SpawnObjectAtLocation(new GPSCoordinate { Latitude = latitude, Longitude = longitude });
                }
                else
                    Debug.Log("Spawner is null");
            }

            if (rawMessage.Contains("gps"))
            {
                Debug.Log("GPS Phone");

            }

            Debug.Log("Info section handling: " + message);
            //SaveLoadManager.AddMessage(int.Parse(message.Split(',')[1]), rawMessage, long.Parse(message.Split(',')[3]));
        }
    }
    // This method is called when the connection is closed.
    async void OnConnectionClosed(object sender, CloseEventArgs e)
    {
        Debug.Log($"Connection closed. Attempting to reconnect. Total attempts: {++attempt}");

        // Wait for a delay before attempting to reconnect. The delay increases with each failed attempt.
        int delay = 3000;
        await Task.Delay(delay);

        // Check if the application is quitting before attempting to reconnect.
        if (!isApplicationQuitting)
            ConnectToServer();
    }

    public void SendInputMessage()
    {
        string message = inputField.text;
        if (message == "ID")
        {
            SendMessageToServer(SystemInfo.deviceUniqueIdentifier);
        }
        else
        {
            SendMessageToServer(message);
        }
        inputField.text = ""; // Clear the input field
    }

    void SendMessageToServer(string message)
    {
        try
        {
            if (ws.ReadyState == WebSocketState.Open)
            {
                ws.Send(message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("An error occurred when trying to send a message: " + ex.Message);
        }
    }

    public static string RawMessage(string input)
    {
        // Split the message by commas
        string[] parts = input.Split(',');

        // Known positions
        string prefix = parts[0] + "," + parts[1]; // "safran4,4"
        string suffix = parts[parts.Length - 2] + "," + parts[parts.Length - 1]; // "5,info"

        // Extract raw_message starting from index 2 to parts.Length - 3
        string rawMessage = string.Join(",", parts, 2, parts.Length - 4);

        // Reformat into the desired structure
        return rawMessage;
    }


    // This method is called when the application is quitting.
    void OnApplicationQuit()
    {
        // Set the flag to indicate that the application is quitting.
        isApplicationQuitting = true;

        // Unsubscribe from the OnMessage event.
        ws.OnMessage -= OnMessageReceived;
        // Unsubscribe from the OnClose event.
        ws.OnClose -= OnConnectionClosed;
        ws.OnOpen -= OnConnectionOpened;

        Debug.Log("CYA");
        ws.CloseAsync();
    }
}
