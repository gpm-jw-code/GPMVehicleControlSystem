namespace GPMVehicleControlSystem
{
    public class LogItem
    {
        public LogLevel level { get; } = LogLevel.Information;

        public string logMsg { get; } = string.Empty;
        public DateTime Time { get; internal set; }

        public LogItem(LogLevel level, string logMsg)
        {
            this.level = level;
            this.logMsg = logMsg;
        }
        public LogItem(string logMsg)
        {
            this.logMsg = logMsg;
        }

        public string logFullLine => $" [{level}][{Caller}] {logMsg}{(exception != null ? exception.StackTrace : "")}";

        public string Caller { get; internal set; }

        public Exception exception { get; set; }
    }
}
