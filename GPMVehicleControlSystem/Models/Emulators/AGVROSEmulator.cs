using GPMVehicleControlSystem.Tools;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.GPMRosMessageNet.Services;
using RosSharp.RosBridgeClient.Actionlib;
using AGVSystemCommonNet6.GPMRosMessageNet.Actions;
using AGVSystemCommonNet6;

namespace GPMVehicleControlSystem.Models.Emulators
{

    public class AGVROSEmulator
    {
        RosSocket? rosSocket;
        private ModuleInformation module_info = new ModuleInformation()
        {
            IMU = new GpmImuMsg
            {
                state = 1
            },
            Battery = new BatteryState
            {
                state = 1,
                batteryLevel = 90,
                batteryID = 100
            },
            CSTReader = new CSTReaderState
            {
                state = 1
            },
            nav_state = new NavigationState
            {
                lastVisitedNode = new RosSharp.RosBridgeClient.MessageTypes.Std.Int32(31)
            },
            reader = new BarcodeReaderState
            {
                tagID = 31
            }
        };
        private LocalizationControllerResultMessage0502 localizeResult = new LocalizationControllerResultMessage0502();

        public AGVROSEmulator()
        {
            string RosBridge_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:RosBridge:IP");
            int RosBridge_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:RosBridge:Port");
            rosSocket = new RosSocket(new WebSocketSharpProtocol($"ws://{RosBridge_IP}:{RosBridge_Port}"));

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                rosSocket.Advertise<ModuleInformation>("AGVC_Emu", "/module_information");
                rosSocket.AdvertiseService<CSTReaderCommandRequest, CSTReaderCommandResponse>("/CSTReader_action", CstReaderServiceCallack);
                rosSocket.Advertise<LocalizationControllerResultMessage0502>("SICK_Emu", "localizationcontroller/out/localizationcontroller_result_message_0502");
                InitNewTaskCommandActionServer();
                _ = PublishModuleInformation(rosSocket);
                // _ = PublishLocalizeResult(rosSocket);
            });
        }

        private void InitNewTaskCommandActionServer()
        {
            TaskCommandActionServer actionServer = new TaskCommandActionServer("/barcodemovebase", rosSocket);
            actionServer.Initialize();
            actionServer.OnNAVGoalReceived += NavGaolHandle;
            Console.WriteLine("ROS Enum Action Server Created!");
        }

        private void NavGaolHandle(object sender, TaskCommandGoal obj)
        {
            TaskCommandActionServer actionServer = (TaskCommandActionServer)sender;
            Console.WriteLine($"[ROS 車控模擬器] New Task , Task Name = {obj.taskID}, Tags Path = {string.Join("->", obj.planPath.poses.Select(p => p.header.seq))}");
            actionServer.AcceptedInvoke();

            //模擬走型
            Task.Run(() =>
            {
                foreach (RosSharp.RosBridgeClient.MessageTypes.Geometry.PoseStamped? item in obj.planPath.poses)
                {
                    uint tag = item.header.seq;
                    double tag_pose_x = item.pose.position.x;
                    double tag_pose_y = item.pose.position.y;
                    double tag_theta = item.pose.orientation.ToTheta();

                    module_info.nav_state.lastVisitedNode.data = (int)tag;
                    module_info.nav_state.robotPose.pose.position.x = tag_pose_x;
                    module_info.nav_state.robotPose.pose.position.y = tag_pose_y;
                    module_info.nav_state.robotPose.pose.orientation = tag_theta.ToQuaternion();

                    module_info.reader.tagID = tag;

                    module_info.reader.xValue = tag_pose_x;
                    module_info.reader.yValue = tag_pose_y;
                    module_info.reader.theta = tag_theta;
                    Thread.Sleep(1500);
                }

                actionServer.OnNAVGoalReceived -= NavGaolHandle;
                actionServer.SucceedInvoke();

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(500);
                    actionServer.Terminate();
                    actionServer = null;
                    InitNewTaskCommandActionServer();
                });
            });

        }

        private async Task PublishLocalizeResult(RosSocket rosSocket)
        {
            await Task.Delay(1);
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(10);
                    try
                    {
                        rosSocket.Publish("SICK_Emu", localizeResult);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            });
        }

        private async Task PublishModuleInformation(RosSocket rosSocket)
        {
            await Task.Delay(1);
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(10);
                    try
                    {
                        rosSocket.Publish("AGVC_Emu", module_info);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            });
        }

        private bool CstReaderServiceCallack(CSTReaderCommandRequest tin, out CSTReaderCommandResponse tout)
        {
            tout = new CSTReaderCommandResponse
            {
                confirm = true,
            };

            Task.Factory.StartNew(async () =>
            {
                //模擬拍照
                await Task.Delay(1000);
                module_info.CSTReader.data = $"Try_ID_{DateTime.Now.ToString("yyyyMMddHHmmssffff")}";
                rosSocket.CallServiceAndWait<CSTReaderCommandRequest, CSTReaderCommandResponse>("/CSTReader_done_action", new CSTReaderCommandRequest
                {
                    model = tin.model,
                    command = "done",
                });

            });

            return true;
        }
    }
}
