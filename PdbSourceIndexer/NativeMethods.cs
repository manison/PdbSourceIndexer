namespace PdbSourceIndexer
{
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        public enum InstallState
        {
            MoreData = -3,
            Local = 3,
        }

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        public static extern int MsiLocateComponent(string szComponent, [Out] StringBuilder lpPathBuf, ref int pcchBuf);
    }
}
