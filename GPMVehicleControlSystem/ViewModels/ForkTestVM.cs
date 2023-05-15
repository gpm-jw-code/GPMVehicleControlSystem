namespace GPMVehicleControlSystem.ViewModels
{
    public class ForkTestVM
    {

        public class clsForkTesetOption
        {
            public int loopNum { get; set; } = 100;
            public double up_limit_pose { get; set; } = 120;
            public double down_limit_pose { get; set; } = 0;
            public double speed { get; set; } = 1;
            public bool useRandomPose { get; set; } = true;
            public bool initalizeBeforeTest { get; set; } = false;
            public bool pauseWhenReachQuarter { get; set; } = true;
            private double GetRandom()
            {
                var random = new Random(Environment.TickCount);
                double randomNumber = random.NextDouble() * 1 - 0.5;
                return randomNumber;
            }
            internal double up_limit_pose_random
            {
                get
                {
                    return up_limit_pose + GetRandom();
                }
            }
            internal double down_limit_pose_random
            {
                get
                {
                    return down_limit_pose + GetRandom();
                }
            }
        }
        public class clsForkTestState
        {
            public enum EState
            {
                NOT_RUN,
                RUNNING,
                FINISH,
                CANCELED,
                READY_TO_STOP,
                PAUSE
            }
            public enum EFORK_ACTION
            {
                FIND_HOME,
                GO_UP,
                GO_DOWN,
                GO_HOME,
                IDLE
            }
            public string state => estate.ToString();
            public string fork_action => efork_action.ToString();
            public int complete_loop_num { get; set; } = 0;
            public clsForkTesetOption option { get; set; } = new clsForkTesetOption();
            internal EState estate { get; set; }
            internal EFORK_ACTION efork_action { get; set; }

            internal bool testContinueFlag = false;
            internal bool IsFinish => complete_loop_num == option.loopNum;
            internal int quarterNum => option.loopNum / 4;
            internal bool IsReachQuarter => complete_loop_num != 0 && complete_loop_num % quarterNum == 0;
            internal void Reset()
            {
                estate = EState.NOT_RUN;
                complete_loop_num = 0;
                testContinueFlag = false;
            }
        }
    }
}
