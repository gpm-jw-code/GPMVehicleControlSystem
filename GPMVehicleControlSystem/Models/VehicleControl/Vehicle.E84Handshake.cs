using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.Log;
using AGVSystemCommonNet6.MAP;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System.Diagnostics;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {

        private bool IsULReqOn(ACTION_TYPE action)
        {
            if (action == ACTION_TYPE.Load)
            {
                return EQ_HS_Method == EQ_HS_METHOD.EMULATION ? WagoDO.GetState(DO_ITEM.EMU_EQ_L_REQ) : WagoDI.GetState(clsDIModule.DI_ITEM.EQ_L_REQ);
            }
            else //unload
            {
                return EQ_HS_Method == EQ_HS_METHOD.EMULATION ? WagoDO.GetState(DO_ITEM.EMU_EQ_U_REQ) : WagoDI.GetState(clsDIModule.DI_ITEM.EQ_U_REQ);
            }
        }

        private bool IsEQReadyOn()
        {
            return EQ_HS_Method == EQ_HS_METHOD.EMULATION ? WagoDO.GetState(DO_ITEM.EMU_EQ_READY) : WagoDI.GetState(clsDIModule.DI_ITEM.EQ_READY);
        }

        private bool IsEQBusyOn()
        {
            return EQ_HS_Method == EQ_HS_METHOD.EMULATION ? WagoDO.GetState(DO_ITEM.EMU_EQ_BUSY) : WagoDI.GetState(clsDIModule.DI_ITEM.EQ_BUSY);
        }

        #region 交握


        /// <summary>
        /// 開始與EQ進行交握;
        /// - AGV VALID ON => 等待L/U_REQ ON => TR_REQ ON => 等待EQ READY ON
        /// </summary>
        internal async Task<(bool eqready, AlarmCodes alarmCode)> WaitEQReadyON(ACTION_TYPE action)
        {
            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task wait_eq_UL_req_ON = new Task(() =>
            {
                while (!IsULReqOn(action))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        throw new OperationCanceledException();
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_ready = new Task(() =>
            {
                while (!IsEQReadyOn())
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        throw new OperationCanceledException();
                    Thread.Sleep(1);
                }
            });

            WagoDO.SetState(DO_ITEM.AGV_VALID, true);
            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                wait_eq_UL_req_ON.Start();
                wait_eq_UL_req_ON.Wait(waitEQSignalCST.Token);
            }
            catch (OperationCanceledException ex)
            {
                return (false, action == ACTION_TYPE.Load ? AlarmCodes.Handshake_Fail_EQ_L_REQ : AlarmCodes.Handshake_Fail_EQ_U_REQ);
            }
            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            WagoDO.SetState(DO_ITEM.AGV_TR_REQ, true);
            try
            {
                wait_eq_ready.Start();
                wait_eq_ready.Wait(waitEQSignalCST.Token);
                WagoDO.SetState(DO_ITEM.AGV_BUSY, true);
                WatchE84AlarmWhenAGVBUSY();
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException ex)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_READY);
            }

        }
        private bool EQAlarmWhenEQBusyFlag = false;
        private bool AGVAlarmWhenEQBusyFlag = false;

        /// <summary>
        /// 結束EQ交握_等待EQ READY OFF
        /// </summary>
        internal async Task<(bool eqready_off, AlarmCodes alarmCode)> WaitEQReadyOFF(ACTION_TYPE action)
        {
            LOG.Critical("[EQ Handshake] 等待EQ READY OFF");
            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task wait_eq_UL_req_OFF = new Task(() =>
            {
                while (IsULReqOn(action))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_ready_off = new Task(() =>
            {
                while (IsEQReadyOn())
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });

            WagoDO.SetState(DO_ITEM.AGV_BUSY, false);
            WagoDO.SetState(DO_ITEM.AGV_COMPT, true);


            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                wait_eq_UL_req_OFF.Start();
                wait_eq_UL_req_OFF.Wait(waitEQSignalCST.Token);
            }
            catch (OperationCanceledException ex)
            {
                return (false, action == ACTION_TYPE.Load ? AlarmCodes.Handshake_Fail_EQ_L_REQ : AlarmCodes.Handshake_Fail_EQ_U_REQ);
            }
            try
            {
                wait_eq_ready_off.Start();
                wait_eq_ready_off.Wait(waitEQSignalCST.Token);
                WagoDO.SetState(DO_ITEM.AGV_COMPT, false);
                WagoDO.SetState(DO_ITEM.AGV_TR_REQ, false);
                WagoDO.SetState(DO_ITEM.AGV_VALID, false);

                LOG.Critical("[EQ Handshake] EQ READY OFF=>Handshake Done");
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException ex)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_READY);

            }

        }


        /// <summary>
        /// 等待EQ交握訊號 BUSY OFF＝＞表示ＡＧＶ可以退出了(模擬模式下:用CST在席有無表示是否BUSY結束 LOAD=>貨被拿走. Unload=>貨被放上來)
        /// </summary>
        internal async Task<(bool eq_busy_off, AlarmCodes alarmCode)> WaitEQBusyOFF(ACTION_TYPE action)
        {
            LOG.Critical("[EQ Handshake] 等待EQ BUSY OFF");
            DirectionLighter.Flash(new DO_ITEM[] { DO_ITEM.AGV_DiractionLight_Right, DO_ITEM.AGV_DiractionLight_Left }, 200);
            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            WagoDO.SetState(DO_ITEM.AGV_BUSY, false);
            WagoDO.SetState(DO_ITEM.AGV_AGV_READY, true);

            Task wait_eq_busy_ON = new Task(() =>
            {
                while (!IsEQBusyOn())
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        throw new OperationCanceledException();
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_busy_OFF = new Task(() =>
            {
                while (IsEQBusyOn())
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        throw new OperationCanceledException();
                    Thread.Sleep(1);
                }
            });

            try
            {
                wait_eq_busy_ON.Start();
                wait_eq_busy_ON.Wait(waitEQSignalCST.Token);
            }
            catch (OperationCanceledException)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_BUSY_ON);
            }
            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 15 : 90));
            WatchE84EQAlarmWhenEQBUSY(waitEQSignalCST);
            try
            {
                wait_eq_busy_OFF.Start();
                wait_eq_busy_OFF.Wait(waitEQSignalCST.Token);
                WagoDO.SetState(DO_ITEM.AGV_AGV_READY, false); //AGV BUSY 開始退出
                WagoDO.SetState(DO_ITEM.AGV_BUSY, true); //AGV BUSY 開始退出
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException ex)
            {
                if (!EQAlarmWhenEQBusyFlag)
                    return (false, AlarmCodes.Handshake_Fail_EQ_BUSY_OFF);
                else
                    return (false, AlarmCodes.Handshake_Fail);
            }
        }

        #endregion

        private async Task WatchE84AlarmWhenAGVBUSY()
        {
            await Task.Delay(1);
            _ = Task.Factory.StartNew(async () =>
            {
                while (WagoDO.GetState(DO_ITEM.AGV_BUSY))
                {
                    await Task.Delay(1);
                    if (Sub_Status == AGVSystemCommonNet6.clsEnums.SUB_STATUS.DOWN | Sub_Status == AGVSystemCommonNet6.clsEnums.SUB_STATUS.ALARM)
                    {
                        WagoDO.SetState(DO_ITEM.AGV_TR_REQ, false);
                        AlarmManager.AddAlarm(AlarmCodes.Handshake_Fail_AGV_DOWN, false);
                        throw new Exception("AGV Abnormal When AGV BUSY");
                    }
                    if (!IsEQReadyOn())//異常發生
                    {
                        AGVC.AbortTask(RESET_MODE.ABORT);
                        Sub_Status = AGVSystemCommonNet6.clsEnums.SUB_STATUS.DOWN;
                        AlarmManager.AddAlarm(AlarmCodes.Handshake_Fail_EQ_READY, false);
                        await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
                        throw new Exception("EQ Abnormal When AGV BUSY");
                    }
                }
            });
        }
        private async Task WatchE84EQAlarmWhenEQBUSY(CancellationTokenSource waitEQSignalCST)
        {
            AGVAlarmWhenEQBusyFlag = false;
            EQAlarmWhenEQBusyFlag = false;
            await Task.Delay(1);
            _ = Task.Factory.StartNew(async () =>
            {
                while (IsEQBusyOn())
                {
                    await Task.Delay(1);
                    if (Sub_Status == AGVSystemCommonNet6.clsEnums.SUB_STATUS.DOWN | Sub_Status == AGVSystemCommonNet6.clsEnums.SUB_STATUS.ALARM)
                    {
                        AGVAlarmWhenEQBusyFlag = true;
                        WagoDO.SetState(DO_ITEM.AGV_TR_REQ, false);
                        AlarmManager.AddAlarm(AlarmCodes.Handshake_Fail_AGV_DOWN, false);

                        while (IsEQBusyOn())
                        {
                            await Task.Delay(1);
                        }
                        WagoDO.SetState(DO_ITEM.AGV_AGV_READY, false);
                        WagoDO.SetState(DO_ITEM.AGV_VALID, false);
                        break;
                    }
                    if (!IsEQReadyOn() && Sub_Status == AGVSystemCommonNet6.clsEnums.SUB_STATUS.RUN)//異常發生
                    {
                        EQAlarmWhenEQBusyFlag = true;
                        waitEQSignalCST.Cancel();
                        AGVC.AbortTask(RESET_MODE.ABORT);
                        Sub_Status = AGVSystemCommonNet6.clsEnums.SUB_STATUS.DOWN;
                        AlarmManager.AddAlarm(AlarmCodes.Handshake_Fail_EQ_READY, false);
                        await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
                        while (IsEQBusyOn())
                        {
                            await Task.Delay(1);
                        }
                        WagoDO.SetState(DO_ITEM.AGV_AGV_READY, false);
                        WagoDO.SetState(DO_ITEM.AGV_TR_REQ, false);
                        WagoDO.SetState(DO_ITEM.AGV_VALID, false);
                    }

                }
            });
        }
    }
}
