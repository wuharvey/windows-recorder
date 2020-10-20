using ScreenRecorderLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommandLine;

namespace TestConsoleApp
{
    class Program
    {
        private static bool _isRecording;
        private static Stopwatch _stopWatch;

        public class Options
        {
            [Option(Required = false)]
            public int Top { get; set; }

            [Option(Required = false)]
            public int Bottom { get; set; }

            [Option(Required = false)]
            public int Left { get; set; }

            [Option(Required = false)]
            public int Right { get; set; }

            [Option()]
            public bool Audio_disabled { get; set; }

            [Option(Required = true)]
            public string Path { get; set; }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run);
        }


        static void Run(Options cmd_opts)
        {
            //This is how you can select audio devices. If you want the system default device,
            //just leave the AudioInputDevice or AudioOutputDevice properties unset or pass null or empty string.
            var audioInputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.InputDevices);
            var audioOutputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.OutputDevices);
            string selectedAudioInputDevice = audioInputDevices.Count > 0 ? audioInputDevices.First().Key : null;
            string selectedAudioOutputDevice = audioOutputDevices.Count > 0 ? audioOutputDevices.First().Key : null;

            var opts = new RecorderOptions
            {
                AudioOptions = new AudioOptions
                {
                    AudioInputDevice = selectedAudioInputDevice,
                    AudioOutputDevice = selectedAudioOutputDevice,
                    IsAudioEnabled = cmd_opts.Audio_disabled == false,
                    IsInputDeviceEnabled = true,
                    IsOutputDeviceEnabled = true,
                },
                DisplayOptions = new DisplayOptions
                {
                    Top = cmd_opts.Top,
                    Bottom = cmd_opts.Bottom,
                    Left = cmd_opts.Left,
                    Right = cmd_opts.Right,
                }
            };

            Recorder rec = Recorder.CreateRecorder(opts);
            rec.OnRecordingFailed += Rec_OnRecordingFailed;
            rec.OnRecordingComplete += Rec_OnRecordingComplete;
            rec.OnStatusChanged += Rec_OnStatusChanged;
            
            rec.Record(cmd_opts.Path);
            CancellationTokenSource cts = new CancellationTokenSource();
            var token = cts.Token;
            Task.Run(async () =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                        return;
                    if (_isRecording)
                    {
                        Dispatcher.CurrentDispatcher.Invoke(() =>
                        {
                            Console.Write(String.Format("\rElapsed: {0}s:{1}ms", _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds));
                        });
                    }
                    await Task.Delay(10);
                }
            }, token);
            while (true)
            {
                string info = Console.ReadLine();
                if (info == "stop")
                {
                    cts.Cancel();
                    rec.Stop();
                    break;
                }
                else if (info == "pause") 
                {
                    rec.Pause();
                }
                else if (info == "resume")
                {
                    rec.Resume();
                }
            }

            Console.ReadLine();
        }

        private static void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
        {
            switch (e.Status)
            {
                case RecorderStatus.Idle:
                    //Console.WriteLine("Recorder is idle");
                    break;
                case RecorderStatus.Recording:
                    _stopWatch = new Stopwatch();
                    _stopWatch.Start();
                    _isRecording = true;
                    Console.WriteLine("Recording started");
                    Console.WriteLine("Enter 'stop' to stop recording");
                    break;
                case RecorderStatus.Paused:
                    Console.WriteLine("Recording paused");
                    break;
                case RecorderStatus.Finishing:
                    Console.WriteLine("Finishing encoding");
                    break;
                default:
                    break;
            }
        }

        private static void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            Console.WriteLine("Recording completed");
            _isRecording = false;
            _stopWatch?.Stop();
            Console.WriteLine("Press any key to exit");
        }

        private static void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            Console.WriteLine("Recording failed with: " + e.Error);
            _isRecording = false;
            _stopWatch?.Stop();
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
        }
    }
}
