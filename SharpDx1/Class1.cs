using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using MapFlags = SharpDX.DXGI.MapFlags;
using Rectangle = System.Drawing.Rectangle;
using Resource = SharpDX.DXGI.Resource;

namespace SharpDx1
{
    class Class1
    {
        public Image<Gray, double> LatestImage
        {
            get;
            private set;
        }

        public void Method()
        {
            uint numAdapter = 0; // # of graphics card adapter
            uint numOutput = 0; // # of output device (i.e. monitor)

            // create device and factory
            SharpDX.Direct3D11.Device device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware);
            Factory1 factory = new Factory1();

            var width = factory.Adapters1[numAdapter].Outputs[numOutput].Description.DesktopBounds.Width;
            var height = factory.Adapters1[numAdapter].Outputs[numOutput].Description.DesktopBounds.Height;

            // creating CPU-accessible texture resource
            SharpDX.Direct3D11.Texture2DDescription texdes = new SharpDX.Direct3D11.Texture2DDescription();
            texdes.CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read;
            texdes.BindFlags = SharpDX.Direct3D11.BindFlags.None;
            texdes.Format = Format.B8G8R8A8_UNorm;
            texdes.Height = height;
            texdes.Width = width;
            texdes.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None;
            texdes.MipLevels = 1;
            texdes.ArraySize = 1;
            texdes.SampleDescription.Count = 1;
            texdes.SampleDescription.Quality = 0;
            texdes.Usage = SharpDX.Direct3D11.ResourceUsage.Staging;
            SharpDX.Direct3D11.Texture2D screenTexture = new SharpDX.Direct3D11.Texture2D(device, texdes);

            // duplicate output stuff
            Output1 output = new Output1(factory.Adapters1[numAdapter].Outputs[numOutput].NativePointer);
            OutputDuplication duplicatedOutput = output.DuplicateOutput(device);
            Resource screenResource = null;
            SharpDX.DataStream dataStream;
            Surface screenSurface;

            int i = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                i++;
                // try to get duplicated frame within given time
                try
                {
                    OutputDuplicateFrameInformation duplicateFrameInformation;
                    duplicatedOutput.AcquireNextFrame(1000, out duplicateFrameInformation, out screenResource);
                }
                catch (SharpDX.SharpDXException e)
                {
                    if (e.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                    {
                        // this has not been a successful capture
                        // thanks @Randy
                        i--;

                        // keep retrying
                        continue;
                    }
                    else
                    {
                        throw e;
                    }
                }

                // copy resource into memory that can be accessed by the CPU
                device.ImmediateContext.CopyResource(screenResource.QueryInterface<Texture2D>(), screenTexture);

                var outputFilename = string.Format("{0}.bmp", i);
                GetValue2(width, height, device, screenTexture, outputFilename);

                screenResource.Dispose();
                duplicatedOutput.ReleaseFrame();

                // print how many frames we could process within the last second
                // note that this also depends on how often windows will &gt;need&lt; to redraw the interface
                if (sw.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(i + "fps");
                    sw.Reset();
                    sw.Start();
                    i = 0;
                }
            }
        }

        private static void GetValue1(int width, int height, SharpDX.Direct3D11.Device device, Texture2D screenTexture, string outputFilename)
        {
            Surface screenSurface;
            DataStream dataStream;
            // cast from texture to surface, so we can access its bytes
            screenSurface = screenTexture.QueryInterface<Surface>();

            // map the resource to access it
            screenSurface.Map(MapFlags.Read, out dataStream);

            // seek within the stream and read one byte
            dataStream.Position = 4;
            dataStream.ReadByte();

            var bitmap = getImageFromDXStream(width, height, dataStream);

            //var outputFilename = string.Format("{0}.bmp", i);
            //bitmap.Save(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, outputFilename)));

            // free resources
            dataStream.Close();
            screenSurface.Unmap();
            screenSurface.Dispose();
        }

        private void GetValue2(int width, int height, SharpDX.Direct3D11.Device device, Texture2D screenTexture, string outputFilename)
        {
            // Get the desktop capture texture
            var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

            // Create Drawing.Bitmap
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var boundsRect = new Rectangle(0, 0, width, height);

                // Copy pixels from screen capture Texture to GDI bitmap
                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                var sourcePtr = mapSource.DataPointer;
                var destPtr = mapDest.Scan0;
                for (int y = 0; y < height; y++)
                {
                    // Copy a single line 
                    Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                    // Advance pointers
                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                // Release source and dest locks
                bitmap.UnlockBits(mapDest);
                device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                // Save the output
                //bitmap.Save(outputFilename);

                using (var emguImage = new Image<Gray, byte>(bitmap))
                //using (var cannyImage = emguImage.Canny(50, 300))
                {
                    //cannyImage.Save(outputFilename);

                    //var oldImage = this.LatestImage;

                    this.LatestImage = emguImage.Convert<Gray, Double>();

                    //if (oldImage != null)
                    //    oldImage.Dispose();

                }
            }
        }

        static Bitmap getImageFromDXStream(int Width, int Height, SharpDX.DataStream stream)
        {
            var b = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            var BoundsRect = new Rectangle(0, 0, Width, Height);
            BitmapData bmpData = b.LockBits(BoundsRect, ImageLockMode.WriteOnly, b.PixelFormat);
            int bytes = bmpData.Stride * b.Height;

            var rgbValues = new byte[bytes * 4];

            // copy bytes from the surface's data stream to the bitmap stream
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    stream.Seek(y * (Width * 4) + x * 4, System.IO.SeekOrigin.Begin);
                    stream.Read(rgbValues, y * (Width * 4) + x * 4, 4);
                }
            }

            Marshal.Copy(rgbValues, 0, bmpData.Scan0, bytes);
            b.UnlockBits(bmpData);
            return b;
        }
    }
}
