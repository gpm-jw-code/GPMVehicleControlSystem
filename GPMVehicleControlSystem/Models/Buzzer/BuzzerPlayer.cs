using System;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Media;

namespace GPMVehicleControlSystem.Models.Buzzer
{
    public class BuzzerPlayer
    {
        private static string PlayListFileName = "playlist.json";
        public static clsPlayList playList { get; private set; } = new clsPlayList();
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

        internal static async Task BuzzerStop()
        {

            playCancelTS.Cancel();
            IsAlarmPlaying = IsActionPlaying = IsMovingPlaying = false;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                if (IsAlarmPlaying | IsActionPlaying | IsMovingPlaying)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = "pkill",
                        Arguments = "ffmpeg",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    Process killProcess = new Process
                    {
                        StartInfo = startInfo
                    };
                    killProcess.Start();
                    killProcess.WaitForExit();
                    Console.WriteLine("music process killed");
                }
            }
            else
            {
                player?.Stop();
            }

            await Task.Delay(100);
        }

        private static void Play(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LOG.ERROR($"Can't play {filePath}, File not exist");
                return;
            }
            try
            {
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

        private static void PlayInLinux(string filePath)
        {
            Task.Factory.StartNew(() =>
            {
                string ffmpegPath = "/usr/bin/ffmpeg";
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i {filePath} -f alsa default",
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                Process _ffmpegProcess = new Process
                {
                    StartInfo = startInfo
                };

                playIngPlayingProcesses = _ffmpegProcess;

                try
                {
                    _ffmpegProcess.Start();

                }
                catch (System.Exception ex)
                {
                    LOG.ERROR($"Couldn't play music by ffmepg...Error Message:{ex.Message}", ex);
                    return;
                }
                _ffmpegProcess.WaitForExit();
                Console.WriteLine("music process exit");
                if (!playCancelTS.IsCancellationRequested)
                    StartNewFFmpegProcess(_ffmpegProcess, startInfo);
            });

        }

        private static void StartNewFFmpegProcess(Process ffmpegProcess, ProcessStartInfo startInfo)
        {
            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("start new music process");
                Process _ffmpegProcess = new Process
                {
                    StartInfo = startInfo
                };
                playIngPlayingProcesses = _ffmpegProcess;
                _ffmpegProcess.Start();
                _ffmpegProcess.WaitForExit();
                if (!playCancelTS.IsCancellationRequested)
                    StartNewFFmpegProcess(_ffmpegProcess, startInfo);

            });

        }
    }
}
