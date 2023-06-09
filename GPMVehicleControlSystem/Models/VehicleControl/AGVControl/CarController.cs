using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.GPMRosMessageNet.Actions;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;
using AGVSystemCommonNet6.GPMRosMessageNet.Services;
using AGVSystemCommonNet6.Log;
using Newtonsoft.Json;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Actionlib;

namespace GPMVehicleControlSystem.Models.VehicleControl.AGVControl
{
    /// <summary>
    /// 使用ＲＯＳ與車控端通訊
    /// </summary>
    public partial class CarController : Connection
    {
        public enum LOCALIZE_STATE : byte
        {
            //  1 byte LocalizationStatus [0...100, 10: OK, 20: Warning, 30: Not localized, 40: System error]
            OK = 10,
            Warning = 20,
            Not_Localized = 30,
            System_Error = 40
        }
        public enum ROBOT_CONTROL_CMD : byte
        {
            /// <summary>
            /// 速度恢復
            /// </summary>
            SPEED_Reconvery = 0,
            /// <summary>
            /// 減速
            /// </summary>
            DECELERATE = 1,
            /// <summary>
            /// 停止
            /// </summary>
            STOP = 2,
            /// <summary>
            /// 停止(停止計算道路封閉)
            /// </summary>
            STOP_CALCULATE_PATH_CLOSE = 3,
            /// <summary>
            /// 二次定位減速
            /// </summary>
            DECELERATE_SECONDARY_LOCALIZATION = 21,
            /// <summary>
            /// 請求停止或到目標點停止，清空當前車控軌跡任務，需附帶任務ID
            /// </summary>
            STOP_WHEN_REACH_GOAL = 100,
            /// <summary>
            /// 立即停止請求，需附帶任務 ID
            /// </summary>
            STOP_RIGHTNOW = 101

        }
        public RosSocket? rosSocket;

        /// <summary>
        /// 地圖比對率
        /// </summary>
        public double MapRatio => LocalizationControllerResult.map_match_status / 100.0;
        public LOCALIZE_STATE Localize_State => (LOCALIZE_STATE)LocalizationControllerResult.loc_status;
        private LocalizationControllerResultMessage0502 LocalizationControllerResult = new LocalizationControllerResultMessage0502();

        public event EventHandler<ModuleInformation> OnModuleInformationUpdated;
        public event EventHandler<LocalizationControllerResultMessage0502> OnSickDataUpdated;

        /// <summary>
        /// 機器人任務結束且是成功完成的狀態
        /// </summary>
        public event EventHandler<clsTaskDownloadData> OnTaskActionFinishAndSuccess;
        /// <summary>
        /// 機器人任務結束因為被中斷
        /// </summary>
        public event EventHandler<clsTaskDownloadData> OnTaskActionFinishCauseAbort;
        public event EventHandler<clsTaskDownloadData> OnTaskActionFinishButNeedToExpandPath;
        public event EventHandler<clsTaskDownloadData> OnMoveTaskStart;

        internal TaskCommandActionClient actionClient;

        private ActionStatus _currentTaskCmdActionStatus = ActionStatus.PENDING;
        public ActionStatus currentTaskCmdActionStatus
        {
            get => _currentTaskCmdActionStatus;
            private set
            {
                if (value == ActionStatus.ACTIVE)
                    OnMoveTaskStart?.Invoke(this, RunningTaskData);
                _currentTaskCmdActionStatus = value;
            }
        }

        private ModuleInformation _module_info;
        private int CurrentTag => _module_info.nav_state.lastVisitedNode.data;
        public ModuleInformation module_info
        {
            get => _module_info;
            private set
            {
                if (value != null)
                    OnModuleInformationUpdated?.Invoke(this, value);
                _module_info = value;
            }
        }
        public int lastVisitedNode => module_info.nav_state.lastVisitedNode.data;
        public clsTaskDownloadData RunningTaskData { get; private set; } = new clsTaskDownloadData();
        /// <summary>
        /// 手動操作控制器
        /// </summary>
        public MoveControl ManualController { get; set; }
        public double CurrentSpeedLimit { get; internal set; }

        /// <summary>
        /// 車控是否在執行任務
        /// </summary>
        /// <value></value>
        public bool IsAGVExecutingTask { get; set; } = false;
        public bool TaskIsSegment => RunningTaskData.IsTaskSegmented;
        private bool EmergencyStopFlag = false;
        public CarController()
        {

        }

        public CarController(string IP, int Port) : base(IP, Port)
        {
        }

        public override bool Connect()
        {
            while (!IsConnected())
            {
                Thread.Sleep(1000);
                LOG.WARN($"Connect to ROSBridge Server (ws://{IP}:{Port}) Processing...");
                try
                {
                    rosSocket = new RosSocket(new RosSharp.RosBridgeClient.Protocols.WebSocketSharpProtocol($"ws://{IP}:{Port}"));
                }
                catch (Exception ex)
                {
                    rosSocket = null;
                    Console.WriteLine("ROS Bridge Server Connect Fail...Will Retry After 5 Secnonds...Error Message : " + ex.Message);
                    Thread.Sleep(5000);
                }
            }
            rosSocket.protocol.OnClosed += Protocol_OnClosed;
            LOG.INFO($"ROS Connected ! ws://{IP}:{Port}");
            rosSocket.Subscribe<ModuleInformation>("/module_information", new SubscriptionHandler<ModuleInformation>(ModuleInformationCallback));
            rosSocket.Subscribe<LocalizationControllerResultMessage0502>("localizationcontroller/out/localizationcontroller_result_message_0502", SickStateCallback, 100);
            rosSocket.AdvertiseService<CSTReaderCommandRequest, CSTReaderCommandResponse>("/CSTReader_done_action", CSTReaderDoneActionHandle);
            ManualController = new MoveControl(rosSocket);
            return true;
        }


        private void Protocol_OnClosed(object? sender, EventArgs e)
        {
            rosSocket.protocol.OnClosed -= Protocol_OnClosed;
            LOG.WARN("Rosbridger Server On Closed...Retry connecting...");
            Connect();
        }

        public override void Disconnect()
        {
            rosSocket.Close();
            rosSocket = null;
        }


        internal void EMOHandler(object? sender, EventArgs e)
        {
            Console.Error.WriteLine($"EMO 觸發,緊急停止.");
            AbortTask();
            ManualController.Stop();
            if (wait_agvc_execute_action_cts != null)
                wait_agvc_execute_action_cts.Cancel();

            _currentTaskCmdActionStatus = ActionStatus.ABORTED;
            //CarSpeedControl(ROBOT_CONTROL_CMD.STOP, "");
        }
        public override bool IsConnected()
        {
            return rosSocket != null && rosSocket.protocol.IsAlive();
        }


        private void InitTaskCommandActionClient()
        {
            if (actionClient != null)
            {
                actionClient.OnTaskCommandActionDone -= this.OnTaskCommandActionDone;
                actionClient.Terminate();
                actionClient.Dispose();
            }

            actionClient = new TaskCommandActionClient("/barcodemovebase", rosSocket);
            actionClient.OnTaskCommandActionDone += this.OnTaskCommandActionDone;
            actionClient.OnActionStatusChanged += (status) =>
            {
                currentTaskCmdActionStatus = status;
            };
            actionClient.Initialize();
        }

        internal void AbortTask(RESET_MODE mode)
        {
            if (mode == RESET_MODE.ABORT)
                AbortTask();
            else
                CycleStop();

        }

        private void CycleStop()
        {
            CarSpeedControl(ROBOT_CONTROL_CMD.DECELERATE);
            while (module_info.reader.tagID == 0)
            {
                Thread.Sleep(1);
            }
            AbortTask();
        }

        internal void AbortTask()
        {
            _currentTaskCmdActionStatus = ActionStatus.ABORTED;
            EmergencyStopFlag = true;
            if (actionClient != null)
            {
                actionClient.goal = new TaskCommandGoal();
                actionClient.SendGoal();
            }
            DisposeTaskCommandActionClient();
            IsAGVExecutingTask = false;
        }

        internal bool NavPathExpandedFlag { get; private set; } = false;
        private void OnTaskCommandActionDone(ActionStatus Status)
        {
            bool isReachFinalTag = CurrentTag == RunningTaskData.Destination;
            if (isReachFinalTag)
            {
                if (Status == ActionStatus.SUCCEEDED | Status == ActionStatus.PENDING)
                    OnTaskActionFinishAndSuccess?.Invoke(this, this.RunningTaskData);
                else
                    OnTaskActionFinishCauseAbort?.Invoke(this, this.RunningTaskData);
                _currentTaskCmdActionStatus = ActionStatus.NO_GOAL;
            }
            else
            {
                OnTaskActionFinishButNeedToExpandPath?.Invoke(this, this.RunningTaskData);
            }
        }


        private void DisposeTaskCommandActionClient()
        {
            if (actionClient != null)
            {
                try
                {
                    actionClient.OnTaskCommandActionDone -= OnTaskCommandActionDone;
                    actionClient.Terminate();
                    actionClient.Dispose();
                    actionClient = null;

                }
                catch (Exception ex)
                {
                    LOG.WARN("DisposeTaskCommandActionClient Exception Occur :　" + ex.Message);
                }
            }
        }

        private void SickStateCallback(LocalizationControllerResultMessage0502 _LocalizationControllerResult)
        {
            LocalizationControllerResult = _LocalizationControllerResult;
            OnSickDataUpdated?.Invoke(this, _LocalizationControllerResult);
        }

        private void ModuleInformationCallback(ModuleInformation _ModuleInformation)
        {
            module_info = _ModuleInformation;
            if (module_info.AlarmCode.Length > 0)
            {

            }
        }

        internal async Task CarSpeedControl(ROBOT_CONTROL_CMD cmd)
        {
            await Task.Delay(1);
            CarSpeedControl(cmd, RunningTaskData.Task_Name);
        }
        public bool CarSpeedControl(ROBOT_CONTROL_CMD cmd, string task_id)
        {
            ComplexRobotControlCmdRequest req = new ComplexRobotControlCmdRequest()
            {
                taskID = task_id == null ? "" : task_id,
                reqsrv = (byte)cmd
            };
            ComplexRobotControlCmdResponse? res = rosSocket?.CallServiceAndWait<ComplexRobotControlCmdRequest, ComplexRobotControlCmdResponse>("/complex_robot_control_cmd", req);
            if (res == null)
            {
                return false;
            }
            //LOG.TRACE($"要求車控 {cmd},Result: {(res.confirm ? "OK" : "NG")}");
            return res.confirm;
        }
     
        internal async Task<bool> AGVSTaskDownloadHandler(clsTaskDownloadData taskDownloadData)
        {
            NavPathExpandedFlag = false;
            this.RunningTaskData = taskDownloadData;
            InitTaskCommandActionClient();
            bool agvc_accept = await SendGoal(RunningTaskData.RosTaskCommandGoal);
            return agvc_accept;
        }

        CancellationTokenSource wait_agvc_execute_action_cts;
        internal async Task<bool> SendGoal(TaskCommandGoal rosGoal)
        {
            string new_path = string.Join("->", rosGoal.planPath.poses.Select(p => p.header.seq));

            LOG.INFO($"====================Send Goal To AGVC===================" +
                $"\r\nTaskID        = {rosGoal.taskID}" +
                $"\r\nFinal Goal ID = {rosGoal.finalGoalID}" +
                $"\r\nPlanPath      = {string.Join("->", rosGoal.planPath.poses.Select(pose => pose.header.seq).ToArray())}" +
                $"\r\nmobilityModes = {rosGoal.mobilityModes}" +
                $"\r\n==========================================================");

            EmergencyStopFlag = false;
            actionClient.goal = rosGoal;
            actionClient.SendGoal();
            //wait goal status change to  ACTIVE
            wait_agvc_execute_action_cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            LOG.INFO($"AGVC Accept Task and Start Executing：Path Tracking = {new_path}");
            return true;

        }

        internal void Replan(clsTaskDownloadData taskDownloadData)
        {
            actionClient.goal = taskDownloadData.RosTaskCommandGoal;
            actionClient.SendGoal();
        }

    }
}
