namespace PdbSourceIndexer
{
    public interface ILogger
    {
        void Message(MessageLevel level, string format, params object[] args);
    }

    public enum MessageLevel
    {
        Info,
        Warn,
        Error
    }

    public static class LoggerExtensions
    {
        public static void Info(this ILogger logger, string format, params object[] args) => logger.Message(MessageLevel.Info, format, args);
        public static void Warn(this ILogger logger, string format, params object[] args) => logger.Message(MessageLevel.Warn, format, args);
        public static void Error(this ILogger logger, string format, params object[] args) => logger.Message(MessageLevel.Error, format, args);
    }
}
