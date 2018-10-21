using System.Collections.Generic;

namespace CM_UM_API
{
    public class FileManager
    {
        public readonly List<LstFile> FileList;

        public FileManager()
        {
            FileList = new List<LstFile>();
        }

        public void Add_File(LstFile meta)
        {
            FileList.Add(meta);
        }
    }
}
