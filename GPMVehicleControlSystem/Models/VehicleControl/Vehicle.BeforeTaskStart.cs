using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Tools;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System.Diagnostics;
using static AGVSystemCommonNet6.clsEnums;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {

        /// <summary>
        /// 開始移動任務前要做的事
        /// </summary>
        private async Task<(bool confirm, AlarmCodes alarm_code)> ExecuteActionBeforeMoving(clsTaskDownloadData taskDownloadData)
        {

            ACTION_TYPE action = taskDownloadData.Action_Type;

            if (action != ACTION_TYPE.None && action != ACTION_TYPE.Discharge && action != ACTION_TYPE.Escape)
                DirectionLighter.Forward();
            else if (action == ACTION_TYPE.Discharge | taskDownloadData.IsAfterLoadingAction)
                DirectionLighter.Backward();
            else
                DirectionLighter.Forward();
            //Laser模式變更

            if (action == ACTION_TYPE.Charge | action == ACTION_TYPE.Unload | action == ACTION_TYPE.Load | action == ACTION_TYPE.Discharge)
            {
                Laser.Mode = LASER_MODE.Loading;
            }
            else
            {
                Laser.Mode = LASER_MODE.Bypass;
                AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.SPEED_Reconvery);
            }


            if (action == ACTION_TYPE.Charge)
            {
                WagoDO.SetState(clsDOModule.DO_ITEM.Recharge_Circuit, true);
            }
            else if (action == ACTION_TYPE.Discharge)
            {
                WagoDO.SetState(clsDOModule.DO_ITEM.Recharge_Circuit, false);
            }
            else if (action == ACTION_TYPE.Load)
            {
                //TODO 在席檢查開關
                ////檢查在席全ON(車上應該要有貨)
                //if (!HasAnyCargoOnAGV())
                //{
                //    return (false, AlarmCodes.Has_Job_Without_Cst);
                //}


                bool Enable = AppSettingsHelper.GetValue<bool>("VCS:LOAD_OBS_DETECTION:Enable_Load");
                if (Enable)
                    StartFrontendObstcleDetection("ACTION_TYPE.Load");
            }
            else if (action == ACTION_TYPE.Unload)
            {
                //TODO 在席檢查開關
                ////檢查在席全ON(車上應該要沒貨)
                //if (HasAnyCargoOnAGV())
                //{
                //    return (false, AlarmCodes.Has_Cst_Without_Job);
                //}


                bool Enable = AppSettingsHelper.GetValue<bool>("VCS:LOAD_OBS_DETECTION:Enable_Unload");
                if (Enable)
                    StartFrontendObstcleDetection("ACTION_TYPE.Unload");
            }

            //EQ LDULD需要交握
            if (taskDownloadData.Station_Type == STATION_TYPE.EQ)
            {
                //交握
                (bool eqready, AlarmCodes alarmCode) eqReady_HS_Result = await WaitEQReadyON(action);
                if (!eqReady_HS_Result.eqready)
                {
                    return (false, eqReady_HS_Result.alarmCode);
                }
                else
                {
                    LOG.Critical("[EQ Handshake] EQ Ready,AGV 開始侵入");
                }
            }

            return (true, AlarmCodes.None);

        }

    }
}
