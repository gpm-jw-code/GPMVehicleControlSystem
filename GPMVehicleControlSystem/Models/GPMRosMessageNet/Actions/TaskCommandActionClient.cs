using GPMRosMessageNet.Actions;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Actionlib;

namespace GPMVehicleControlSystem.Models.GPMRosMessageNet.Actions
{
    public class TaskCommandActionClient : ActionClient<TaskCommandAction, TaskCommandActionGoal, TaskCommandActionResult, TaskCommandActionFeedback, TaskCommandGoal, TaskCommandResult, TaskCommandFeedback>, IDisposable
    {
        public Action<ActionStatus> OnTaskCommandActionDone;
        public TaskCommandGoal goal;
        private bool disposedValue;

        public TaskCommandActionClient(string actionName, RosSocket rosSocket)
        {
            this.actionName = actionName;
            this.rosSocket = rosSocket;
            action = new TaskCommandAction();
            goalStatus = new RosSharp.RosBridgeClient.MessageTypes.Actionlib.GoalStatus();
        }

        protected override TaskCommandActionGoal GetActionGoal()
        {
            if (action == null)
                return new TaskCommandActionGoal();
            action.action_goal.goal = goal;
            return action.action_goal;
        }

        protected override void OnFeedbackReceived()
        {
            if (action == null)
                return;
            var _feedback = action.action_feedback;
        }

        protected override void FeedbackCallback(TaskCommandActionFeedback actionFeedback)
        {
            try
            {
                base.FeedbackCallback(actionFeedback);
            }
            catch (Exception ex)
            {
            }
        }

        protected override void OnResultReceived()
        {
            if (action == null)
                return;
            TaskCommandActionResult result = action.action_result;
            ActionStatus status = (ActionStatus)(result.status.status);
            if (OnTaskCommandActionDone != null)
            {
                OnTaskCommandActionDone(status);
            }
        }

        protected override void OnStatusUpdated()
        {
            if (goalStatus != null)
            {
                string? status = ((ActionStatus)(goalStatus.status)).ToString();
                Console.WriteLine("[TaskCommandActionClient] OnStatusUpdated : Status : " + status);
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }

                OnTaskCommandActionDone = null;
                goalStatus = null;
                action = null;
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        // ~TaskCommandActionClient()
        // {
        //     // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
