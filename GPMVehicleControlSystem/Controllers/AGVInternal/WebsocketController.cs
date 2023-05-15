using GPMVehicleControlSystem.Models.WebsocketMiddleware;
using Microsoft.AspNetCore.Mvc;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebsocketController : ControllerBase
    {


        [HttpGet("/ws/AGVCState")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

                await WebsocketAgent.ClientRequest(HttpContext, WebsocketAgent.WEBSOCKET_CLIENT_ACTION.GETVMSStates);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        [HttpGet("/ws/ConnectionState")]
        public async Task ConnectionStateEcho()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                await WebsocketAgent.ClientRequest(HttpContext, WebsocketAgent.WEBSOCKET_CLIENT_ACTION.GETConnectionStates);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }


        [HttpGet("/ws/ModuleInformation")]
        public async Task ModuleInformation()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

                await WebsocketAgent.ClientRequest(HttpContext, WebsocketAgent.WEBSOCKET_CLIENT_ACTION.GETAGVCModuleInformation);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [HttpGet("/ws/DIOTableData")]
        public async Task DIOTableData()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

                await WebsocketAgent.ClientRequest(HttpContext, WebsocketAgent.WEBSOCKET_CLIENT_ACTION.GETDIOTable);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }


        [HttpGet("/ws/AGVS_MSG_IO")]
        public async Task AGVS_MSG_IO()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

                await WebsocketAgent.ClientRequest(HttpContext, WebsocketAgent.WEBSOCKET_CLIENT_ACTION.GETAGVSMSGIODATA);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
