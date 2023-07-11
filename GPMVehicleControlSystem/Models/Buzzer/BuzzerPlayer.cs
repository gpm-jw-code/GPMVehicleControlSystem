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
                playList.sounds_folder = Debugger.IsAttached ? "/home/jinwei/param/sounds" : "/home/gpm/param/sounds";
            }
            else if (Debugger.IsAttached)
            {
                playList.sounds_folder = "/home/gpm/param/sounds";

            }

            // Task.Run(() => MusicWatchDog());
        }

        private static async Task MusicWatchDog()
        {
            while (true)
            {
                await Task.Delay(1);
                var agv = StaStored.CurrentVechicle;

                if (agv.Sub_Status == AGVSystemCommonNet6.clsEnums.SUB_STATUS.RUN)
                {
                    if (agv.ExecutingTask.action == AGVSystemCommonNet6.AGVDispatch.Messages.ACTION_TYPE.None && !IsMovingPlaying)
                    {
                        BuzzerMoving();
                        LOG.WARN("Music Watch Dog Trigger, Buzzer Moving");
                    }
                    if (agv.ExecutingTask.action != AGVSystemCommonNet6.AGVDispatch.Messages.ACTION_TYPE.None && !IsActionPlaying)
                    {
                        BuzzerAction();
                        LOG.WARN("Music Watch Dog Trigger, Buzzer Action ");
                    }
                }
            }
        }

        public static async void BuzzerAlarm()
        {
            if (IsAlarmPlaying)
                return;
            Play(playList.Alarm);
            IsAlarmPlaying = true;
        }
        public static async void BuzzerAction()
        {
            if (IsActionPlaying)
                return;
            Play(playList.Action);
            IsActionPlaying = true;
        }
        public static async void BuzzerMoving()
        {
            if (IsMovingPlaying)
                return;
            Play(playList.Moving);
            IsMovingPlaying = true;
        }
        private static bool _linux_music_stopped = false;
        internal static async Task BuzzerStop()
        {
            _linux_music_stopped = false;
            if (Environment.OSVersion.Platform == PlatformID.Unix | Debugger.IsAttached)
                PlayWithRosService("stop");
            else
                player?.Stop();
            IsAlarmPlaying = IsActionPlaying = IsMovingPlaying = false;
        }

        private static async void Play(string filePath)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix | Debugger.IsAttached)
                {
                    PlayWithRosService(filePath);
                }
                else
                    PlayInWindows(filePath);
            }
            catch (Exception ex)
            {
            }
        }
        public static void PlayWithRosService(string filePath)
        {
            if (rossocket == null)
                return;

            Task.Factory.StartNew(() =>
            {
                PlayMusicResponse response = rossocket.CallServiceAndWait<PlayMusicRequest, PlayMusicResponse>("/play_music", new PlayMusicRequest
                {
                    file_path = filePath
                });
            });
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
