using System;
using System.Json;
using System.Linq;

namespace NosData.Converter
{
    public static class NosTaleDatToJsonConverter
    {
        public static string Convert(string dat)
        {
            var split = dat.Split("\r");
            var items = new JsonArray();
            JsonObject obj = null;

            for (var i = 0; i < split.Length; i++)
            {
                var line = split[i];

                /* Ignoring slash-slash comments found e.g. in team.dat */
                if (line.Contains("//"))
                    line = line.Substring(0, line.IndexOf("//", StringComparison.Ordinal));

                /* Skipping empty lines */
                if (line.Length == 0) continue;

                var splitLine = line.Split('\t', ' ').Where(e => e.Length > 0).ToList();

                /* Treat ~, #, END as possible object separators */
                if (line[0] == '~' || line[0] == '#' || splitLine[0] == "END")
                {
                    if (obj != null)
                    {
                        items.Add(obj);
                        obj = null;
                    }

                    continue;
                }

                /* Treat VNUM as an element that starts an object definition */
                if (splitLine[0] == "VNUM") obj = new JsonObject();

                /* Ignore BEGIN keyword found e.g. in quest.dat */
                if (splitLine[0] == "BEGIN") continue;
                ;

                /* LINEDESC needs some special treatment */
                if (splitLine[0] == "LINEDESC")
                {
                    var lineDesc = int.Parse(splitLine[1]);
                    obj[splitLine[0].ToLower()] = lineDesc;
                    i++;
                    line = split[i];
                    obj["desc"] = line;
                    continue;
                }

                obj[splitLine[0].ToLower()] = new JsonArray(splitLine.Skip(1).Select(o =>
                    o.StartsWith("z") ? new JsonPrimitive(o) : new JsonPrimitive(int.Parse(o))));
            }

            return items.ToString();
        }
    }
}