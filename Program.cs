
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace Recorder
{


    namespace RTSPRecorderService
    {
        public partial class RecorderService : ServiceBase
        {
            private Thread recordingThread;
            private bool isRunning = true;
            private const string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // Update the path if needed
            //private const string rtspUrl = "rtsp://admin:oWOSAN@192.168.1.10:554/stream1"; // Replace with actual RTSP URL
            //private const string rtspUrl = "rtsp://192.168.1.10:4747/video"; // Replace with actual RTSP URL
            private  string rtspUrl = ConfigurationManager.AppSettings["rtspUrl"]; // Replace with actual RTSP URL
            private  string outputDirectory = ConfigurationManager.AppSettings["outputDirectory"];
        
            public RecorderService()
            {
                //InitializeComponent();
            }

            protected override void OnStart(string[] args)
            {
                recordingThread = new Thread(new ThreadStart(StartRecording))
                {
                    IsBackground = true
                };
                recordingThread.Start();
            }

            public void StartRecording()
            {
                try
                {
                    while (isRunning)
                    {
                        if (!Directory.Exists(outputDirectory))
                            Directory.CreateDirectory(outputDirectory);

                        string dateStamp = DateTime.Now.ToString("dd-MMMM-yyyy");

                        string childDir = Path.Combine(outputDirectory, dateStamp);

                        if (!Directory.Exists(childDir))
                            Directory.CreateDirectory(childDir);

                        var files = Directory.EnumerateFiles(childDir, "*.*", SearchOption.AllDirectories).Where(file => Path.GetFileNameWithoutExtension(file).Equals(dateStamp, StringComparison.OrdinalIgnoreCase));
                        if (files.Any())
                        {
                            dateStamp = DateTime.Now.ToString("dd-MMMM-yyyy HH:mm:ss");
                        }


                        string outputFile = Path.Combine(childDir, $"{dateStamp}.mp4");


                        DateTime now = DateTime.Now;
                        // Get the end of the current day (midnight of the next day)
                        DateTime endOfDay = now.Date.AddDays(1);
                        // Calculate the remaining time
                        TimeSpan timeLeft = endOfDay - now;
                        // Get remaining seconds
                        int secondsLeft = 20;// (int)timeLeft.TotalSeconds;


                        Process ffmpegProcess = new Process();
                        ffmpegProcess.StartInfo.FileName = ffmpegPath;
                        ffmpegProcess.StartInfo.Arguments = $"-i \"{rtspUrl}\" -c copy -t {secondsLeft} \"{outputFile}\""; // 24-hour recording
                        //string ffmpegArguments = $"-rtsp_transport tcp -i \"{rtspUrl}\" -c:v libx264 -preset ultrafast -crf 18 -b:v 4M -bufsize 8M -c:a aac -b:a 128k -t 20 \"{outputFile}\"";

                        //string ffmpegArguments = $"-f dshow -i \"{rtspUrl}\" -c:v libx264 -preset ultrafast -crf 18 -r {secondsLeft} -t 10 \"" + outputFile + "\"";

                        //ffmpegProcess.StartInfo.Arguments = ffmpegArguments; //$"-i \"{rtspUrl}\" -c:v copy -t {secondsLeft} \"{outputFile}\""; // 24-hour recording
                        ffmpegProcess.StartInfo.UseShellExecute = false;
                        ffmpegProcess.StartInfo.CreateNoWindow = true;

                        ffmpegProcess.Start();
                        ffmpegProcess.WaitForExit();

                      

                    }
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("RTSPRecorderService", "Error: " + ex.Message, EventLogEntryType.Error);
                }
            }

            protected override void OnStop()
            {
                isRunning = false;
                recordingThread?.Abort();
            }



            public static void Main(string[] args)
            {
                if (Environment.UserInteractive)
                {
                    // Run as console app
                    RecorderService service = new RecorderService();
                    service.OnStart(null);
                    Console.WriteLine("Service running... Press any key to exit.");
                    Console.ReadKey();
                    service.OnStop();
                }
                else
                {
                    // Run as a Windows Service
                    ServiceBase.Run(new RecorderService());
                }
            }


        }
    }
}




