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
        /// 移動任務結束後的處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskData"></param>
        private async void AGVMoveTaskActionSuccessHandle(object? sender, clsTaskDownloadData taskData)
        {
            if (AGV_Reset_Flag)
                return;

            Sub_Status = SUB_STATUS.IDLE;
            //AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.STOP_WHEN_REACH_GOAL);
            await Task.Delay(500);
            try
            {
                bool isActionFinish = Navigation.Data.lastVisitedNode.data == taskData.Destination;
                if (Main_Status != MAIN_STATUS.IDLE)
                {
                    throw new Exception("ACTION FINISH Feedback But AGV MAIN STATUS is not IDLE");
                }
                if (taskData.Action_Type != ACTION_TYPE.None)
                {
                    CurrentTaskRunStatus = TASK_RUN_STATUS.NAVIGATING;
                    if (taskData.IsTaskSegmented)
                    {
                        //侵入Port後

                        await FeedbackTaskStatus(CurrentTaskRunStatus);
                        var check_result_after_Task = await ExecuteWorksWhenReachPort(taskData);

                        if (!check_result_after_Task.confirm)
                        {
                            AlarmManager.AddAlarm(check_result_after_Task.alarm_code);
                            Sub_Status = SUB_STATUS.DOWN;
                            return;
                        }
                    }
                    else
                    {
                        /// 退出Port後
                        if (taskData.Station_Type == STATION_TYPE.EQ)
                        {
                            (bool eqready_off, AlarmCodes alarmCode) result = await WaitEQReadyOFF(taskData.Action_Type);
                            if (!result.eqready_off)
                            {
                                AlarmManager.AddAlarm(result.alarmCode);
                                Sub_Status = SUB_STATUS.DOWN;
                            }
                            else
                            {
                                LOG.Critical("[EQ Handshake] HADNSHAKE NORMAL Done,AGV Next TASK Will START");
                            }
                        }
                        await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);

                    }
                }
                else
                {
                    await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
                }

            }
            catch (Exception ex)
            {
                LOG.ERROR("AGVMoveTaskActionSuccessHandle", ex);
            }
            await Task.Delay(500);
            DirectionLighter.CloseAll();
        }

        /// <summary>
        /// 移動任務結束後
        /// </summary>
        /// <param name="taskDownloadData"></param>
        private async Task<(bool confirm, AlarmCodes alarm_code)> ExecuteWorksWhenReachPort(clsTaskDownloadData taskDownloadData)
        {
            ACTION_TYPE action = taskDownloadData.Action_Type;

            if (action == ACTION_TYPE.Load | action == ACTION_TYPE.Unload)
            {

                if (taskDownloadData.Station_Type == STATION_TYPE.EQ)
                {
                    //交握
                    var eqBusy_OFF_HS_Result = await WaitEQBusyOFF(action);
                    if (!eqBusy_OFF_HS_Result.eq_busy_off)
                    {
                        return (false, eqBusy_OFF_HS_Result.alarmCode);
                    }
                    else
                        LOG.Critical("[EQ Handshake] EQ BUSY OFF,AGV 開始退出EQ");
                    DirectionLighter.AbortFlash();
                }
                //TODO 在席檢查開關
                //if (action == ACTION_TYPE.Load)
                //{
                //    //檢查在席全ON(車上應該要沒貨)
                //    if (HasAnyCargoOnAGV())
                //    {
                //        return (false, AlarmCodes.Has_Cst_Without_Job);
                //    }
                //}

                //if (action == ACTION_TYPE.Unload)
                //{
                //    //檢查在席全ON(車上應該要沒貨)
                //    if (!HasAnyCargoOnAGV())
                //    {
                //        return (false, AlarmCodes.Has_Job_Without_Cst);
                //    }
                //}

                clsTaskDownloadData _AGVBackTaskDownloadData = taskDownloadData.TurnToBackTaskData();
                if (!await AGVC.AGVSTaskDownloadHandler(_AGVBackTaskDownloadData))
                {
                    return (false, AlarmCodes.Cant_TransferTask_TO_AGVC);

                }
            }
            return (true, AlarmCodes.None);

        }

    }
}
