using System;
using System.IO;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CaptureScreen : MonoBehaviour
{
    [SerializeField] private ARCameraManager cameraManager; // XR camera
    [SerializeField] private TMP_Text trackingText; // text showing tracking state
    [SerializeField] private TMP_Text photoCountText;   // text showing photos taken
    [SerializeField] private TMP_Text scanButtonText;   // text on scan button (Démarrer / Terminer)

    [SerializeField, Min(0.05f)]
    private float minCaptureInterval = 0.5f;    // min interval time to take new photo
    [SerializeField, Min(0.01f)]
    private float minCaptureDistance = 0.15f;   // min distance to take new photo
    [SerializeField, Range(1f, 180f)]
    private float minCaptureAngle = 15f;    // min rotation to take new photos

    private Vector3 lastCapturePosition;
    private Quaternion lastCaptureRotation;

    private string scanFolder;  // where we stock all photos
    private int photoCount; // photo numbers
    private float nextPhotoTime; // Earliest unscaled time for another photo.

    // load AR Session
    private void OnEnable()
    {
        ARSession.stateChanged += OnTrackingChanged;    // registe callback for event(AR state changed)
        ShowTrackingState(ARSession.state);
    }

    private void OnDisable()
    {
        ARSession.stateChanged -= OnTrackingChanged;    // unregiste callback
    }

    private void OnTrackingChanged(ARSessionStateChangedEventArgs args)
    {
        ShowTrackingState(args.state);
    }

    // update tracking status text
    private void ShowTrackingState(ARSessionState state)
    {
        trackingText.text = state == ARSessionState.SessionTracking
            ? "Suivi AR actif"
            : "Suivi AR en attente";
    }

    // change scan state
    public void ToggleScan()
    {
        // scan finished
        if (scanFolder != null)
        {
            Debug.Log($"Scan terminé. Photos enregistrées dans {scanFolder}");
            scanFolder = null;
            scanButtonText.text = "Démarrer le scan";
            return;
        }

        // start to scan
        var folder = Path.Combine(
            Application.persistentDataPath, "Scans", Guid.NewGuid().ToString("N"), "images");
        Directory.CreateDirectory(folder);

        scanFolder = folder;
        photoCount = 0;
        photoCountText.text = "Photos 0";
        scanButtonText.text = "Terminer le scan";
        nextPhotoTime = Time.unscaledTime;  // we set nextPhotoTime = now
    }

    public void GoHome()
    {
        SceneManager.LoadScene("Home");
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        // if no scan folder, AR not ready, not arrive to next photo time, return
        if (scanFolder == null
            || ARSession.state != ARSessionState.SessionTracking
            || now < nextPhotoTime)
        {
            return;
        }

        // calculate camera position and rotation offset
        // if not satisfy min distance, rotation, return
        // only the first photo dont need to satisfy this requirement
        cameraManager.transform.GetPositionAndRotation(out var positionNow, out var rotationNow);
        bool movedEnough = Vector3.Distance(lastCapturePosition, positionNow) > minCaptureDistance;
        bool rotatedEnough = Quaternion.Angle(lastCaptureRotation, rotationNow) > minCaptureAngle;
        if (photoCount > 0 && !(movedEnough || rotatedEnough))
        {
            return;
        }

        // try to get camera image, if failed, return
        if (!cameraManager.TryAcquireLatestCpuImage(out var image))
        {
            return;
        }

        // convert image from camera to jpeg and store it in scan folder
        using (image)
        {
            var conversion = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32);
            using var pixels = new NativeArray<byte>(image.GetConvertedDataSize(conversion), Allocator.Temp);
            image.Convert(conversion, pixels);

            using var jpeg = EncodeScanJpeg(pixels, image.width, image.height);

            var file = Path.Combine(scanFolder, $"{photoCount + 1:D4}.jpg");
            File.WriteAllBytes(file, jpeg.ToArray());
        }

        // update camera state, time, infos
        lastCapturePosition = positionNow;
        lastCaptureRotation = rotationNow;
        nextPhotoTime = now + minCaptureInterval;

        photoCount++;
        photoCountText.text = $"Photos {photoCount}";
    }

    // rotate image -90 degree
    private static NativeArray<byte> EncodeScanJpeg(
        NativeArray<byte> pixels, int width, int height)
    {
        const GraphicsFormat format = GraphicsFormat.R8G8B8A8_UNorm;

        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            return ImageConversion.EncodeNativeArrayToJPG(
                pixels, format, (uint)width, (uint)height, quality: 90);
        }

        // Read each group of four RGBA bytes as one pixel.
        var source = pixels.Reinterpret<Color32>(sizeof(byte));

        int outputWidth = height;
        int outputHeight = width;
        var rotated = new NativeArray<Color32>(source.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        try
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceIndex = y * width + x;
                    int destinationX = outputWidth -1 -y;
                    int destinationY = width - 1 - x;
                    int destinationIndex = destinationY * outputWidth + destinationX;

                    rotated[destinationIndex] = source[sourceIndex];
                }
            }

            return ImageConversion.EncodeNativeArrayToJPG(
                rotated, format, (uint)outputWidth, (uint)outputHeight, quality: 90);
        }
        finally
        {
            rotated.Dispose();
        }
    }
}
