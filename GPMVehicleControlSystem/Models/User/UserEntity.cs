
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SQLite;

namespace GPMVehicleControlSystem.Models.User
{
    public class UserEntity
    {
        [PrimaryKey]
        public string UserName { get; set; }
        public string Password { get; set; }
        public ERole Role { get; set; }
    }
}
