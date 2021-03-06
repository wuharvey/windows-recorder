﻿using ScreenRecorderLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

            [Option(Required = false)]
            public string Display_name { get; set; }

            [Option()]
            public bool Audio_disabled { get; set; }

            [Option(Required = false)]
            public string Audio_input_device { get; set; }

            [Option(Required = true)]
            public string Path { get; set; }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run);
        }

        private static RecorderOptions GetRecorderOptions(Options cmd_opts)
        {
            //This is how you can select audio devices. If you want the system default device,
            //just leave the AudioInputDevice or AudioOutputDevice properties unset or pass null or empty string.
            var audioInputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.InputDevices);
            var audioOutputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.OutputDevices);
            string selectedAudioInputDevice = cmd_opts.Audio_input_device ?? (audioInputDevices.Count > 0 ? audioInputDevices.First().Key : null);
            string selectedAudioOutputDevice = audioOutputDevices.Count > 0 ? audioOutputDevices.First().Key : null;

            // Default to primary display, but allow overriding using CLI opt
            string defaultDisplayName = System.Windows.Forms.Screen.PrimaryScreen.DeviceName;
            string selectedDisplayName = string.IsNullOrEmpty(cmd_opts.Display_name) ? defaultDisplayName : cmd_opts.Display_name;

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
                    MonitorDeviceName = selectedDisplayName
                }
            };

            return opts;

        }

        static void Run(Options cmd_opts)
        {
            Recorder rec = Recorder.CreateRecorder(GetRecorderOptions(cmd_opts));
            rec.OnRecordingFailed += Rec_OnRecordingFailed;
            rec.OnRecordingComplete += Rec_OnRecordingComplete;
            rec.OnStatusChanged += Rec_OnStatusChanged;
            Console.Write("Initialized");
            while (true)
            {
                string line = Console.ReadLine();
                string[] cmd = line.Split(':');
                Console.Write(line);

                if (cmd[0] == "set_options")
                {
                    rec.SetOptions(GetRecorderOptions(new Options
                    {
                        Top = Int32.Parse(cmd[1]),
                        Bottom = Int32.Parse(cmd[2]),
                        Left = Int32.Parse(cmd[3]),
                        Right = Int32.Parse(cmd[4]),
                        Audio_disabled = Boolean.Parse(cmd[5]),
                        Display_name = cmd[6] ?? "",
                        Audio_input_device = cmd[7] ?? ""
                    }));
                    rec.Record(cmd_opts.Path);
                    break;
                }
                else if (cmd[0] == "start")
                {
                    Console.Write(cmd);
                    Console.Write(cmd.Length);

                    rec.SetOptions(GetRecorderOptions(new Options
                    {
                        Audio_disabled = Boolean.Parse(cmd[1]),
                        Display_name = cmd[2] ?? "",
                        Audio_input_device = cmd[3] ?? ""
                    }));
                    rec.Record(cmd_opts.Path);
                    break;
                }
                else if (cmd[0] == "list_audio_devices")
                {
                    Rec_ListAudioInputDevices();
                }
            }

            while (true)
            {
                string info = Console.ReadLine();
                if (info == "stop")
                {
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
                Task.Delay(100);
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
                    _isRecording = true;
                    Console.WriteLine("Recording started");
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
            Console.WriteLine("Press any key to exit");
        }

        private static void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            Console.WriteLine("Recording failed with: " + e.Error);
            _isRecording = false;
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
        }

        private static void Rec_ListAudioInputDevices()
        {
            var audioInputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.InputDevices);
            var devicesStr = string.Join(",", audioInputDevices.Select(x => "\"" + x.Key + "\":\"" + x.Value + "\""));
            // var devicesStr = JsonConvert.SerializeObject(audioInputDevices);
            Console.WriteLine("list_audio_devices_start{" + devicesStr + "}list_audio_devices_end");
        }
    }
}
