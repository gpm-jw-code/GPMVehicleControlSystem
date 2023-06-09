﻿using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.TASK;
using GPMVehicleControlSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskDispatchController : ControllerBase
    {
        [HttpPost("Execute")]
        public async Task<IActionResult> Execute([FromBody] object taskDto)
        {
            clsTaskDownloadData? data = JsonConvert.DeserializeObject<clsTaskDownloadData>(taskDto.ToString());
            StaStored.CurrentVechicle.ExecuteAGVSTask(this, data);
            await Task.Delay(200);
            SimpleRequestResponse clsTaskDto = new SimpleRequestResponse()
            {
                ReturnCode = RETURN_CODE.OK
            };
            return Ok(clsTaskDto);
        }
    }
}
