using System;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Media;
using AGVSystemCommonNet6.Log;

namespace GPMVehicleControlSystem.Models.Buzzer
{
    public class BuzzerPlayer
    {
        private static string PlayListFileName = "playlist.json";
        public static clsPlayList playList { get; private set; } = new clsPlayList();
        public static List<Process> ProcList { get; private set; } = new List<Process>();

        static SoundPlayer player;
        static bool IsAlarmPlaying = false;
        static bool IsActionPlaying = false;
        static bool IsMovingPlaying = false;
        static Process playIngPlayingProcesses;

        static CancellationTokenSource playCancelTS = new CancellationTokenSource();

        public static void Initialize()
        {
            BuzzerAlarm();
        }


        public static async void BuzzerAlarm()
        {
            if (IsAlarmPlaying)
                return;
            await BuzzerStop();
            IsAlarmPlaying = true;
            Play(playList.Alarm);
        }
        public static async void BuzzerAction()
        {
            if (IsActionPlaying)
                return;
            await BuzzerStop();
            IsActionPlaying = true;
            Play(playList.Action);
        }
        public static async void BuzzerMoving()
        {
            if (IsMovingPlaying)
                return;
            await BuzzerStop();
            IsMovingPlaying = true;
            Play(playList.Moving);
        }
        private static bool _linux_music_stopped = false;
        internal static async Task BuzzerStop()
        {
            playCancelTS.Cancel();
            _linux_music_stopped = false;
            IsAlarmPlaying = IsActionPlaying = IsMovingPlaying = false;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                foreach (var item in ProcList)
                {
                    if (item != null)
                    {
                        try
                        {
                            item.Kill();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                ProcList.Clear();
            }
            else
            {
                player?.Stop();
            }
            await Task.Delay(12);
        }

        private static async void Play(string filePath)
        {

            if (!File.Exists(filePath))
            {
                LOG.ERROR($"Can't play {filePath}, File not exist");
                return;
            }
            try
            {
                playCancelTS = new CancellationTokenSource();
                CancellationTokenSource cst = new CancellationTokenSource(TimeSpan.FromSeconds(.4));
                while (!_linux_music_stopped)
                {
                    if (cst.IsCancellationRequested)
                        break;
                    await Task.Delay(1);
                }

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    PlayInLinux(filePath);
                }
                else
                    PlayInWindows(filePath);

            }
            catch (Exception ex)
            {
            }
        }

        private static void PlayInWindows(string filePath)
        {
            // 初始化 SoundPlayer 物件
            player = new SoundPlayer(filePath);
            // 播放
            player.PlayLooping();
        }
        static Process _ffmpegProcess;
        private static void PlayInLinux(string filePath)
        {
            Task.Run(() =>
             {
                 string ffmpegPath = "/usr/bin/ffmpeg";
                 ProcessStartInfo startInfo = new ProcessStartInfo()
                 {
                     FileName = ffmpegPath,
                     Arguments = $"-i {filePath} -f alsa default -loglevel quiet",
                     RedirectStandardOutput = false,
                     UseShellExecute = false,
                 };

                 Process _ffmpegProcess = new Process
                 {
                     StartInfo = startInfo
                 };

                 playIngPlayingProcesses = _ffmpegProcess;

                 try
                 {
                     ProcList.Add(_ffmpegProcess);
                     _ffmpegProcess.Start();

                 }
                 catch (System.Exception ex)
                 {
                     LOG.ERROR($"Couldn't play music by ffmepg...Error Message:{ex.Message}", ex);
                     return;
                 }
                 Task tk = _ffmpegProcess.WaitForExitAsync(playCancelTS.Token);
                 tk.ContinueWith(_tk =>
                 {
                     Console.WriteLine("music process exit");
                     if (!playCancelTS.IsCancellationRequested)
                         StartNewFFmpegProcess(_ffmpegProcess, startInfo);
                     else
                     {
                         BuzzerStop();
                         _linux_music_stopped = true;
                     }
                 });

             });

        }

        private static void StartNewFFmpegProcess(Process ffmpegProcess, ProcessStartInfo startInfo)
        {
            Task.Run(() =>
            {
                playCancelTS = new CancellationTokenSource();
                Console.WriteLine("start new music process");
                Process _ffmpegProcess = new Process
                {
                    StartInfo = startInfo
                };
                playIngPlayingProcesses = _ffmpegProcess;
                ProcList.Add(_ffmpegProcess);
                _ffmpegProcess.Start();
                Task tk = _ffmpegProcess.WaitForExitAsync(playCancelTS.Token);
                tk.ContinueWith(_tk =>
                {
                    Console.WriteLine("music process exit");
                    if (!playCancelTS.IsCancellationRequested)
                        StartNewFFmpegProcess(_ffmpegProcess, startInfo);
                    else
                    {
                        BuzzerStop();
                        _linux_music_stopped = true;
                    }
                });
            });

        }
    }
}
