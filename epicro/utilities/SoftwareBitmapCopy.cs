using System.Windows.Media.Imaging;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Media;
using SharpDX.DXGI;
using Composition.WindowsRuntimeHelpers;
using System.Threading.Tasks;
using System.Windows;
using System;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using System.IO;

namespace epicro.Utilites
{
    public static class SoftwareBitmapCopy
    {
        public static async Task<BitmapSource> CaptureSingleFrameAsync(IntPtr hwnd)
        {
            var item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item == null) return null;

            var d3dDevice = Direct3D11Helper.CreateDevice(); // 기존 샘플에 있는 SharpDX Device 생성 함수 사용
            var device = Direct3D11Helper.CreateSharpDXDevice(d3dDevice);

            var framePool = Direct3D11CaptureFramePool.Create(
                d3dDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                item.Size);

            var session = framePool.CreateCaptureSession(item);

            TaskCompletionSource<BitmapSource> tcs = new TaskCompletionSource<BitmapSource>();

            framePool.FrameArrived += (s, e) =>
            {
                try
                {
                    using (var frame = s.TryGetNextFrame())
                    using (var texture = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface))
                    {
                        var bitmap = ConvertToBitmapSource(texture, device);
                        tcs.SetResult(bitmap);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    session.Dispose();
                    framePool.Dispose();
                }
            };

            session.StartCapture();
            return await tcs.Task;
        }

        public static BitmapSource ConvertToBitmapSource(SharpDX.Direct3D11.Texture2D texture, SharpDX.Direct3D11.Device device)
        {
            using (var surface = texture.QueryInterface<SharpDX.DXGI.Surface>())
            {
                var description = texture.Description;
                description.CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read;
                description.Usage = SharpDX.Direct3D11.ResourceUsage.Staging;
                description.BindFlags = SharpDX.Direct3D11.BindFlags.None;
                description.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None;

                var width = description.Width;
                var height = description.Height;
                var stride = width * 4;

                using (var stagingTexture = new SharpDX.Direct3D11.Texture2D(device, description))
                {
                    device.ImmediateContext.CopyResource(texture, stagingTexture);

                    var dataStream = new SharpDX.DataStream(height * stride, true, true);

                    var dataBox = device.ImmediateContext.MapSubresource(
                        stagingTexture,
                        0,
                        SharpDX.Direct3D11.MapMode.Read,
                        SharpDX.Direct3D11.MapFlags.None);

                    for (int y = 0; y < height; y++)
                    {
                        var srcPtr = dataBox.DataPointer + y * dataBox.RowPitch;
                        dataStream.Position = y * stride;
                        dataStream.WriteRange(srcPtr, stride);
                    }

                    device.ImmediateContext.UnmapSubresource(stagingTexture, 0);

                    var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                    bitmap.WritePixels(
                        new Int32Rect(0, 0, width, height),
                        dataStream.DataPointer,
                        (int)dataStream.Length,
                        stride);

                    dataStream.Dispose();
                    return bitmap;

                }
            }
        }
    }
}
