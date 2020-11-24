using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace ConsoleApp2
{
    public class Program
    {
        public static async Task Main()
        {
            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            var processId = 13736; // vscode or devenv as an example

            for (int i = 0; i < 100; i++)
            {
                using (var img = Capture(Process.GetProcessById(processId).MainWindowHandle))
                {
                    using (var vscode = await GetSoftwareBitmapAsync(img))
                    {
                        try
                        {
                            var results = await engine.RecognizeAsync(vscode);
                        }
                        catch (System.ObjectDisposedException e)
                        {
                            // Does not break here, it's an external call
                            throw;
                        }
                    }
                }
            }
        }

        protected static async Task<SoftwareBitmap> GetSoftwareBitmapAsync(Bitmap bitmap)
        {
            try
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    bitmap.Save(stream.AsStream(), ImageFormat.Jpeg);
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.JpegDecoderId, stream);
                    return await decoder.GetSoftwareBitmapAsync();
                }
            }
            catch (ObjectDisposedException e)
            {
                // Does not break here, it's an external call
                throw;
            }
        }

        public static Bitmap Capture(IntPtr handle, Rectangle? region = null)
        {
            Rectangle bounds = region ?? GetDimensions(handle);

            using (Graphics source = Graphics.FromHwnd(handle))
            {
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, source);

                using (Graphics destination = Graphics.FromImage(bitmap))
                {
                    IntPtr deviceContextSource = source.GetHdc();
                    IntPtr deviceContextDestination = destination.GetHdc();

                    NativeMethods.BitBlt(
                        deviceContextDestination, 0, 0, bounds.Width, bounds.Height,
                        deviceContextSource, bounds.Left, bounds.Top,
                        TernaryRasterOperations.SRCCOPY);

                    destination.ReleaseHdc(deviceContextDestination);
                    source.ReleaseHdc(deviceContextSource);

                    return bitmap;
                }
            }
        }


        public static Rectangle GetDimensions(IntPtr handle)
        {
            NativeMethods.GetClientRect(handle, out RECT rect);
            var rectangle = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            return rectangle;
        }

    }



    // You can define other methods, fields, classes and namespaces here


    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public enum TernaryRasterOperations : uint
    {
        SRCCOPY = 0x00CC0020,
        SRCPAINT = 0x00EE0086,
        SRCAND = 0x008800C6,
        SRCINVERT = 0x00660046,
        SRCERASE = 0x00440328,
        NOTSRCCOPY = 0x00330008,
        NOTSRCERASE = 0x001100A6,
        MERGECOPY = 0x00C000CA,
        MERGEPAINT = 0x00BB0226,
        PATCOPY = 0x00F00021,
        PATPAINT = 0x00FB0A09,
        PATINVERT = 0x005A0049,
        DSTINVERT = 0x00550009,
        BLACKNESS = 0x00000042,
        WHITENESS = 0x00FF0062,
        CAPTUREBLT = 0x40000000
    }


    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        internal static extern bool BitBlt(IntPtr hdcDest, int nxDest, int nyDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);
    }
}