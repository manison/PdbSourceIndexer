namespace PdbSourceIndexer
{
    using System;
    using System.Text;

    public class ConsoleLogger : BaseLogger
    {
        protected override void MessageCore(MessageLevel level, string format, params object[] args)
        {
            var s = new StringBuilder();
            s.Append(level.ToString().ToUpper());
            s.Append(": ");
            s.AppendFormat(format, args);
            Console.WriteLine(s.ToString());
        }
    }
}
