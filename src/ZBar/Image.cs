using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ZBar
{
    /// <summary>
    /// Representation of an image in ZBar
    /// </summary>
    public class Image : IDisposable
    {
        /// <summary>
        /// Handle to unmanaged ressource
        /// </summary>
        private IntPtr handle = IntPtr.Zero;

        /// <summary>
        /// Create a new image from a pointer to an unmanaged resource
        /// </summary>
        /// <remarks>This resource will be managed by this Image instance.</remarks>
        /// <param name="handle">
        /// A <see cref="IntPtr"/> to unmananged ZBar image.
        /// </param>
        /// <param name="incRef">
        /// Whether or not to increment the reference counter.
        /// </param>
        internal Image(IntPtr handle, bool incRef)
        {
            this.handle = handle;
            if (this.handle == IntPtr.Zero)
                throw new Exception("Can't create an image from a null pointer!");
            //If we must increment the reference counter here
            if (incRef)
                NativeZBar.zbar_image_ref(this.handle, 1);
        }

        /// <summary>
        /// Create/allocate a new uninitialized image
        /// </summary>
        /// <remarks>
        /// Be aware that this image is NOT initialized, allocated.
        /// And you must set width, height, format, data etc...
        /// </remarks>
        public Image()
        {
            this.handle = NativeZBar.zbar_image_create();
            if (this.handle == IntPtr.Zero)
                throw new Exception("Failed to create new image!");
        }

        /// <summary>
        /// Create image from an instance of System.Drawing.Image
        /// </summary>
        /// <param name="image">
        /// Image to convert to ZBar.Image
        /// </param>
        /// <remarks>
        /// The converted image is in RGB3 format, so it should be converted using Image.Convert()
        /// before it is scanned, as ZBar only reads images in GREY/Y800
        /// </remarks>
        public Image(System.Drawing.Image image) : this()
        {
            Byte[] data = new byte[image.Width * image.Height * 3];
            //Convert the image to RBG3
            using (Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.PageUnit = GraphicsUnit.Pixel;
                    g.DrawImageUnscaled(image, 0, 0);
                }
                // Vertically flip image as we are about to store it as BMP on a memory stream below
                // This way we don't need to worry about BMP being upside-down when copying to byte array
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Bmp);
                    ms.Seek(54, SeekOrigin.Begin);
                    ms.Read(data, 0, data.Length);
                }
            }
            //Set the data
            this.Data = data;
            this.Width = (uint)image.Width;
            this.Height = (uint)image.Height;
            this.Format = FourCC('R', 'G', 'B', '3');
        }

        /// <value>
        /// Get a pointer to the unmanaged image resource.
        /// </value>
        internal IntPtr Handle
        {
            get
            {
                return this.handle;
            }
        }

        #region Wrapper methods

        /// <summary>
        /// Convert bitmap
        /// </summary>
        /// <returns>
        /// A <see cref="System.Drawing.Bitmap"/> representation of this image
        /// </returns>
        public System.Drawing.Bitmap ToBitmap()
        {
            Bitmap img = new Bitmap((int)Width, (int)Height, PixelFormat.Format24bppRgb);
            //TODO: Test and optimize this :)
            using (Image rgb = Convert(FourCC('R', 'G', 'B', '3')))
            {
                byte[] data = rgb.Data;
                BitmapData bdata = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),
                                                ImageLockMode.WriteOnly,
                                                PixelFormat.Format24bppRgb);
                Marshal.Copy(data, 0, bdata.Scan0, data.Length);
                img.UnlockBits(bdata);
            }

            return img;
        }

        /// <value>
        /// Get/set the width of the image, doesn't affect the data
        /// </value>
        public uint Width
        {
            get
            {
                return NativeZBar.zbar_image_get_width(this.handle);
            }
            set
            {
                NativeZBar.zbar_image_set_size(this.handle, value, this.Height);
            }
        }

        /// <value>
        /// Get/set the height of the image, doesn't affect the data
        /// </value>
        public uint Height
        {
            get
            {
                return NativeZBar.zbar_image_get_height(this.handle);
            }
            set
            {
                NativeZBar.zbar_image_set_size(this.handle, this.Width, value);
            }
        }

        /// <value>
        /// Get/set the fourcc image format code for image sample data.
        /// </value>
        /// <remarks>
        /// Chaning this doesn't affect the data.
        /// See Image.FourCC for how to get the fourCC code.
        /// For information on supported format see:
        /// http://sourceforge.net/apps/mediawiki/zbar/index.php?title=Supported_image_formats
        /// </remarks>
        public uint Format
        {
            get
            {
                return NativeZBar.zbar_image_get_format(this.handle);
            }
            set
            {
                NativeZBar.zbar_image_set_format(this.handle, value);
            }
        }

        /// <value>
        /// Get/set a "sequence" (page/frame) number associated with this image.
        /// </value>
        public uint SequenceNumber
        {
            get
            {
                return NativeZBar.zbar_image_get_sequence(this.handle);
            }
            set
            {
                NativeZBar.zbar_image_set_sequence(this.handle, value);
            }
        }

        /// <value>
        /// Get/set the data associated with this image
        /// </value>
        /// <remarks>This method copies that data, using Marshal.Copy.</remarks>
        public byte[] Data
        {
            get
            {
                IntPtr pData = NativeZBar.zbar_image_get_data(this.handle);
                if (pData == IntPtr.Zero)
                    throw new Exception("Image data pointer is null!");
                uint length = NativeZBar.zbar_image_get_data_length(this.handle);
                byte[] data = new byte[length];
                Marshal.Copy(pData, data, 0, (int)length);
                return data;
            }
            set
            {
                IntPtr data = Marshal.AllocHGlobal(value.Length);
                Marshal.Copy(value, 0, data, value.Length);
                NativeZBar.zbar_image_set_data(this.handle, data, (uint)value.Length, Image.CleanupHandler);
            }
        }

        /// <summary>
        /// Cleanup handler, by holding the reference statically the delegate won't be released
        /// </summary>
        private static NativeZBar.zbar_image_cleanup_handler CleanupHandler = new NativeZBar.zbar_image_cleanup_handler(ReleaseAllocatedUnmanagedMemory);

        private static void ReleaseAllocatedUnmanagedMemory(IntPtr image)
        {
            IntPtr pData = NativeZBar.zbar_image_get_data(image);
            if (pData != IntPtr.Zero)
                Marshal.FreeHGlobal(pData);
        }

        /// <value>
        /// Get ImageScanner decode result iterator.
        /// </value>
        public IEnumerable<Symbol> Symbols
        {
            get
            {
                IntPtr pSym = NativeZBar.zbar_image_first_symbol(this.handle);
                while (pSym != IntPtr.Zero)
                {
                    yield return new Symbol(pSym);
                    pSym = NativeZBar.zbar_symbol_next(pSym);
                }
            }
        }

        /// <summary>
        /// Image format conversion. refer to the documentation for supported image formats
        /// </summary>
        /// <remarks>
        /// The converted image size may be rounded (up) due to format constraints.
        /// See Image.FourCC for how to get the fourCC code.
        /// </remarks>
        /// <param name="format">
        /// FourCC format to convert to.
        /// </param>
        /// <returns>
        /// A new <see cref="Image"/> with the sample data from the original image converted to the requested format.
        /// The original image is unaffected.
        /// </returns>
        public Image Convert(uint format)
        {
            IntPtr img = NativeZBar.zbar_image_convert(this.handle, format);
            if (img == IntPtr.Zero)
                throw new Exception("Conversation failed!");
            return new Image(img, false);
        }

        /// <summary>
        /// Get FourCC code from four chars
        /// </summary>
        /// <remarks>
        /// See FourCC.org for more information on FourCC.
        /// For information on format supported by zbar see:
        /// http://sourceforge.net/apps/mediawiki/zbar/index.php?title=Supported_image_formats
        /// </remarks>
        public static uint FourCC(char c0, char c1, char c2, char c3)
        {
            return (uint)c0 | ((uint)c1) << 8 | ((uint)c2) << 16 | ((uint)c3) << 24;
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
            if (this.handle != IntPtr.Zero)
            {
                NativeZBar.zbar_image_destroy(this.handle);
                this.handle = IntPtr.Zero;
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
        ~Image()
        {
            //Dispose this object, but do NOT release finalizable objects, we don't know in which order
            //these are release and they may already be finalized.
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

    }
}