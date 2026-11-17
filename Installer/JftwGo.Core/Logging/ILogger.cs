using System;

namespace JftwGo.Core.Logging
{
    /// <summary>简单日志接口。UI 可将日志事件渲染到可展开日志区。</summary>
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        event EventHandler<LogEventArgs> Logged;
    }

    public class LogEventArgs : EventArgs
    {
        public DateTime Time { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }

        public string Formatted
        {
            get { return string.Format("[{0:HH:mm:ss}] [{1}] {2}", Time, Level, Message); }
        }
    }

    public enum LogLevel { Info, Warn, Error }

    /// <summary>空实现，用于不需要日志的场景。</summary>
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();
        public event EventHandler<LogEventArgs> Logged;
        public void Info(string message) { var h = Logged; if (h != null) { } }
        public void Warn(string message) { }
        public void Error(string message) { }
    }

    /// <summary>日志基类，统一触发事件。</summary>
    public abstract class LoggerBase : ILogger
    {
        public event EventHandler<LogEventArgs> Logged;

        protected void Emit(LogLevel level, string message)
        {
            var handler = Logged;
            if (handler != null)
                handler(this, new LogEventArgs { Time = DateTime.Now, Level = level, Message = message });
            Write(level, message);
        }

        protected abstract void Write(LogLevel level, string message);

        public void Info(string message) { Emit(LogLevel.Info, message); }
        public void Warn(string message) { Emit(LogLevel.Warn, message); }
        public void Error(string message) { Emit(LogLevel.Error, message); }
    }
}
