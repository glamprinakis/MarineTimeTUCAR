using UnityEngine;
using Microsoft.MixedReality.QR;
using System; // For Guid, EventHandler
using System.Collections.Generic;

#if WINDOWS_UWP
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;
#endif

public class MultiPhaseQRDemo : MonoBehaviour
{
    /// <summary>
    /// Simple struct to store how we want to align the EnvironmentRoot when a particular QR code is recognized.
    /// </summary>
    [Serializable]
    public struct QROffsetData
    {
        public Guid codeId;
        public Vector3 positionOffset;
        public Quaternion rotationOffset;
    }

    [Header("References")]
    [Tooltip("Parent of all your virtual objects. Moving this repositions everything.")]
    public Transform environmentRoot;

    // -------------------- Internals for QR scanning --------------------
    private QRCodeWatcher qrWatcher = null;

    // We remember each QR code's ID once we see it during calibration.
    // We also store the offset that tells us how to align EnvironmentRoot
    // when we see that code again in operation mode.
    private Dictionary<Guid, QROffsetData> codeOffsets = new Dictionary<Guid, QROffsetData>();

    // Keep track if we've seen/registered this code ID during calibration
    private HashSet<Guid> rememberedCodes = new HashSet<Guid>();

    private bool isCalibrationPhase = true;

    // -------------------- Unity Lifecycle --------------------
    private async void Start()
    {
        // Ask for permission to use the camera and detect QR codes
        var accessStatus = await QRCodeWatcher.RequestAccessAsync();
        if (accessStatus == QRCodeWatcherAccessStatus.Allowed)
        {
            qrWatcher = new QRCodeWatcher();
            qrWatcher.Added += OnQRCodeAdded;
            qrWatcher.Updated += OnQRCodeUpdated;
            qrWatcher.Removed += OnQRCodeRemoved;

            qrWatcher.Start();
            Debug.Log("[MultiPhaseQRDemo] QRCodeWatcher started. Calibration phase active.");
        }
        else
        {
            Debug.LogError("[MultiPhaseQRDemo] Access to QR codes is not allowed.");
        }
    }

    // -------------------- QR Events --------------------
    private void OnQRCodeAdded(object sender, QRCodeAddedEventArgs e)
    {
        // Make sure we jump to Unity main thread for Unity API calls
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            HandleNewOrUpdatedCode(e.Code);
        }, false);
    }

    private void OnQRCodeUpdated(object sender, QRCodeUpdatedEventArgs e)
    {
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            HandleNewOrUpdatedCode(e.Code);
        }, false);
    }

    private void OnQRCodeRemoved(object sender, QRCodeRemovedEventArgs e)
    {
        // Not strictly necessary for this example, but let's log it
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            Debug.Log($"[MultiPhaseQRDemo] QR code removed: {e.Code.Data}, ID = {e.Code.Id}");
        }, false);
    }

    /// <summary>
    /// Called whenever a code is initially detected (Added) or updated (improved tracking).
    /// </summary>
    private void HandleNewOrUpdatedCode(QRCode code)
    {
        Guid id = code.Id;
        string data = code.Data; // The actual text/data in the QR

        if (isCalibrationPhase)
        {
            // In calibration phase, if we haven't seen this code ID yet, remember it
            if (!rememberedCodes.Contains(id))
            {
                rememberedCodes.Add(id);
                Debug.Log($"[MultiPhaseQRDemo] Calibration: New QR code scanned & saved! Data = '{data}', ID = {id}");
            }
            // We don't reposition anything yet – user is still scanning codes.
        }
        else
        {
            // In operation phase: if this code was recognized/calibrated before, let's reposition environmentRoot.
            if (codeOffsets.ContainsKey(id))
            {
                // Re-align environmentRoot so this code's position matches the offset we saved.
                AlignEnvironmentRootToCode(codeOffsets[id], code);
            }
            else
            {
                Debug.Log($"[MultiPhaseQRDemo] Operation: Detected code {id}, but it was never calibrated.");
            }
        }
    }

    /// <summary>
    /// Align environmentRoot based on the offset we recorded in calibration phase.
    /// We transform the offset from code-local space to world space.
    /// </summary>
    private void AlignEnvironmentRootToCode(QROffsetData offsetData, QRCode code)
    {
#if WINDOWS_UWP
        SpatialCoordinateSystem codeCoord =
            SpatialGraphInteropPreview.CreateCoordinateSystemForNode(code.SpatialGraphNodeId);

        if (codeCoord == null)
            return;

        var rootCoord = WindowsMixedRealityUtilities.SpatialCoordinateSystem;
        var relativePose = codeCoord.TryGetTransformTo(rootCoord);
        if (relativePose.HasValue)
        {
            var mat = WindowsMixedRealityUtilities.SystemNumericsMatrixToUnityMatrix(relativePose.Value);
            Vector3 codeWorldPos = mat.GetColumn(3);
            Quaternion codeWorldRot = Quaternion.LookRotation(
                mat.GetColumn(2),
                mat.GetColumn(1)
            );

            // codeWorldPos + codeWorldRot => This is where the code is in Unity world space
            // offsetData.positionOffset/rotationOffset => The environmentRoot's local offset from the code (from calibration)

            Vector3 newRootPos = codeWorldPos + codeWorldRot * offsetData.positionOffset;
            Quaternion newRootRot = codeWorldRot * offsetData.rotationOffset;

            environmentRoot.SetPositionAndRotation(newRootPos, newRootRot);

            Debug.Log($"[MultiPhaseQRDemo] Operation: Re-aligned root using code {offsetData.codeId}.");
        }
#endif
    }

    // -------------------- Public Functions --------------------
    /// <summary>
    /// Called by a UI button when the user finishes scanning all codes
    /// and wants to "save positions" & transition to operation phase.
    /// </summary>
    public void SavePositionsAndEnterOperation()
    {
        if (!isCalibrationPhase)
        {
            Debug.LogWarning("[MultiPhaseQRDemo] Already in operation phase.");
            return;
        }

        // For each code we recognized, record the offset from that code to the environmentRoot.
        // We'll do a simplified approach: we assume the code is currently findable in the same place
        // as when we scanned it. That means we can do 1 code -> environmentRoot offset.
        foreach (var id in rememberedCodes)
        {
            // We create an offset record for each code
            QROffsetData offsetData = new QROffsetData
            {
                codeId = id,
                positionOffset = Vector3.zero,
                rotationOffset = Quaternion.identity
            };

            // (Optional) If you want to get the code's transform from a "Tracker" object, you could do so.
            // For simplicity, let's just store 0 offset now and rely on the code's transform at runtime.
            // But typically, you'd do something like:
            //
            // 1) Find the code's Unity transform (e.g. a "QRCodeTracker" object).
            // 2) offsetData.positionOffset = codeTracker.transform.InverseTransformPoint(environmentRoot.position);
            // 3) offsetData.rotationOffset = Quaternion.Inverse(codeTracker.transform.rotation) * environmentRoot.rotation;
            //
            // That ensures we know exactly how environmentRoot was placed relative to the code.

            codeOffsets[id] = offsetData;
            Debug.Log($"[MultiPhaseQRDemo] Saving offset for code {id}.");
        }

        // Now switch to operation phase
        isCalibrationPhase = false;
        Debug.Log("[MultiPhaseQRDemo] Transition to operation phase complete!");
    }
}

