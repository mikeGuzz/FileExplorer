using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.WebSockets;
using System.Security.AccessControl;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace FileExplorer
{
    public enum IconIndex { None = 0, FolderClosed = 1, FolderOpen = 2 };

    public partial class Form1 : Form
    {
        private static readonly string SettingsFilePath = @"settings.xml";

        private string DirectoryName = string.Empty;
        private List<string> allFilesExt = new List<string>();
        private long itemsCount = 1;

        public Form1()
        {
            InitializeComponent();
            ResetIcons();
            TryLoadSettings();

            this.MinimumSize = new Size(256, 144);

            UpdateMenuStrip();
            UpdateContextMenuStrip();
        }

        public Form1(string path)
        {
            InitializeComponent();
            ResetIcons();

            if (!Directory.Exists(path))
                throw new ArgumentException(nameof(path));
            LoadDirectory(path);

            this.MinimumSize = new Size(256, 144);

            UpdateMenuStrip();
            UpdateContextMenuStrip();
        }

        private void ResetIcons()
        {
            allFilesExt.Clear();
            imageList1.Images.Clear();
            imageList1.Images.Add(new Bitmap("error_icn.png"));
            allFilesExt.Add("none");
            imageList1.Images.Add(new Bitmap("directory_closed.png"));
            allFilesExt.Add("folder_closed");
            imageList1.Images.Add(new Bitmap("directory_open.png"));
            allFilesExt.Add("folder_open");
        }

        private int GetIconIndex(string path)
        {
            if (Directory.Exists(path))
                return (int)IconIndex.FolderClosed;
            if(!File.Exists(path))
                throw new FileNotFoundException(path);

            FileInfo file = new FileInfo(path);
            var index = allFilesExt.IndexOf(file.Extension);
            if (index != -1)
                return index;

            var icon = Icon.ExtractAssociatedIcon(file.FullName);
            if (icon == null)
                return (int)IconIndex.None;

            imageList1.Images.Add(icon);
            allFilesExt.Add(file.Extension);

            return imageList1.Images.Count - 1;
        }

        private void LoadDirectory(string path)
        {
            size_toolStripStatusLabel.Visible = false;
            treeView.Nodes.Clear();
            ResetIcons();
            itemsCount = 1;

            DirectoryInfo dir = new DirectoryInfo(path);
            TreeNode nodeRef = treeView.Nodes.Add(dir.FullName);
            nodeRef.Tag = dir.FullName;
            nodeRef.ImageIndex = (int)IconIndex.FolderClosed;
            nodeRef.SelectedImageIndex = (int)IconIndex.FolderClosed;

            LoadFiles(path, nodeRef);
            LoadSubDirectories(path, nodeRef);

            nodeRef.Expand();

            dir_textBox.Text = path;
            items_toolStripStatusLabel.Text = $"{itemsCount} items";
        }

        private void LoadSubDirectories(string path, TreeNode node)
        {
            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(path);
            }
            catch
            {
                return;
            }
            foreach (var ob in subDirs)
            {
                DirectoryInfo dir = new DirectoryInfo(ob);
                var att = dir.Attributes;

                bool isHidden = (att & FileAttributes.Hidden) == FileAttributes.Hidden, isSystem = (att & FileAttributes.System) == FileAttributes.System;
                if (!(isHidden && !hiddenToolStripMenuItem.Checked) && !(isSystem && !systemToolStripMenuItem.Checked))
                {
                    TreeNode subNode = node.Nodes.Add(dir.Name);
                    subNode.Tag = dir.FullName;
                    if (isHidden)
                        subNode.ForeColor = Color.DimGray;
                    else if (isSystem)
                        subNode.ForeColor = Color.DarkBlue;
                    subNode.ImageIndex = (int)IconIndex.FolderClosed;
                    subNode.SelectedImageIndex= (int)IconIndex.FolderClosed;
                    LoadFiles(ob, subNode);
                    LoadSubDirectories(ob, subNode);// приклад рекурсii

                    itemsCount++;
                }
            }
        }

        private void LoadFiles(string path, TreeNode node)
        {
            string[] subFiles;
            try
            {
                subFiles = Directory.GetFiles(path);
            }
            catch
            {
                return;
            }

            foreach (var ob in subFiles)
            {
                FileInfo file = new FileInfo(ob);
                var att = file.Attributes;
                bool isHidden = (att & FileAttributes.Hidden) == FileAttributes.Hidden, isSystem = (att & FileAttributes.System) == FileAttributes.System;
                if (!(isHidden && !hiddenToolStripMenuItem.Checked) && !(isSystem && !systemToolStripMenuItem.Checked))
                {
                    var subNode = node.Nodes.Add(file.Name);
                    subNode.Tag = file.FullName;
                    if (isHidden)
                        subNode.ForeColor = Color.DimGray;
                    else if(isSystem)
                        subNode.ForeColor = Color.DarkBlue;
                    var index = GetIconIndex(file.FullName);
                    subNode.ImageIndex = index;
                    subNode.SelectedImageIndex = index;

                    itemsCount++;
                }
            }
        }

        private long DirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            var files = dir.GetFiles();
            foreach (var ob in files)
            {
                size += ob.Length;
            }

            var dirs = dir.GetDirectories();
            foreach(var ob in dirs)
            {
                size += DirectorySize(ob);
            }
            return size;
        }

        private void UpdateMenuStrip()
        {
            bool value = treeView.SelectedNode != null;
            openInFileExplorerToolStripMenuItem.Enabled = value;
            copyPathToolStripMenuItem.Enabled = value;
            propertiesToolStripMenuItem.Enabled = value;
        }

        private void UpdateContextMenuStrip()
        {
            bool value = treeView.SelectedNode != null;
            if (treeView.SelectedNode != null)
                newWindowToolStripMenuItem1.Enabled = Directory.Exists((string)treeView.SelectedNode.Tag);
            openInFileExplorerToolStripMenuItem1.Enabled = value;
            copyPathToolStripMenuItem1.Enabled = value;
            propertiesToolStripMenuItem1.Enabled = value;
        }

        private void SaveSettings()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(string));
            using(Stream stream = File.Create(SettingsFilePath))
            {
                serializer.Serialize(stream, DirectoryName);
            }
        }

        private void TryLoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(string));
                    using(Stream stream = File.OpenRead(SettingsFilePath))
                    {
                        if(serializer.Deserialize(stream) is string directoryName)
                        {
                            dir_textBox.Text = directoryName;
                            DirectoryName = directoryName;
                        }
                        else
                        {
                            stream.Dispose();
                            File.Delete(SettingsFilePath);
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.Delete(SettingsFilePath);
                    this.Close();
                }
            }
        }

        private void load_button_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(dir_textBox.Text))
            {
                MessageBox.Show($"Empty directory", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(dir_textBox.Text))
            {
                MessageBox.Show($"Directory '{dir_textBox.Text}' does not exist", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DirectoryName = dir_textBox.Text;
            LoadDirectory(DirectoryName);
            SaveSettings();
        }

        private void select_button_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (Directory.Exists(dir_textBox.Text))
                dialog.SelectedPath = dir_textBox.Text;
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                dir_textBox.Text = dialog.SelectedPath;
            }
        }

        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new Form1();
            form.Show();
        }

        private void openDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                var form = new Form1(dialog.SelectedPath);
                form.Show();
            }
        }

        private void openInFileExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null)
                return;
            try
            {
                var path = (string)treeView.SelectedNode.Tag;
                if (!Directory.Exists(path))
                    path = $"/select,{path}";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = path,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null)
                return;
            Clipboard.SetText((string)treeView.SelectedNode.Tag);
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null)
                return;
            var path = (string)treeView.SelectedNode.Tag;
            string message = string.Empty;
            if (File.Exists(path))
            {
                FileInfo file = new FileInfo(path);
                message += $"Name: {file.Name}\nPath: {file.FullName}\nSize: {file.Length} Bytes\nCreation time: {file.CreationTime}\nLast write time: {file.LastWriteTime}\nAttributes: {file.Attributes}";
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                message += $"Name: {dir.Name}\nPath: {dir.FullName}\nSize: {DirectorySize(dir)} Bytes\nCreation time: {dir.CreationTime}\nLast write time: {dir.LastWriteTime}\nAttributes: {dir.Attributes}";
            }
            MessageBox.Show(message);
        }

        private void newWindowToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode == null || !Directory.Exists((string)treeView.SelectedNode.Tag))
                return;
            var form = new Form1((string)treeView.SelectedNode.Tag);
            form.Show();
        }

        private void openInFileExplorerToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openInFileExplorerToolStripMenuItem_Click(sender, e);
        }

        private void copyPathToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            copyPathToolStripMenuItem_Click(sender, e);
        }

        private void propertiesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            propertiesToolStripMenuItem_Click(sender, e);
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateMenuStrip();
            UpdateContextMenuStrip();
            if (e.Node == null)
            {
                size_toolStripStatusLabel.Visible = false;
                return;
            }
            else
                size_toolStripStatusLabel.Visible = true;
            long size = 0;
            if (Directory.Exists((string)e.Node.Tag))
                size = DirectorySize(new DirectoryInfo((string)e.Node.Tag));
            else
                size = new FileInfo((string)e.Node.Tag).Length;
            size_toolStripStatusLabel.Text = $"{size} bytes";
        }

        private void recentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dir_textBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
            dir_textBox.SelectAll();
            dir_textBox.Focus();
        }

        private void documentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dir_textBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dir_textBox.SelectAll();
            dir_textBox.Focus();
        }

        private void desktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dir_textBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dir_textBox.SelectAll();
            dir_textBox.Focus();
        }

        private void userToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dir_textBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dir_textBox.SelectAll();
            dir_textBox.Focus();
        }

        private void picturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dir_textBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            dir_textBox.SelectAll();
            dir_textBox.Focus();
        }

        private void musicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dir_textBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            dir_textBox.SelectAll();
            dir_textBox.Focus();
        }

        private void treeView_MouseMove(object sender, MouseEventArgs e)
        {
            var node = treeView.GetNodeAt(e.Location);
            if (node == null || node.Tag == null)
                return;

        }

        private void hiddenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //save
        }

        private void systemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //save
        }

        private void treeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == null)
                return;
            if (!Directory.Exists((string)e.Node.Tag))
                return;

            e.Node.ImageIndex = (int)IconIndex.FolderOpen;
            e.Node.SelectedImageIndex = (int)IconIndex.FolderOpen;
        }

        private void treeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == null)
                return;
            if (!Directory.Exists((string)e.Node.Tag))
                return;

            e.Node.ImageIndex = (int)IconIndex.FolderClosed;
            e.Node.SelectedImageIndex = (int)IconIndex.FolderClosed;
        }

        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (Directory.Exists((string)e.Node.Tag))
                return;

            var path = (string)e.Node.Tag;
            if (!Directory.Exists(path))
                path = $"/select,{path}";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = path,
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }
    }
}