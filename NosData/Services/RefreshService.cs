﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NosData.Services
{
    public class RefreshService
    {
        private static readonly string Container = "meta";
        private static readonly string UpdateSha256FileName = "updateSha256";

        private readonly DataService _dataService;
        private readonly ExecutableVersionService _executableVersionService;
        private readonly IconsService _iconsService;
        private readonly TranslationsService _translationsService;
        private readonly ILogger<RefreshService> _logger;
        private readonly BlobsService _blobsService;
        private readonly NosFileService _nosFileService;
        private readonly MapZonesService _mapsZonesService;

        public RefreshService(ILogger<RefreshService> logger, DataService dataService, ExecutableVersionService executableVersionService,
            IconsService iconsService, TranslationsService translationsService, BlobsService blobsService,
            NosFileService nosFileService, MapZonesService mapZonesService)
        {
            _logger = logger;
            _blobsService = blobsService;
            _dataService = dataService;
            _executableVersionService = executableVersionService;
            _iconsService = iconsService;
            _translationsService = translationsService;
            _nosFileService = nosFileService;
            _mapsZonesService = mapZonesService;
        }

        public async Task<bool> RefreshAll(bool force = false)
        {

            var latestHash = _nosFileService.GetUpdateHash();

            if (!force)
            {
                var blobStream = await _blobsService.GetBlob(Container, UpdateSha256FileName);

                if (blobStream != null)
                {
                    await using var currentHashStream = new MemoryStream();
                    await blobStream.CopyToAsync(currentHashStream);
                    var currentHash = Encoding.UTF8.GetString(currentHashStream.ToArray());
                    if (currentHash == latestHash)
                    {
                        _logger.LogInformation("Refresh is not needed - currentHash == latestHash.");
                        return false;
                    }
                }
            }

            await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(latestHash));
            await _blobsService.UploadBlob(Container, UpdateSha256FileName, ms);

            var startTime = DateTime.Now;
            _logger.LogInformation($"Full refresh started at {startTime}.");;

            await _executableVersionService.RefreshExecutableVersion();
            await _translationsService.RefreshTranslations();
            await _iconsService.RefreshIcons();
            await _mapsZonesService.RefreshZones();
            await _dataService.RefreshData();

            _logger.LogInformation($"Full refresh done in {(DateTime.Now - startTime).TotalSeconds} seconds!");
            return true;
        }
    }
}
