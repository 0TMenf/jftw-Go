namespace JftwGo.Core.Logging
{
    /// <summary>仅向内存事件订阅者广播、不落盘的日志器。适合向导页内展示。</summary>
    public class EventLogger : LoggerBase
    {
        protected override void Write(LogLevel level, string message) { }
    }
}
