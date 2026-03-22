// TrackingSpaceTest.cs
// Test script to verify the relationship between tracking space and world space.
// Logs head node position (tracking space) vs CenterEyeAnchor position (world space) to CSV.
//
// Usage:
// 1. Attach this script to an empty GameObject in your scene
// 2. CenterEyeAnchor will be auto-populated from ResXRPlayer
// 3. (Optional) Set a custom save path
// 4. Play the scene and move around
// 5. Stop the scene to save the CSV file
//
// The CSV will show:
// - HeadNode position (tracking space from OVRPlugin)
// - CenterEye position (world space from Unity Transform)
// - Difference between them (world - tracking)
// - Recenter detection (shouldRecenter and recenterEvent columns)
// - Tracking origin from OVRPlugin (to verify offset source)

using System;
using System.IO;
using System.Text;
using UnityEngine;
using static OVRPlugin;

public class TrackingSpaceTest : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto populated if you have a ResXRPlayer")]
    public Transform centerEyeAnchor;
    
    [Header("Recording Settings")]
    [Tooltip("Toggle recording on/off")]
    public bool isRecording = true;
    
    [Header("Verification")]
    [Tooltip("Enable periodic logging to compare converter output vs Transform")]
    public bool enableVerificationLogging = true;
    [Tooltip("Log interval in seconds")]
    public float verificationLogInterval = 2.0f;
    
    [Header("Output")]
    [Tooltip("CSV save location. Leave empty to use Application.persistentDataPath")]
    public string savePath = "";
    
    private StringBuilder csvData;
    private string fullFilePath;
    private int previousShouldRecenter = 0;
    private bool hasStarted = false;
    private float lastVerificationLog = 0f;

    private void Start()
    {
        ResXRPlayer player = ResXRPlayer.Instance;
        if (player != null)
        {
            centerEyeAnchor = player.PlayerHead;
        }
        else
        {
            Debug.LogError("[TrackingSpaceTest] ResXRPlayer is null.");
            enabled = false;
            return;
        }
        if (centerEyeAnchor == null)
        {
            Debug.LogError("[TrackingSpaceTest] CenterEyeAnchor is null.");
            enabled = false;
            return;
        }

        // Initialize CSV data
        csvData = new StringBuilder();
        
        // Generate filename with timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"TrackingSpaceTest_{timestamp}.csv";
        
        // Determine save path
        string directory = string.IsNullOrWhiteSpace(savePath) 
            ? Application.persistentDataPath 
            : savePath;
        
        fullFilePath = Path.Combine(directory, filename);
        
        // Ensure directory exists
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TrackingSpaceTest] Failed to create directory: {e.Message}");
            enabled = false;
            return;
        }
        
        // Write CSV header
        csvData.AppendLine("timeSinceStartup,HeadNode_x,HeadNode_y,HeadNode_z,CenterEye_x,CenterEye_y,CenterEye_z,Diff_x,Diff_y,Diff_z,Diff_Magnitude,shouldRecenter,recenterEvent,TrackingOrigin_x,TrackingOrigin_y,TrackingOrigin_z,TrackingOriginType,TrackingSpace_x,TrackingSpace_y,TrackingSpace_z,CameraRig_x,CameraRig_y,CameraRig_z");
        
        // Prime recenter state
        if (TryGetShouldRecenter(out int initialRecenter))
        {
            previousShouldRecenter = initialRecenter;
        }
        
        hasStarted = true;
        Debug.Log($"[TrackingSpaceTest] Recording started. File will be saved to: {fullFilePath}");
        Debug.Log($"[TrackingSpaceTest] Recording will continue until scene stops.");
    }

    private void Update()
    {
        if (!hasStarted || !isRecording)
            return;
        
        // Get timestamp
        float time = Time.realtimeSinceStartup;
        
        // Get head node pose (tracking space from OVRPlugin - raw, right-handed)
        Posef headPose = GetNodePose(Node.Head, Step.Render);
        Vector3 headNode = new Vector3(headPose.Position.x, headPose.Position.y, headPose.Position.z);
        
        // Get CenterEyeAnchor world position (Unity Transform - left-handed world space)
        Vector3 centerEye = centerEyeAnchor.position;  // .position = WORLD position
        
        // Get parent transforms world positions
        Transform trackingSpace = centerEyeAnchor.parent;
        Vector3 trackingSpacePos = trackingSpace != null ? trackingSpace.position : Vector3.zero;
        
        Transform cameraRig = trackingSpace != null ? trackingSpace.parent : null;
        Vector3 cameraRigPos = cameraRig != null ? cameraRig.position : Vector3.zero;
        
        // Calculate difference (world - tracking)
        Vector3 diff = centerEye - headNode;
        float diffMagnitude = diff.magnitude;
        
        // Get recenter state
        int currentShouldRecenter = 0;
        int recenterEvent = 0;
        
        if (TryGetShouldRecenter(out int recenterValue))
        {
            currentShouldRecenter = recenterValue;
            
            // Detect rising edge (0 -> 1 transition)
            if (previousShouldRecenter == 0 && currentShouldRecenter == 1)
            {
                recenterEvent = 1;
                Debug.Log($"[TrackingSpaceTest] Recenter event detected at time {time:F3}s");
            }
            
            previousShouldRecenter = currentShouldRecenter;
        }
        
        // Get tracking origin directly from OVRPlugin (without Unity transforms)
        Posef trackingOrigin = GetTrackingCalibratedOrigin();
        Vector3 trackingOriginPos = new Vector3(trackingOrigin.Position.x, trackingOrigin.Position.y, trackingOrigin.Position.z);
        string trackingOriginType = GetTrackingOriginType().ToString();
        
        // Append data row
        csvData.AppendLine($"{time:F6},{headNode.x:F6},{headNode.y:F6},{headNode.z:F6}," +
                          $"{centerEye.x:F6},{centerEye.y:F6},{centerEye.z:F6}," +
                          $"{diff.x:F6},{diff.y:F6},{diff.z:F6},{diffMagnitude:F6}," +
                          $"{currentShouldRecenter},{recenterEvent}," +
                          $"{trackingOriginPos.x:F6},{trackingOriginPos.y:F6},{trackingOriginPos.z:F6},{trackingOriginType}," +
                          $"{trackingSpacePos.x:F6},{trackingSpacePos.y:F6},{trackingSpacePos.z:F6}," +
                          $"{cameraRigPos.x:F6},{cameraRigPos.y:F6},{cameraRigPos.z:F6}");
        
        // Periodic verification: Compare TrackingSpaceConverter output vs Unity Transform
        if (enableVerificationLogging && time - lastVerificationLog >= verificationLogInterval)
        {
            lastVerificationLog = time;
            
            // Method 1: TrackingSpaceConverter (new world space conversion)
            Vector3 converterOutput = ResXRData.TrackingSpaceConverter.ToWorldSpacePosition(headPose);
            
            // Method 2: Unity Transform (existing world space)
            Vector3 transformOutput = centerEye;
            
            // Calculate difference
            Vector3 verifyDiff = converterOutput - transformOutput;
            float verifyDiffMagnitude = verifyDiff.magnitude;
            
            // Log results
            Debug.Log($"[TrackingSpaceTest] Verification at t={time:F3}s:\n" +
                     $"  Converter:  {converterOutput}\n" +
                     $"  Transform:  {transformOutput}\n" +
                     $"  Difference: {verifyDiff} (magnitude: {verifyDiffMagnitude:F6}m)\n" +
                     $"  Expected: < 0.001m");
            
            if (verifyDiffMagnitude > 0.001f)
            {
                Debug.LogWarning($"[TrackingSpaceTest] Verification failed! Difference exceeds threshold.");
            }
        }
    }

    private void OnDestroy()
    {
        if (hasStarted)
        {
            SaveCSV();
        }
    }

    private void SaveCSV()
    {
        if (csvData == null || csvData.Length == 0)
        {
            Debug.LogWarning("[TrackingSpaceTest] No data to save.");
            return;
        }
        
        try
        {
            File.WriteAllText(fullFilePath, csvData.ToString());
            Debug.Log($"[TrackingSpaceTest] CSV saved successfully to: {fullFilePath}");
            Debug.Log($"[TrackingSpaceTest] Total rows: {csvData.ToString().Split('\n').Length - 2}"); // -2 for header and final newline
        }
        catch (Exception e)
        {
            Debug.LogError($"[TrackingSpaceTest] Failed to save CSV: {e.Message}");
        }
    }

    private bool TryGetShouldRecenter(out int value)
    {
        try
        {
            value = shouldRecenter ? 1 : 0;
            return true;
        }
        catch (Exception)
        {
            value = 0;
            return false;
        }
    }
}
