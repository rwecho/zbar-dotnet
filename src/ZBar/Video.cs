using System;
using System.Runtime.InteropServices;

/// <summary>
/// ZBar is a library for reading bar codes from video streams
/// </summary>
namespace ZBar
{
    /// <summary>
    /// Mid-level video source abstraction. captures images from a video device
    /// </summary>
    public class Video : IDisposable
    {
        private IntPtr video = IntPtr.Zero;
        private bool enabled = false;

        /// <summary>
        /// Create a video instance
        /// </summary>
        public Video()
        {
            this.video = NativeZBar.zbar_video_create();
            if (this.video == IntPtr.Zero)
                throw new Exception("Didn't create an unmanaged Video instance, don't know what happened.");
        }

        #region Wrapper methods

        /// <summary>
        /// Open and probe a video device.
        /// </summary>
        /// <param name="device">
        /// The device specified by platform specific unique name (v4l device node path in *nix eg "/dev/video", DirectShow DevicePath property in windows).
        /// </param>
        public void Open(string device)
        {
            if (NativeZBar.zbar_video_open(this.video, device) != 0)
            {
                throw new ZBarException(this.video);
            }
        }

        /// <summary>
        /// Close the video device
        /// </summary>
        public void Close()
        {
            if (NativeZBar.zbar_video_open(this.video, null) != 0)
            {
                throw new ZBarException(this.video);
            }
        }

        /// <value>
        /// Start/stop video capture, must be called after Open()
        /// </value>
        public bool Enabled
        {
            set
            {
                if (NativeZBar.zbar_video_enable(this.video, value ? 1 : 0) != 0)
                    throw new ZBarException(this.video);
                this.enabled = value;
            }
            get
            {
                return this.enabled;
            }
        }

        /// <value>
        /// Get output handle width
        /// </value>
        public int Width
        {
            get
            {
                int width = NativeZBar.zbar_video_get_width(this.video);
                if (width == 0)
                    throw new Exception("Video device not opened!");
                return width;
            }
        }

        /// <value>
        /// Get output image height
        /// </value>
        public int Height
        {
            get
            {
                int height = NativeZBar.zbar_video_get_height(this.video);
                if (height == 0)
                    throw new Exception("Video device not opened!");
                return height;
            }
        }

        /// <summary>
        /// Request a other output image size
        /// </summary>
        /// <remarks>
        /// The request may be adjusted or completely ignored by the driver.
        /// </remarks>
        /// <param name="width">
        /// Desired output width
        /// </param>
        /// <param name="height">
        /// Desired output height
        /// </param>
        public void RequestSize(uint width, uint height)
        {
            if (NativeZBar.zbar_video_request_size(this.video, width, height) != 0)
                throw new ZBarException(this.video);
        }

        /// <summary>
        /// Retrieve next captured image
        /// </summary>
        /// <remarks>This method blocks untill an image have been captured.</remarks>
        /// <returns>
        /// A <see cref="Image"/> representating the next image captured
        /// </returns>
        public Image NextFrame()
        {
            IntPtr image = NativeZBar.zbar_video_next_image(this.video);
            if (image == IntPtr.Zero)
                throw new ZBarException(this.video);
            return new Image(image, false); //I don't think we need to increment reference count here..
        }

        #endregion Wrapper methods

        #region IDisposable Implementation

        //This pattern for implementing IDisposable is recommended by:
        //Framework Design Guidelines, 2. Edition, Section 9.4

        /// <summary>
        /// Dispose this object
        /// </summary>
        /// <remarks>
        /// This boolean disposing parameter here ensures that objects with a finalizer is not disposed,
        /// this is method is invoked from the finalizer. Do overwrite, and call, this method in base
        /// classes if you use any unmanaged resources.
        /// </remarks>
        /// <param name="disposing">
        /// A <see cref="System.Boolean"/> False if called from the finalizer, True if called from Dispose.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.video != IntPtr.Zero)
            {
                NativeZBar.zbar_video_destroy(this.video);
                this.video = IntPtr.Zero;
            }
            if (disposing)
            {
                //Release finalizable resources, at the moment none.
            }
        }

        /// <summary>
        /// Release resources held by this object
        /// </summary>
        public void Dispose()
        {
            //We're disposing this object and can release objects that are finalizable
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalize this object
        /// </summary>
        ~Video()
        {
            //Dispose this object, but do NOT release finalizable objects, we don't know in which order
            //these are release and they may already be finalized.
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

    }
}