using UnityEngine;
using Microsoft.MixedReality.QR; // For QRCodeWatcher, QRCodeAddedEventArgs, etc.
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the detection of QR codes and spawns/updates a QRCodeTracker prefab for each unique code.
/// </summary>
public class QRTrackingManager : MonoBehaviour
{
    [Tooltip("A prefab that has a QRCodeTracker component to track each code in the scene.")]
    public GameObject qrCodeTrackerPrefab;

    private QRCodeWatcher qrWatcher;
    private bool watcherStarted = false;

    // Dictionary of codeId -> QRCodeTracker instance
    public Dictionary<Guid, QRCodeTracker> ActiveTrackers = new Dictionary<Guid, QRCodeTracker>();

    private async void Start()
    {
        Debug.Log("[QRTrackingManager] Requesting access to QR code detection...");

        // Request access to the camera for QR scanning
        var status = await QRCodeWatcher.RequestAccessAsync();
        if (status == QRCodeWatcherAccessStatus.Allowed)
        {
            Debug.Log("[QRTrackingManager] Access to QR codes granted. Setting up QRCodeWatcher...");

            qrWatcher = new QRCodeWatcher();
            qrWatcher.Added   += OnQRCodeAdded;
            qrWatcher.Updated += OnQRCodeUpdated;
            qrWatcher.Removed += OnQRCodeRemoved;

            qrWatcher.Start();
            watcherStarted = true;

            Debug.Log("[QRTrackingManager] QRCodeWatcher started!");
        }
        else
        {
            Debug.LogError("[QRTrackingManager] Access to QR codes NOT allowed.");
        }
    }

    // -------------------------------------------------
    // Event: A new code is detected
    // -------------------------------------------------
    private void OnQRCodeAdded(object sender, QRCodeAddedEventArgs e)
    {
        // The QR events come off the main Unity thread,
        // so we marshal back using InvokeOnAppThread:
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            Debug.Log("[QRTrackingManager] OnQRCodeAdded fired.");

            var code = e.Code;
            Debug.Log($"[QRTrackingManager] Code ID: {code.Id}, Data: '{code.Data}'");
            Debug.Log($"[QRTrackingManager] Already in dictionary? {ActiveTrackers.ContainsKey(code.Id)}");

            // If we already have this code, do NOT create another tracker
            if (ActiveTrackers.ContainsKey(code.Id))
            {
                Debug.Log($"[QRTrackingManager] Code {code.Id} is already in dictionary, skipping creation.");
            }
            else
            {
                // This is a new code -> create a tracker
                Debug.Log($"[QRTrackingManager] Creating a new tracker object for code {code.Id}...");

                GameObject trackerObj = Instantiate(qrCodeTrackerPrefab);
                trackerObj.name = $"QRCodeTracker_{code.Id}"; // So you see it in the Hierarchy

                var tracker = trackerObj.GetComponent<QRCodeTracker>();
                if (tracker == null)
                {
                    Debug.LogError("[QRTrackingManager] The prefab does NOT have a QRCodeTracker component!");
                    return;
                }

                // Initialize the tracker with the newly detected QR code info
                tracker.Initialize(code);

                // Store it in the dictionary
                ActiveTrackers[code.Id] = tracker;

                Debug.Log($"[QRTrackingManager] Added code: '{code.Data}', ID: {code.Id}");
            }
        }, false);
    }

    // -------------------------------------------------
    // Event: An existing code has updated tracking info
    // -------------------------------------------------
    private void OnQRCodeUpdated(object sender, QRCodeUpdatedEventArgs e)
    {
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            var code = e.Code;
            Debug.Log($"[QRTrackingManager] OnQRCodeUpdated fired for code ID: {code.Id}, Data: '{code.Data}'");

            if (ActiveTrackers.TryGetValue(code.Id, out var tracker))
            {
                Debug.Log($"[QRTrackingManager] Found existing tracker for code {code.Id}, re-initializing it.");
                tracker.Initialize(code);  // or partial updates if needed
            }
            else
            {
                Debug.LogWarning($"[QRTrackingManager] Updated code {code.Id}, but no tracker found in dictionary.");
            }
        }, false);
    }

    // -------------------------------------------------
    // Event: A previously detected code is removed/lost
    // -------------------------------------------------
    private void OnQRCodeRemoved(object sender, QRCodeRemovedEventArgs e)
    {
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            var code = e.Code;
            Debug.Log($"[QRTrackingManager] OnQRCodeRemoved fired for code ID: {code.Id}, Data: '{code.Data}'");

            if (ActiveTrackers.TryGetValue(code.Id, out var tracker))
            {
                Destroy(tracker.gameObject);
                ActiveTrackers.Remove(code.Id);
                Debug.Log($"[QRTrackingManager] Removed code: '{code.Data}', ID: {code.Id}");
            }
            else
            {
                Debug.LogWarning($"[QRTrackingManager] Trying to remove code {code.Id}, but no tracker found in dictionary.");
            }
        }, false);
    }
}
