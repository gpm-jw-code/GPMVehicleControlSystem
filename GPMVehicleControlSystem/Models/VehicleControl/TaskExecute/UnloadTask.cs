using AGVSystemCommonNet6.AGVDispatch.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class UnloadTask : LoadTask
    {
        public UnloadTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
            action = ACTION_TYPE.Unload;
        }
    }
}
