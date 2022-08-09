using System;
using System.Runtime.InteropServices;

/// <summary>
/// ZBar is a library for reading bar codes from video streams
/// </summary>
namespace ZBar
{
    /// <summary>
    /// An exception that happened in ZBar
    /// </summary>
    public sealed class ZBarException : Exception
    {
        /// <summary>
        /// Verbosity constant, for errors
        /// </summary>
        private const int verbosity = 10;

        /// <summary>
        /// Error message
        /// </summary>
        private string message;

        /// <summary>
        /// Error code
        /// </summary>
        private ZBarError code;

        internal ZBarException(IntPtr obj)
        {
            this.code = (ZBarError)NativeZBar._zbar_get_error_code(obj);
            this.message = Marshal.PtrToStringAnsi(NativeZBar._zbar_error_string(obj, verbosity));
        }

        /// <value>
        /// Error message from ZBar
        /// </value>
        public override string Message
        {
            get
            {
                return this.message;
            }
        }

        /// <value>
        /// Error code of this exception, from ZBar
        /// </value>
        public ZBarError ErrorCode
        {
            get
            {
                return this.code;
            }
        }
    }

    /// <summary>
    /// Error codes
    /// </summary>
    /// <remarks>
    /// The ordering matches zbar_error_t from zbar.h
    /// </remarks>
    public enum ZBarError
    {
        /// <summary>
        /// No error, or zbar is not aware of the error
        /// </summary>
        Ok = 0,

        /// <summary>
        /// Out of memory
        /// </summary>
        OutOfMemory,

        /// <summary>
        /// Internal library error
        /// </summary>
        InternalLibraryError,

        /// <summary>
        /// Unsupported request
        /// </summary>
        Unsupported,

        /// <summary>
        /// Invalid request
        /// </summary>
        InvalidRequest,

        /// <summary>
        /// System error
        /// </summary>
        SystemError,

        /// <summary>
        /// Locking error
        /// </summary>
        LockingError,

        /// <summary>
        /// All resources busy
        /// </summary>
        AllResourcesBusyError,

        /// <summary>
        /// X11 display error
        /// </summary>
        X11DisplayError,

        /// <summary>
        /// X11 Protocol error
        /// </summary>
        X11ProtocolError,

        /// <summary>
        /// Output window closed
        /// </summary>
        OutputWindowClosed,

        /// <summary>
        /// Windows system error
        /// </summary>
        WindowsAPIError
    }
}