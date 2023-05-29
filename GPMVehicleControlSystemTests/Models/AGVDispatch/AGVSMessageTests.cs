using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using static AGVSystemCommonNet6.clsEnums;
using AGVSystemCommonNet6;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Tests
{
    [TestClass()]
    public class AGVSMessageTests
    {
        [TestMethod()]
        public void OnlineQueryJsonTest()
        {
            clsOnlineModeQueryMessage online_query = new clsOnlineModeQueryMessage();
            online_query.Header = new Dictionary<string, OnlineModeQuery>
            {
                { "0101",new OnlineModeQuery
                    {
                        TimeStamp =  DateTime.Now.ToAGVSTimeFormat(),
                    }
                }
            };
            string json = JsonConvert.SerializeObject(online_query);

            clsOnlineModeQueryMessage de = JsonConvert.DeserializeObject<clsOnlineModeQueryMessage>(json);


            clsOnlineModeQueryResponseMessage response = new clsOnlineModeQueryResponseMessage();
            response.Header = new Dictionary<string, OnlineModeQueryResponse>()
            {
                {"0102", new OnlineModeQueryResponse
                {
                     RemoteMode = REMOTE_MODE.ONLINE,
                     TimeStamp = DateTime.Now.ToAGVSTimeFormat(),
                } }

            };
            clsOnlineModeQueryResponseMessage response_test = JsonConvert.DeserializeObject<clsOnlineModeQueryResponseMessage>(response.ToJson());
        }

        [TestMethod()]
        public void RunningStatusJsonTest()
        {
            clsRunningStatusReportMessage running_status_ = new clsRunningStatusReportMessage();
            running_status_.Header = new Dictionary<string, RunningStatus>
            {
                {"0105", new RunningStatus(){

                        Cargo_Status = 1,
                        AGV_Status =  MAIN_STATUS.DOWN
                } }
            };
            string json = running_status_.ToJson();
        }
    }
}
