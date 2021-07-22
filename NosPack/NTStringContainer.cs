using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace NosCDN.NosPack
{
    class NTStringContainer
    {
        public readonly Dictionary<string, NTStringContainerEntry> Entries = new();

        public static NTStringContainer Load(byte[] file)
        {
            using var stream = new MemoryStream(file);
            using var reader = new BinaryReader(stream);
            NTStringContainer container = new NTStringContainer();


            int fileCount = reader.ReadInt32();

            for (var i = 0; i < fileCount; i++)
            {
                var fileNumber = reader.ReadInt32();
                var nameSize = reader.ReadInt32();

                var name = "";
                while (true)
                {
                    var c = reader.ReadChar();
                    if (c == '\0') break;
                    name += c;
                }

                var isDat = reader.ReadInt32() > 0;
                var fileSize = reader.ReadInt32();
                var content = new byte[fileSize];

                reader.Read(content);
                container.Entries.Add(name, new NTStringContainerEntry(isDat, content));
            }

            return container;
        }

        internal class NTStringContainerEntry
        {
            private bool isDat;
            private byte[] content;

            public NTStringContainerEntry(bool isDat, byte[] content)
            {
                this.isDat = isDat;
                this.content = content;
            }
        }
    }
}
