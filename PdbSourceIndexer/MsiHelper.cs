namespace PdbSourceIndexer
{
    using System.Text;

    internal static class MsiHelper
    {
        public static string GetComponentPath(string componentId)
        {
            int capacity = 260;
            var path = new StringBuilder(capacity);
            var state = (NativeMethods.InstallState)NativeMethods.MsiLocateComponent(componentId, path, ref capacity);
            if (state == NativeMethods.InstallState.MoreData)
            {
                path.Capacity = capacity + 1;
                state = (NativeMethods.InstallState)NativeMethods.MsiLocateComponent(componentId, path, ref capacity);
            }

            if (state == NativeMethods.InstallState.Local)
            {
                return path.ToString();
            }

            return null;
        }
    }
}
