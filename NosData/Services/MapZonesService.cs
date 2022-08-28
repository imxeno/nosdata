using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NosData.NosPack;
using NosData.Services;
using SixLabors.ImageSharp;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NosData.DTOs;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NosData
{
    public class MapZonesService
    {
        private readonly ILogger<IconsService> _logger;
        private readonly NosFileService _nosFileService;
        private readonly BlobsService _blobsService;

        public MapZonesService(ILogger<IconsService> logger, NosFileService nosFileService, BlobsService blobsService)
        {
            _logger = logger;
            _nosFileService = nosFileService;
            _blobsService = blobsService;
        }

        public async Task<Stream?> GetMapZone(int id)
        {
            return await _blobsService.GetBlob("map-zones", $"{id}.json");
        }

        public async Task RefreshZones()
        {
            var startTime = DateTime.Now;
            _logger.LogInformation($"Map zones refresh started at {startTime}.");

            var zonesContainer = _nosFileService.FetchDataContainer("NStcData.NOS");
            foreach (var zone in zonesContainer.Entries)
            {
                var dto = ZoneToDto(zone);
                var json = JsonConvert.SerializeObject(dto, Formatting.Indented, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await _blobsService.UploadBlob("map-zones", $"{zone.Id}.json", ms);
            }

            _logger.LogInformation($"Map zones refresh done in {(DateTime.Now - startTime).TotalSeconds} seconds!");
        }

        private static NosTaleMapZoneDto ZoneToDto(NTDataContainer.NTDataContainerEntry zone)
        {
            using var imageDataStream = new MemoryStream(zone.Content);
            var reader = new BinaryReader(imageDataStream);

            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();

            var cells = new int[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[y, x] = reader.ReadByte();
                }
            }

            return new NosTaleMapZoneDto
            {
                Width = width,
                Height = height,
                Cells = cells
            };
        }
    }
}
