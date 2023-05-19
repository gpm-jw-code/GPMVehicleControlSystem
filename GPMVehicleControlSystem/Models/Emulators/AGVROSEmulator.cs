using GPMVehicleControlSystem.Tools;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.Emulators
{

    public class AGVROSEmulator
    {

        private ModuleInformation module_info = new ModuleInformation()
        {
            IMU = new GpmImuMsg
            {
                state = 1
            },
            Battery = new BatteryState
            {
                state = 1,
                batteryLevel = 24,
            },
            CSTReader = new CSTReaderState
            {
                state = 1
            }
        };
        private LocalizationControllerResultMessage0502 localizeResult = new LocalizationControllerResultMessage0502();

        public AGVROSEmulator()
        {
            string RosBridge_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:RosBridge:IP");
            int RosBridge_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:RosBridge:Port");

            var rosSocket = new RosSocket(new WebSocketSharpProtocol($"ws://{RosBridge_IP}:{RosBridge_Port}"));

            rosSocket.Advertise<ModuleInformation>("AGVC_Emu", "/module_information");
            rosSocket.Advertise<LocalizationControllerResultMessage0502>("SICK_Emu", "localizationcontroller/out/localizationcontroller_result_message_0502");

            _ = PublishModuleInformation(rosSocket);
            // _ = PublishLocalizeResult(rosSocket);
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
    }
}
