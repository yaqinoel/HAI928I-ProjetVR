using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScanHistoryCard : MonoBehaviour
{
    [SerializeField] private RawImage cover;
    [SerializeField] private AspectRatioFitter coverAspect;
    [SerializeField] private TMP_Text dateLabel;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Button uploadButton;
    [SerializeField] private Button deleteButton;

    private Texture2D thumbnail;

    // bind data and action for this card
    public void Bind(ScanSummary scan, string title, Action upload, Action delete)
    {
        dateLabel.text = title;
        countLabel.text = scan.PhotoCount == 1 ? "1 photo" : $"{scan.PhotoCount} photos";
        uploadButton.onClick.AddListener(() => upload());
        deleteButton.onClick.AddListener(() => delete());
    }

    public void SetStatus(string message)
    {
        if (statusLabel.text != message)
        {
            statusLabel.text = message;
        }
    }

    // set delete and upload button status
    public void SetActions(bool canUpload, bool canDelete)
    {
        uploadButton.interactable = canUpload;
        deleteButton.interactable = canDelete;
    }

    // set a cover image for this card
    public void LoadCover(string path)
    {
        const int thumbnailSize = 320;
        var fullSize = new Texture2D(2, 2, TextureFormat.RGB24, false);
        RenderTexture target = null;
        RenderTexture previous = RenderTexture.active;

        try
        {
            if (!fullSize.LoadImage(File.ReadAllBytes(path), markNonReadable: true))
            {
                return;
            }

            float scale = Mathf.Min(1, (float)thumbnailSize / Mathf.Max(fullSize.width, fullSize.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(fullSize.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(fullSize.height * scale));
            target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(fullSize, target);
            RenderTexture.active = target;

            thumbnail = new Texture2D(width, height, TextureFormat.RGB24, false);
            thumbnail.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            thumbnail.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            coverAspect.aspectRatio = (float)width / height;
            cover.texture = thumbnail;
            cover.enabled = true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Could not load a scan cover. {error.Message}", this);
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null)
            {
                RenderTexture.ReleaseTemporary(target);
            }

            Destroy(fullSize);
        }
    }

    private void OnDestroy()
    {
        if (thumbnail != null)
        {
            Destroy(thumbnail);
        }
    }
}
