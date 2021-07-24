using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NosCDN.Converter;
using NosCDN.NosPack;
using NosCDN.Utils;

namespace NosCDN
{
    public class NosCDN
    {
        private readonly AzureBlobCache _blobCache;

        private readonly Dictionary<string, string> _genericDatFiles = new()
        {
            {"bcards", "BCard.dat"},
            {"cards", "Card.dat"},
            {"items", "Item.dat"},
            {"monsters", "monster.dat"},
            {"skills", "Skill.dat"},
            {"quests", "quest.dat"},
            {"questprizes", "qstprize.dat"},
            {"teams", "team.dat"},
            {"fishes", "fish.dat"}
        };

        private readonly ILogger<NosCDN> _logger;

        private readonly Dictionary<string, string> _rawOnlyDatFiles = new()
        {
            {"npctalks", "npctalk.dat"},
            {"tutorial", "tutorial.dat"},
            {"shoptypes", "shoptype.dat"},
            {"mapids", "MapIDData.dat"},
            {"mappoints", "MapPointData.dat"},
            {"questnpcs", "qstnpc.dat"}
        };

        public NosCDN(ILogger<NosCDN> logger, AzureBlobCache blobCache)
        {
            _blobCache = blobCache;
            _logger = logger;
        }

        [Function("Data/{type}")]
        public HttpResponseData Get([HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequestData req,
            FunctionContext executionContext, string type)
        {
            var lowerCaseType = type.ToLower();

            if (!_genericDatFiles.ContainsKey(lowerCaseType)) return req.CreateResponse(HttpStatusCode.BadRequest);

            var datFile = FetchDatFile(_genericDatFiles[lowerCaseType]);

            if (datFile == null) return req.CreateResponse(HttpStatusCode.NotFound);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            response.WriteString(NosTaleDatToJsonConverter.Convert(datFile).ToString());

            return response;
        }

        [Function("Data/{type}/Raw")]
        public HttpResponseData GetRaw([HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequestData req,
            FunctionContext executionContext, string type)
        {
            var lowerCaseType = type.ToLower();
            string datFileName;

            if (_genericDatFiles.ContainsKey(lowerCaseType))
                datFileName = _genericDatFiles[lowerCaseType];
            else if (!_rawOnlyDatFiles.ContainsKey(lowerCaseType))
                return req.CreateResponse(HttpStatusCode.BadRequest);
            else
                datFileName = _rawOnlyDatFiles[lowerCaseType];

            var datFile = FetchDatFile(datFileName);

            if (datFile == null) return req.CreateResponse(HttpStatusCode.NotFound);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(datFile);

            return response;
        }

        [Function("Icons/{id}")]
        public HttpResponseData GetIcon([HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequestData req,
            FunctionContext executionContext, int id)
        {
            var icon = FetchIconFile(id);

            if (icon == null) return req.CreateResponse(HttpStatusCode.NotFound);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "image/bmp; charset=utf-8");

            response.WriteBytes(icon);

            return response;
        }

        private string FetchDatFile(string name)
        {
            var itemDatBytes = _blobCache.Load("gtd/" + name);
            if (itemDatBytes == null)
            {
                _logger.LogInformation("Serving freshly fetched data");
                var nosGtdData = FetchStringContainer("NSGtdData.NOS");

                if (!nosGtdData.Entries.TryGetValue(name, out var datFile)) return null;

                itemDatBytes = datFile.Content;
                _blobCache.Save("gtd/" + name, itemDatBytes);
            }
            else
            {
                _logger.LogInformation("Serving data from Azure Blob Cache");
            }

            return Encoding.ASCII.GetString(itemDatBytes);
        }

        private byte[] FetchIconFile(int id)
        {
            var iconBytes = _blobCache.Load("ip/" + id + ".bmp");
            if (iconBytes == null)
            {
                _logger.LogInformation("Serving freshly fetched data");
                var iconContainer = FetchDataContainer("NSipData.NOS");
                var icon = iconContainer.GetEntry(id);

                if (icon == null) return null;

                var imageData = icon.Content;
                var imageDataStream = new MemoryStream(imageData);
                var reader = new BinaryReader(imageDataStream);

                imageDataStream.Seek(1, SeekOrigin.Current);
                var xDim = reader.ReadUInt16();
                var yDim = reader.ReadUInt16();
                var xCenter = reader.ReadUInt16();
                var yCenter = reader.ReadUInt16();
                imageDataStream.Seek(4, SeekOrigin.Current);
                var bitmap = new Bitmap(xDim, yDim);

                for (var y = 0; y < yDim; y++)
                for (var x = 0; x < xDim; x++)
                {
                    int gb = reader.ReadByte();
                    int ar = reader.ReadByte();
                    var g = (gb >> 4) / 15d;
                    var b = (gb & 0xF) / 15d;
                    var a = (ar >> 4) / 15d;
                    var r = (ar & 0xF) / 15d;
                    bitmap.SetPixel(x, y,
                        Color.FromArgb((int) (a * 255), (int) (r * 255), (int) (g * 255), (int) (b * 255)));
                }

                var converter = new ImageConverter();
                iconBytes = (byte[]) converter.ConvertTo(bitmap, typeof(byte[])) ??
                            throw new InvalidOperationException();

                _blobCache.Save("ip/" + id + ".bmp", iconBytes);
            }
            else
            {
                _logger.LogInformation("Serving data from Azure Blob Cache");
            }

            return iconBytes;
        }

        private NTDataContainer FetchDataContainer(string name)
        {
            var data = FetchNosTaleUpdateFile(name);
            return NTDataContainer.Load(data);
        }

        private NTStringContainer FetchStringContainer(string name)
        {
            var data = FetchNosTaleUpdateFile(name);
            return NTStringContainer.Load(data);
        }

        private byte[] FetchNosTaleUpdateFile(string name)
        {
            var lowerCaseName = name.ToLower();
            var data = _blobCache.Load("dat/" + lowerCaseName);
            if (data == null)
            {
                _logger.LogInformation("Serving freshly fetched data for " + name);
                var spark = SparkNosTaleDataSource.Latest();
                data = spark.FileEntries().Single(e => e.Key.ToLower().Contains(lowerCaseName)).Value.Download();

                _blobCache.Save("dat/" + lowerCaseName, data);
            }
            else
            {
                _logger.LogInformation("Serving data from Azure Blob Cache");
            }

            return data;
        }
    }
}