#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFtoPS
{
    public partial class PDFtoPS : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const uint LVM_SETINSERTMARK = 0x10A6;
        private const uint LVM_GETHEADER = 0x101F;
        private const uint HDM_GETITEM = 0x120B;
        private const uint HDM_SETITEM = 0x120C;
        private const int HDI_FORMAT = 0x0004;
        private const int HDF_STRING = 0x4000;
        private const int HDF_SORTUP = 0x0400;
        private const int HDF_SORTDOWN = 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        public struct LVINSERTMARK
        {
            public uint cbSize;
            public uint dwFlags;
            public int iItem;
            public uint dwReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HDITEM
        {
            public uint mask;
            public int cxy;
            public string pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public int fmt;
            public IntPtr lParam;
            public int iImage;
            public int iOrder;
            public uint type;
            public IntPtr pvFilter;
            public uint state;
        }

        private readonly Dictionary<string, string> profiles = new()
        {
            { "Standard (PS Level 3)", "-sDEVICE=ps2write -dLanguageLevel=3" },
            { "Legacy (PS Level 2)", "-sDEVICE=ps2write -dLanguageLevel=2" },
            { "Grayscale", "-sDEVICE=ps2write -dLanguageLevel=3" }
        };

        private bool isAscending = true;
        private int lastInsertionIndex = -1;
        private int lastSortColumn = -1;
        private SortOrder lastSortOrder = SortOrder.Ascending;
        private bool isConversionRunning;

        public PDFtoPS()
        {
            InitializeComponent();

            SetWindowTheme(listViewFiles.Handle, "explorer", null);
            IntPtr hHeader = SendMessage(listViewFiles.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            SetWindowTheme(hHeader, "explorer", null);

            PropertyInfo dbProperty = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            dbProperty?.SetValue(listViewFiles, true, null);

            comboBoxProfiles.Items.Clear();
            comboBoxProfiles.Items.AddRange(profiles.Keys.ToArray());
            if (comboBoxProfiles.Items.Count > 0)
            {
                comboBoxProfiles.SelectedIndex = 0;
            }

            listViewFiles.AllowDrop = true;
            InitInterfaceComponents();
            comboBoxProfiles.SelectedIndexChanged += comboBoxProfiles_SelectedIndexChanged;
        }

        private string GetImageSettingsArgs()
        {
            List<string> args = new();

            if (cmbColorDPI.SelectedIndex > 0)
            {
                string dpiText = cmbColorDPI.SelectedItem.ToString().Split(' ')[0];
                string method = "/" + cmbDownsampleType.SelectedItem;
                args.Add("-dDownsampleColorImages=true");
                args.Add($"-dColorImageResolution={dpiText}");
                args.Add($"-dColorImageDownsampleType={method}");
                args.Add("-dDownsampleGrayImages=true");
                args.Add($"-dGrayImageResolution={dpiText}");
                args.Add($"-dGrayImageDownsampleType={method}");
            }

            if (cmbMonoDPI.SelectedIndex > 0)
            {
                string monoDpiText = cmbMonoDPI.SelectedItem.ToString();
                if (monoDpiText == "1200")
                {
                    args.Add("-dDownsampleMonoImages=false");
                }
                else
                {
                    args.Add("-dDownsampleMonoImages=true");
                    args.Add($"-dMonoImageResolution={monoDpiText}");
                    args.Add("-dMonoImageDownsampleType=/Bicubic");
                }
            }

            if (cmbJpegQuality.SelectedIndex == 0)
            {
                args.Add("-sColorImageFilter=/FlateEncode");
                args.Add("-sGrayImageFilter=/FlateEncode");
            }
            else
            {
                args.Add("-sColorImageFilter=/DCTEncode");
                args.Add("-sGrayImageFilter=/DCTEncode");
            }

            return string.Join(" ", args);
        }

        private string GetFontArgs() => cmbFonts.SelectedIndex == 0
            ? "-dEmbedAllFonts=true -dSubsetFonts=true"
            : "-dNoOutputFonts";

        private string GetColorArgs()
        {
            return cmbColorModel.SelectedIndex switch
            {
                1 => "-sProcessColorModel=DeviceCMYK -sColorConversionStrategy=CMYK -dOverrideICC=true",
                2 => "-sProcessColorModel=DeviceGray -sColorConversionStrategy=Gray",
                _ => "-sColorConversionStrategy=LeaveColorUnchanged"
            };
        }

        private string GetPageSizeArgs()
        {
            if (cmbPageSize.SelectedIndex == 0) return string.Empty;

            return cmbPageSize.SelectedIndex switch
            {
                1 => "-dDEVICEWIDTHpoints=1446 -dDEVICEHEIGHTpoints=1134 -dFIXEDMEDIA",
                2 => "-sPAPERSIZE=a4 -dFIXEDMEDIA",
                3 => "-sPAPERSIZE=a3 -dFIXEDMEDIA",
                _ => string.Empty
            };
        }

        private void InitInterfaceComponents()
        {
            cmbColorDPI.Items.Clear();
            cmbColorDPI.Items.AddRange(new object[] { "Без изменений", "300 (Print)", "150 (Office)", "72 (Screen)" });
            cmbColorDPI.SelectedIndex = 1;

            cmbDownsampleType.Items.Clear();
            cmbDownsampleType.Items.AddRange(new object[] { "Bicubic", "Average", "Subsample" });
            cmbDownsampleType.SelectedIndex = 0;

            cmbMonoDPI.Items.Clear();
            cmbMonoDPI.Items.AddRange(new object[] { "Без изменений", "1200", "600", "300" });
            cmbMonoDPI.SelectedIndex = 1;

            cmbJpegQuality.Items.Clear();
            cmbJpegQuality.Items.AddRange(new object[] { "Maximum", "High", "Medium", "Low" });
            cmbJpegQuality.SelectedIndex = 0;

            cmbFonts.Items.Clear();
            cmbFonts.Items.AddRange(new object[] { "Встраивать шрифты (Standard)", "Преобразовать в кривые (Safe Mode)" });
            cmbFonts.SelectedIndex = 1;

            cmbColorModel.Items.Clear();
            cmbColorModel.Items.AddRange(new object[] { "Без изменений", "Преобразовать в CMYK (Print)", "Преобразовать в Gray" });
            cmbColorModel.SelectedIndex = 1;

            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(new object[]
            {
                "Как в документе (From PDF)",
                "CTP Plate (510 x 400 mm)",
                "A4 (210 x 297 mm)",
                "A3 (297 x 420 mm)"
            });
            cmbPageSize.SelectedIndex = 0;
        }

        private void comboBoxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedProfile = comboBoxProfiles.SelectedItem?.ToString() ?? string.Empty;
            cmbColorModel.SelectedIndex = selectedProfile.Contains("Grayscale") ? 2 : 1;

            if (selectedProfile.Contains("Standard") || selectedProfile.Contains("Level 3"))
            {
                cmbColorDPI.SelectedIndex = 1;
                cmbDownsampleType.SelectedIndex = 0;
                cmbMonoDPI.SelectedIndex = 1;
            }
            else if (selectedProfile.Contains("Legacy") || selectedProfile.Contains("Level 2"))
            {
                cmbColorDPI.SelectedIndex = 2;
                cmbDownsampleType.SelectedIndex = 1;
                cmbMonoDPI.SelectedIndex = 2;
            }
            else if (selectedProfile.Contains("Grayscale"))
            {
                cmbColorDPI.SelectedIndex = 1;
                cmbDownsampleType.SelectedIndex = 0;
                cmbMonoDPI.SelectedIndex = 1;
            }
        }

        private async void btnConvert_Click(object sender, EventArgs e)
        {
            if (isConversionRunning || listViewFiles.Items.Count == 0)
            {
                return;
            }

            string gsPath = FindGhostscriptPath();
            if (string.IsNullOrWhiteSpace(gsPath))
            {
                MessageBox.Show("Ghostscript не найден. Установите его или добавьте gswin64c.exe в PATH.");
                return;
            }

            string outputDir = txtOutputPath.Text;
            if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Выберите существующую папку вывода.");
                return;
            }

            var files = listViewFiles.Items.Cast<ListViewItem>()
                .Select(x => x.Tag?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();

            if (files.Count == 0)
            {
                return;
            }

            isConversionRunning = true;
            ToggleUiEnabled(false);

            progressBar1.Minimum = 0;
            progressBar1.Maximum = files.Count;
            progressBar1.Value = 0;
            progressBar1.Visible = true;

            string pdfSettings = GetPdfSettings();
            string colorArgs = GetColorArgs();
            string imageSettingsArgs = GetImageSettingsArgs();
            string sizeArgs = GetPageSizeArgs();
            string pdfFontArgs = "-dEmbedAllFonts=true -dSubsetFonts=true";
            string psFontArgs = GetFontArgs();
            string profileArgs = profiles[comboBoxProfiles.SelectedItem.ToString()];

            int completedCount = 0;
            int successCount = 0;
            int maxParallel = Math.Max(1, Math.Min(Environment.ProcessorCount / 2, 4));

            List<string> errors = new();
            object errorsLock = new();
            string originalTitle = Text;

            using var throttler = new SemaphoreSlim(maxParallel);

            try
            {
                var tasks = files.Select(async inputPath =>
                {
                    await throttler.WaitAsync();
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(inputPath);
                        UpdateWindowTitle($"Конвертация: {fileName}...");

                        bool converted = await ConvertSingleFileAsync(
                            gsPath,
                            inputPath,
                            outputDir,
                            pdfSettings,
                            colorArgs,
                            imageSettingsArgs,
                            sizeArgs,
                            pdfFontArgs,
                            psFontArgs,
                            profileArgs);

                        if (converted)
                        {
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            lock (errorsLock)
                            {
                                errors.Add($"{Path.GetFileName(inputPath)}: ошибка Ghostscript.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (errorsLock)
                        {
                            errors.Add($"{Path.GetFileName(inputPath)}: {ex.Message}");
                        }
                    }
                    finally
                    {
                        int done = Interlocked.Increment(ref completedCount);
                        BeginInvoke(new Action(() => progressBar1.Value = Math.Min(done, progressBar1.Maximum)));
                        throttler.Release();
                    }
                }).ToArray();

                await Task.WhenAll(tasks);
            }
            finally
            {
                Text = originalTitle;
                progressBar1.Value = 0;
                ToggleUiEnabled(true);
                isConversionRunning = false;
            }

            StringBuilder summary = new();
            summary.AppendLine($"Готово. Успешно: {successCount} из {files.Count}.");
            if (errors.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("Ошибки:");
                foreach (string error in errors.Take(10))
                {
                    summary.AppendLine($"- {error}");
                }

                if (errors.Count > 10)
                {
                    summary.AppendLine($"... и еще {errors.Count - 10}.");
                }
            }

            MessageBox.Show(summary.ToString());
            if (successCount > 0)
            {
                Process.Start("explorer.exe", outputDir);
            }
        }

        private async Task<bool> ConvertSingleFileAsync(
            string gsPath,
            string inputPath,
            string outputDir,
            string pdfSettings,
            string colorArgs,
            string imageSettingsArgs,
            string sizeArgs,
            string pdfFontArgs,
            string psFontArgs,
            string profileArgs)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string tempWorkDir = Path.Combine(Path.GetTempPath(), "PDFtoPS_Work", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempWorkDir);

            string safeInput = Path.Combine(tempWorkDir, "input.pdf");
            string safeNorm = Path.Combine(tempWorkDir, "norm.pdf");
            string finalPsPath = Path.Combine(outputDir, fileName + ".ps");

            try
            {
                File.Copy(inputPath, safeInput, true);

                string pass1Args = $"-dNOPAUSE -dBATCH -sDEVICE=pdfwrite -dCompatibilityLevel=1.4 " +
                                   $"{pdfSettings} {colorArgs} {imageSettingsArgs} {pdfFontArgs} " +
                                   $"-sOutputFile=\"{safeNorm}\" \"{safeInput}\"";

                if (!await RunGhostscriptAsync(gsPath, pass1Args))
                {
                    return false;
                }

                string pass2Args = $"-dNOPAUSE -dBATCH {profileArgs} -r2400 {sizeArgs} {psFontArgs} " +
                                   $"-sOutputFile=\"{finalPsPath}\" \"{safeNorm}\"";

                return await RunGhostscriptAsync(gsPath, pass2Args);
            }
            finally
            {
                try { Directory.Delete(tempWorkDir, true); } catch { }
            }
        }

        private string GetPdfSettings()
        {
            return cmbColorDPI.SelectedIndex switch
            {
                0 => "-dPDFSETTINGS=/prepress",
                1 => "-dPDFSETTINGS=/printer",
                2 => "-dPDFSETTINGS=/ebook",
                3 => "-dPDFSETTINGS=/screen",
                _ => "-dPDFSETTINGS=/printer"
            };
        }

        private async Task<bool> RunGhostscriptAsync(string gsPath, string arguments)
        {
            ProcessStartInfo psi = new()
            {
                FileName = gsPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using Process process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            string stdOut = await process.StandardOutput.ReadToEndAsync();
            string stdErr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Debug.WriteLine($"Ghostscript error ({process.ExitCode}).");
                Debug.WriteLine($"Command: \"{gsPath}\" {arguments}");
                Debug.WriteLine($"StdErr: {stdErr}");
                Debug.WriteLine($"StdOut: {stdOut}");
                return false;
            }

            return true;
        }

        private void ToggleUiEnabled(bool enabled)
        {
            btnConvert.Enabled = enabled;
            btnAdd.Enabled = enabled;
            btnSort.Enabled = enabled;
            btnRemove.Enabled = enabled;
            button5.Enabled = enabled;
            button6.Enabled = enabled;
            btnBrowse.Enabled = enabled;
            comboBoxProfiles.Enabled = enabled;
            listViewFiles.Enabled = enabled;
        }

        private void UpdateWindowTitle(string title)
        {
            if (!IsHandleCreated)
            {
                return;
            }

            BeginInvoke(new Action(() => Text = title));
        }

        private string FindGhostscriptPath()
        {
            string[] directCandidates =
            {
                @"C:\Program Files\gs\gs10.03.1\bin\gswin64c.exe",
                @"C:\Program Files\gs\gs10.03.0\bin\gswin64c.exe"
            };

            foreach (string candidate in directCandidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            string fromPath = ResolveFromPath("gswin64c.exe") ?? ResolveFromPath("gswin32c.exe");
            if (!string.IsNullOrWhiteSpace(fromPath))
            {
                return fromPath;
            }

            string gsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "gs");
            if (!Directory.Exists(gsRoot))
            {
                return null;
            }

            foreach (string versionDir in Directory.GetDirectories(gsRoot, "gs*").OrderByDescending(x => x))
            {
                string candidate = Path.Combine(versionDir, "bin", "gswin64c.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string ResolveFromPath(string executableName)
        {
            string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string pathPart in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(pathPart.Trim(), executableName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                {
                    // ignore malformed PATH segment
                }
            }

            return null;
        }

        private void SetInsertionMark(int index)
        {
            LVINSERTMARK insertMark = new()
            {
                cbSize = (uint)Marshal.SizeOf(typeof(LVINSERTMARK)),
                dwFlags = 0,
                iItem = index
            };

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(insertMark));
            Marshal.StructureToPtr(insertMark, ptr, false);
            SendMessage(listViewFiles.Handle, LVM_SETINSERTMARK, IntPtr.Zero, ptr);
            Marshal.FreeHGlobal(ptr);
        }

        private void UpdateRowNumbers()
        {
            for (int i = 0; i < listViewFiles.Items.Count; i++)
            {
                listViewFiles.Items[i].Text = (i + 1).ToString();
            }
        }

        private string GetFileSize(string path)
        {
            try
            {
                long bytes = new FileInfo(path).Length;
                return bytes >= 1048576
                    ? (bytes / 1048576.0).ToString("0.##") + " MB"
                    : (bytes / 1024.0).ToString("0.##") + " KB";
            }
            catch
            {
                return "0 KB";
            }
        }

        private void AddFileToListView(string path)
        {
            if (!string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            FileInfo fileInfo = new(path);
            ListViewItem item = new("");
            item.SubItems.Add(fileInfo.Name);
            item.SubItems.Add(GetFileSize(path));
            item.SubItems.Add(fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
            item.Tag = path;
            listViewFiles.Items.Add(item);
            UpdateRowNumbers();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new() { Multiselect = true, Filter = "PDF Files|*.pdf" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string f in ofd.FileNames)
                {
                    AddFileToListView(f);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0) return;

            int selectedIndex = listViewFiles.SelectedIndices[0];
            foreach (ListViewItem i in listViewFiles.SelectedItems.Cast<ListViewItem>().ToList())
            {
                i.Remove();
            }

            UpdateRowNumbers();
            if (listViewFiles.Items.Count > 0)
            {
                if (selectedIndex >= listViewFiles.Items.Count)
                {
                    selectedIndex = listViewFiles.Items.Count - 1;
                }

                listViewFiles.Items[selectedIndex].Selected = true;
                listViewFiles.Items[selectedIndex].Focused = true;
            }
        }

        private void listViewFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                e.Handled = true;
                btnRemove_Click(sender, e);
            }
        }

        private void btnSort_Click(object sender, EventArgs e)
        {
            var items = listViewFiles.Items.Cast<ListViewItem>().ToList();
            items = isAscending
                ? items.OrderBy(i => i.SubItems[1].Text).ToList()
                : items.OrderByDescending(i => i.SubItems[1].Text).ToList();

            isAscending = !isAscending;
            listViewFiles.Items.Clear();
            listViewFiles.Items.AddRange(items.ToArray());
            UpdateRowNumbers();
        }

        private void listViewFiles_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0) return;

            if (e.Column == lastSortColumn)
            {
                lastSortOrder = lastSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                lastSortOrder = SortOrder.Ascending;
                lastSortColumn = e.Column;
            }

            SetSortArrow(e.Column, lastSortOrder);

            var items = listViewFiles.Items.Cast<ListViewItem>().ToList();
            items = lastSortOrder == SortOrder.Ascending
                ? items.OrderBy(x => x.SubItems[e.Column].Text).ToList()
                : items.OrderByDescending(x => x.SubItems[e.Column].Text).ToList();

            listViewFiles.BeginUpdate();
            listViewFiles.Items.Clear();
            listViewFiles.Items.AddRange(items.ToArray());
            UpdateRowNumbers();
            listViewFiles.EndUpdate();
        }

        private void listViewFiles_ItemDrag(object sender, ItemDragEventArgs e)
            => listViewFiles.DoDragDrop(e.Item, DragDropEffects.Move);

        private void listViewFiles_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
            Point cp = listViewFiles.PointToClient(new Point(e.X, e.Y));
            ListViewItem hover = listViewFiles.GetItemAt(cp.X, cp.Y);

            if (hover != null)
            {
                if (hover.Index != lastInsertionIndex)
                {
                    lastInsertionIndex = hover.Index;
                    SetInsertionMark(hover.Index);
                }
            }
            else
            {
                SetInsertionMark(listViewFiles.Items.Count);
            }
        }

        private void listViewFiles_DragLeave(object sender, EventArgs e)
        {
            lastInsertionIndex = -1;
            SetInsertionMark(-1);
        }

        private void listViewFiles_DragDrop(object sender, DragEventArgs e)
        {
            lastInsertionIndex = -1;
            SetInsertionMark(-1);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    AddFileToListView(f);
                }

                return;
            }

            if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                Point cp = listViewFiles.PointToClient(new Point(e.X, e.Y));
                ListViewItem dragItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));
                ListViewItem dropItem = listViewFiles.GetItemAt(cp.X, cp.Y);

                if (dropItem != null)
                {
                    if (dragItem.Index != dropItem.Index)
                    {
                        listViewFiles.Items.Remove(dragItem);
                        listViewFiles.Items.Insert(dropItem.Index, dragItem);
                    }
                }
                else
                {
                    listViewFiles.Items.Remove(dragItem);
                    listViewFiles.Items.Add(dragItem);
                }

                UpdateRowNumbers();
            }
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0) return;

            ListViewItem item = listViewFiles.SelectedItems[0];
            int index = item.Index;
            if (index > 0)
            {
                listViewFiles.Items.RemoveAt(index);
                listViewFiles.Items.Insert(index - 1, item);
                UpdateRowNumbers();
            }
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0) return;

            ListViewItem item = listViewFiles.SelectedItems[0];
            int index = item.Index;
            if (index < listViewFiles.Items.Count - 1)
            {
                listViewFiles.Items.RemoveAt(index);
                listViewFiles.Items.Insert(index + 1, item);
                UpdateRowNumbers();
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folder = new();
            if (folder.ShowDialog() == DialogResult.OK)
            {
                txtOutputPath.Text = folder.SelectedPath;
            }
        }

        private void btnExit_Click(object sender, EventArgs e) => Application.Exit();

        private void SetSortArrow(int columnIndex, SortOrder order)
        {
            IntPtr header = SendMessage(listViewFiles.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            for (int i = 0; i < listViewFiles.Columns.Count; i++)
            {
                HDITEM item = new() { mask = HDI_FORMAT };
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(item));

                Marshal.StructureToPtr(item, ptr, false);
                SendMessage(header, HDM_GETITEM, (IntPtr)i, ptr);
                item = Marshal.PtrToStructure<HDITEM>(ptr);

                item.fmt &= ~(HDF_SORTUP | HDF_SORTDOWN);
                item.fmt |= HDF_STRING;

                if (i == columnIndex)
                {
                    item.fmt |= order == SortOrder.Ascending ? HDF_SORTUP : HDF_SORTDOWN;
                }

                Marshal.StructureToPtr(item, ptr, false);
                SendMessage(header, HDM_SETITEM, (IntPtr)i, ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
