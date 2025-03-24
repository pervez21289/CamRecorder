using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Configuration;
using System.Threading.Tasks;

public partial class RecorderService : ServiceBase
{
    private Task recordingTask;
    private CancellationTokenSource cancellationTokenSource;
    private readonly string ffmpegPath;
    private readonly string rtspUrl;
    private readonly string outputDirectory;
    private const int retryDelaySeconds = 10;

    public RecorderService()
    {
        // Load configuration values
        rtspUrl = ConfigurationManager.AppSettings["rtspUrl"];
        outputDirectory = ConfigurationManager.AppSettings["outputDirectory"];
        ffmpegPath = ConfigurationManager.AppSettings["ffmpegPath"] ?? @"C:\ffmpeg\bin\ffmpeg.exe"; // Update if needed

        // Validate configuration
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(rtspUrl) || string.IsNullOrEmpty(outputDirectory))
        {
            throw new ConfigurationErrorsException("Configuration settings (rtspUrl or outputDirectory) are missing.");
        }

        if (!File.Exists(ffmpegPath))
        {
            throw new FileNotFoundException($"FFmpeg not found at: {ffmpegPath}");
        }
    }

    protected override void OnStart(string[] args)
    {
        try
        {
            cancellationTokenSource = new CancellationTokenSource();
            recordingTask = Task.Run(() => StartRecording(cancellationTokenSource.Token), cancellationTokenSource.Token);
            Logger.Log("Service started successfully.");
        }
        catch (Exception ex)
        {
            Logger.Log("Error on start: " + ex.Message);
            Stop();
        }
    }

    private void StartRecording(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string dateStamp = DateTime.Now.ToString("dd-MMMM-yyyy");
                string childDir = Path.Combine(outputDirectory, dateStamp);

                if (!Directory.Exists(childDir))
                    Directory.CreateDirectory(childDir); // Ensures directory exists

                // Generate unique filename if needed
                string outputFile = Path.Combine(childDir, $"{DateTime.Now:dd-MMMM-yyyy_HH-mm-ss}.mp4");

                int secondsLeft = 20; // Adjust recording duration as needed

                using (Process ffmpegProcess = new Process())
                {
                    ffmpegProcess.StartInfo.FileName = ffmpegPath;
                    ffmpegProcess.StartInfo.Arguments = $"-i \"{rtspUrl}\" -c copy -t {secondsLeft} \"{outputFile}\"";
                    ffmpegProcess.StartInfo.UseShellExecute = false;
                    ffmpegProcess.StartInfo.CreateNoWindow = true;
                    ffmpegProcess.Start();
                    ffmpegProcess.WaitForExit();

                    if (ffmpegProcess.ExitCode != 0)
                    {
                        Logger.Log("FFmpeg process exited with error. Restarting recording...");
                        Task.Delay(retryDelaySeconds * 1000, cancellationToken).Wait();
                        continue; // Restart the recording loop
                    }
                }

                Task.Delay(5000, cancellationToken).Wait(); // Wait before starting next recording
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Error in recording loop: " + ex.Message);
            Task.Delay(retryDelaySeconds * 1000, cancellationToken).Wait();
        }
    }

    protected override void OnStop()
    {
        try
        {
            cancellationTokenSource?.Cancel();
            recordingTask?.Wait();
            Logger.Log("Service stopped successfully.");
        }
        catch (Exception ex)
        {
            Logger.Log("Error on stop: " + ex.Message);
        }
    }

    public static void Main(string[] args)
    {
        if (Environment.UserInteractive)
        {
            // Run as console app for debugging
            RecorderService service = new RecorderService();
            service.OnStart(null);
            Console.WriteLine("Service running... Press any key to exit.");
            Console.ReadKey();
            service.OnStop();
        }
        else
        {
            // Run as Windows Service
            ServiceBase.Run(new RecorderService());
        }
    }
}
