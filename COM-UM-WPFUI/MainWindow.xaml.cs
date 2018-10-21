using CM_UM_API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;
using MessageBox = System.Windows.MessageBox;

namespace COM_UM_WPFUI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow
    {
        private bool _setupSource;
        private bool _setupDest;
        private bool _blockingCopy;
        private Arch _clientArch;
        private string _reqGamever = "";
        private Dictionary<string, FileManager> _lstManager = new Dictionary<string, FileManager>();

        public MainWindow()
        {
            InitializeComponent();
            Log("Welcome to CUSTOM ORDER MAID 3D2 Update Manager!");
        }

        private void Button_State(bool reset)
        {
            if (reset)
            {
                _blockingCopy = true;
                _setupSource = false;
                _setupDest = false;
                _lstManager = new Dictionary<string, FileManager>();
                SourceDir.Text = "Please Source Folder";
                DestDir.Text = "Please Destination Folder";
                ArchiveList.Items.Clear();
                RunScan.IsEnabled = false;
                RunCopy.IsEnabled = false;
                SelectSource.IsEnabled = true;
                SelectDest.IsEnabled = true;
                SelectProd.IsEnabled = true;
                Log("Reset Environment.");
                Status.Value = 0;
                Status.Maximum = 1;
                StatusView.Text = "";
            }
            else
            {
                if (_setupDest && _setupSource && SelectProd.SelectedValue != null)
                {
                    RunScan.IsEnabled = true;
                }
                else
                {
                    RunScan.IsEnabled = false;
                }
            }
        }

        private void Select_Source_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "プラグイン・DLC・アップデータのあるディレクトリを選択してください。";
                dialog.ShowNewFolderButton = false;
                dialog.ShowDialog();
                SourceDir.Text = dialog.SelectedPath;
            }
            _setupSource = true;
            Button_State(false);
        }

        private void Select_Dest_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "保存先のディレクトリを選択してください。";
                dialog.ShowDialog();
                DestDir.Text = dialog.SelectedPath;
            }
            _setupDest = true;
            Button_State(false);
        }

        private async void Run_Scan_Click(object sender, RoutedEventArgs e)
        {
            _blockingCopy = false;
            RunScan.IsEnabled = false;
            SelectSource.IsEnabled = false;
            SelectDest.IsEnabled = false;
            SelectProd.IsEnabled = false;
            var lstList = new List<LstManager>();

            var fileList = new List<string>();
            fileList.AddRange(Directory.GetFileSystemEntries(SourceDir.Text, "*.iso", SearchOption.AllDirectories));
            fileList.AddRange(Directory.GetFileSystemEntries(SourceDir.Text, "*.zip", SearchOption.AllDirectories));

            fileList.Add(SourceDir.Text + ".rootpath");

            foreach (var file in fileList)
            {
                try
                {
                    using (var archive = OpenArchive(file))
                    {
                        if (archive != null)
                        {
                            lstList.AddRange(await GetLstFile(archive));
                            ArchiveList.Items.Add(archive.ArchivePath);
                        }
                    }
                }
                catch (Exception err)
                {
                    Log("ERROR : " + file + "\n" + err.Message);
                }
            }

            foreach (var lst in lstList)
            {
                Log("Load Metadata : " + lst.UpdateLst[0].LstFileMetadata.FullName);
                foreach (var file in lst.UpdateLst)
                {
                    if (!_lstManager.ContainsKey(file.Destination))
                    {
                        _lstManager[file.Destination] = new FileManager();
                    }
                    _lstManager[file.Destination].Add_File(file);
                }
            }
            Log("Metadata Loaded.");
            RunCopy.IsEnabled = true;
        }

        private async void Run_Copy_Click(object sender, RoutedEventArgs e)
        {
            RunCopy.IsEnabled = false;
            var rootPath = DestDir.Text;
            var newLst = "";

            var resultText = "Finished.";
            Status.Maximum = _lstManager.Count;
            Status.Value = 0;
            foreach (var file in _lstManager)
            {
                if (!_blockingCopy)
                {
                    var dataDir = rootPath + "\\data\\";
                    file.Value.FileList.Sort();
                    file.Value.FileList.Reverse();
                    var head = file.Value.FileList[0];
                    using (var src = OpenArchive(head.SrcFileMetadata.ArcPath))
                    {
                        new FileInfo(dataDir + head.Destination).Directory?.Create();
                        using (var dest = new FileStream(dataDir + head.Destination, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            var srcFile = src.FindFile(head.SrcFileMetadata.FullName);
                            await src.CopyTo(srcFile, dest);
                            newLst += "0,0," + head.Destination + "," + head.FileSize + "," + head.Crc + "," + head.Revision + "\n";
                            Log("File Copied : " + head.Destination);
                            Status.Value += 1;
                        }
                    }
                    using (var sw = new StreamWriter(rootPath + "\\update.lst", false, Encoding.ASCII))
                    {
                        sw.Write(newLst);
                    }
                }
                else
                {
                    resultText = "Interrupted.";
                    _blockingCopy = false;
                    break;
                }

            }
            File.Copy(Directory.GetCurrentDirectory() + "\\assets\\readme.txt", rootPath + "\\readme.txt");
            Log(resultText);
        }

        private void Log(string log)
        {
            StatusBar.Content = log;
            StatusView.Text += log + "\n";
            LogScroll.ScrollToEnd();
        }

        private async Task<List<LstManager>> GetLstFile(CompressedIo archive)
        {
            var lstList = new List<LstManager>();
            var archiveItems = archive.ListItem();
            foreach (var item in archiveItems)
            {
                if (item.Name == "update.ini")
                {
                    var data = await archive.GetFileBinAsync(item);
                    var ini = new DescribeIni(data);

                    if (ini.Get("UPDATER", "AppExe") == _reqGamever)
                    {
                        lstList.Add(new LstManager(item.FullName, archive, _clientArch));
                    }
                }
            }
            return lstList;
        }

        private CompressedIo OpenArchive(string path)
        {
            var ext = path.Split('.').Last().ToLower();
            CompressedIo archive;
            try
            {
                switch (ext)
                {
                    case "iso":
                        archive = new CdIo(path);
                        break;
                    case "zip":
                        archive = new ZipArchiveIo(path);
                        break;
                    case "rootpath":
                        archive = new RawDir(path);
                        break;
                    default:
                        throw new ApplicationException("Not supported type of file.");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return archive;
        }

        private void Select_Prod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = SelectProd.SelectedValue.ToString().Split(' ')[1];
            switch (selected)
            {
                case "CM3D2_x86":
                    _clientArch = Arch.X86;
                    _reqGamever = "CM3D2.exe";
                    break;
                case "CM3D2_x64":
                    _clientArch = Arch.X64;
                    _reqGamever = "CM3D2.exe";
                    break;
                case "CM3D2OH_x86":
                    _clientArch = Arch.X86;
                    _reqGamever = "CM3D2OH.exe";
                    break;
                case "CM3D2OH_x64":
                    _clientArch = Arch.X64;
                    _reqGamever = "CM3D2OH.exe";
                    break;
                case "COM3D2":
                    _clientArch = Arch.X64;
                    _reqGamever = "COM3D2.exe";
                    break;
                case "COM3D2OH":
                    _clientArch = Arch.X64;
                    _reqGamever = "COM3D2OH.exe";
                    break;
            }
            Button_State(false);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Button_State(true);
        }

        private void Version_Click(object sender, RoutedEventArgs e)
        {
            var text = "カスタムメイド3D2・カスタムオーダーメイド3D2のプラグインやアップデータを結合します。\n" +
                       "このソフトウェアはKISSの許諾を受けたものではありません。\n\n" +
                       "このソフトウェアは、MITライセンスのもとで公開されています。\n" +
                       "©2018 YUKIMOCHI Laboratory\n\n" +
                       "Github: https://github.com/yukimochi/COM_Update_Manager\n" +
                       "Twitter: @Naoki_Kosaka_\n" +
                       "ActivityPub: @YUKIMOCHI@toot.yukimochi.jp\n" +
                       "Web: https://lab.yukimochi.jp/";
            MessageBox.Show(text);
        }
    }
}
