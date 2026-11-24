using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ScanHistory : MonoBehaviour
{
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform content;
    [SerializeField] private ScanHistoryCard cardPrefab;
    [SerializeField] private TMP_Text emptyMessage;
    [SerializeField] private GameObject deleteDialog;
    [SerializeField] private TMP_Text deleteMessage;
    [SerializeField] private Button confirmDelete;
    [SerializeField] private Button cancelDelete;

    [Tooltip("The HTTPS upload URL. POST an application/zip body with X-Scan-Id and return JSON with a jobId.")]
    [SerializeField] private string uploadEndpoint; // server URL

    private ScanStorage storage;
    private Coroutine coverLoading;
    private ScanSummary pendingDelete;  // the record that we want to delete
    private ScanHistoryCard pendingDeleteCard;  // the card we want to delete
    private string activeUpload;

    // bind all scan records data
    private void Awake()
    {
        storage = new ScanStorage(Application.persistentDataPath);
        confirmDelete.onClick.AddListener(DeleteSelected);
        cancelDelete.onClick.AddListener(CloseDeleteDialog);
        CloseDeleteDialog();
    }

    // calcute safe area
    private void Start()
    {
        Rect bounds = Screen.safeArea;
        if (Screen.width > 0 && Screen.height > 0)
        {
            var size = new Vector2(Screen.width, Screen.height);
            safeArea.anchorMin = bounds.min / size;
            safeArea.anchorMax = bounds.max / size;
        }

        Refresh();
    }

    public void GoHome()
    {
        SceneManager.LoadScene("Home");
    }

    // refresh all scan history card by scan storage
    private void Refresh()
    {
        emptyMessage.text = "No scans yet";
        try
        {
            var scans = storage.GetScans();
            // set empty message when no scans
            emptyMessage.gameObject.SetActive(scans.Count == 0);

            var cards = new ScanHistoryCard[scans.Count];
            for (int index = 0; index < scans.Count; index++)
            {
                ScanSummary scan = scans[index];

                // initialize a scan history card objet in scene
                var card = Instantiate(cardPrefab, content);
                string title = ScanTitle(scan); // set titile
                bool submitted = HasReceipt(scan.Id);   // get submitted state

                card.Bind(scan, title, () => Upload(scan, card), () => RequestDelete(scan, card));
                card.SetStatus(submitted ? "Submitted" : string.Empty);
                card.SetActions(!submitted, true);
                cards[index] = card;    // add this card to cards list
            }

            // use coroutine to load covers for all card
            if (cards.Length > 0)
            {
                coverLoading = StartCoroutine(LoadCovers());
            }

            IEnumerator LoadCovers()
            {
                for (int index = 0; index < cards.Length; index++)
                {
                    yield return null;
                    if (cards[index] != null && cards[index].gameObject.activeSelf)
                    {
                        cards[index].LoadCover(scans[index].CoverPath);
                    }
                }

                coverLoading = null;
            }
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            emptyMessage.text = "Could not read local scans.";
            emptyMessage.gameObject.SetActive(true);
            Debug.LogWarning(error.Message, this);
        }
    }

    // to see if is a scan record is upload to server
    // return false if json is null, or dosen't have jobid
    // return true if json is not null and has jobid
    private bool HasReceipt(string scanId)
    {
        try
        {
            string json = storage.ReadUploadReceipt(scanId);

            return !string.IsNullOrEmpty(json)
                && !string.IsNullOrWhiteSpace(JsonUtility.FromJson<ScanUploadReceipt>(json)?.jobId);
        }
        catch (Exception error) when (
            error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
        {
            Debug.LogWarning($"Could not read the scan's upload receipt. {error.Message}", this);
            return false;
        }
    }

    // set the scan record and card to pending objet
    // only click confirm button, we delete it
    // if click cancal, we don't clean it
    private void RequestDelete(ScanSummary scan, ScanHistoryCard card)
    {
        // can not delete a scan record when uplaoding it
        if (activeUpload == scan.Id)
        {
            return;
        }

        pendingDelete = scan;
        pendingDeleteCard = card;
        deleteMessage.text = $"Delete the scan from {ScanTitle(scan)} and its {scan.PhotoCount} local photos?";
        deleteDialog.SetActive(true);
    }

    // cancel delete, clean pending data
    private void CloseDeleteDialog()
    {
        pendingDelete = null;
        pendingDeleteCard = null;
        deleteDialog.SetActive(false);
    }

    // confirm to delete
    // delete the pending objet
    private void DeleteSelected()
    {
        ScanSummary scan = pendingDelete;
        ScanHistoryCard card = pendingDeleteCard;
        CloseDeleteDialog();
        if (scan == null || activeUpload == scan.Id)
        {
            return;
        }

        try
        {
            storage.DeleteScan(scan.Id);
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
            emptyMessage.gameObject.SetActive(content.GetComponentsInChildren<ScanHistoryCard>().Length == 0);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            card.SetStatus("Could not delete the scan.");
            Debug.LogWarning(error.Message, this);
        }
    }

    private async void Upload(ScanSummary scan, ScanHistoryCard card)
    {
        // can not upload two record at the same time
        if (activeUpload != null)
        {
            card.SetStatus("Another scan is uploading.");
            return;
        }

        if (!Uri.TryCreate(uploadEndpoint?.Trim(), UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
        {
            card.SetStatus("Upload server is not configured.");
            return;
        }

        var cancellationToken = destroyCancellationToken;
        activeUpload = scan.Id;
        card.SetActions(false, false);
        bool submitted = false;
        try
        {
            string receipt = await ScanUpload.SendAsync(
                endpoint, scan.Id, storage.GetPhotos(scan.Id), card.SetStatus, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            submitted = true;
            storage.SaveUploadReceipt(scan.Id, receipt);
            card.SetStatus("Submitted");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception error) when (
            error is IOException || error is UnauthorizedAccessException
                || error is InvalidOperationException || error is ArgumentException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                card.SetStatus(submitted ? "Submitted. Could not save the receipt." : "Upload failed. Try again.");
                Debug.LogWarning(error.Message, this);
            }
        }
        finally
        {
            activeUpload = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                card.SetActions(!submitted, true);
            }
        }
    }

    // return a scan records name, by default is data
    private static string ScanTitle(ScanSummary scan)
    {
        return scan.DateUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }

    private void OnDestroy()
    {
        if (coverLoading != null)
        {
            StopCoroutine(coverLoading);
        }
    }
}
