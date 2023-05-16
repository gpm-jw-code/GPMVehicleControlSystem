using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GPMVehicleControlSystem.Models.AGVDispatch
{
    public class clsAGVSConnection : Connection
    {
        TcpClient tcpClient;
        clsSocketState socketState = new clsSocketState();
        Dictionary<uint, ManualResetEvent> WaitAGVSReplyMREDictionary = new Dictionary<uint, ManualResetEvent>();
        Dictionary<uint, MessageBase> AGVSMessageStoreDictionary = new Dictionary<uint, MessageBase>();
        internal delegate bool taskDonwloadExecuteDelage(clsTaskDownloadData taskDownloadData);
        internal delegate bool onlineModeChangeDelelage(REMOTE_MODE mode);
        internal delegate bool taskResetReqDelegate(RESET_MODE reset_data);
        internal taskDonwloadExecuteDelage OnTaskDownload;
        internal onlineModeChangeDelelage OnRemoteModeChanged;
        internal taskResetReqDelegate OnTaskResetReq;
        public enum MESSAGE_TYPE
        {
            REQ_0101 = 0101,
            ACK_0102 = 0102,
            REQ_0103 = 0103,
            ACK_0104 = 0104,
            REQ_0105 = 0105,
            ACK_0106 = 0106,
            REQ_0301 = 0301,
            ACK_0302 = 0302,
            REQ_0303 = 0303,
            ACK_0304 = 0304,
            REQ_0305 = 0305,
            ACK_0306 = 0306,
            UNKNOWN = 9999,
        }

        public string LocalIP { get; }
        public clsAGVSConnection(string IP, int Port) : base(IP, Port)
        {
            this.IP = IP;
            this.Port = Port;
            LocalIP = null;
        }
        public clsAGVSConnection(string HostIP, int HostPort, string localIP)
        {
            this.IP = HostIP;
            this.Port = HostPort;
            this.LocalIP = localIP;
        }


        public override bool Connect()
        {
            try
            {
                if (LocalIP != null)
                {
                    IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(LocalIP), 0);
                    tcpClient = new TcpClient(ipEndpoint);
                    tcpClient.ReceiveBufferSize = 65535;
                    tcpClient.Connect(IP, Port);
                }
                else
                {
                    tcpClient = new TcpClient();
                    tcpClient.Connect(IP, Port);
                }
                socketState.stream = tcpClient.GetStream();
                socketState.Reset();
                socketState.stream.BeginRead(socketState.buffer, socketState.offset, clsSocketState.buffer_size - socketState.offset, ReceieveCallbaak, socketState);
                LOG.INFO($"[AGVS] Connect To AGVS Success !!");
                return true;
            }
            catch (Exception ex)
            {
                LOG.ERROR($"[AGVS] Connect Fail..{ex.Message}. Can't Connect To AGVS ({IP}:{Port})..Will Retry it ...");
                tcpClient = null;
                return false;
            }
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    Thread.Sleep(200);
                    if (!IsConnected())
                    {
                        Connect();
                        continue;
                    }
                    (bool, OnlineModeQueryResponse onlineModeQuAck) result = await TryOnlineModeQueryAsync();
                    if (!result.Item1)
                    {
                        Console.WriteLine("[AGVS] OnlineMode Query Fail...AGVS No Response");
                        continue;
                    }
                    (bool, SimpleRequestResponse runningStateReportAck) runningStateReport_result = await TryRnningStateReportAsync();
                    if (!runningStateReport_result.Item1)
                        Console.WriteLine("[AGVS] Running State Report Fail...AGVS No Response");

                }
            });
        }
        void ReceieveCallbaak(IAsyncResult ar)
        {
            clsSocketState _socketState = (clsSocketState)ar.AsyncState;
            int rev_len = _socketState.stream.EndRead(ar);

            string _revStr = Encoding.ASCII.GetString(_socketState.buffer, _socketState.offset, rev_len);
            _socketState.revStr += _revStr;
            _socketState.offset += rev_len;

            if (_revStr.EndsWith("*\r"))
            {
                string strHandle = _socketState.revStr.Replace("*\r", "$");
                string[] splited = strHandle.Split('$');//預防粘包，包含多個message包

                foreach (var str in splited)
                {
                    if (str == "" | str == null | str == "\r")
                        continue;
                    string _json = str.TrimEnd(new char[] { '*' });

                    if (_json.Contains("\"Header\": {\"03"))
                    {
                       LOG.WARN(_json);
                    }
                    HandleAGVSJsonMsg(_json);
                }
                _socketState.Reset();
                _socketState.waitSignal.Set();

            }
            else
            {

            }

            try
            {
                Task.Factory.StartNew(() => _socketState.stream.BeginRead(_socketState.buffer, _socketState.offset, clsSocketState.buffer_size - _socketState.offset, ReceieveCallbaak, _socketState));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        private REMOTE_MODE CurrentREMOTE_MODE_Downloaded = REMOTE_MODE.OFFLINE;
        private RETURN_CODE AGVOnlineReturnCode;
        private ManualResetEvent WaitAGVSAcceptOnline = new ManualResetEvent(false);
        internal Task CarrierRemovedRequestAsync(string v, string[] vs)
        {
            throw new NotImplementedException();
        }

        private void HandleAGVSJsonMsg(string _json)
        {
            try
            {
                var _Message = JsonConvert.DeserializeObject<Dictionary<string, object>>(_json);
                MESSAGE_TYPE msgType = GetMESSAGE_TYPE(_Message);
                MessageBase? MSG = null;
                if (msgType == MESSAGE_TYPE.ACK_0102)
                {
                    clsOnlineModeQueryResponseMessage? onlineModeQuAck = JsonConvert.DeserializeObject<clsOnlineModeQueryResponseMessage>(_json);
                    CurrentREMOTE_MODE_Downloaded = onlineModeQuAck.OnlineModeQueryResponse.RemoteMode;
                    OnRemoteModeChanged(CurrentREMOTE_MODE_Downloaded);
                    MSG = onlineModeQuAck;
                }
                else if (msgType == MESSAGE_TYPE.ACK_0104)  //AGV上線請求的回覆
                {
                    clsOnlineModeRequestResponseMessage? onlineModeRequestResponse = JsonConvert.DeserializeObject<clsOnlineModeRequestResponseMessage>(_json);
                    AGVOnlineReturnCode = onlineModeRequestResponse.ReturnCode;
                    WaitAGVSAcceptOnline.Set();
                    MSG = onlineModeRequestResponse;
                }
                else if (msgType == MESSAGE_TYPE.ACK_0106)  //Running State Report的回覆
                {
                    clsRunningStatusReportResponseMessage? runningStateReportAck = JsonConvert.DeserializeObject<clsRunningStatusReportResponseMessage>(_json);
                    MSG = runningStateReportAck;
                }
                else if (msgType == MESSAGE_TYPE.REQ_0301)  //TASK DOWNLOAD
                {
                    clsTaskDownloadMessage? taskDownloadReq = JsonConvert.DeserializeObject<clsTaskDownloadMessage>(_json);
                    MSG = taskDownloadReq;
                    bool accept_task = OnTaskDownload(taskDownloadReq.TaskDownload);
                    TryTaskDownloadReqAckAsync(accept_task, taskDownloadReq.SystemBytes);
                }
                else if (msgType == MESSAGE_TYPE.ACK_0304)  //TASK Feedback的回傳
                {
                    clsSimpleReturnMessage? taskFeedbackAck = JsonConvert.DeserializeObject<clsSimpleReturnMessage>(_json);
                    MSG = taskFeedbackAck;
                }
                else if (msgType == MESSAGE_TYPE.REQ_0305)
                {
                    clsTaskResetReqMessage? taskResetMsg = JsonConvert.DeserializeObject<clsTaskResetReqMessage>(_json);
                    MSG = taskResetMsg;
                    bool reset_accept = OnTaskResetReq(taskResetMsg.ResetData.ResetMode);
                    TryTaskResetReqAckAsync(reset_accept, taskResetMsg.SystemBytes);
                }

                AGVSMessageStoreDictionary.TryAdd(MSG.SystemBytes, MSG);

                if (WaitAGVSReplyMREDictionary.TryGetValue(MSG.SystemBytes, out ManualResetEvent mse))
                {
                    mse.Set();
                    WaitAGVSReplyMREDictionary.Remove(MSG.SystemBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleAGVSJsonMsg_Code Error" + ex.Message);
            }
        }

        public MESSAGE_TYPE GetMESSAGE_TYPE(Dictionary<string, object> message_obj)
        {
            string headerContent = message_obj["Header"].ToString();
            if (headerContent.Contains("0101"))
                return MESSAGE_TYPE.REQ_0101;
            if (headerContent.Contains("0102"))
                return MESSAGE_TYPE.ACK_0102;
            if (headerContent.Contains("0103"))
                return MESSAGE_TYPE.REQ_0103;
            if (headerContent.Contains("0104"))
                return MESSAGE_TYPE.ACK_0104;
            if (headerContent.Contains("0105"))
                return MESSAGE_TYPE.REQ_0105;
            if (headerContent.Contains("0106"))
                return MESSAGE_TYPE.ACK_0106;
            if (headerContent.Contains("0301"))
                return MESSAGE_TYPE.REQ_0301;
            if (headerContent.Contains("0302"))
                return MESSAGE_TYPE.ACK_0302;

            if (headerContent.Contains("0303"))
                return MESSAGE_TYPE.REQ_0303;

            if (headerContent.Contains("0304"))
                return MESSAGE_TYPE.ACK_0304;

            if (headerContent.Contains("0305"))
                return MESSAGE_TYPE.REQ_0305;

            if (headerContent.Contains("0306"))
                return MESSAGE_TYPE.ACK_0306;
            else
                return MESSAGE_TYPE.UNKNOWN;
        }
        public override void Disconnect()
        {
            tcpClient?.Dispose();
        }

        public override bool IsConnected()
        {
            return tcpClient != null && tcpClient.Connected;
        }


        private bool TryTaskDownloadReqAckAsync(bool accept_task, uint system_byte)
        {

            byte[] data = AGVSMessageFactory.CreateTaskDownloadReqAckData(accept_task, system_byte, out clsSimpleReturnMessage ackMsg);
            LOG.INFO($"TaskDownload Ack : {ackMsg.Json}");
            return WriteDataOut(data);
        }

        internal async Task TryTaskFeedBackAsync(clsTaskDownloadData taskData, int point_index, TASK_RUN_STATUS task_status)
        {
            await Task.Run(async () =>
            {
                byte[] data = AGVSMessageFactory.CreateTaskFeekbackMessageData(taskData, point_index, task_status, out clsTaskFeedbackMessage msg);
                bool success = await WriteDataOut(data, msg.SystemBytes);


                if (AGVSMessageStoreDictionary.TryGetValue(msg.SystemBytes, out MessageBase _retMsg))
                {
                    clsSimpleReturnMessage msg_return = (clsSimpleReturnMessage)_retMsg;
                    msg_return.HeaderKey = "0304";
                    Console.WriteLine($"[Task Feedback ] {taskData.Task_Name}_{taskData.Task_Simplex} :: AGVS Return Code = { msg_return.ReturnData.ReturnCode}");
                }
            });

        }


        private async Task<(bool, SimpleRequestResponse runningStateReportAck)> TryRnningStateReportAsync()
        {
            try
            {
                byte[] data = AGVSMessageFactory.CreateRunningStateReportQueryData(out clsRunningStatusReportMessage msg);
                await WriteDataOut(data, msg.SystemBytes);

                if (AGVSMessageStoreDictionary.TryGetValue(msg.SystemBytes, out MessageBase mesg))
                {
                    clsRunningStatusReportResponseMessage QueryResponseMessage = mesg as clsRunningStatusReportResponseMessage;
                    if (QueryResponseMessage != null)
                        return (true, QueryResponseMessage.RuningStateReportAck);
                    else
                        return (false, null);
                }
                else
                    return (false, null);
            }
            catch (Exception)
            {
                return (false, null);
            }
        }

        private void TryTaskResetReqAckAsync(bool reset_accept, uint system_byte)
        {
            byte[] data = AGVSMessageFactory.CreateSimpleReturnMessageData("0306", reset_accept, system_byte, out clsSimpleReturnMessage msg);
            Console.WriteLine(msg.Json);
            bool writeOutSuccess = WriteDataOut(data);
            Console.WriteLine("TryTaskResetReqAckAsync : " + writeOutSuccess);

        }
        internal async Task<(bool success, RETURN_CODE return_code)> TrySendOnlineModeChangeRequest(int currentTag, REMOTE_MODE mode)
        {
            Console.WriteLine($"[Online Mode Change] 車載請求 {mode} , Tag {currentTag}");
            if (CurrentREMOTE_MODE_Downloaded == mode)
            {
                return (true, RETURN_CODE.OK);
            }
            try
            {
                WaitAGVSAcceptOnline = new ManualResetEvent(false);
                byte[] data = AGVSMessageFactory.CreateOnlineModeChangeRequesData(currentTag, mode, out clsOnlineModeRequestMessage msg);
                await WriteDataOut(data, msg.SystemBytes);

                if (AGVSMessageStoreDictionary.TryGetValue(msg.SystemBytes, out MessageBase mesg))
                {
                    AGVSMessageStoreDictionary.Remove(msg.SystemBytes);
                    clsOnlineModeRequestResponseMessage QueryResponseMessage = mesg as clsOnlineModeRequestResponseMessage;
                    return (QueryResponseMessage.ReturnCode == RETURN_CODE.OK, QueryResponseMessage.ReturnCode);
                }
                else
                {

                    if (mode == REMOTE_MODE.ONLINE)
                    {
                        WaitAGVSAcceptOnline.WaitOne(1000);
                        bool success = CurrentREMOTE_MODE_Downloaded == REMOTE_MODE.ONLINE && AGVOnlineReturnCode == RETURN_CODE.OK;

                        return (success, AGVOnlineReturnCode);
                    }
                    else
                    {
                        bool success = CurrentREMOTE_MODE_Downloaded == REMOTE_MODE.OFFLINE;
                        return (success, success ? RETURN_CODE.OK : RETURN_CODE.NG);
                    }
                }


                //else
                //{
                //    Console.WriteLine("[AGVS] OnlineModeChangeRequest Fail...AGVS No Response");
                //    return (false, RETURN_CODE.No_Response);
                //}
            }
            catch (Exception ex)
            {
                LOG.WARN($"[AGVS] OnlineModeChangeRequest Fail...Code Error:{ex.Message}");
                return (false, RETURN_CODE.System_Error);
            }
        }


        public async Task<(bool, OnlineModeQueryResponse onlineModeQuAck)> TryOnlineModeQueryAsync()
        {
            try
            {
                byte[] data = AGVSMessageFactory.CreateOnlineModeQueryData(out clsOnlineModeQueryMessage msg);
                await WriteDataOut(data, msg.SystemBytes);

                if (AGVSMessageStoreDictionary.TryGetValue(msg.SystemBytes, out MessageBase mesg))
                {
                    clsOnlineModeQueryResponseMessage QueryResponseMessage = mesg as clsOnlineModeQueryResponseMessage;
                    return (true, QueryResponseMessage.OnlineModeQueryResponse);
                }
                else
                    return (false, null);
            }
            catch (Exception)
            {
                return (false, null);
            }
        }
        public bool WriteDataOut(byte[] dataByte)
        {
            try
            {
                socketState.stream.Write(dataByte, 0, dataByte.Length);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        private object lockObject = new object();

        public async Task<bool> WriteDataOut(byte[] dataByte, uint systemBytes)
        {
            Task _task = new Task(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                        socketState.stream.Write(dataByte, 0, dataByte.Length);
                        bool addsucess = WaitAGVSReplyMREDictionary.TryAdd(systemBytes, manualResetEvent);

                        if (addsucess)
                            manualResetEvent.WaitOne();
                        else
                        {
                            LOG.WARN($"[WriteDataOut] 將 'ManualResetEvent' 加入 'WaitAGVSReplyMREDictionary' 失敗");
                        }
                    }

                }
                catch (IOException ioex)
                {
                    Console.WriteLine($"[AGVS] 發送訊息的過程中發生 IOException : {ioex.Message}");
                    Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AGVS] 發送訊息的過程中發生未知的錯誤  {ex.Message}");
                    Disconnect();
                }

            });
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 13 : 3));

            try
            {
                _task.Start();
                _task.Wait(cts.Token);
                return true;
            }
            catch (OperationCanceledException ex)
            {
                return false;
            }
        }
    }
}
