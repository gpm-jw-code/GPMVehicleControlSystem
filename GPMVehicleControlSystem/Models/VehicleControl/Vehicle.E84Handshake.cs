using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System.Diagnostics;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {

        #region 交握

        /// <summary>
        /// 開始與EQ進行交握_等待EQ READY
        /// </summary>
        internal async Task<(bool eqready, AlarmCodes alarmCode)> WaitEQReadyON(ACTION_TYPE action)
        {
            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task wait_eq_UL_req_ON = new Task(() =>
            {
                while (action == ACTION_TYPE.Load ? !WagoDI.GetState(clsDIModule.DI_ITEM.EQ_L_REQ) : !WagoDI.GetState(clsDIModule.DI_ITEM.EQ_U_REQ))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_ready = new Task(() =>
            {
                while (!WagoDI.GetState(clsDIModule.DI_ITEM.EQ_READY))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });

            WagoDO.SetState(clsDOModule.DO_ITEM.AGV_VALID, true);

            if (Hs_Method == HS_METHOD.EMULATION)
            {
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_TR_REQ, true);
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_BUSY, true);
                return (true, AlarmCodes.None);
            }

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

            WagoDO.SetState(clsDOModule.DO_ITEM.AGV_TR_REQ, true);
            try
            {
                wait_eq_ready.Start();
                wait_eq_ready.Wait(waitEQSignalCST.Token);
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_BUSY, true);
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException ex)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_READY);

            }

        }

        /// <summary>
        /// 開始與EQ進行交握_等待EQ READY OFF
        /// </summary>
        internal async Task<(bool eqready_off, AlarmCodes alarmCode)> WaitEQReadyOFF(ACTION_TYPE action)
        {
            LOG.Critical("[EQ Handshake] 等待EQ READY OFF");
            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task wait_eq_UL_req_OFF = new Task(() =>
            {
                while (action == ACTION_TYPE.Load ? WagoDI.GetState(clsDIModule.DI_ITEM.EQ_L_REQ) : WagoDI.GetState(clsDIModule.DI_ITEM.EQ_U_REQ))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_ready_off = new Task(() =>
            {
                while (WagoDI.GetState(clsDIModule.DI_ITEM.EQ_READY))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });

            WagoDO.SetState(clsDOModule.DO_ITEM.AGV_BUSY, false);
            WagoDO.SetState(clsDOModule.DO_ITEM.AGV_COMPT, true);


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
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_COMPT, false);
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_TR_REQ, false);
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_VALID, false);

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
                while (!WagoDI.GetState(clsDIModule.DI_ITEM.EQ_BUSY))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_busy_OFF = new Task(() =>
            {
                while (Hs_Method != HS_METHOD.EMULATION ? WagoDI.GetState(clsDIModule.DI_ITEM.EQ_BUSY) : (action == ACTION_TYPE.Load ? HasAnyCargoOnAGV() : !HasAnyCargoOnAGV()))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });

            if (Hs_Method != HS_METHOD.EMULATION)
            {
                try
                {
                    wait_eq_busy_ON.Start();
                    wait_eq_busy_ON.Wait(waitEQSignalCST.Token);
                }
                catch (OperationCanceledException)
                {
                    return (false, AlarmCodes.Handshake_Fail_EQ_BUSY_ON);
                }
            }

            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 15 : 90));

            try
            {
                wait_eq_busy_OFF.Start();
                wait_eq_busy_OFF.Wait(waitEQSignalCST.Token);
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_AGV_READY, false); //AGV BUSY 開始退出
                WagoDO.SetState(clsDOModule.DO_ITEM.AGV_BUSY, true); //AGV BUSY 開始退出
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_BUSY_OFF);

            }
        }


        #endregion

    }
}
