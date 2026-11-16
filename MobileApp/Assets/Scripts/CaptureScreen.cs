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
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private TMP_Text trackingText;
    [SerializeField] private TMP_Text photoCountText;
    [SerializeField] private TMP_Text scanButtonText;

    private string scanFolder;
    private int photoCount;
    private float nextPhotoTime;

    // load AR Session
    private void OnEnable()
    {
        ARSession.stateChanged += OnTrackingChanged;
        ShowTrackingState(ARSession.state);
    }

    private void OnDisable()
    {
        ARSession.stateChanged -= OnTrackingChanged;
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
        if (scanFolder != null)
        {
            Debug.Log($"Scan terminé. Photos enregistrées dans {scanFolder}");
            scanFolder = null;
            scanButtonText.text = "Démarrer le scan";
            return;
        }

        var folder = Path.Combine(
            Application.persistentDataPath, "Scans", Guid.NewGuid().ToString("N"), "images");
        Directory.CreateDirectory(folder);

        scanFolder = folder;
        photoCount = 0;
        photoCountText.text = "Photos 0";
        scanButtonText.text = "Terminer le scan";
        nextPhotoTime = Time.unscaledTime;
    }

    public void GoHome()
    {
        SceneManager.LoadScene("Home");
    }

    private void Update()
    {
        if (scanFolder == null
            || ARSession.state != ARSessionState.SessionTracking
            || Time.unscaledTime < nextPhotoTime)
        {
            return;
        }

        nextPhotoTime = Time.unscaledTime + 1f;

        if (!cameraManager.TryAcquireLatestCpuImage(out var image))
        {
            return;
        }

        using (image)
        {
            var conversion = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32);
            using var pixels = new NativeArray<byte>(
                image.GetConvertedDataSize(conversion), Allocator.Temp);
            image.Convert(conversion, pixels);

            using var jpeg = ImageConversion.EncodeNativeArrayToJPG(
                pixels, GraphicsFormat.R8G8B8A8_UNorm,
                (uint)image.width, (uint)image.height, quality: 90);

            var file = Path.Combine(scanFolder, $"{photoCount + 1:D4}.jpg");
            File.WriteAllBytes(file, jpeg.ToArray());
        }

        photoCount++;
        photoCountText.text = $"Photos {photoCount}";
    }
}