﻿using GPMRosMessageNet.Actions;
using GPMRosMessageNet.Messages;
using GPMRosMessageNet.Messages.SickMsg;
using GPMRosMessageNet.Services;
using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.GPMRosMessageNet.Actions;
using Newtonsoft.Json;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Actionlib;

namespace GPMVehicleControlSystem.Models.VehicleControl.AGVControl
{
    /// <summary>
    /// 使用ＲＯＳ與車控端通訊
    /// </summary>
    public class CarController : Connection
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
        RosSocket? rosSocket;

        /// <summary>
        /// 地圖比對率
        /// </summary>
        public double MapRatio => LocalizationControllerResult.map_match_status / 100.0;
        public LOCALIZE_STATE Localize_State => (LOCALIZE_STATE)LocalizationControllerResult.loc_status;
        private LocalizationControllerResultMessage0502 LocalizationControllerResult = new LocalizationControllerResultMessage0502();

        public event EventHandler<ModuleInformation> OnModuleInformationUpdated;

        /// <summary>
        /// 機器人任務結束且是成功完成的狀態
        /// </summary>
        public event EventHandler<clsTaskDownloadData> OnTaskActionFinishAndSuccess;
        /// <summary>
        /// 機器人任務結束因為被中斷
        /// </summary>
        public event EventHandler<clsTaskDownloadData> OnTaskActionFinishCauseAbort;
        public event EventHandler<clsTaskDownloadData> OnMoveTaskStart;

        TaskCommandActionClient taskCommandActionClient;

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
        public bool IsAGVExecutingTask => _currentTaskCmdActionStatus == ActionStatus.ACTIVE;

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

        internal void FarAreaLaserTriggerHandler(object? sender, EventArgs e)
        {
            Console.Error.WriteLine($"遠處雷射觸發,減速停止請求. {sender?.ToJson()}");
            //CarSpeedControl(ROBOT_CONTROL_CMD.DECELERATE, "");
        }

        internal void FarAreaLaserRecoveryHandler(object? sender, EventArgs e)
        {
            Console.Error.WriteLine($"遠處雷射解除,速度恢復請求. {sender?.ToJson()}");
            CarSpeedControl(ROBOT_CONTROL_CMD.SPEED_Reconvery, "");
        }
        internal void EMOHandler(object? sender, EventArgs e)
        {
            Console.Error.WriteLine($"EMO 觸發,緊急停止. {sender?.ToJson()}");
            taskCommandActionClient.goal = new TaskCommandGoal();
            taskCommandActionClient.SendGoal();
            //CarSpeedControl(ROBOT_CONTROL_CMD.STOP, "");
        }
        public override bool IsConnected()
        {
            return rosSocket != null && rosSocket.protocol.IsAlive();
        }



        private void InitTaskCommandActionClient()
        {
            if (taskCommandActionClient != null)
            {
                taskCommandActionClient.OnTaskCommandActionDone -= this.OnTaskCommandActionDone;
                taskCommandActionClient.Terminate();
                taskCommandActionClient.Dispose();
            }

            taskCommandActionClient = new TaskCommandActionClient("/barcodemovebase", rosSocket);
            taskCommandActionClient.OnTaskCommandActionDone += this.OnTaskCommandActionDone;
            taskCommandActionClient.OnActionStatusChanged += (status) =>
            {
                currentTaskCmdActionStatus = status;
            };
            taskCommandActionClient.Initialize();
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
            AbortTask();

        }

        internal void AbortTask()
        {
            EmergencyStopFlag = true;
            taskCommandActionClient.goal = new TaskCommandGoal();
            taskCommandActionClient.SendGoal();
        }

        private void OnTaskCommandActionDone(ActionStatus Status)
        {
            taskCommandActionClient.OnTaskCommandActionDone -= OnTaskCommandActionDone;
            if (Status == ActionStatus.SUCCEEDED)
                OnTaskActionFinishAndSuccess?.Invoke(this, this.RunningTaskData);
            else
                OnTaskActionFinishCauseAbort?.Invoke(this, this.RunningTaskData);

        }


        private void SickStateCallback(LocalizationControllerResultMessage0502 _LocalizationControllerResult)
        {
            LocalizationControllerResult = _LocalizationControllerResult;
        }

        private void ModuleInformationCallback(ModuleInformation _ModuleInformation)
        {
            module_info = _ModuleInformation;
        }


        public bool CarSpeedControl(ROBOT_CONTROL_CMD cmd, string task_id)
        {
            ComplexRobotControlCmdRequest req = new ComplexRobotControlCmdRequest()
            {
                taskID = task_id,
                reqsrv = (byte)cmd
            };
            ComplexRobotControlCmdResponse? res = rosSocket?.CallServiceAndWait<ComplexRobotControlCmdRequest, ComplexRobotControlCmdResponse>("/complex_robot_control_cmd", req);
            if (res == null)
            {
                return false;
            }
            LOG.TRACE($"要求車控 {cmd},Result: {(res.confirm ? "OK" : "NG")}");
            return res.confirm;
        }
        internal async Task<bool> AGVSTaskDownloadHandler(clsTaskDownloadData taskDownloadData)
        {
            string new_path = string.Join("->", taskDownloadData.TagsOfTrajectory);

            this.RunningTaskData = taskDownloadData;
            if (IsAGVExecutingTask)
            {
                SendGoal(taskDownloadData.RosTaskCommandGoal);
                RunningTaskData = taskDownloadData;
                string ori_path = string.Join("->", RunningTaskData.TagsOfTrajectory);
                LOG.TRACE($"AGV導航路徑變更\r\n-原路徑：{ori_path}\r\n新路徑:{new_path}");
            }
            else
            {
                InitTaskCommandActionClient();
                SendGoal(RunningTaskData.RosTaskCommandGoal);

            }
            //wait goal status change to  ACTIVE
            CancellationTokenSource wait_cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (currentTaskCmdActionStatus != ActionStatus.ACTIVE)
            {
                if (wait_cts.IsCancellationRequested)
                {
                    LOG.Critical($"Send Goal To AGVC But Status Not Change To ACTIVE(AGV NOT RUNNING.)");
                    return false;
                }
                await Task.Delay(100);
            }
            return true;
        }
        internal void SendGoal(TaskCommandGoal rosGoal)
        {
            LOG.WARN($"====================Send Goal To AGVC===================" +
                $"\r\nPlanPath      = {string.Join("->", rosGoal.planPath.poses.Select(pose => pose.header.seq).ToArray())}" +
                $"\r\nmobilityModes = {rosGoal.mobilityModes}" +
                $"\r\nTaskID        = {rosGoal.taskID}" +
                $"\r\nFinal Goal ID = {rosGoal.finalGoalID}" +
                $"\r\n==========================================================");

            EmergencyStopFlag = false;

            Thread.Sleep(100);

            taskCommandActionClient.goal = rosGoal;
            taskCommandActionClient.SendGoal();
        }

        internal int GetCurrentTagIndexOfTrajectory(int currentTag)
        {
            try
            {
                return RunningTaskData.ExecutingTrajecory.ToList().IndexOf(RunningTaskData.ExecutingTrajecory.First(pt => pt.Point_ID == currentTag));

            }
            catch (Exception)
            {
                return 0;
            }

        }
    }
}
