using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Xml.Serialization;

namespace FileExplorer
{
    public partial class Form1 : Form
    {
        private static readonly string SettingsFilePath = @"settings.xml";

        private string DirectoryName = string.Empty;

        public Form1()
        {
            InitializeComponent();
            TryLoadSettings();

            this.MinimumSize = this.Size;

            UpdateMenuStrip();
            UpdateContextMenuStrip();
        }

        public Form1(string path)
        {
            InitializeComponent();

            if (!Directory.Exists(path))
                throw new ArgumentException(nameof(path));
            LoadDirectory(path);
            DirectoryName = path;
            dir_textBox.Text = path;

            this.MinimumSize = this.Size;

            UpdateMenuStrip();
            UpdateContextMenuStrip();
        }

        public void LoadDirectory(string path)
        {
            treeView.Nodes.Clear();

            DirectoryInfo dir = new DirectoryInfo(path);
            TreeNode node = treeView.Nodes.Add(dir.Name);
            node.Tag = dir.FullName;

            LoadFiles(path, node);
            LoadSubDirectories(path, node);

            node.Expand();
        }

        private void LoadSubDirectories(string path, TreeNode node)
        {
            var subDir = Directory.GetDirectories(path);
            foreach (var ob in subDir)
            {
                DirectoryInfo dir = new DirectoryInfo(ob);
                TreeNode subNode = node.Nodes.Add(dir.Name);
                subNode.Tag = dir.FullName;
                try
                {
                    LoadFiles(ob, subNode);
                    LoadSubDirectories(ob, subNode);
                }
                catch { }
            }
        }

        private void LoadFiles(string path, TreeNode node)
        {
            var Files = Directory.GetFiles(path);
            foreach (var ob in Files)
            {
                FileInfo file = new FileInfo(ob);
                var subNode = node.Nodes.Add(file.Name);
                subNode.Tag = file.FullName;
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
                            DirectoryName = directoryName;
                            LoadDirectory(DirectoryName);
                            dir_textBox.Text = DirectoryName;
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
            form.Show();// trouble with owner
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
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Arguments = (string)treeView.SelectedNode.Tag,
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
                message += $"Name: {file.Name}\nPath: {file.FullName}\nSize: {file.Length} Bytes\nCreation time: {file.CreationTime}\nLast write time: {file.LastWriteTime}";
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                message += $"Name: {dir.Name}\nPath: {dir.FullName}\nSize: {DirectorySize(dir)} Bytes\nCreation time: {dir.CreationTime}\nLast write time: {dir.LastWriteTime}";
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
        }
    }
}