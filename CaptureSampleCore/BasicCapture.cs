//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using Composition.WindowsRuntimeHelpers;
using System;
using System.Diagnostics;
using System.Linq;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace CaptureSampleCore
{
    public class BasicCapture : IDisposable
    {
        private GraphicsCaptureItem item;
        private Direct3D11CaptureFramePool framePool;
        private GraphicsCaptureSession session;
        private SizeInt32 lastSize;

        private IDirect3DDevice device;
        private SharpDX.Direct3D11.Device d3dDevice;
        private SharpDX.DXGI.SwapChain1 swapChain;
        private readonly object _textureLock = new object();

        public SharpDX.Direct3D11.Texture2D LatestFrameTexture { get; private set; }

        public BasicCapture(IDirect3DDevice d, GraphicsCaptureItem i)
        {
            item = i;
            device = d;
            d3dDevice = Direct3D11Helper.CreateSharpDXDevice(device);

            var dxgiFactory = new SharpDX.DXGI.Factory2();
            var description = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = item.Size.Width,
                Height = item.Size.Height,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                AlphaMode = SharpDX.DXGI.AlphaMode.Premultiplied,
                Flags = SharpDX.DXGI.SwapChainFlags.None
            };
            swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, d3dDevice, ref description);

            framePool = Direct3D11CaptureFramePool.Create(
                device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                i.Size);
            session = framePool.CreateCaptureSession(i);
            lastSize = i.Size;

            framePool.FrameArrived += OnFrameArrived;
        }

        public void Dispose()
        {
            StopCapture();
            d3dDevice?.Dispose();
            d3dDevice = null;
        }

        public void StartCapture()
        {
            Debug.WriteLine("StartCapture 호출됨");
            session.StartCapture();
        }
        public void StopCapture()
        {
            Debug.WriteLine("StopCapture 호출됨");

            session?.Dispose();  // 캡처 세션 해제
            framePool?.Dispose();  // 프레임 풀 해제
            swapChain?.Dispose();  // 스왑체인 해제

            session = null;
            framePool = null;
            swapChain = null;

            // 마지막으로 보관 중인 GPU 텍스처 해제 (메모리 누수 방지)
            lock (_textureLock)
            {
                LatestFrameTexture?.Dispose();
                LatestFrameTexture = null;
            }
        }

        public ICompositionSurface CreateSurface(Compositor compositor)
        {
            return compositor.CreateCompositionSurfaceForSwapChain(swapChain);
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            //Debug.WriteLine($"[FrameArrived] New frame received at {DateTime.Now}");
            var newSize = false;

            using (var frame = sender.TryGetNextFrame())
            {
                if (frame.ContentSize.Width != lastSize.Width ||
                    frame.ContentSize.Height != lastSize.Height)
                {
                    // The thing we have been capturing has changed size.
                    // We need to resize the swap chain first, then blit the pixels.
                    // After we do that, retire the frame and then recreate the frame pool.
                    newSize = true;
                    lastSize = frame.ContentSize;
                    swapChain.ResizeBuffers(
                        2,
                        lastSize.Width,
                        lastSize.Height,
                        SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        SharpDX.DXGI.SwapChainFlags.None);
                }

                using (var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0))
                {
                    var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface);
                    d3dDevice.ImmediateContext.CopyResource(bitmap, backBuffer);
                    // GetSafeTextureCopy()와 동일한 락 사용 → 레이스 컨디션 방지
                    lock (_textureLock)
                    {
                        LatestFrameTexture?.Dispose();
                        LatestFrameTexture = bitmap;
                    }
                }

            } // Retire the frame.

            swapChain.Present(0, SharpDX.DXGI.PresentFlags.None);

            if (newSize)
            {
                framePool.Recreate(
                    device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    lastSize);
            }
        }
        public Texture2D GetSafeTextureCopy()
        {
            try
            {
                lock (_textureLock)
                {
                    if (LatestFrameTexture == null)
                    {
                        Debug.WriteLine("[DEBUG] LatestFrameTexture is null");
                        return null;
                    }

                    if (LatestFrameTexture.IsDisposed)
                    {
                        Debug.WriteLine("[DEBUG] LatestFrameTexture is already disposed");
                        return null;
                    }

                    Texture2DDescription desc;

                    try
                    {
                        desc = LatestFrameTexture.Description;
                    }
                    catch(Exception ex) 
                    {
                        Debug.WriteLine($"[ERROR] Description 가져오는 중 예외 발생: {ex.Message}");
                        return null;
                    }

                    var staging = new Texture2D(d3dDevice, new Texture2DDescription
                    {
                        Width = desc.Width,
                        Height = desc.Height,
                        Format = desc.Format,
                        Usage = ResourceUsage.Staging,
                        CpuAccessFlags = CpuAccessFlags.Read,
                        BindFlags = BindFlags.None,
                        MipLevels = 1,
                        ArraySize = 1,
                        SampleDescription = new SampleDescription(1, 0),
                        OptionFlags = ResourceOptionFlags.None
                    });

                    try
                    {
                        d3dDevice.ImmediateContext.CopyResource(LatestFrameTexture, staging);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Texture 복사 중 예외 발생: {ex.Message}");
                        staging.Dispose();
                        return null;
                    }

                    return staging; // ← 안전하게 복사된 텍스처 반환
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"[FATAL] GetSafeTextureCopy 예외 발생: {ex.Message}");
                return null;
            }
        }
    }

}
