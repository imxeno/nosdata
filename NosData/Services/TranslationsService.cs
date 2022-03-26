﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NosData.NosPack;
using NosData.Services;
using SixLabors.ImageSharp;
using System.IO;
using System.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using SixLabors.ImageSharp.PixelFormats;

namespace NosData
{
    public class TranslationsService
    {
        public static readonly List<string> Languages = new()
        {
            "it",
            "fr",
            "uk",
            "es",
            "pl",
            "cz",
            "de",
            "ru",
            "tr"
        };

        private readonly ILogger<TranslationsService> _logger;
        private readonly NosFileService _nosFileService;
        private readonly BlobsService _blobsService;

        public TranslationsService(ILogger<TranslationsService> logger, NosFileService nosFileService, BlobsService blobsService)
        {
            _logger = logger;
            _nosFileService = nosFileService;
            _blobsService = blobsService;
        }

        public async Task<string?> GetTranslations(string language, string type)
        {
            var stream = await _blobsService.GetBlob("lang", $"{language}/json/{type}.json");
            if (stream == null) return null;
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public async Task<Stream?> GetRawTranslations(string language, string type)
        {
            return await _blobsService.GetBlob("lang", $"{language}/raw/{type}.txt");
        }

        [FunctionName("RefreshTranslations")]
        public async Task RefreshTranslations([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
        {
            var startTime = DateTime.Now;
            log.LogInformation($"Translations refresh started at {startTime}");
            foreach(var language in Languages)
            {
                var langContainer = _nosFileService.FetchStringContainer($"NSlangData_{language.ToUpper()}.NOS");
                foreach (var entry in langContainer.Entries)
                {
                    var fileName = entry.Key
                        .Replace($"_code_{language}", "")
                        .Replace(".txt", "")
                        .Replace("_", "")
                        .ToLower();
                    {
                        await using var ms = new MemoryStream(entry.Value.Content);
                        await _blobsService.UploadBlob("lang", $"{language}/raw/{fileName}.txt", ms);
                    }
                    {
                        var dict = Encoding.GetEncoding(1252).GetString(entry.Value.Content).Split("\r").ToList()
                            .Select((s) => s.Split('\t'));

                        JsonObject obj = new();
                        foreach (var kv in dict)
                        {
                            if (kv.Length != 2) continue;
                            obj[kv[0]] = kv[1];
                        }

                        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(obj.ToString()));
                        await _blobsService.UploadBlob("lang", $"{language}/json/{fileName}.json", ms);
                    }
                }
            }
            log.LogInformation($"Translations refresh done in {(DateTime.Now - startTime).TotalSeconds} seconds!");
        }
    }
}