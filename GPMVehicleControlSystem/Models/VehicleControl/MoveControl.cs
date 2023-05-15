using GPMVehicleControlSystem.Models.VehicleControl;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public class MoveControl
    {
        public MoveControl(Vehicle vehicle, RosSocket rosSocket)
        {
            this.rosSocket = rosSocket;
            this.vehicle = vehicle;
        }
        public MoveControl( RosSocket rosSocket)
        {
            this.rosSocket = rosSocket;
        }
        public RosSocket rosSocket { get; set; }

        private Vehicle vehicle;
        private bool CheckSwitchState = false;

        public void Stop()
        {

            if (rosSocket == null)
                return;
            string id = rosSocket.Advertise<Twist>("/cmd_vel");
            Twist message = new Twist();
            message.linear.x = 0;
            message.linear.y = 0;
            message.linear.z = 0;
            rosSocket.Publish(id, message);
            vehicle?.DirectionLighter.CloseAll();
        }
        public void Backward(double speed = 0.08)
        {
            if (rosSocket == null)
                return;
            string id = rosSocket.Advertise<Twist>("/cmd_vel");
            Twist message = new Twist();
            message.linear.x = -speed;
            message.linear.y = 0;
            message.linear.z = 0;
            rosSocket.Publish(id, message);
            vehicle?.DirectionLighter.CloseAll();
            vehicle?.DirectionLighter.Backward();
        }

        public void Forward(double speed = 0.08)
        {

            if (rosSocket == null)
                return;

            string id = rosSocket.Advertise<Twist>("/cmd_vel");
            Twist message = new Twist();
            message.linear.x = speed;
            message.linear.y = 0;
            message.linear.z = 0;
            rosSocket.Publish(id, message);

            vehicle?.DirectionLighter.CloseAll();
            vehicle?.DirectionLighter.Forward();
        }


        /// <summary>
        /// 向左轉
        /// </summary>
        public void TurnLeft(double speed = 0.1)
        {
            if (rosSocket == null)
                return;
            string id = rosSocket.Advertise<Twist>("/cmd_vel");
            Twist message = new Twist();
            message.angular.x = 0;
            message.angular.y = 0;
            message.angular.z = speed;
            rosSocket.Publish(id, message);
            vehicle?.DirectionLighter.CloseAll();
            vehicle?.DirectionLighter.TurnLeft(true);
        }

        public void TurnRight(double speed = 0.1)
        {

            if (rosSocket == null)
                return;
            string id = rosSocket.Advertise<Twist>("/cmd_vel");
            Twist message = new Twist();
            message.angular.x = 0;
            message.angular.y = 0;
            message.angular.z = -speed;
            rosSocket.Publish(id, message);
            vehicle?.DirectionLighter.CloseAll();
            vehicle?.DirectionLighter.TurnRight(true);

        }
    }
}