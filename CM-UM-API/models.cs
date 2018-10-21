using System;

namespace CM_UM_API
{
    public enum Arch
    {
        X64,
        X86
    }

    public class CompressedFile
    {
        public string ArcPath;
        public string FullName;
        public string Name;
    }

    public class LstFile : IComparable
    {
        public CompressedFile LstFileMetadata;
        public CompressedFile SrcFileMetadata;
        public string Source;
        public string Destination;
        public ulong FileSize;
        public string Crc;
        public int Revision;

        int IComparable.CompareTo(object obj)
        {
            var compare = (LstFile)obj;
            return Revision - compare.Revision;
        }
    }
}
