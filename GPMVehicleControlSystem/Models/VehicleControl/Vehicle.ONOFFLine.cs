using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Log;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {
        private REMOTE_MODE _Remote_Mode = REMOTE_MODE.OFFLINE;
        /// <summary>
        /// Online/Offline 模式
        /// </summary>
        public REMOTE_MODE Remote_Mode
        {
            get => _Remote_Mode;
            set
            {
                _Remote_Mode = value;
                if (SimulationMode)
                    return;
                if (value == REMOTE_MODE.ONLINE)
                {
                    StatusLighter.ONLINE();
                }
                else
                    StatusLighter.OFFLINE();
            }
        }

        private bool OnlineModeChangingFlag = false;
        internal async Task<(bool success, RETURN_CODE return_code)> Online_Mode_Switch(REMOTE_MODE mode)
        {
            (bool success, RETURN_CODE return_code) result = await AGVS.TrySendOnlineModeChangeRequest(Navigation.LastVisitedTag, mode);
            if (!result.success)
                LOG.ERROR($"車輛上線失敗 : Return Code : {result.return_code}");
            else
                Remote_Mode = mode;
            return result;
        }
    }
}
