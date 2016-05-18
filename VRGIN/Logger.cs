using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace VRGIN.Core
{
    /// <summary>
    /// Very simple logger.
    /// </summary>
    public static class Logger
    {
        private static string LOG_PATH = "vr.log";
        private static StreamWriter LogFile;

        static Logger()
        {
            if (File.Exists(LOG_PATH))
            {
                LogFile = new StreamWriter(File.OpenWrite(LOG_PATH));
                LogFile.BaseStream.SetLength(0);
            }
        }

        public static LogMode Level = LogMode.Info;
        public enum LogMode
        {
            Debug,
            Info,
            Warning,
            Error
        }

        public static void Debug(string text, params object[] args)
        {
            Log(text, args, LogMode.Debug);
        }

        public static void Info(string text, params object[] args) {
            Log(text, args, LogMode.Info);
        }

        public static void Warn(string text, params object[] args)
        {
            Log(text, args, LogMode.Warning);
        }

        public static void Error(string text, params object[] args)
        {
            Log(text, args, LogMode.Error);
        }

        public static void Debug(object obj)
        {
            Log("{0}", new object[]{ obj }, LogMode.Debug);
        }

        public static void Info(object obj)
        {
            Log("{0}", new object[] { obj }, LogMode.Info);
        }

        public static void Warn(object obj)
        {
            Log("{0}", new object[] { obj }, LogMode.Warning);
        }

        public static void Error(object obj)
        {
            Log("{0}", new object[] { obj }, LogMode.Error);
        }


        private static void Log(string text, object[] args, LogMode severity)
        {
            try
            {
                if (severity < Level) return;

#if COLOR_SUPPORT
                ConsoleColor foregroundColor = ConsoleColor.White;
                ConsoleColor backgroundColor = ConsoleColor.Black;

                switch (severity)
                {
                    case LogMode.Debug:
                        foregroundColor = ConsoleColor.Gray;
                        break;
                    case LogMode.Warning:
                        foregroundColor = ConsoleColor.Yellow;
                        break;
                    case LogMode.Error:
                        backgroundColor = ConsoleColor.Red;
                        break;
                }

                var oldForegroundColor = Console.ForegroundColor;
                var oldBackgroundColor = Console.BackgroundColor;

                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
#endif
                string formatted = String.Format(Format(text, severity), args);
                Console.WriteLine(formatted);

                LogFile.WriteLine(formatted);

#if COLOR_SUPPORT
                Console.ForegroundColor = oldForegroundColor;
                Console.BackgroundColor = oldBackgroundColor;
#endif
            } catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static String Format(string text, LogMode mode)
        {
            var trace = new StackTrace(3);
            var caller = trace.GetFrame(0);
            return String.Format("[{0}][{1}][{3}#{4}] {2}", DateTime.Now.ToString("HH':'mm':'ss"), mode.ToString().ToUpper(), text, caller.GetMethod().DeclaringType.Name, caller.GetMethod().Name, caller.GetFileLineNumber());
        }
    }
}
