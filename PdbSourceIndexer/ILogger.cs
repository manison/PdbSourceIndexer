namespace PdbSourceIndexer
{
    public interface ILogger
    {
        MessageLevel MinimumLevel { get; set; }

        void Message(MessageLevel level, string format, params object[] args);
    }

    public enum MessageLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    public static class LoggerExtensions
    {
        public static void Debug(this ILogger logger, string format, params object[] args) => logger.Message(MessageLevel.Debug, format, args);
        public static void Info(this ILogger logger, string format, params object[] args) => logger.Message(MessageLevel.Info, format, args);
        public static void Warn(this ILogger logger, string format, params object[] args) => logger.Message(MessageLevel.Warn, format, args);
        public static void Error(this ILogger logger, string format, params object[] args) => logger.Message(MessageLevel.Error, format, args);
    }
}
