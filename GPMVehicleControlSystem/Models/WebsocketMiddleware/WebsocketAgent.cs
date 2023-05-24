using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.ViewModels;
using Newtonsoft.Json;
using System.Diagnostics;
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

        public static void ClientRequest(HttpContext _HttpContext, WEBSOCKET_CLIENT_ACTION client_req)
        {
            Console.WriteLine("WS_CLient IN");
            var webSocket = _HttpContext.WebSockets.AcceptWebSocketAsync().Result;
            byte[] buffer = new byte[256];
            var bufferSegment = new ArraySegment<byte>(buffer);
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                try
                {
                    webSocket.ReceiveAsync(bufferSegment, CancellationToken.None);

                    //string s = Encoding.ASCII.GetString(buffer);
                    //s = s.Replace("\0", "");
                    //if (s == "alive")
                    //    sw.Restart();
                    //if (sw.ElapsedMilliseconds > 10000)
                    //    break;

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
            webSocket.Dispose();
            Console.WriteLine($"{webSocket.SubProtocol} CLIENT CLOSED");
        }
    }
}
