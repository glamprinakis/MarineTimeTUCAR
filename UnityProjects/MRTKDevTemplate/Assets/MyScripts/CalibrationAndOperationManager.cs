using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class CodeOffsetData
{
    public Guid codeId;
    public Vector3 positionOffset; 
    public Quaternion rotationOffset;
    public bool isCalibrated;
}
/// <summary>
/// Manages a two-phase process: 
/// 1) Calibration (scan & remember codes, record offsets).
/// 2) Operation (reposition environmentRoot whenever it sees a known code).
/// </summary>
public class CalibrationAndOperationManager : MonoBehaviour
{
    // The parent of all virtual objects. Moving this repositions everything.
    [Header("References")]
    public Transform environmentRoot;

    // Whether we're in calibration phase or operation phase
    private bool isCalibrationPhase = true;

    // Dictionary from codeId -> offset data
    private Dictionary<Guid, CodeOffsetData> codeOffsets 
        = new Dictionary<Guid, CodeOffsetData>();

    private void Start()
    {
        Debug.Log("[CalibrationAndOperationManager] In calibration phase. Scan your codes!");
    }

    /// <summary>
    /// Called by each QRCodeTracker when it sees/updates a code.
    /// If we're in calibration, we 'remember' the code. 
    /// If we're in operation, we try to reposition environmentRoot if offset is known.
    /// </summary>
    public void OnCodeRecognized(QRCodeTracker tracker)
    {
        var codeId = tracker.CodeId;
        if (codeId == Guid.Empty) return;  // Safety check

        if (isCalibrationPhase)
        {
            // If we haven't seen this code yet, record that we've "saved" it
            if (!codeOffsets.ContainsKey(codeId))
            {
                codeOffsets[codeId] = new CodeOffsetData()
                {
                    codeId = codeId,
                    isCalibrated = false  // We'll set offsets once the user says so
                };

                Debug.Log($"[CalibrationAndOperationManager] Calibration: Code {codeId} scanned & saved!");
            }
        }
        else
        {
            // Operation phase: if this code was calibrated, reposition environmentRoot
            if (codeOffsets.TryGetValue(codeId, out CodeOffsetData offsetData)
                && offsetData.isCalibrated)
            {
                // The tracker's own transform is the code's current position in Unity space
                AlignEnvironmentRoot(tracker.transform, offsetData);
            }
            else
            {
                Debug.Log($"[CalibrationAndOperationManager] Operation: Code {codeId} not calibrated or unknown.");
            }
        }
    }

    /// <summary>
    /// Called by a UI button once the user is done scanning codes
    /// and wants to record environmentRoot offsets & switch to operation.
    /// </summary>
    public void SavePositionsAndEnterOperationMode()
    {
        if (!isCalibrationPhase)
        {
            Debug.LogWarning("[CalibrationAndOperationManager] Already in operation mode.");
            return;
        }

        // For each code we've seen, calculate the offset from the code's transform to the environmentRoot
        foreach (var kvp in codeOffsets)
        {
            Guid codeId = kvp.Key;
            CodeOffsetData offsetData = kvp.Value;
            if (offsetData.isCalibrated)
            {
                // Already calibrated, skip
                continue;
            }

            // We need the code's *current* transform in the scene 
            // (i.e., the QRCodeTracker that matches this codeId).
            // Let's find it by searching the tracking manager or by a simple FindObjects approach
            // For brevity, let's do a naive approach:
            QRCodeTracker tracker = FindCodeTrackerById(codeId);
            if (tracker != null)
            {
                // code's transform
                var codeTf = tracker.transform;
                // root's transform
                var rootTf = environmentRoot;

                // offset = codeTf.InverseTransformPoint(rootTf.position)
                offsetData.positionOffset = codeTf.InverseTransformPoint(rootTf.position);
                offsetData.rotationOffset = 
                    Quaternion.Inverse(codeTf.rotation) * rootTf.rotation;
                offsetData.isCalibrated = true;

                Debug.Log($"[CalibrationAndOperationManager] Recorded offset for code {codeId}.");
            }
            else
            {
                Debug.LogWarning($"[CalibrationAndOperationManager] Could not find tracker for code {codeId}.");
            }

            codeOffsets[codeId] = offsetData;
        }

        // Switch phases
        isCalibrationPhase = false;
        Debug.Log("[CalibrationAndOperationManager] Entered operation mode. " +
                  "Will now realign environmentRoot when we see any calibrated code.");
    }

    /// <summary>
    /// When we see a known code in operation mode, we reposition environmentRoot
    /// so that code is in the same relative spot as during calibration.
    /// </summary>
    private void AlignEnvironmentRoot(Transform codeTransform, CodeOffsetData offsetData)
    {
        // codeTransform is the real-world transform of the code right now
        // offsetData describes how environmentRoot was offset from the code in calibration

        Vector3 newPos = codeTransform.TransformPoint(offsetData.positionOffset);
        Quaternion newRot = codeTransform.rotation * offsetData.rotationOffset;

        environmentRoot.SetPositionAndRotation(newPos, newRot);

        Debug.Log($"[CalibrationAndOperationManager] Re-aligned environmentRoot using code {offsetData.codeId}.");
    }

    /// <summary>
    /// Finds the QRCodeTracker in the scene that matches a given codeId (naive approach).
    /// </summary>
    private QRCodeTracker FindCodeTrackerById(Guid codeId)
    {
        // If you have a QRTrackingManager with a dictionary, you'd query that.
        // For demonstration, let's just do a simple iteration:
        QRCodeTracker[] allTrackers = FindObjectsOfType<QRCodeTracker>();
        foreach (var t in allTrackers)
        {
            if (t.CodeId == codeId) return t;
        }
        return null;
    }
}
