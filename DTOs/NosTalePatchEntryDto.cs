namespace NosData.DTOs
{
    public class NosTalePatchEntryDto
    {
        public string Path { get; set; }
        public string Sha1 { get; set; }
        public string File { get; set; }
        public uint Flags { get; set; }
        public ulong Size { get; set; }
        public bool Folder { get; set; }
    }
}