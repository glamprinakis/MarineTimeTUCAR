using System;
using UnityEngine;
using Microsoft.MixedReality.QR;

#if WINDOWS_UWP
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;
using Microsoft.MixedReality.Toolkit.Utilities;
#endif

public class QRCodeTracker : MonoBehaviour
{
    private QRCode qrCode;
    public string CodeData => qrCode != null ? qrCode.Data : "";
    private bool isInitialized = false;

    private void Start()
    {
        Debug.LogError("QRCodeTracker: Start() called. Awaiting Initialize().");
    }

    public void Initialize(QRCode code)
    {
        qrCode = code;
        isInitialized = true;
        Debug.LogError($"QRCodeTracker: Initialized with code.Data='{CodeData}', ID={code.Id}");
        UpdateTransformFromQRCode();
    }

    private void Update()
    {
        if (isInitialized && qrCode != null)
        {
            UpdateTransformFromQRCode();
        }
    }

#if WINDOWS_UWP
    private SpatialCoordinateSystem rootCoordinateSystem = null;
#endif

    private void UpdateTransformFromQRCode()
    {
#if WINDOWS_UWP
        if (rootCoordinateSystem == null)
        {
            rootCoordinateSystem = WindowsMixedRealityUtilities.SpatialCoordinateSystem;
            if (rootCoordinateSystem == null)
            {
                Debug.LogError("QRCodeTracker: rootCoordinateSystem is NULL!");
                return;
            }
        }
        SpatialCoordinateSystem codeCoordSystem = SpatialGraphInteropPreview.CreateCoordinateSystemForNode(qrCode.SpatialGraphNodeId);
        if (codeCoordSystem == null)
        {
            Debug.LogError($"QRCodeTracker: CreateCoordinateSystemForNode returned null for code '{CodeData}'");
            return;
        }
        var relativePose = codeCoordSystem.TryGetTransformTo(rootCoordinateSystem);
        if (!relativePose.HasValue)
        {
            Debug.LogError($"QRCodeTracker: TryGetTransformTo failed for code '{CodeData}'");
            return;
        }
        Matrix4x4 unityMat = WindowsMixedRealityUtilities.SystemNumericsMatrixToUnityMatrix(relativePose.Value);
        Vector3 newPosition = unityMat.GetColumn(3);
        Vector3 forward = unityMat.GetColumn(2);
        Vector3 upwards = unityMat.GetColumn(1);
        Quaternion newRotation = Quaternion.identity;
        if (forward.sqrMagnitude > 1e-8f && upwards.sqrMagnitude > 1e-8f)
            newRotation = Quaternion.LookRotation(forward, upwards);
        else
            Debug.LogError($"QRCodeTracker: forward/up near zero for code '{CodeData}', using identity rotation.");
        transform.SetPositionAndRotation(newPosition, newRotation);
        Debug.LogError($"QRCodeTracker: Updated transform for code '{CodeData}' => pos = {newPosition}, rot = {newRotation}");
#else
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
#endif
    }
}
