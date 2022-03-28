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
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        public async Task<Stream?> GetSpriteSheet(string format)
        {
            if (format != "json" && format != "webp" && format != "png") return null;
            return await _blobsService.GetBlob("icons", $"sheet/spritesheet.{format}");
        }

        public async Task<Stream?> GetIcon(int id)
        {
            return await _blobsService.GetBlob("icons", $"single/{id}.png");
        }
        
        public async Task RefreshIcons()
        {
            var startTime = DateTime.Now;
            _logger.LogInformation($"Icons refresh started at {startTime}.");

            var iconContainer = _nosFileService.FetchDataContainer("NSipData.NOS");
            foreach(var icon in iconContainer.Entries)
            {
                var image = IconToImage(icon);
                await using var outStream = new MemoryStream();
                await image.SaveAsPngAsync(outStream);
                var data = outStream.ToArray();
                await using var ms = new MemoryStream(data);
                await _blobsService.UploadBlob("icons", $"single/{icon.Id}.png", ms);
            }

            _logger.LogInformation($"Icons refresh done in {(DateTime.Now - startTime).TotalSeconds} seconds!");

            await RefreshSpriteSheet(iconContainer);
        }

        private async Task RefreshSpriteSheet(NTDataContainer iconContainer)
        {
            var startTime = DateTime.Now;
            _logger.LogInformation($"Icon sprite sheet refresh started at {startTime}.");

            Dictionary<int, Image<Rgba32>> imageIds = new();

            var images = (from icon in iconContainer.Entries let image = IconToImage(icon) where imageIds.TryAdd(icon.Id, image) select image).ToList();

            images = images.OrderBy(image => image.Width).ThenBy(image => image.Height).ToList();

            const int outWidth = 2880;

            var xPos = 0;
            var yPos = 0;
            var biggestHeight = 0;

            foreach (var image in images)
            {
                if (xPos + image.Width > outWidth)
                {
                    yPos += biggestHeight;
                    xPos = 0;
                    biggestHeight = 0;
                }

                if (image.Height > biggestHeight)
                    biggestHeight = image.Height;

                xPos += image.Width;
            }

            var outHeight = yPos + biggestHeight;

            var outImage = new Image<Rgba32>(outWidth, outHeight);
            var outImageDesc = new Dictionary<int, int[]>();

            xPos = 0;
            yPos = 0;
            biggestHeight = 0;

            foreach (var image in images)
            {
                if (xPos + image.Width > outWidth)
                {
                    yPos += biggestHeight;
                    xPos = 0;
                    biggestHeight = 0;
                }

                if (image.Height > biggestHeight)
                    biggestHeight = image.Height;

                var pos = xPos;
                var pos1 = yPos;
                outImage.Mutate(o => o.DrawImage(image, new Point(pos, pos1), 1f));
                outImageDesc.Add(imageIds.First(k => k.Value == image).Key, new int[] { xPos, yPos, image.Width, image.Height });

                xPos += image.Width;
            }

            {
                await using var ms = new MemoryStream();
                await outImage.SaveAsPngAsync(ms);
                ms.Position = 0;
                await _blobsService.UploadBlob("icons", "sheet/spritesheet.png", ms);
            }

            {
                await using var ms = new MemoryStream();
                await outImage.SaveAsWebpAsync(ms);
                ms.Position = 0;
                await _blobsService.UploadBlob("icons", "sheet/spritesheet.webp", ms);
            }

            {
                await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(outImageDesc)));
                ms.Position = 0;
                await _blobsService.UploadBlob("icons", "sheet/spritesheet.json", ms);
            }

            _logger.LogInformation($"Icon sprite sheet refresh done in {(DateTime.Now - startTime).TotalSeconds} seconds!");
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
