using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{
    public class clsTaskFeedbackMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0303";
        public Dictionary<string, FeedbackData> Header { get; set; } = new Dictionary<string, FeedbackData>();
    }

    public class FeedbackData
    {
        [JsonProperty("Time Stamp")]
        public string TimeStamp { get; set; }

        [JsonProperty("Task Name")]
        public string TaskName { get; set; }
        [JsonProperty("Task Simplex")]
        public string TaskSimplex { get; set; }

        [JsonProperty("Task Sequence")]
        public int TaskSequence { get; set; }


        [JsonProperty("Point Index")]
        public int PointIndex { get; set; }

        [JsonProperty("Task Status")]
        public int TaskStatus { get; set; }
    }
}
