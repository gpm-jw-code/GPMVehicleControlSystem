using GPMVehicleControlSystem.Models.User;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SQLite;
using System.Drawing.Printing;
using System.Security.Claims;
using GPMVehicleControlSystem.Models.Alarm;

namespace GPMVehicleControlSystem.Models.Database
{
    public class DBhelper
    {
        private static SQLiteConnection db;

        public static void Initialize()
        {
            var databasePath = Path.Combine(Environment.CurrentDirectory, "VMS.db");
            db = new SQLiteConnection(databasePath);
            db.CreateTable<clsAlarmCode>();
            db.CreateTable<UserEntity>();

            CreateDefaultUsers();

        }

        public static void InsertAlarm(clsAlarmCode alarm)
        {
            db.Insert(alarm);
        }


        public static void InsertUser(UserEntity user)
        {
            try
            {
                db.Insert(user);
            }
            catch (Exception ex)
            {
            }
        }

        internal static int AlarmsTotalNum()
        {
            return db.Table<clsAlarmCode>().Count();
        }

        internal static int ClearAllAlarm()
        {
            try
            {
                return db.Table<clsAlarmCode>().Delete(a => a.Time != null);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public static List<clsAlarmCode> QueryAlarm(int page, int page_size = 16)
        {
            try
            {
                var query = db.Table<clsAlarmCode>().OrderByDescending(f => f.Time).Skip(page_size * (page - 1)).Take(page_size);
                return query.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static UserEntity QueryUserByName(string userName)
        {
            try
            {
                return db.Table<UserEntity>().FirstOrDefault(user => user.UserName.ToUpper() == userName.ToUpper());
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private static void CreateDefaultUsers()
        {
            InsertUser(new UserEntity
            {
                Role = ERole.Engineer,
                UserName = "ENG",
                Password = "12345678"
            });
            InsertUser(new UserEntity
            {
                Role = ERole.GOD,
                UserName = "GOD",
                Password = "66669999"
            });
        }


    }
}
