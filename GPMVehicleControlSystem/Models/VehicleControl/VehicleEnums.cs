namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public  partial class Vehicle
    {
        public enum AGV_TYPE
        {
            FORK, SUBMERGED_SHIELD
        }
        public enum OPERATOR_MODE
        {
            MANUAL,
            AUTO,
        }

        public enum MAIN_STATUS
        {
            IDLE = 1, RUN = 2, DOWN = 3, Charging = 4
        }
        public enum SUB_STATUS
        {
            IDLE = 1, RUN = 2, DOWN = 3, Charging = 4,
            Initializing = 5,
            ALARM = 6,
            WARNING = 7
        }
    }
}
