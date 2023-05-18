namespace GPMVehicleControlSystem.Models.Emulators
{
    public class VehicleEmu :VehicleControl.Vehicle
    {

        public void SwitchON()
        {
            StaEmuManager.wagoEmu.SetState(VehicleControl.DIOModule.clsDIModule.DI_ITEM.Horizon_Motor_Switch, true);
        }

    }
}
