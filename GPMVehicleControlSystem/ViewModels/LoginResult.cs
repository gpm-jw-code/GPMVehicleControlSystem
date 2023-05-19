using AGVSystemCommonNet6.User;

namespace GPMVehicleControlSystem.ViewModels
{
    public class LoginResult:UserEntity
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
