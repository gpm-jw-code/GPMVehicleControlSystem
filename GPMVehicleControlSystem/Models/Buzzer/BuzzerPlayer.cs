using System;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Media;
using AGVSystemCommonNet6.Log;
using AGVSystemCommonNet6.GPMRosMessageNet.Services;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;

namespace GPMVehicleControlSystem.Models.Buzzer
{
    public class BuzzerPlayer
    {
        public static clsPlayList playList { get; private set; } = new clsPlayList();
        static SoundPlayer player;
        static bool IsAlarmPlaying = false;
        static bool IsActionPlaying = false;
        static bool IsMovingPlaying = false;
        public static RosSocket rossocket;

        public static void Initialize()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                playList.sounds_folder = "/home/jinwei/param/sounds";
            }
        }


        public static async void BuzzerAlarm()
        {
            if (IsAlarmPlaying)
                return;
            await BuzzerStop();
            IsAlarmPlaying = true;
            Play(playList.Alarm, 5);
        }
        public static async void BuzzerAction()
        {
            if (IsActionPlaying)
                return;
            await BuzzerStop();
            IsActionPlaying = true;
            Play(playList.Action, 25);
        }
        public static async void BuzzerMoving()
        {
            if (IsMovingPlaying)
                return;
            await BuzzerStop();
            IsMovingPlaying = true;
            Play(playList.Moving, 25);
        }
        private static bool _linux_music_stopped = false;
        internal static async Task BuzzerStop()
        {
            _linux_music_stopped = false;
            IsAlarmPlaying = IsActionPlaying = IsMovingPlaying = false;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                PlayWithRosService("");
            else
                player?.Stop();

            await Task.Delay(12);
        }

        private static async void Play(string filePath, float total_sec = 22)
        {
            if (!File.Exists(filePath) && !Debugger.IsAttached)
            {
                LOG.ERROR($"Can't play {filePath}, File not exist");
                return;
            }
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    PlayWithRosService(filePath, total_sec);
                }
                else
                    PlayInWindows(filePath);

            }
            catch (Exception ex)
            {
            }
        }
        public static void PlayWithRosService(string filePath, float total_sec = 22)
        {
            if (rossocket == null)
                return;
            PlayMusicResponse response = rossocket.CallServiceAndWait<PlayMusicRequest, PlayMusicResponse>("/play_music", new PlayMusicRequest
            {
                file_path = filePath,
                total_sec = total_sec
            });
            Console.WriteLine(response.success);
        }
        private static void PlayInWindows(string filePath)
        {
            // 初始化 SoundPlayer 物件
            player = new SoundPlayer(filePath);
            // 播放
            player.PlayLooping();
        }
    }
}
