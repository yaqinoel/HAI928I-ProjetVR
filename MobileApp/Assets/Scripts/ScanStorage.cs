using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// class coresspondent to one scan record
public sealed class ScanSummary
{
    public string Id { get; }   // scan id
    public DateTime DateUtc { get; }    // scan time
    public int PhotoCount { get; }  // photo numbers
    public string CoverPath { get; } 

    public ScanSummary(string id, DateTime dateUtc, int photoCount, string coverPath)
    {
        Id = id;
        DateUtc = dateUtc;
        PhotoCount = photoCount;
        CoverPath = coverPath;
    }
}

// class to get all scan records
public sealed class ScanStorage
{
    private readonly string scansDirectory; // directory for all scans

    public ScanStorage(string persistentDataPath)
    {
        scansDirectory = Path.Combine(persistentDataPath, "Scans"); // app file root + scans
    }

    // get all scans as a list
    public IReadOnlyList<ScanSummary> GetScans()
    {
        if (!Directory.Exists(scansDirectory))
        {
            return Array.Empty<ScanSummary>();
        }

        var scans = new List<ScanSummary>();

        foreach (string directory in Directory.EnumerateDirectories(scansDirectory))
        {
            string scanId = Path.GetFileName(directory);
            // skip invalid scanid
            if (!Guid.TryParseExact(scanId, "N", out _))
            {
                continue;
            }

            var photos = GetPhotos(scanId);
            if (photos.Count == 0)
            {
                continue;
            }

            string coverPath = photos[0];   // set first photo as cover

            // add a scan record into list
            scans.Add(new ScanSummary(
                scanId,
                File.GetLastWriteTimeUtc(coverPath),
                photos.Count,
                coverPath));
        }

        // sort scan record by time (new -> old)
        return scans.OrderByDescending(scan => scan.DateUtc).ToArray();
    }

    // get a list of photo contained in this scan record
    public IReadOnlyList<string> GetPhotos(string scanId)
    {
        string imagesDirectory = Path.Combine(GetScanDirectory(scanId), "images");
        if (!Directory.Exists(imagesDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(imagesDirectory)
            .Where(path => string.Equals(
                Path.GetExtension(path), ".jpg", StringComparison.OrdinalIgnoreCase))
            .OrderBy(GetPhotoNumber)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    // delete a scan record by scanid
    public void DeleteScan(string scanId)
    {
        string directory = GetScanDirectory(scanId);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    // read json file for this scan record
    public string ReadUploadReceipt(string scanId)
    {
        string path = Path.Combine(GetScanDirectory(scanId), "scan.json");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    // save a load receipt from server for this scan record
    public void SaveUploadReceipt(string scanId, string json)
    {
        string path = Path.Combine(GetScanDirectory(scanId), "scan.json");
        string temporaryPath = path + ".tmp";
        // write in a tmp file, after move it
        try
        {
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        // clean tmp file
        finally
        {
            File.Delete(temporaryPath);
        }
    }


    // get scan record path by scanid
    private string GetScanDirectory(string scanId)
    {
        if (!Guid.TryParseExact(scanId, "N", out _))
        {
            throw new ArgumentException("Invalid scan ID.", nameof(scanId));
        }

        return Path.Combine(scansDirectory, scanId);
    }

    // get photo number in this directory
    private static int GetPhotoNumber(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        return int.TryParse(fileName, out int number) ? number : int.MaxValue;
    }
}
