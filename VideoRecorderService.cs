//using System;
//using System.Diagnostics;
//using System.ServiceProcess;
//using System.Timers;
//using AForge.Video;
//using Accord.Video.FFMPEG;
//using System.Drawing;
//using AForge.Video.DirectShow;
//using Recorder.RTSPRecorderService;
//namespace Recorder
//{
//    public class VideoRecorderService : ServiceBase
//    {
//        private VideoCaptureDevice videoSource;
//        private VideoFileWriter writer;
//        private Timer timer;
//        // private string rtspUrl = "rtsp://admin:password@192.168.1.100:554/stream"; // Change this
//        private const string rtspUrl = "rtsp://192.168.1.10:8080/h264_pcm.sdp"; // Replace with actual RTSP URL

//        private string outputFolder = @"C:\Recordings\";
//        private int frameRate = 30;

//        public VideoRecorderService()
//        {
//            this.ServiceName = "CCTVVideoRecorderService";
//        }

//        protected override void OnStart(string[] args)
//        {
//            //EventLog.WriteEntry("CCTV Video Recorder Service Started");

//            if (!System.IO.Directory.Exists(outputFolder))
//            {
//                System.IO.Directory.CreateDirectory(outputFolder);
//            }

//            StartRecording();

//            // Timer to restart recording every hour (to prevent huge files)
//            timer = new Timer(20000); // 1 hour
//            timer.Elapsed += (sender, e) => RestartRecording();
//            timer.Start();
//        }

//        private void StartRecording()
//        {
//            try
//            {
//                string outputFile = $"{outputFolder}record_{DateTime.Now:yyyyMMdd_HHmmss}.avi";
//                videoSource = new VideoCaptureDevice(rtspUrl);
//                writer = new VideoFileWriter();
//                writer.Open(outputFile, 1920, 1080, frameRate, VideoCodec.MPEG4, 4000000);

//                videoSource.NewFrame += (sender, eventArgs) =>
//                {
//                    writer.WriteVideoFrame(eventArgs.Frame);
//                };

//                videoSource.Start();
//                //EventLog.WriteEntry("Recording started: " + outputFile);
//            }
//            catch (Exception ex)
//            {
//                EventLog.WriteEntry("Error in StartRecording: " + ex.Message, EventLogEntryType.Error);
//            }
//        }

//        private void RestartRecording()
//        {
//            try
//            {
//                videoSource.SignalToStop();
//                writer.Close();
//                StartRecording();
//            }
//            catch (Exception ex)
//            {
//                EventLog.WriteEntry("Error in RestartRecording: " + ex.Message, EventLogEntryType.Error);
//            }
//        }

//        protected override void OnStop()
//        {
//            try
//            {
//                timer.Stop();
//                videoSource.SignalToStop();
//                writer.Close();
//                EventLog.WriteEntry("CCTV Video Recorder Service Stopped");
//            }
//            catch (Exception ex)
//            {
//                EventLog.WriteEntry("Error in OnStop: " + ex.Message, EventLogEntryType.Error);
//            }
//        }


//        public static void Main(string[] args)
//        {
//            if (Environment.UserInteractive)
//            {
//                // Run as console app
//                VideoRecorderService service = new VideoRecorderService();
//                service.OnStart(null);
//                Console.WriteLine("Service running... Press any key to exit.");
//                Console.ReadKey();
//                service.OnStop();
//            }
//            else
//            {
//                // Run as a Windows Service
//                ServiceBase.Run(new VideoRecorderService());
//            }
//        }
//    }

//}