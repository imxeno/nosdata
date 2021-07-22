﻿using System;
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
                var nameLength = reader.ReadInt32();

                var name = "";
                for(int j = 0; j < nameLength; j++)
                {
                    var c = reader.ReadChar();
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
            private static readonly byte[] _crypto
                = { 0x00, 0x20, 0x2D, 0x2E, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x0A, 0x00 };

            public bool IsDat { get; }
            private byte[] _content;

            public byte[] Content
            {
                get
                {
                    using var stream = new MemoryStream(_content);
                    using var reader = new BinaryReader(stream);
                    var result = new List<byte>();
                    if (IsDat)
                    {
                        var decryptedFile = new List<byte>();
                        var currIndex = 0;
                        while (currIndex < _content.Length)
                        {
                            byte currentByte = _content[currIndex];
                            currIndex++;
                            if (currentByte == (byte) 0xFF)
                            {
                                decryptedFile.Add((byte) 0xD);
                                continue;
                            }

                            int validate = currentByte & 0x7F;

                            if ((currentByte & 0x80) > 0)
                            {
                                for (; validate > 0; validate -= 2)
                                {
                                    if (currIndex >= _content.Length)
                                        break;

                                    currentByte = _content[currIndex];
                                    currIndex++;

                                    byte firstByte = _crypto[(currentByte & 0xF0) >> 4];
                                    decryptedFile.Add(firstByte);

                                    if (validate <= 1)
                                        break;
                                    byte secondByte = _crypto[currentByte & 0xF];

                                    if (secondByte <= 0)
                                        break;

                                    decryptedFile.Add(secondByte);
                                }
                            }
                            else
                            {
                                for (; validate > 0; --validate)
                                {
                                    if (currIndex >= _content.Length)
                                        break;

                                    currentByte = _content[currIndex];
                                    currIndex++;

                                    decryptedFile.Add((byte) (currentByte ^ 0x33));
                                }
                            }
                        }
                        return decryptedFile.ToArray();
                    }
                    else
                    {
                        var lines = reader.ReadInt32();
                        for (var i = 0; i < lines; i++)
                        {
                            var strLen = reader.ReadInt32();
                            var temp = new byte[strLen];
                            reader.Read(temp);
                            result.AddRange(temp.Select(b => (byte) (b ^ 0x1)));
                            result.Add((byte)'\n');
                        }

                        return result.ToArray();
                    }
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public NTStringContainerEntry(bool isDat, byte[] content)
            {
                this.IsDat = isDat;
                this._content = content;
            }
        }
    }
}
