using GPMVehicleControlSystem.ViewModels;
using Newtonsoft.Json;
using System.Text;
using System.Threading;

namespace GPMVehicleControlSystem.Models.WebsocketMiddleware
{
    public class WebsocketAgent
    {
        public enum WEBSOCKET_CLIENT_ACTION
        {
            GETConnectionStates,
            GETVMSStates,
            GETAGVCModuleInformation,
            GETDIOTable,
            GETFORKTestState,
            GETAGVSMSGIODATA,
        }

        public async static Task ClientRequest(HttpContext _HttpContext, WEBSOCKET_CLIENT_ACTION client_req)
        {
            await Task.Delay(1);
            //#region AGVS Message Transfer Use

            //(DateTime time, string revStr, AGVS.AGVSSocketClient.MSG_DIRECTION direction) agvs_msg_io_data = new(DateTime.Now, "", AGVS.AGVSSocketClient.MSG_DIRECTION.OUT);
            //ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            //void AGVSHandler_BeforeMsgIOStringWriteToFile(object? sender, (DateTime time, string revStr, AGVS.AGVSSocketClient.MSG_DIRECTION direction) e)
            //{
            //    agvs_msg_io_data = e;
            //    manualResetEvent.Set();
            //}

            //if (client_req == WEBSOCKET_CLIENT_ACTION.GETAGVSMSGIODATA)
            //{
            //    AgvEntity.KGS_AGVSHandler.BeforeMsgIOStringWriteToFile += AGVSHandler_BeforeMsgIOStringWriteToFile;
            //}
            //#endregion
            await Task.Run(() =>
            {
                using var webSocket = _HttpContext.WebSockets.AcceptWebSocketAsync().Result;

                while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    try
                    {
                        webSocket.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);
                        object viewmodel = null;
                        switch (client_req)
                        {
                            case WEBSOCKET_CLIENT_ACTION.GETConnectionStates:
                                viewmodel = ViewModelFactory.GetConnectionStatesVM();

                                break;
                            case WEBSOCKET_CLIENT_ACTION.GETVMSStates:
                                viewmodel = ViewModelFactory.GetVMSStatesVM();
                                break;
                            case WEBSOCKET_CLIENT_ACTION.GETAGVCModuleInformation:
                                //viewmodel = AgvEntity.ModuleInformation;
                                break;
                            case WEBSOCKET_CLIENT_ACTION.GETDIOTable:
                                viewmodel = ViewModelFactory.GetDIOTableVM();
                                break;
                            case WEBSOCKET_CLIENT_ACTION.GETFORKTestState:
                                // viewmodel = ViewModelFactory.GetForkTestStateVM();
                                break;
                            case WEBSOCKET_CLIENT_ACTION.GETAGVSMSGIODATA:
                                // manualResetEvent.WaitOne();
                                //  manualResetEvent.Reset();
                                // viewmodel = new {Time=agvs_msg_io_data.time, Direction = agvs_msg_io_data.direction , Message = agvs_msg_io_data.revStr };
                                break;
                            default:
                                break;
                        }

                        if (viewmodel != null)
                            webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewmodel))), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        LOG.ERROR(ex.Message);
                        break;
                    }


                }
                //if (client_req == WEBSOCKET_CLIENT_ACTION.GETAGVSMSGIODATA)
                //    AgvEntity.KGS_AGVSHandler.BeforeMsgIOStringWriteToFile -= AGVSHandler_BeforeMsgIOStringWriteToFile;

                webSocket.Dispose();
                Console.WriteLine($"{webSocket.SubProtocol} CLIENT CLOSED");
            });
        }


    }
}
