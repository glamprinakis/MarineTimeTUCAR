using UnityEngine;
using Microsoft.MixedReality.QR;
using System;

#if WINDOWS_UWP
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;
using Microsoft.MixedReality.Toolkit.Utilities; // MRTK namespace for WindowsMixedRealityUtilities
#endif

public class QRCodeTracker : MonoBehaviour
{
    private QRCode qrCode;
    public Guid CodeId => qrCode == null ? Guid.Empty : qrCode.Id;

    private bool isDetected = false;
    public bool IsDetected => isDetected;

    // Reference to the calibration manager to notify when a QR code is recognized.
    private CalibrationAndOperationManager calibrationOpManager;

    private void Awake()
    {
        // Find the calibration manager in the scene
        calibrationOpManager = FindObjectOfType<CalibrationAndOperationManager>();
    }

    public void Initialize(QRCode code)
    {
        qrCode = code;
        isDetected = true;
        UpdatePoseFromCode();

        // Notify the calibration/operation manager that the code has been recognized.
        calibrationOpManager?.OnCodeRecognized(this);
    }

    private void Update()
    {
        if (isDetected && qrCode != null)
        {
            UpdatePoseFromCode();
        }
    }

    /// <summary>
    /// Gets the real-world (Unity) position/rotation from the QR code's SpatialGraphNodeId
    /// and applies it to this GameObject.
    /// </summary>
    private void UpdatePoseFromCode()
    {
        #if WINDOWS_UWP
        Debug.Log("Attempting to update pose for code " + qrCode?.Id);

        // Create a SpatialCoordinateSystem for the QR code node.
        SpatialCoordinateSystem codeCoord =
            SpatialGraphInteropPreview.CreateCoordinateSystemForNode(qrCode.SpatialGraphNodeId);

        if (codeCoord == null)
        {
            Debug.LogWarning($"[{name}] codeCoord is null for code {qrCode?.Id}.");
            return;
        }

        // Get the root coordinate system from MRTK utilities.
        var rootCoord = WindowsMixedRealityUtilities.SpatialCoordinateSystem;
        var relativePose = codeCoord.TryGetTransformTo(rootCoord);
        if (!relativePose.HasValue)
        {
            Debug.LogWarning($"[{name}] relativePose is null; cannot locate code {qrCode?.Id} in world.");
            return;
        }

        Debug.Log($"[{name}] relativePose is valid. Updating transform...");

        // Convert the system numerics matrix to Unity matrix.
        var mat = WindowsMixedRealityUtilities.SystemNumericsMatrixToUnityMatrix(relativePose.Value);
        Vector3 position = mat.GetColumn(3);
        Vector3 forward = mat.GetColumn(2);  // Z-axis
        Vector3 up = mat.GetColumn(1);       // Y-axis

        // Ensure the forward vector is not nearly zero.
        if (forward.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"[{name}] Forward vector is nearly zero. Rotation may fail.");
            return;
        }

        // Update the transform with the new position and rotation.
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        transform.SetPositionAndRotation(position, rotation);
        #else
        Debug.Log("UpdatePoseFromCode is only implemented for UWP platforms.");
        #endif
    }
}
