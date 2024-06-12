using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mp4_Converter
{
    public partial class ListViewConverter : Form
    {
        public ListViewConverter()
        {
            InitializeComponent();
        }

        List<string> videos = new List<string>();
        List<string> videosMp4 = new List<string>();
        string inputDirectory;
        private List<Button> buttonList = new List<Button>();

        private void ListViewConverter_Load(object sender, EventArgs e)
        {
            if (!IsFFmpegInstalled())
            {
                MessageBox.Show("FFmpeg is not installed.", "Alert!");
                Application.Exit();
            }

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            // Show the dialog and check if the user selects a folder
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                // Get the selected folder
                inputDirectory = folderBrowserDialog.SelectedPath;
            }

            if (inputDirectory == null)
            {
                MessageBox.Show("Invalid Directory!", "Alert!");
                Application.Exit();
            }

            timer1.Enabled = true;
        }

        private bool IsFFmpegInstalled()
        {
            try
            {
                Process ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = "ffmpeg";
                ffmpegProcess.StartInfo.Arguments = "-version";
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.Start();

                string output = ffmpegProcess.StandardOutput.ReadToEnd();
                ffmpegProcess.WaitForExit();

                if (output.Contains("ffmpeg version"))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var videosTemp = Directory.GetFiles(inputDirectory, "*.ts", SearchOption.AllDirectories)
                                    .ToList();
            var mp4Videos = Directory.GetFiles(inputDirectory, "*.mp4", SearchOption.AllDirectories)
                                    .ToList();
            videosMp4.Clear();
            mp4Videos.ForEach(video =>
            {
                FileInfo fileInfo = new FileInfo(video);
                if (fileInfo.Length != 0)
                {
                    videosMp4.Add(video);
                }
            });
            if (videos.Count() != videosTemp.Count())
            {
                videos = videosTemp;
                CreateButtons(videos);
            }
            
        }

        private void CreateButtons(List<string> videos)
        {
            string currentPath = Directory.GetCurrentDirectory();
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.FlowDirection = FlowDirection.LeftToRight;
            flowLayoutPanel1.WrapContents = true;
            //flowLayoutPanel1.Controls.Clear();
            for (int i = 0; i < videos.Count(); i++)
            {
                if(!buttonList.Any(x => x.Text == videos[i]))
                {
                    if (videosMp4.Any(x => x == videos[i].Replace(".ts", ".mp4")))
                    {
                        Button button = new Button();
                        button.Text = $"{videos[i]}";
                        button.Size = new Size(685, 60);
                        button.Click += Button_Click;
                        button.BackColor = Color.Lime;
                        buttonList.Add(button);
                        flowLayoutPanel1.Controls.Add(button);
                    }
                    else
                    {
                        Button button = new Button();
                        button.Text = $"{videos[i]}";
                        button.Size = new Size(685, 60);
                        button.Click += Button_Click;
                        button.BackColor = Color.White;
                        buttonList.Add(button);
                        flowLayoutPanel1.Controls.Add(button);
                    }
                }
            }
        }

        private async void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            if(clickedButton.BackColor == Color.White)
            {
                await Task.Run(async () =>
                {
                    await ExecuteCommandAndPrintOutputAsync(clickedButton.Text, clickedButton);
                });
            }
            else if (clickedButton.BackColor == Color.Lime)
            {
                PlayVideo(clickedButton.Text);
            }
            else
            {
                MessageBox.Show("Conversion in progress !!!" + Environment.NewLine + $"{clickedButton.Text}", "Alert");
            }

        }

        private void PlayVideo(string src)
        {
            var process = new Process();
            string file = src.Replace(".ts", ".mp4");

            if (File.Exists(file))
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(processInfo);
                    Console.WriteLine("Video is playing in the default media player.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        async Task ExecuteCommandAndPrintOutputAsync(string input, Button button)
        {
            var process = new Process();

            if (button.InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    button.BackColor = Color.Yellow;
                }));
            }
            else
            {
                button.BackColor = Color.Yellow;
            }

            string cmd = $"ffmpeg -i \"{input}\" -c:v h264_nvenc \"{input.Replace(".ts", ".mp4")}\"";
            // Configure the process using the StartInfo properties
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {cmd}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true; // Redirect the error output
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            var tcs = new TaskCompletionSource<bool>();

            // Subscribe to the OutputDataReceived event to asynchronously read the output
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                    
                }
            };

            // Subscribe to the ErrorDataReceived event to asynchronously read the error output
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                    
                }
            };

            process.Exited += (sender, e) =>
            {
                tcs.SetResult(true);
                if (button.InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        button.BackColor = Color.Lime;
                    }));
                }
                else
                {
                    button.BackColor = Color.Lime;
                }

                process.Dispose();
            };

            process.EnableRaisingEvents = true;

            // Start the process
            process.Start();

            // Begin asynchronously reading the output and error
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for the process to exit asynchronously
            await tcs.Task;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
