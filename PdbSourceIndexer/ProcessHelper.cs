namespace PdbSourceIndexer
{
    using System.Collections.Generic;
    using System.Diagnostics;

    internal static class ProcessHelper
    {
        public static IEnumerable<string> ReadProcessLines(string exeName, string arguments)
        {
            var lines = new List<string>();

            using (var process = InitializeProcess(exeName, arguments))
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                string line;
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        public static int Execute(string exeName, string arguments)
        {
            using (var process = InitializeProcess(exeName, arguments))
            {
                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private static Process InitializeProcess(string exeName, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = exeName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            return process;
        }
    }
}
