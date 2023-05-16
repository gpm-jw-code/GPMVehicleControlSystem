using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;

namespace GPMVehicleControlSystem
{
    public class LOG
    {
        internal static string LogFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "VMS LOG");

        private static Task WriteLogToFileTask;
        private static ConcurrentQueue<LogItem> logItemQueue = new ConcurrentQueue<LogItem>();

        internal static void TRACE(string info)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Trace, info), caller_class_name);
        }
        public static void INFO(string info)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Information, info), caller_class_name);
        }
        public static void WARN(string info)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Warning, info), caller_class_name);
        }
        public static void ERROR(string info, Exception ex)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Error, string.Format("{0}。Exception Message:{1}", info, ex.Message + "\r\n" + ex.StackTrace)) { exception = ex }, caller_class_name);
        }
        public static void ERROR(string info)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Error, string.Format("{0}", info)), caller_class_name);
        }

        public static void ERROR(Exception ex)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Error, string.Format("Message:{0}。StackTrace:{1}", ex.Message, ex.StackTrace)) { exception = ex }, caller_class_name);
        }

        public static void Critical(string msg, Exception ex)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;
            Log(new LogItem(LogLevel.Critical, string.Format("Message:{0}。StackTrace:{1}", msg, ex.StackTrace)) { exception = ex }, caller_class_name);
        }
        public static void Critical(Exception ex)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Critical, string.Format("Message:{0}。StackTrace:{1}", ex.Message, ex.StackTrace)) { exception = ex }, caller_class_name);
        }

        public static void Critical(string info)
        {
            var caller_class_name = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name; ;

            Log(new LogItem(LogLevel.Critical, info), caller_class_name);
        }

        private static void Log(LogItem logItem, string caller_class_name = "")
        {
            if (WriteLogToFileTask == null)
            {
                WriteLogToFileTask = Task.Factory.StartNew(() => WriteLogWorker());
            }
            logItem.Caller = caller_class_name;
            logItem.Time = DateTime.Now;
            logItemQueue.Enqueue(logItem);


        }

        private static async void WriteLogWorker()
        {
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);

            while (true)
            {
                await Task.Delay(100);

                if (logItemQueue.TryDequeue(out var logItem))
                {
                    string subFolder = Path.Combine(LogFolder, DateTime.Now.ToString("yyyy-MM-dd"));

                    if (!Directory.Exists(subFolder))
                        Directory.CreateDirectory(subFolder);

                    string fileName = Path.Combine(subFolder, $"{logItem.level.ToString()}.log");

                    using (StreamWriter writer = new StreamWriter(fileName, true))
                    {
                        writer.WriteLine(logItem.logFullLine);
                    }


                    ConsoleColor foreColor = ConsoleColor.White;
                    ConsoleColor backColor = ConsoleColor.Black;
                    switch (logItem.level)
                    {
                        case LogLevel.Trace:
                            foreColor = ConsoleColor.White;
                            break;
                        case LogLevel.Debug:
                            break;
                        case LogLevel.Information:
                            backColor = ConsoleColor.Blue;
                            break;
                        case LogLevel.Warning:
                            foreColor = ConsoleColor.Yellow;
                            break;
                        case LogLevel.Error:
                            backColor = ConsoleColor.Red;
                            break;
                        case LogLevel.Critical:
                            backColor = ConsoleColor.Red;
                            break;
                        case LogLevel.None:
                            break;
                        default:
                            break;
                    }

                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write(logItem.Time + " ");

                    Console.ForegroundColor = foreColor;
                    Console.BackgroundColor = backColor;

                    Console.WriteLine(logItem.logFullLine);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;

                }
            }
        }
    }
}
