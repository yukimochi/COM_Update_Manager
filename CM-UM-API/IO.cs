using DiscUtils.Iso9660;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace CM_UM_API
{
    public abstract class CompressedIo : IDisposable
    {
        protected string Path;
        protected FileStream File;

        public abstract IEnumerable<CompressedFile> ListItem();
        public abstract CompressedFile FindFile(string fullPath);
        public abstract byte[] GetFileBin(CompressedFile metadata);
        public abstract Task<byte[]> GetFileBinAsync(CompressedFile metadata);
        public abstract Task CopyTo(CompressedFile sourceMetadata, Stream destinationStream);
        public abstract void Dispose();

        public string ArchivePath => Path;
    }

    public class ZipArchiveIo : CompressedIo
    {
        private readonly ZipArchive _arc;

        public ZipArchiveIo(string arcPath)
        {
            Path = arcPath;
            try
            {
                File = new FileStream(arcPath, FileMode.Open, FileAccess.Read);
                _arc = new ZipArchive(File, ZipArchiveMode.Read);
            }
            catch (Exception)
            {
                if (_arc != null)
                    _arc.Dispose();
                if (File != null)
                    File.Dispose();
                throw;
            }
        }

        public override IEnumerable<CompressedFile> ListItem()
        {
            var itemList = new List<CompressedFile>();

            foreach (var item in _arc.Entries)
            {
                if (item.Name == "") continue;
                var content = new CompressedFile
                {
                    ArcPath = Path,
                    FullName = item.FullName,
                    Name = item.Name
                };

                itemList.Add(content);
            }

            return itemList;
        }

        public override CompressedFile FindFile(string fullPath)
        {
            ZipArchiveEntry item;
            if ((item = _arc.Entries.SingleOrDefault(x => x.FullName == fullPath)) == null) return null;
            var content = new CompressedFile
            {
                ArcPath = Path,
                FullName = item.FullName,
                Name = item.Name
            };

            return content;
        }

        public override byte[] GetFileBin(CompressedFile metadata)
        {
            var entry = _arc.Entries.SingleOrDefault(x => x.FullName == metadata.FullName)?.Open();
            if (entry == null) return null;
            byte[] data;
            using (var ms = new MemoryStream())
            {
                entry.CopyTo(ms);
                data = ms.ToArray();
            }
            return data;
        }

        public override async Task<byte[]> GetFileBinAsync(CompressedFile metadata)
        {
            using (var entry = _arc.Entries.SingleOrDefault(x => x.FullName == metadata.FullName)?.Open())
            {
                if (entry == null) return null;
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    await entry.CopyToAsync(ms);
                    data = ms.ToArray();
                }
                return data;
            }
        }

        public override async Task CopyTo(CompressedFile sourceMetadata, Stream destinationStream)
        {
            using (var entry = _arc.Entries.SingleOrDefault(x => x.FullName == sourceMetadata.FullName)?.Open())
            {
                if (entry != null)
                {
                    await entry.CopyToAsync(destinationStream);
                }
            }
        }

        public override void Dispose()
        {
            _arc.Dispose();
            File.Dispose();
        }
    }

    public class RawDir : CompressedIo
    {
        public RawDir(string rootPath)
        {
            this.Path = rootPath;
        }

        public override IEnumerable<CompressedFile> ListItem()
        {
            var itemList = new List<CompressedFile>();
            var listItem = ListFiles("");

            foreach (var item in listItem)
            {
                var content = new CompressedFile
                {
                    ArcPath = Path,
                    FullName = item
                };
                content.Name = content.FullName.Split('\\').LastOrDefault();

                itemList.Add(content);
            }

            return itemList;
        }

        private IEnumerable<string> ListFiles(string dirPath)
        {
            var fileList = new List<string>();
            if (dirPath == "") dirPath = Path.Replace(".rootpath", ""); ;
            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                fileList.AddRange(Directory.GetFiles(subDir));
                fileList.AddRange(ListFiles(subDir));
            }

            return fileList;
        }

        public override CompressedFile FindFile(string fullPath)
        {
            if (!System.IO.File.Exists(fullPath)) return null;
            var content = new CompressedFile
            {
                ArcPath = Path,
                FullName = fullPath
            };
            content.Name = content.FullName.Split('\\').LastOrDefault();

            return content;
        }

        public override byte[] GetFileBin(CompressedFile metadata)
        {
            if (!System.IO.File.Exists(metadata.FullName)) return null;
            var fs = System.IO.File.Open(metadata.FullName, FileMode.Open, FileAccess.Read);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                data = ms.ToArray();
            }
            return data;
        }

        public override async Task<byte[]> GetFileBinAsync(CompressedFile metadata)
        {
            if (!System.IO.File.Exists(metadata.FullName)) return null;
            var fs = System.IO.File.Open(metadata.FullName, FileMode.Open, FileAccess.Read);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                data = ms.ToArray();
            }
            return data;
        }

        public override async Task CopyTo(CompressedFile sourceMetadata, Stream destinationStream)
        {
            if (System.IO.File.Exists(sourceMetadata.FullName))
            {
                var fs = System.IO.File.Open(sourceMetadata.FullName, FileMode.Open, FileAccess.Read);
                await fs.CopyToAsync(destinationStream);
            }
        }

        public override void Dispose()
        {
        }
    }

    public class CdIo : CompressedIo
    {
        private readonly CDReader _cd;

        public CdIo(string cdPath)
        {
            Path = cdPath;
            try
            {
                File = new FileStream(cdPath, FileMode.Open);
                _cd = new CDReader(File, true);
            }
            catch (Exception)
            {
                _cd?.Dispose();
                File?.Dispose();
                throw;
            }
        }

        public override IEnumerable<CompressedFile> ListItem()
        {
            var itemList = new List<CompressedFile>();
            var listItem = ListFiles("");

            foreach (var item in listItem)
            {
                var content = new CompressedFile
                {
                    ArcPath = Path,
                    FullName = item.Remove(item.Length - 2)
                };
                content.Name = content.FullName.Split('\\').LastOrDefault();

                itemList.Add(content);
            }

            return itemList;
        }

        private IEnumerable<string> ListFiles(string dirPath)
        {
            var fileList = new List<string>();
            foreach (var subDir in _cd.GetDirectories(dirPath))
            {
                fileList.AddRange(_cd.GetFiles(subDir));
                fileList.AddRange(ListFiles(subDir));
            }

            return fileList;
        }

        public override CompressedFile FindFile(string fullPath)
        {
            if (!_cd.FileExists(fullPath)) return null;
            var content = new CompressedFile
            {
                ArcPath = Path,
                FullName = fullPath
            };
            content.Name = content.FullName.Split('\\').LastOrDefault();

            return content;
        }

        public override byte[] GetFileBin(CompressedFile metadata)
        {
            if (!_cd.FileExists(metadata.FullName)) return null;
            var fs = _cd.OpenFile(metadata.FullName, FileMode.Open, FileAccess.Read);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                data = ms.ToArray();
            }
            return data;
        }

        public override async Task<byte[]> GetFileBinAsync(CompressedFile metadata)
        {
            if (!_cd.FileExists(metadata.FullName)) return null;
            var fs = _cd.OpenFile(metadata.FullName, FileMode.Open, FileAccess.Read);
            byte[] data;
            using (var ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                data = ms.ToArray();
            }
            return data;
        }

        public override async Task CopyTo(CompressedFile sourceMetadata, Stream destinationStream)
        {
            if (_cd.FileExists(sourceMetadata.FullName))
            {
                var fs = _cd.OpenFile(sourceMetadata.FullName, FileMode.Open, FileAccess.Read);
                await fs.CopyToAsync(destinationStream);
            }
        }

        public override void Dispose()
        {
            _cd.Dispose();
            File.Dispose();
        }
    }
}
