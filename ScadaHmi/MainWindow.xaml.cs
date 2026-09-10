using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using ScadaHmi.Comm;
using ScadaHmi.Services;

namespace ScadaHmi
{
    public partial class MainWindow : Window
    {
        // 刹车：谁持有 _cts，谁就能喊停。
        private readonly CancellationTokenSource _cts = new();

        // 传送带：容量 100。
        private readonly Channel<Dictionary<string, double>> _channel =
            Channel.CreateBounded<Dictionary<string, double>>(100);

        public MainWindow()
        {
            InitializeComponent();
            StartPipeline();
        }

        private void StartPipeline()
        {
            var driver = new MockDriver();

            var producer = new AcquisitionService(driver, _channel.Writer);
            var consumer = new DataProcessor(_channel.Reader, OnData);

            // 两个都丢后台线程，别堵 UI。
            _ = Task.Run(() => producer.RunAsync(_cts.Token));
            _ = Task.Run(() => consumer.RunAsync(_cts.Token));
        }

        // ★ 这是在后台线程上执行的！
        private void OnData(Dictionary<string, double> data)
        {
            if (!data.ContainsKey("温度")) return;

            System.Diagnostics.Debug.WriteLine($"[处理] 温度 = {data["温度"]:F1}");

            // ★ 后台线程不能直接碰 UI 控件，Dispatcher.Invoke 送它回 UI 线程。
            Dispatcher.Invoke(() =>
            {
                Title = $"实时温度: {data["温度"]:F1} ℃";
            });
        }

        // 关窗口踩刹车，两个后台循环优雅退出。
        protected override void OnClosed(EventArgs e)
        {
            _cts.Cancel();
            _cts.Dispose();
            base.OnClosed(e);
        }
    }
}