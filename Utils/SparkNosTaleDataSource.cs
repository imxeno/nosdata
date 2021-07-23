using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text.Json;
using NosCDN.DTOs;

namespace NosCDN.Utils
{
    public class SparkNosTaleDataSource
    {
        public static readonly string IndexUrl =
            "https://spark.gameforge.com/api/v1/patching/download/latest/nostale/default?locale=en&architecture=x64&branchToken";

        public static readonly string PatchesBaseUrl = "https://patches.gameforge.com";

        private readonly List<SparkNosTaleDataSourceEntry> _entries;
        public ulong TotalSize { get; set; }
        public ulong Build { get; set; }

        private SparkNosTaleDataSource(NosTalePatchDto dto)
        {
            Build = dto.Build;
            TotalSize = dto.TotalSize;
            _entries = dto.Entries.Select((e) => new SparkNosTaleDataSourceEntry(this, e)).ToList();
        }

        public byte[] DownloadPatch(string path)
        {
            var sslFailureCallback = new RemoteCertificateValidationCallback(
                (sender, _, _, sslPolicyErrors) =>
                    (sender is HttpWebRequest && ((HttpWebRequest) sender).Host == "patches.gameforge.com" &&
                     sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch));
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += sslFailureCallback;
                var client = new WebClient();
                var data = client.DownloadData(PatchesBaseUrl + path);
                return data;
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback -= sslFailureCallback;
            }
        }

        public Dictionary<string, SparkNosTaleDataSourceEntry> FileEntries()
        {
            return _entries.ToDictionary(e => e.File);
        }

        public Dictionary<string, SparkNosTaleDataSourceEntry> ResourceEntries()
        {
            return _entries.ToDictionary(e => e.Path);
        }

        public Dictionary<string, SparkNosTaleDataSourceEntry> Sha1Entries()
        {
            return _entries.ToDictionary(e => e.Sha1);
        }

        public static SparkNosTaleDataSource Latest()
        {
            var dto = FetchLatestNosTalePatch();
            return new SparkNosTaleDataSource(dto);
        }

        private static NosTalePatchDto FetchLatestNosTalePatch()
        {
            using var webClient = new WebClient();
            var body = webClient.DownloadString(IndexUrl);
            var obj = JsonSerializer.Deserialize<NosTalePatchDto>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return obj;
        }
    }

    public class SparkNosTaleDataSourceEntry
    {
        private readonly SparkNosTaleDataSource _dataSource;
        public string Path { get; }
        public string Sha1 { get; }
        public string File { get; }
        public uint Flags { get; }
        public ulong Size { get; }
        public bool Folder { get; }

        internal SparkNosTaleDataSourceEntry(SparkNosTaleDataSource dataSource, NosTalePatchEntryDto dto)
        {
            _dataSource = dataSource;
            Path = dto.Path;
            Sha1 = dto.Sha1;
            File = dto.File;
            Flags = dto.Flags;
            Size = dto.Size;
            Folder = dto.Folder;
        }

        public byte[] Download()
        {
            return _dataSource.DownloadPatch(Path);
        }
    }
}