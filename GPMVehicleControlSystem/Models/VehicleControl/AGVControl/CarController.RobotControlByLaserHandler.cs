namespace GPMVehicleControlSystem.Models.VehicleControl.AGVControl
{
    public partial class CarController
    {
        internal void LaserRecoveryHandler(object? sender, ROBOT_CONTROL_CMD cmd)
        {
            if (cmd != ROBOT_CONTROL_CMD.NONE)
                CarSpeedControl(cmd, "");
        }

        internal void FarLaserTriggerHandler(object? sender, EventArgs e)
        {
            CarSpeedControl(ROBOT_CONTROL_CMD.DECELERATE, "");
        }

        internal void NearLaserTriggerHandler(object? sender, EventArgs e)
        {
            CarSpeedControl(ROBOT_CONTROL_CMD.STOP, "");
        }

    }
}
