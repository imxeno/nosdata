using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Json;

namespace NosCDN.Converter
{
    public static class NosTaleDatToJsonConverter
    {
        public static JsonArray Convert(string dat)
        {
            var splitted = dat.Split("\r");
            var items = new JsonArray();
            JsonObject obj = null;
            
            for (int i = 0; i < splitted.Length; i++)
            {
                string line = splitted[i];
                if (line.Length == 0) continue;
                var splittedLine = line.Split("\t").Where(e => e.Length > 0).ToList();

                if (line[0] == '~' || line[0] == '#' || splittedLine[0] == "END")
                {
                    if (obj != null)
                    {
                        items.Add(obj);
                        obj = null;
                    }
                    continue;
                }

                if (splittedLine[0] == "VNUM")
                {
                    obj = new JsonObject();
                }

                if (splittedLine[0] == "LINEDESC")
                {
                    var lineDesc = Int32.Parse(splittedLine[1]);
                    var descLines = new JsonArray();
                    for (int c = 0; c < lineDesc; c++)
                    {
                        i++;
                        line = splitted[i];
                        descLines.Add(new JsonPrimitive(line));
                    }

                    obj[splittedLine[0].ToLower()] = descLines;
                    continue;
                }

                obj[splittedLine[0].ToLower()] = new JsonArray(splittedLine.Skip(1).Select((o) =>
                {
                    if (o.StartsWith("z"))
                    {
                        return new JsonPrimitive(o);
                    }
                    else
                    {
                        return new JsonPrimitive(int.Parse(o));
                    }
                }));
            }

            return items;
        }
    }
}
