using System;
using System.IO;
using System.Text;

namespace JftwGo.Core.Logging
{
    /// <summary>落盘日志器：按天写入 %LocalAppData%\jftwGo\logs\jftwgo-yyyyMMdd.log。</summary>
    public class FileLogger : LoggerBase
    {
        private readonly object _sync = new object();
        private readonly string _file;

        public FileLogger() : this(Path.Combine(AppPaths.LogDir,
            "jftwgo-" + DateTime.Now.ToString("yyyyMMdd") + ".log")) { }

        public FileLogger(string file)
        {
            _file = file;
        }

        protected override void Write(LogLevel level, string message)
        {
            try
            {
                lock (_sync)
                {
                    string dir = Path.GetDirectoryName(_file);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    string line = string.Format("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}{3}",
                        DateTime.Now, level, message, Environment.NewLine);
                    File.AppendAllText(_file, line, Encoding.UTF8);
                }
            }
            catch
            {
                // 日志失败绝不影响主流程
            }
        }
    }
}
