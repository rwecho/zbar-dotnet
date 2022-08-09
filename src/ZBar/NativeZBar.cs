using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// ZBar is a library for reading bar codes from video streams
/// </summary>
namespace ZBar
{
    internal static class NativeZBar
    {
        private const string DllName = "libzbar-0";
        static NativeZBar()
        {
            NativeLibrary.SetDllImportResolver(typeof(ZBar).Assembly, PathResolver);
        }

        private static IntPtr PathResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            var libHandle = IntPtr.Zero;
            if (libraryName != DllName)
            {
                return libHandle;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var x86OrX64 = IntPtr.Size == 4 ? "x86" : "x64";

                var libPath = Path.Combine("lib", "3rdparty", "zbar", $"win-{x86OrX64}", "libzbar-0.dll");
                NativeLibrary.TryLoad(libPath, out libHandle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                throw new NotSupportedException("ZBar does not supported non-windows system.");
            }

            return libHandle;
        }

        [DllImport(DllName)]
        public static extern IntPtr _zbar_error_string(IntPtr obj, int verbosity);

        [DllImport(DllName)]
        public static extern int _zbar_get_error_code(IntPtr obj);

        [DllImport(DllName)]
        public static extern unsafe int zbar_version(uint* major, uint* minor);

        #region Extern C functions

        /// <summary> constructor. </summary>
        [DllImport(DllName)]
        public static extern IntPtr zbar_video_create();

        /// <summary> destructor. </summary>
        [DllImport(DllName)]
        public static extern void zbar_video_destroy(IntPtr video);

        /// <summary>
        /// open and probe a video device.
        /// the device specified by platform specific unique name
        /// (v4l device node path in *nix eg "/dev/video",
        ///  DirectShow DevicePath property in windows).
        /// </summary>
        /// <returns> 0 if successful or -1 if an error occurs</returns>
        [DllImport(DllName)]
        public static extern int zbar_video_open(IntPtr video, string device);

        /// <summary>
        /// retrieve file descriptor associated with open *nix video device
        /// useful for using select()/poll() to tell when new images are
        /// available (NB v4l2 only!!).
        /// </summary>
        /// <returns> the file descriptor or -1 if the video device is not open
        /// or the driver only supports v4l1</returns>
        [DllImport(DllName)]
        public static extern int zbar_video_get_fd(IntPtr video);

        /// <summary>
        /// request a preferred size for the video image from the device.
        /// the request may be adjusted or completely ignored by the driver.
        /// </summary>
        /// <returns> 0 if successful or -1 if the video device is already
        /// initialized</returns>
        [DllImport(DllName)]
        public static extern int zbar_video_request_size(IntPtr video, uint width, uint height);

        /// <summary>
        /// request a preferred driver interface version for debug/testing.
        /// </summary>
        [DllImport(DllName)]
        public static extern int zbar_video_request_interface(IntPtr video, int version);

        /// <summary>
        /// request a preferred I/O mode for debug/testing.
        /// </summary>
        /// <remarks>You will get
        /// errors if the driver does not support the specified mode.
        /// @verbatim
        ///     0 = auto-detect
        ///     1 = force I/O using read()
        ///     2 = force memory mapped I/O using mmap()
        ///     3 = force USERPTR I/O (v4l2 only)
        /// @endverbatim
        /// must be called before zbar_video_open()
        /// </remarks>
        [DllImport(DllName)]
        public static extern int zbar_video_request_iomode(IntPtr video, int iomode);

        /// <summary>
        /// retrieve current output image width.
        /// </summary>
        /// <returns>the width or 0 if the video device is not open</returns>
        [DllImport(DllName)]
        public static extern int zbar_video_get_width(IntPtr video);

        /// <summary>
        /// retrieve current output image height.
        /// </summary>
        /// <returns>the height or 0 if the video device is not open</returns>
        [DllImport(DllName)]
        public static extern int zbar_video_get_height(IntPtr video);

        /// <summary>
        /// initialize video using a specific format for debug.
        /// use zbar_negotiate_format() to automatically select and initialize
        /// the best available format
        /// </summary>
        [DllImport(DllName)]
        public static extern int zbar_video_init(IntPtr video, uint format);

        /// <summary>
        /// start/stop video capture.
        /// all buffered images are retired when capture is disabled.
        /// </summary>
        /// <returns> 0 if successful or -1 if an error occurs</returns>
        [DllImport(DllName)]
        public static extern int zbar_video_enable(IntPtr video, int enable);

        /// <summary>
        /// retrieve next captured image.  blocks until an image is available.
        /// </summary>
        /// <returns> NULL if video is not enabled or an error occurs</returns>
        [DllImport(DllName)]
        public static extern IntPtr zbar_video_next_image(IntPtr video);

        #endregion Extern C functions

        #region Extern C functions

        /// <summary>
        /// symbol reference count manipulation.
        /// </summary>
        /// <remarks>
        /// increment the reference count when you store a new reference to the
        /// symbol.  decrement when the reference is no longer used.  do not
        /// refer to the symbol once the count is decremented and the
        /// containing image has been recycled or destroyed.
        /// the containing image holds a reference to the symbol, so you
        /// only need to use this if you keep a symbol after the image has been
        /// destroyed or reused.
        /// </remarks>
        [DllImport(DllName)]
        public static extern void zbar_symbol_ref(IntPtr symbol, int refs);

        /// <summary>
        /// retrieve type of decoded symbol.
        /// </summary>
        /// <returns> the symbol type</returns>
        [DllImport(DllName)]
        public static extern int zbar_symbol_get_type(IntPtr symbol);

        /// <summary>
        /// retrieve data decoded from symbol.
        /// </summary>
        /// <returns> the data string</returns>
        [DllImport(DllName)]
        public static extern IntPtr zbar_symbol_get_data(IntPtr symbol);

        /// <summary>
        /// retrieve length of binary data.
        /// </summary>
        /// <returns> the length of the decoded data</returns>
        [DllImport(DllName)]
        public static extern uint zbar_symbol_get_data_length(IntPtr symbol);

        /// <summary>
        /// retrieve a symbol confidence metric.
        /// </summary>
        /// <returns> an unscaled, relative quantity: larger values are better
        /// than smaller values, where "large" and "small" are application
        /// dependent.
        /// </returns>
        /// <remarks>expect the exact definition of this quantity to change as the
        /// metric is refined.  currently, only the ordered relationship
        /// between two values is defined and will remain stable in the future
        /// </remarks>
        [DllImport(DllName)]
        public static extern int zbar_symbol_get_quality(IntPtr symbol);

        /// <summary>
        /// retrieve current cache count.
        /// </summary>
        /// <remarks>when the cache is enabled for the
        /// image_scanner this provides inter-frame reliability and redundancy
        /// information for video streams.
        /// </remarks>
        /// <returns>
        /// < 0 if symbol is still uncertain.
        /// 0 if symbol is newly verified.
        /// > 0 for duplicate symbols
        /// </returns>
        [DllImport(DllName)]
        public static extern int zbar_symbol_get_count(IntPtr symbol);

        /// <summary>
        /// retrieve the number of points in the location polygon.  the
        /// location polygon defines the image area that the symbol was
        /// extracted from.
        /// </summary>
        /// <returns> the number of points in the location polygon</returns>
        /// <remarks>this is currently not a polygon, but the scan locations
        /// where the symbol was decoded</remarks>
        [DllImport(DllName)]
        public static extern uint zbar_symbol_get_loc_size(IntPtr symbol);

        /// <summary>
        /// retrieve location polygon x-coordinates.
        /// points are specified by 0-based index.
        /// </summary>
        /// <returns> the x-coordinate for a point in the location polygon.
        /// -1 if index is out of range</returns>
        [DllImport(DllName)]
        public static extern int zbar_symbol_get_loc_x(IntPtr symbol, uint index);

        /// <summary>
        /// retrieve location polygon y-coordinates.
        /// points are specified by 0-based index.
        /// </summary>
        /// <returns> the y-coordinate for a point in the location polygon.
        ///  -1 if index is out of range</returns>
        [DllImport(DllName)]
        public static extern int zbar_symbol_get_loc_y(IntPtr symbol, uint index);

        /// <summary>
        /// iterate the result set.
        /// </summary>
        /// <returns> the next result symbol, or
        /// NULL when no more results are available</returns>
        /// <remarks>Marked internal because it is used by the symbol iterators.</remarks>
        [DllImport(DllName)]
        internal static extern IntPtr zbar_symbol_next(IntPtr symbol);

        /// <summary>
        /// print XML symbol element representation to user result buffer.
        /// </summary>
        /// <remarks>see http://zbar.sourceforge.net/2008/barcode.xsd for the schema.</remarks>
        /// <param name="symbol">is the symbol to print</param>
        /// <param name="buffer"> is the inout result pointer, it will be reallocated
        /// with a larger size if necessary.</param>
        /// <param name="buflen">  is inout length of the result buffer.</param>
        /// <returns> the buffer pointer</returns>
        [DllImport(DllName)]
        public static extern IntPtr zbar_symbol_xml(IntPtr symbol, out IntPtr buffer, out uint buflen);

        #endregion Extern C functions

        #region Extern C functions

        /// <summary>
        /// Constructor
        /// </summary>
        [DllImport(DllName)]
        public static extern IntPtr zbar_image_scanner_create();

        /// <summary>
        /// Destructor.
        /// </summary>
        [DllImport(DllName)]
        public static extern void zbar_image_scanner_destroy(IntPtr scanner);

        /// <summary>
        /// data handler callback function.
        /// </summary>
        public delegate void zbar_image_data_handler(IntPtr image, IntPtr userdata);

        /// <summary>
        /// setup result handler callback.
        /// the specified function will be called by the scanner whenever
        /// new results are available from a decoded image.
        /// pass a NULL value to disable callbacks.
        /// </summary>
        /// <returns>the previously registered handler</returns>
        [DllImport(DllName)]
        public static extern zbar_image_data_handler zbar_image_scanner_set_data_handler(IntPtr scanner, zbar_image_data_handler handler, IntPtr userdata);

        /// <summary>
        /// set config for indicated symbology (0 for all) to specified value.
        /// </summary>
        /// <returns>0 for success, non-0 for failure (config does not apply to
        /// specified symbology, or value out of range)
        /// </returns>
        [DllImport(DllName)]
        public static extern int zbar_image_scanner_set_config(IntPtr scanner, int symbology, int config, int val);

        /// <summary>
        /// enable or disable the inter-image result cache (default disabled).
        /// mostly useful for scanning video frames, the cache filters
        /// duplicate results from consecutive images, while adding some
        /// consistency checking and hysteresis to the results.
        /// this interface also clears the cache
        /// </summary>
        [DllImport(DllName)]
        public static extern void zbar_image_scanner_enable_cache(IntPtr scanner, int enable);

        /// <summary>
        /// scan for symbols in provided image.  The image format must be
        /// "Y800" or "GRAY".
        /// </summary>
        /// <returns>
        ///  > 0 if symbols were successfully decoded from the image,
        /// 0 if no symbols were found or -1 if an error occurs
        /// </returns>
        [DllImport(DllName)]
        public static extern int zbar_scan_image(IntPtr scanner, IntPtr image);

        #endregion Extern C functions

        #region Extern C functions

        /// <summary>new image constructor.
        /// </summary>
        /// <returns>
        /// a new image object with uninitialized data and format.
        /// this image should be destroyed (using zbar_image_destroy()) as
        /// soon as the application is finished with it
        /// </returns>
        [DllImport(DllName)]
        public static extern IntPtr zbar_image_create();

        /// <summary>image destructor.  all images created by or returned to the
        /// application should be destroyed using this function.  when an image
        /// is destroyed, the associated data cleanup handler will be invoked
        /// if available
        /// </summary><remarks>
        /// make no assumptions about the image or the data buffer.
        /// they may not be destroyed/cleaned immediately if the library
        /// is still using them.  if necessary, use the cleanup handler hook
        /// to keep track of image data buffers
        /// </remarks>
        [DllImport(DllName)]
        public static extern void zbar_image_destroy(IntPtr image);

        /// <summary>image reference count manipulation.
        /// increment the reference count when you store a new reference to the
        /// image.  decrement when the reference is no longer used.  do not
        /// refer to the image any longer once the count is decremented.
        /// zbar_image_ref(image, -1) is the same as zbar_image_destroy(image)
        /// </summary>
        [DllImport(DllName)]
        public static extern void zbar_image_ref(IntPtr image, int refs);

        /// <summary>image format conversion.  refer to the documentation for supported
        /// image formats
        /// </summary>
        /// <returns> a new image with the sample data from the original image
        /// converted to the requested format.  the original image is
        /// unaffected.
        /// </returns>
        /// <remarks> the converted image size may be rounded (up) due to format
        /// constraints
        /// </remarks>
        [DllImport(DllName)]
        public static extern IntPtr zbar_image_convert(IntPtr image, uint format);

        /// <summary>image format conversion with crop/pad.
        /// if the requested size is larger than the image, the last row/column
        /// are duplicated to cover the difference.  if the requested size is
        /// smaller than the image, the extra rows/columns are dropped from the
        /// right/bottom.
        /// </summary>
        /// <returns> a new image with the sample data from the original
        /// image converted to the requested format and size.
        /// </returns>
        /// <remarks>the image is not scaled</remarks>
        [DllImport(DllName)]
        public static extern IntPtr zbar_image_convert_resize(IntPtr image, uint format, uint width, uint height);

        /// <summary>retrieve the image format.
        /// </summary>
        /// <returns> the fourcc describing the format of the image sample data</returns>
        [DllImport(DllName)]
        public static extern uint zbar_image_get_format(IntPtr image);

        /// <summary>retrieve a "sequence" (page/frame) number associated with this image.
        /// </summary>
        [DllImport(DllName)]
        public static extern uint zbar_image_get_sequence(IntPtr image);

        /// <summary>retrieve the width of the image.
        /// </summary>
        /// <returns> the width in sample columns</returns>
        [DllImport(DllName)]
        public static extern uint zbar_image_get_width(IntPtr image);

        /// <summary>retrieve the height of the image.
        /// </summary>
        /// <returns> the height in sample rows</returns>
        [DllImport(DllName)]
        public static extern uint zbar_image_get_height(IntPtr image);

        /// <summary>return the image sample data.  the returned data buffer is only
        /// valid until zbar_image_destroy() is called
        /// </summary>
        [DllImport(DllName)]
        public static extern IntPtr zbar_image_get_data(IntPtr image);

        /// <summary>return the size of image data.
        /// </summary>
        [DllImport(DllName)]
        public static extern uint zbar_image_get_data_length(IntPtr img);

        /// <summary>image_scanner decode result iterator.
        /// </summary>
        /// <returns> the first decoded symbol result for an image
        /// or NULL if no results are available
        /// </returns>
        [DllImport(DllName)]
        public static extern IntPtr zbar_image_first_symbol(IntPtr image);

        /// <summary>specify the fourcc image format code for image sample data.
        /// refer to the documentation for supported formats.
        /// </summary>
        /// <remarks> this does not convert the data!
        /// (see zbar_image_convert() for that)
        /// </remarks>
        [DllImport(DllName)]
        public static extern void zbar_image_set_format(IntPtr image, uint format);

        /// <summary>associate a "sequence" (page/frame) number with this image.
        /// </summary>
        [DllImport(DllName)]
        public static extern void zbar_image_set_sequence(IntPtr image, uint sequence_num);

        /// <summary>specify the pixel size of the image.
        /// </summary>
        /// <remarks>this does not affect the data!</remarks>
        [DllImport(DllName)]
        public static extern void zbar_image_set_size(IntPtr image, uint width, uint height);

        /// <summary>
        /// Cleanup handler callback for image data.
        /// </summary>
        public delegate void zbar_image_cleanup_handler(IntPtr image);

        /// <summary>specify image sample data.  when image data is no longer needed by
        /// the library the specific data cleanup handler will be called
        /// (unless NULL)
        /// </summary>
        /// <remarks>application image data will not be modified by the library</remarks>
        [DllImport(DllName)]
        public static extern void zbar_image_set_data(IntPtr image, IntPtr data, uint data_byte_length, zbar_image_cleanup_handler cleanup_handler);

        /// <summary>built-in cleanup handler.
        /// passes the image data buffer to free()
        /// </summary>
        [DllImport(DllName)]
        public static extern void zbar_image_free_data(IntPtr image);

        /// <summary>associate user specified data value with an image.
        /// </summary>
        [DllImport(DllName)]
        public static extern void zbar_image_set_userdata(IntPtr image, IntPtr userdata);

        /// <summary>return user specified data value associated with the image.
        /// </summary>
        [DllImport(DllName)]
        public static extern IntPtr zbar_image_get_userdata(IntPtr image);

        #endregion Extern C functions
    }
}