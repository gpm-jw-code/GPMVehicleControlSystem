﻿using AGVSystemCommonNet6.GPMRosMessageNet.Services;
using AGVSystemCommonNet6.Log;

namespace GPMVehicleControlSystem.Models.VehicleControl.AGVControl
{
    public partial class CarController
    {

        private bool CSTActionDone = false;
        private string CSTActionResult = "";
        /// <summary>
        /// CST READER 完成動作的 callback
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool CSTReaderDoneActionHandle(CSTReaderCommandRequest request, out CSTReaderCommandResponse response)
        {
            CSTActionResult = request.command;

            response = new CSTReaderCommandResponse
            {
                confirm = true
            };
            CSTActionDone = true;
            return true;
        }

        /// <summary>
        /// 請求CST拍照
        /// </summary>
        /// <returns></returns>
        public async Task<(bool request_success, bool action_done)> TriggerCSTReader()
        {
            CSTReaderCommandResponse? response = rosSocket.CallServiceAndWait<CSTReaderCommandRequest, CSTReaderCommandResponse>("/CSTReader_action", new CSTReaderCommandRequest() { command = "read", model = "FORK" });

            if (response == null | !response.confirm)
            {
                LOG.TRACE("Trigger CST Reader fail. Confirm=False");
                return (false, false);
            }
            else
            {
                LOG.TRACE("Trigger CST Reader Success. Wait CST Reader Action Done.");
                CSTActionDone = false;
                CancellationTokenSource waitCstActionDoneCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

                Task TK = new Task(async () =>
                {
                    while (!CSTActionDone)
                    {
                        if (waitCstActionDoneCts.IsCancellationRequested)
                            break;
                        Thread.Sleep(1);
                    }

                });
                TK.Start();
                try
                {
                    TK.Wait(waitCstActionDoneCts.Token);
                    LOG.TRACE($"CST Reader  Action Done ..{CSTActionResult}--");
                    return (true, true);
                }
                catch (OperationCanceledException)
                {
                    LOG.WARN("Trigger CST Reader Timeout");
                    return (true, false);
                }

            }
        }
    }
}
