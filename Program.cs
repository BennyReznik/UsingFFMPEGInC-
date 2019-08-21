using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace UsingFFMPEGInCSharp
{
    class Program
    {
        // Download FFMPEG.exe from https://ffmpeg.org/ and update the file path
        private static readonly string ffmpegFileName = "ffmpeg.exe";
        private static readonly string emptyVideoName1 = "SampleVideo11.mp4";
        private static readonly string sampleFileName1 = "SampleVideo1.mp4";
        private static readonly string sampleFileName2 = "SampleVideo2.mp4";
        private static readonly string sampleFileName3 = "SampleVideo5.mp4";
        private static readonly string outputFileName = "output.mp4";

        private static string _pathToFfmpeg;
        private static string _workingDirectory;
        private const string FilesList = "videoFilesList.txt";

        static void Main(string[] args)
        {
            _workingDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().FullName).FullName;
            _pathToFfmpeg = Path.Combine(_workingDirectory, ffmpegFileName);

            CreateVideoWithBlackFrames("10");
            CreateVideoFileList();
            MergeMp4Files();
        }

        private static void CreateVideoWithBlackFrames(string videoDuration)
        {
            try
            {
                using (var process = new Process())
                {
                    var processModule = Process.GetCurrentProcess().MainModule;

                    if (processModule == null) return;

                    process.StartInfo.FileName = _pathToFfmpeg;

                    // Make sure the video you create use the same properties (codecs, frame rate, size) as the videos you want to merge with. 
                    // r=25 = frame rate per second
                    // -c:v libx264 = codec video H264
                    // -t {videoDuration} = blank video duration
                    process.StartInfo.Arguments =
                            "-y -f lavfi -i color=black:s=1280x720:r=25 " +
                            "-vf drawtext=\"fontfile=/path/to/font.ttf: " +
                            "\\text='Sample Empty Video': fontcolor=white: fontsize=72: box=1: " +
                            "boxcolor=black@1: \\boxborderw=10: x=(w-200)/2: y=(h-200)/2\" " +
                            $"-c:v libx264 -t {videoDuration} {emptyVideoName1}";

                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void MergeMp4Files()
        {
            try
            {
                var workingFolder = Directory.GetParent(Assembly.GetExecutingAssembly().FullName).FullName;
                var videoFilesPath = Path.Combine(workingFolder, FilesList);
                var outputFilePath = Path.Combine(workingFolder, outputFileName);
                using (var process = new Process())
                {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    // -y for overwriting the output file, -f filter, -safe 0 - for allowing to read absolute paths from the text file.
                    process.StartInfo.FileName = _pathToFfmpeg;
                    process.StartInfo.Arguments =
                        $"-f concat -safe 0 -i \"{videoFilesPath}\" -c copy \"{outputFilePath}\"";

                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void CreateVideoFileList()
        {
            try
            {
                var workingFolder = Directory.GetParent(Assembly.GetExecutingAssembly().FullName).FullName;
                var videoFiles = Directory.GetFiles(workingFolder, "*.mp4");
                using (var file = new StreamWriter(File.Create(Path.Combine(workingFolder, FilesList))))
                {
                    for (var i = 0; i < videoFiles.Length; i++)
                    {
                        if (i == videoFiles.Length - 1)
                        {
                            if (CheckIfFileIsCorrupted(videoFiles[i]))
                            {
                                file.WriteLine($"file '{videoFiles[i]}'");
                            }
                        }
                        else
                        {
                            file.WriteLine($"file '{videoFiles[i]}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private static bool CheckIfFileIsCorrupted(string filePath)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.FileName = _pathToFfmpeg;
                    process.StartInfo.Arguments = $"-v error -i {filePath} -f null -";

                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = false;
                    process.Start();
                    string stdout = process.StandardOutput.ReadToEnd();
                    string err = process.StandardError.ReadToEnd();
                    var s = process.ExitCode;
                    return err.Length > 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
