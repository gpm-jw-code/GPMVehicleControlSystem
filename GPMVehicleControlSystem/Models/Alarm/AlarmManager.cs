﻿using GPMVehicleControlSystem.Models.Database;
using GPMVehicleControlSystem.Tools;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Concurrent;

namespace GPMVehicleControlSystem.Models.Alarm
{
    public class AlarmManager
    {

        public static List<clsAlarmCode> AlarmList { get; private set; } = new List<clsAlarmCode>();
        public static ConcurrentDictionary<DateTime, clsAlarmCode> CurrentAlarms = new ConcurrentDictionary<DateTime, clsAlarmCode>();
        private static SQLiteConnection db;

        internal static event EventHandler OnAllAlarmClear;
        internal static void LoadAlarmList()
        {
            string alarm_JsonFile = AppSettingsHelper.GetValue<string>("VCS:AlarmList_json_Path");
            AlarmList = JsonConvert.DeserializeObject<List<clsAlarmCode>>(File.ReadAllText(alarm_JsonFile));
            LOG.INFO("Alarm List Loaded.");
        }
        public static void ClearAlarm(AlarmCodes Alarm_code)
        {
            var exist_al = CurrentAlarms.FirstOrDefault(i => i.Value.EAlarmCode == Alarm_code);
            if (exist_al.Value != null)
            {
                CurrentAlarms.TryRemove(exist_al);
            }

            if (CurrentAlarms.Count == 0)
            {
                OnAllAlarmClear?.Invoke("AlarmManager",EventArgs.Empty);
            }
        }


        public static void ClearAlarm()
        {
            var currentAlarmCodes = CurrentAlarms.Values.Select(alr => alr.EAlarmCode).ToList();
            foreach (var alarm_code in currentAlarmCodes)
            {
                ClearAlarm(alarm_code);
            }
        }

        public static void AddWarning(AlarmCodes Alarm_code, bool change_state = false)
        {
            clsAlarmCode warning = AlarmList.FirstOrDefault(a => a.EAlarmCode == Alarm_code);
            if (warning == null)
            {
                warning = new clsAlarmCode
                {
                    Code = (int)Alarm_code,
                    Description = Alarm_code.ToString(),
                    CN = Alarm_code.ToString(),
                };
            }

            clsAlarmCode warning_save = warning.Clone();
            warning_save.Time = DateTime.Now;
            warning_save.ELevel = clsAlarmCode.LEVEL.Warning;


            if (CurrentAlarms.TryAdd(warning_save.Time, warning_save))
            {
                DBhelper.InsertAlarm(warning_save);
            }
        }
        public static void AddAlarm(AlarmCodes Alarm_code, bool buzzer_alarm = true)
        {
            clsAlarmCode alarm = AlarmList.FirstOrDefault(a => a.EAlarmCode == Alarm_code);
            if (alarm == null)
            {
                alarm = new clsAlarmCode
                {
                    Code = (int)Alarm_code,
                    Description = Alarm_code.ToString(),
                    CN = Alarm_code.ToString(),
                };
            }
            clsAlarmCode alarm_save = alarm.Clone();
            alarm_save.Time = DateTime.Now;
            alarm_save.ELevel = clsAlarmCode.LEVEL.Alarm;

            if (CurrentAlarms.TryAdd(alarm_save.Time, alarm_save))
            {
                DBhelper.InsertAlarm(alarm_save);
            }
        }

    }
}
