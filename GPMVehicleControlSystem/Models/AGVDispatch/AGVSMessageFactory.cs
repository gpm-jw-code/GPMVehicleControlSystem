using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using System.Text;

namespace GPMVehicleControlSystem.Models.AGVDispatch
{
    public class AGVSMessageFactory
    {
        public static string SID { get; set; } = "001:001:001";
        public static string EQName { get; set; } = "AGV_1";

        internal delegate RunningStatus GetVCSRunningDataDelegate();
        internal static GetVCSRunningDataDelegate OnVCSRunningDataRequest;

        private static int SystemByteStored = 0;
        private static int System_Byte_Cyclic
        {
            get
            {
                SystemByteStored = SystemByteStored + 1;
                if (SystemByteStored >= int.MaxValue)
                    SystemByteStored = 1;
                return int.Parse(SystemByteStored.ToString());
            }
        }
        public static void Setup(string _SID, string _EQName)
        {
            SID = _SID;
            EQName = _EQName;
        }

        internal static byte[] CreateSimpleReturnMessageData(string headerKey, bool confirmed, int system_byte, out clsSimpleReturnWithTimestampMessage ackMesg)
        {
            ackMesg = new clsSimpleReturnWithTimestampMessage()
            {
                EQName = EQName,
                SID = SID,
                SystemBytes = system_byte,
                Header = new Dictionary<string, SimpleRequestResponseWithTimeStamp>()
                    {
                        {headerKey, new SimpleRequestResponseWithTimeStamp()
                        {
                             ReturnCode = (int)(confirmed? RETURN_CODE.OK: RETURN_CODE.NG),
                              TimeStamp = DateTime.Now.ToAGVSTimeFormat()
                        }
                        }
                    }
            };
            return Encoding.ASCII.GetBytes(FormatSendOutString(ackMesg.Json));

        }
        internal static byte[] CreateTaskDownloadReqAckData(bool accept_task, int system_byte, out clsSimpleReturnMessage ackMesg)
        {
            ackMesg = new clsSimpleReturnMessage()
            {
                EQName = EQName,
                SID = SID,
                SystemBytes = system_byte,
                Header = new Dictionary<string, SimpleRequestResponse>()
                    {
                        {"0302", new SimpleRequestResponse()
                        {
                             ReturnCode = (int)(accept_task? RETURN_CODE.OK: RETURN_CODE.NG),
                        }
                        }
                    }
            };
            return Encoding.ASCII.GetBytes(FormatSendOutString(ackMesg.Json));

        }

        //internal static byte[] CreateSimpleReqAckData(bool accept_task, int system_byte, out MessageBase ackMesg)
        //{
        //    ackMesg = new MessageBase()
        //    {
        //        EQName = EQName,
        //        SID = SID,
        //        SystemBytes = system_byte,
        //        Header = new Dictionary<string, MessageHeader>()
        //        {

        //        }
        //    }
        //}

        public static byte[] CreateOnlineModeQueryData(out clsOnlineModeQueryMessage mesg)
        {
            mesg = new clsOnlineModeQueryMessage();
            mesg.SID = SID;
            mesg.EQName = EQName;
            mesg.SystemBytes = System_Byte_Cyclic;
            mesg.Header.Add("0101", new OnlineModeQuery
            {
                TimeStamp = DateTime.Now.ToAGVSTimeFormat(),
            });

            return Encoding.ASCII.GetBytes(FormatSendOutString(mesg.Json));

        }

        public static byte[] CreateOnlineModeQueryData()
        {
            return CreateOnlineModeQueryData(out clsOnlineModeQueryMessage mesg);
        }

        internal static byte[] CreateOnlineModeChangeRequesData(int currentTag, REMOTE_MODE mode, out clsOnlineModeRequestMessage mesg)
        {
            mesg = new clsOnlineModeRequestMessage();
            mesg.SID = SID;
            mesg.EQName = EQName;
            mesg.SystemBytes = System_Byte_Cyclic;
            mesg.Header.Add("0103", new OnlineModeRequest
            {
                TimeStamp = DateTime.Now.ToAGVSTimeFormat(),
                CurrentNode = currentTag,
                ModeRequest = mode

            });

            return Encoding.ASCII.GetBytes(FormatSendOutString(mesg.Json));
        }

        internal static byte[] CreateRunningStateReportQueryData(out clsRunningStatusReportMessage msg)
        {
            RunningStatus? runningData = OnVCSRunningDataRequest?.Invoke();
            msg = new clsRunningStatusReportMessage();
            msg.SID = SID;
            msg.EQName = EQName;
            msg.SystemBytes = System_Byte_Cyclic;
            msg.Header.Add("0105", runningData);
            return Encoding.ASCII.GetBytes(FormatSendOutString(msg.Json));
        }

        public static string FormatSendOutString(string json)
        {
            return string.Format("{0}*{1}", json, "\r");
        }

        internal static byte[] CreateTaskFeedbackMessageData(clsTaskDownloadData taskData, int PointIndex, TASK_RUN_STATUS task_status, out clsTaskFeedbackMessage taskFeedbackMessage)
        {
            taskFeedbackMessage = new clsTaskFeedbackMessage()
            {
                SID = SID,
                EQName = EQName,
                SystemBytes = System_Byte_Cyclic,
                Header = new Dictionary<string, FeedbackData>
                   {
                       {"0303", new FeedbackData
                        {
                            TaskName = taskData.Task_Name,
                             TaskSimplex = taskData.Task_Simplex,
                              TaskSequence = taskData.Task_Sequence,
                               PointIndex = PointIndex,
                                TaskStatus = (int)task_status,
                                 TimeStamp = DateTime.Now.ToAGVSTimeFormat()
                        }
                    }
                   }
            };
            return Encoding.ASCII.GetBytes(FormatSendOutString(taskFeedbackMessage.Json));

        }
    }
}
