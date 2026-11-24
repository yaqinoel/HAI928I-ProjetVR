using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
internal sealed class ScanUploadReceipt
{
    public string jobId;
}

internal static class ScanUpload
{
    public static async Task<string> SendAsync(
        Uri endpoint, string scanId, IReadOnlyList<string> photos,
        Action<string> reportProgress, CancellationToken cancellationToken)
    {
        if (photos.Count == 0)
        {
            throw new InvalidOperationException("This scan has no photos to upload.");
        }

        string archivePath = Path.Combine(
            Application.temporaryCachePath, $"scan-{scanId}-{Guid.NewGuid():N}.zip");
        reportProgress("Preparing photos…");
        try
        {
            await Task.Run(() => WriteArchive(archivePath, photos, cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            using var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST,
                new DownloadHandlerBuffer(), new UploadHandlerFile(archivePath));
            request.SetRequestHeader("Content-Type", "application/zip");
            request.SetRequestHeader("X-Scan-Id", scanId);
            request.SetRequestHeader("Idempotency-Key", scanId);
            request.useHttpContinue = false;

            var operation = request.SendWebRequest();
            try
            {
                while (!operation.isDone)
                {
                    int percent = Mathf.Clamp(Mathf.FloorToInt(request.uploadProgress * 100), 0, 100);
                    reportProgress(percent < 100 ? $"Uploading {percent}%" : "Waiting for server…");
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                request.Abort();
                throw;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"Server upload failed. {request.error}");
            }

            string json = request.downloadHandler.text;
            var receipt = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ScanUploadReceipt>(json);
            if (string.IsNullOrWhiteSpace(receipt?.jobId))
            {
                throw new InvalidDataException("The server response did not include a job ID.");
            }

            return JsonUtility.ToJson(receipt);
        }
        finally
        {
            try
            {
                File.Delete(archivePath);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not remove the temporary scan archive. {error.Message}");
            }
        }
    }

    private static void WriteArchive(
        string archivePath, IReadOnlyList<string> photos, CancellationToken cancellationToken)
    {
        using var file = File.Open(archivePath, FileMode.CreateNew);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        foreach (string photo in photos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = archive.CreateEntry("images/" + Path.GetFileName(photo), System.IO.Compression.CompressionLevel.NoCompression);
            using var destination = entry.Open();
            using var source = File.OpenRead(photo);
            source.CopyTo(destination);
        }
    }
}
