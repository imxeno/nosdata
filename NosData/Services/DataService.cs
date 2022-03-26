using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NosData.Converter;

namespace NosData.Services
{
    public class DataService
    {

        public static readonly Dictionary<string, string> GenericDatFiles = new()
        {
            { "bcards", "BCard.dat" },
            { "items", "Item.dat" },
            { "monsters", "monster.dat" },
            { "skills", "Skill.dat" },
            { "quests", "quest.dat" },
            { "questprizes", "qstprize.dat" },
            { "teams", "team.dat" },
            { "fishes", "fish.dat" }
        };

        public static readonly Dictionary<string, string> RawOnlyDatFiles = new()
        {
            { "actdescs", "act_desc.dat" },
            { "cards", "Card.dat" },
            { "npctalks", "npctalk.dat" },
            { "tutorial", "tutorial.dat" },
            { "shoptypes", "shoptype.dat" },
            { "mapids", "MapIDData.dat" },
            { "mappoints", "MapPointData.dat" },
            { "questnpcs", "qstnpc.dat" }
        };

        private readonly NosFileService _nosFileService;
        private readonly BlobsService _blobsService;

        public DataService(NosFileService nosFileService, BlobsService blobsService)
        {
            _nosFileService = nosFileService;
            _blobsService = blobsService;
        }

        public async Task<string?> GetData(string type)
        {
            if (!GenericDatFiles.ContainsKey(type)) return null;
            var file = GenericDatFiles[type];
            var stream = await _blobsService.GetBlob("gtd", $"{file}.json");
            if (stream == null) return null;
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public async Task<byte[]?> GetRawData(string type)
        {
            if (!GenericDatFiles.TryGetValue(type, out var file))
            {
                if (!RawOnlyDatFiles.TryGetValue(type, out file))
                {
                    return null;
                }
            }

            var stream = await _blobsService.GetBlob("gtd", file);
            if (stream == null) return null;
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }


        [FunctionName("RefreshData")]
        public async Task RefreshData([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
        {
            var nosGtdData = _nosFileService.FetchStringContainer("NSGtdData.NOS");

            foreach (var file in nosGtdData.Entries)
            {
                if (GenericDatFiles.Values.Contains(file.Key))
                {
                    var fileString = Encoding.ASCII.GetString(file.Value.Content);
                    await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(NosTaleDatToJsonConverter.Convert(fileString)));
                    await _blobsService.UploadBlob("gtd", $"{file.Key}.json", ms);
                }

                if (GenericDatFiles.Values.Contains(file.Key) || RawOnlyDatFiles.Values.Contains(file.Key))
                {
                    await using var ms = new MemoryStream(file.Value.Content);
                    await _blobsService.UploadBlob("gtd", file.Key, ms);
                }
            }
        }
    }
}
