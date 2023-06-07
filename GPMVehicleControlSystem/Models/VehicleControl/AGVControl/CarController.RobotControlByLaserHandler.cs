namespace GPMVehicleControlSystem.Models.VehicleControl.AGVControl
{
    public partial class CarController
    {
        private bool IsFrontArea1LaserRecovery = false;
        private bool IsBackArea1LaserRecovery = false;

        private bool IsFrontArea2LaserRecovery = false;
        private bool IsBackArea2LaserRecovery = false;
        internal void FarArea1LaserTriggerHandler(object? sender, EventArgs e)
        {
            IsFrontArea1LaserRecovery = false;
            Console.Error.WriteLine($"雷射 AREA1  觸發,減速請求. ");
            CarSpeedControl(ROBOT_CONTROL_CMD.DECELERATE, "");
        }
        internal void FarArea2LaserTriggerHandler(object? sender, EventArgs e)
        {
            Console.Error.WriteLine($"雷射 AREA2  觸發,減速停止請求. ");
            CarSpeedControl(ROBOT_CONTROL_CMD.STOP, "");
        }

        internal void FrontFarArea1LaserRecoveryHandler(object? sender, EventArgs e)
        {
            IsFrontArea1LaserRecovery = true;
            if (IsBackArea1LaserRecovery)
            {
                Console.Error.WriteLine($"FarArea 1&2 雷射解除,速度恢復請求.");
                CarSpeedControl(ROBOT_CONTROL_CMD.SPEED_Reconvery, "");
            }
            else
            {
                Console.Error.WriteLine($"FrontFarArea 1 雷射解除但 BackFarArea 1 未解除");
            }
        }


        internal void BackFarArea1LaserRecoveryHandler(object? sender, EventArgs e)
        {
            IsBackArea1LaserRecovery = true;
            if (IsFrontArea1LaserRecovery)
            {
                Console.Error.WriteLine($"FarArea 1&2 雷射解除,速度恢復請求.");
                CarSpeedControl(ROBOT_CONTROL_CMD.SPEED_Reconvery, "");
            }
            else
            {
                Console.Error.WriteLine($"BackFarArea 1 雷射解除但 FrontFarArea 1 未解除");
            }
        }


        internal void FrontFarArea2LaserRecoveryHandler(object? sender, EventArgs e)
        {
            IsFrontArea2LaserRecovery = true;


            Console.WriteLine($"FrontFarArea 2 雷射解除,速度恢復請求.");
            CarSpeedControl(ROBOT_CONTROL_CMD.SPEED_Reconvery, "");
            //if (IsFrontArea1LaserRecovery && IsBackArea1LaserRecovery)
            //{
            //    Console.Error.WriteLine($"FrontFarArea1 雷射解除,速度恢復請求.");
            //    CarSpeedControl(ROBOT_CONTROL_CMD.SPEED_Reconvery, "");
            //}
            //else
            //    LOG.TRACE("一段未解除，無速度恢復請求");
        }

        internal void BackFarArea2LaserRecoveryHandler(object? sender, EventArgs e)
        {
            IsBackArea2LaserRecovery = true;


            Console.Error.WriteLine($"BackFarArea 2 雷射解除,速度恢復請求.");
            CarSpeedControl(ROBOT_CONTROL_CMD.SPEED_Reconvery, "");
            //if (IsFrontArea1LaserRecovery && IsBackArea1LaserRecovery)
            //{
            //    Console.Error.WriteLine($"BackFarArea1 雷射解除,速度恢復請求.");
            //    CarSpeedControl(ROBOT_CONTROL_CMD.SPEED_Reconvery, "");
            //}
            //else
            //    LOG.TRACE("一段未解除，無速度恢復請求");
        }
    }
}
