namespace PdbSourceIndexer
{
    public abstract class BaseLogger : ILogger
    {
        public MessageLevel MinimumLevel { get; set; } = MessageLevel.Info;

        protected BaseLogger()
        {
        }

        public void Message(MessageLevel level, string format, params object[] args)
        {
            if (level >= MinimumLevel)
            {
                MessageCore(level, format, args);
            }
        }

        protected abstract void MessageCore(MessageLevel level, string format, params object[] args);
    }
}
