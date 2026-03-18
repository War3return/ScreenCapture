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
using System.Windows;
using System.Windows.Threading;
using Windows.System;

namespace epicro
{
    public partial class App : Application
    {
        public App()
        {
            _controller = CoreMessagingHelper.CreateDispatcherQueueControllerForCurrentThread();

            // UI 스레드 예외
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            // 백그라운드 스레드 예외 (BeltMacro 등)
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            // Task 미관찰 예외 (async void fire-and-forget 등)
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Exit += OnExit;
        }

        private DispatcherQueueController _controller;

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[Unhandled] {e.Exception}");
            MessageBox.Show(
                $"예기치 않은 오류가 발생했습니다.\n\n{e.Exception.Message}",
                "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"[Fatal] {ex}");
            // 백그라운드 스레드 예외 — 앱이 종료되기 전에 메시지 표시
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"백그라운드 오류가 발생했습니다.\n\n{ex?.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void OnUnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();  // 앱 크래시 방지
            System.Diagnostics.Debug.WriteLine($"[Task] {e.Exception?.InnerException?.Message}");
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            _controller?.ShutdownQueueAsync();
        }
    }
}
