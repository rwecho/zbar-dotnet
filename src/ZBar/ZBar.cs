using System;

namespace ZBar
{
    public static class ZBar
    {
        /// <value>
        /// Get version of the underlying zbar library
        /// </value>
        public static Version Version
        {
            get
            {
                uint major = 0;
                uint minor = 0;

                unsafe
                {
                    if (NativeZBar.zbar_version(&major, &minor) != 0)
                    {
                        throw new Exception("Failed to get ZBar version.");
                    }
                }

                return new Version((int)major, (int)minor);
            }
        }
    }
}