using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CM_UM_API
{
    public class LstManager
    {
        public readonly List<LstFile> UpdateLst;

        public LstManager(string iniPath, CompressedIo archive, Arch arch)
        {
            CompressedFile lstFile;
            if ((lstFile = archive.FindFile(iniPath.Replace(".ini", ".lst"))) == null)
            {
                lstFile = archive.FindFile(iniPath.Replace(".ini", "_" + arch + ".lst"));
            }

            UpdateLst = new List<LstFile>();
            string line;
            var text = new StringReader(Encoding.Default.GetString(archive.GetFileBin(lstFile)));
            while ((line = text.ReadLine()) != null)
            {
                var lst = LstParser(line, archive, lstFile);
                lst.LstFileMetadata = lstFile;
                UpdateLst.Add(lst);
            }
        }

        private LstFile LstParser(string line, CompressedIo archive, CompressedFile metadata)
        {
            var splitLine = line.Split(',');
            var updateLst = new LstFile
            {
                Source = splitLine[0] != "0" ? splitLine[1] : splitLine[2],
                Destination = splitLine[2],
                FileSize = ulong.Parse(splitLine[3]),
                Crc = splitLine[4],
                Revision = int.Parse(splitLine[5])
            };

            if (archive.GetType() == typeof(ZipArchiveIo))
            {
                var outerPath = metadata.FullName.Split('/').ToList();
                var innerPath = updateLst.Source.Replace("\\", "/").Split('/').ToList();
                outerPath.RemoveAt(outerPath.Count - 1);
                if (splitLine[0] == "0")
                {
                    outerPath.Add("data");
                }

                foreach (var item in innerPath)
                {
                    if (item == "..")
                    {
                        outerPath.RemoveAt(outerPath.Count - 1);
                    }
                    else
                    {
                        outerPath.Add(item);
                    }
                }

                var query = outerPath[0];
                outerPath.RemoveAt(0);
                foreach (var item in outerPath)
                {
                    if (item != "")
                    {
                        query += "/" + item;
                    }
                }
                updateLst.SrcFileMetadata = archive.FindFile(query);
            }
            else
            {
                var outerPath = metadata.FullName.Replace("\\", "/").Split('/').ToList();
                var innerPath = updateLst.Source.Replace("\\", "/").Split('/').ToList();
                outerPath.RemoveAt(outerPath.Count - 1);
                if (splitLine[0] == "0")
                {
                    outerPath.Add("data");
                }

                foreach (var item in innerPath)
                {
                    if (item == "..")
                    {
                        outerPath.RemoveAt(outerPath.Count - 1);
                    }
                    else
                    {
                        outerPath.Add(item);
                    }
                }

                var query = "";
                if (archive.GetType() == typeof(RawDir))
                {
                    query = outerPath[0];
                    outerPath.RemoveAt(0);
                }
                foreach (var item in outerPath)
                {
                    if (item != "")
                    {
                        query += "\\" + item;
                    }
                }
                updateLst.SrcFileMetadata = archive.FindFile(query);
            }

            return updateLst;
        }
    }
}
