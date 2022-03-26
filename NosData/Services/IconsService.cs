using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NosData.NosPack;
using NosData.Services;
using SixLabors.ImageSharp;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using SixLabors.ImageSharp.PixelFormats;

namespace NosData
{
    public class IconsService
    {
        private readonly ILogger<IconsService> _logger;
        private readonly NosFileService _nosFileService;
        private readonly BlobsService _blobsService;

        public IconsService(ILogger<IconsService> logger, NosFileService nosFileService, BlobsService blobsService)
        {
            _logger = logger;
            _nosFileService = nosFileService;
            _blobsService = blobsService;
        }

        public async Task<Stream?> GetAllIcons()
        {
            return await _blobsService.GetBlob("icons", $"all.json");
        }

        public async Task<Stream?> GetIcon(int id)
        {
            return await _blobsService.GetBlob("icons", $"{id}.png");
        }
        
        public async Task RefreshIcons()
        {
            var startTime = DateTime.Now;
            _logger.LogInformation($"Icons refresh started at {startTime}.");

            var iconContainer = _nosFileService.FetchDataContainer("NSipData.NOS");
            Dictionary<int, byte[]> images = new();
            foreach(var icon in iconContainer.Entries)
            {
                var image = IconToImage(icon);
                await using var outStream = new MemoryStream();
                await image.SaveAsPngAsync(outStream);
                var data = outStream.ToArray();
                if (images.ContainsKey(icon.Id)) images.Remove(icon.Id);
                images.Add(icon.Id, data);
                await using var ms = new MemoryStream(data);
                await _blobsService.UploadBlob("icons", $"{icon.Id}.png", ms);
            }

            await using var allMs = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(images)));
            await _blobsService.UploadBlob("icons", "all.json", allMs);

            _logger.LogInformation($"Icons refresh done in {(DateTime.Now - startTime).TotalSeconds} seconds!");
        }

        private static Image<Rgba32> IconToImage(NTDataContainer.NTDataContainerEntry icon)
        {
            using var imageDataStream = new MemoryStream(icon.Content);
            var reader = new BinaryReader(imageDataStream);

            imageDataStream.Seek(1, SeekOrigin.Current);
            var xDim = reader.ReadUInt16();
            var yDim = reader.ReadUInt16();

            reader.ReadUInt16(); // xCenter
            reader.ReadUInt16(); // yCenter

            imageDataStream.Seek(4, SeekOrigin.Current);
            var image = new Image<Rgba32>(xDim, yDim);

            for (var y = 0; y < yDim; y++)
            for (var x = 0; x < xDim; x++)
            {
                int gb = reader.ReadByte();
                int ar = reader.ReadByte();
                var g = (gb >> 4) / 15f;
                var b = (gb & 0xF) / 15f;
                var a = (ar >> 4) / 15f;
                var r = (ar & 0xF) / 15f;
                image[x, y] = new Rgba32(r, g, b, a);
            }

            return image;
        }
    }
}
