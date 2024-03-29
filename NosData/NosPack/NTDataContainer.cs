﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace NosData.NosPack
{
    public class NTDataContainer
    {
        private readonly string _header;
        private readonly byte _separator;
        private readonly int _timestamp;
        public readonly HashSet<NTDataContainerEntry> Entries;

        private NTDataContainer(string header, int timestamp, byte separator, ISet<NTDataContainerEntry> entries)
        {
            _header = header;
            _timestamp = timestamp;
            _separator = separator;
            Entries = new HashSet<NTDataContainerEntry>(entries);
        }

        public static NTDataContainer Load(byte[] file)
        {
            using var stream = new MemoryStream(file);
            using var reader = new BinaryReader(stream);

            var rawHeader = new byte[12];

            reader.Read(rawHeader);

            var header = Encoding.ASCII.GetString(rawHeader);

            if (!header.Contains("NT Data") && !header.Contains("32GBS V1.0"))
                throw new Exception("Not a valid NT Data file");

            var timestamp = reader.ReadInt32();
            var fileCount = reader.ReadInt32();
            var separator = reader.ReadByte();

            var files = new HashSet<NTDataContainerEntry>();
            var entryHeaders = new List<EntryHeader>();
            for (var i = 0; i < fileCount; i++)
                entryHeaders.Add(new EntryHeader(reader.ReadInt32(), reader.ReadInt32()));
            foreach (var h in entryHeaders)
            {
                reader.BaseStream.Position = h.Offset;
                files.Add(NTDataContainerEntry.Load(h.Id, reader));
            }

            return new NTDataContainer(header, timestamp, separator, files);
        }

        public NTDataContainerEntry GetEntry(int id)
        {
            return Entries.FirstOrDefault(e => e.Id == id);
        }

        internal class EntryHeader
        {
            internal EntryHeader(int id, int offset)
            {
                Id = id;
                Offset = offset;
            }

            public int Id { get; }
            public int Offset { get; }
        }

        public class NTDataContainerEntry
        {
            private NTDataContainerEntry(int id, int timestamp, bool isCompressed, byte[] content)
            {
                Id = id;
                Timestamp = timestamp;
                IsCompressed = isCompressed;
                Content = content;
            }

            public int Id { get; }
            public int Timestamp { get; }
            public bool IsCompressed { get; }
            public byte[] Content { get; }

            public static NTDataContainerEntry Load(int id, BinaryReader reader)
            {
                var timestamp = reader.ReadInt32();
                var inflatedSize = reader.ReadInt32();
                var deflatedSize = reader.ReadInt32();
                var isCompressed = reader.ReadBoolean();
                var content = new byte[deflatedSize];
                reader.Read(content);
                if (!isCompressed) return new NTDataContainerEntry(id, timestamp, isCompressed, content);

                using var contentStream = new MemoryStream(content);
                contentStream.Seek(2, SeekOrigin.Begin);
                using var inflate = new DeflateStream(contentStream, CompressionMode.Decompress);
                var inflated = new byte[inflatedSize];
                inflate.Read(inflated);
                return new NTDataContainerEntry(id, timestamp, isCompressed, inflated);
            }
        }
    }
}