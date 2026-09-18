using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaHmi.Communication;
using ScadaHmi.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ScadaHmi.ViewModels
{
    public partial class RealTimeViewModel : ObservableObject
    {
        //创建新的
        public readonly AcquisitionHost _host;



        // ===== 设备驱动。以后写真的 Modbus 驱动，只改这一行就行 =====
        //private readonly ICommDriver _driver = new MockDriver();

        // ===== 管道家当：只在 Start 里创建，所以是可空的 =====
        // cts 取消过就不能再用，Channel 一旦 Complete 也不能再写，
        // 所以每启动一次，这两样都得重建一套新的。
        //private CancellationTokenSource? _cts;

        // ===== 三个采集值：[ObservableProperty] 自动生成带通知的属性 =====
        [ObservableProperty]
        private double _temperature;

        [ObservableProperty]
        private double _pressure;

        [ObservableProperty]
        private double _flow;

        // ===== 运行状态（你命名习惯：小写 i）=====
        // 它一变，下面两个按钮的可用性会自动跟着变（生成器帮你接的线）。
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        private bool _isconnected;

        public RealTimeViewModel(ICommDriver driver)
        {
            // 打开窗口是"待机"状态，点【启动】才开始采集。
            _host = new AcquisitionHost(driver, UpdateData);
        }

        // ===== 命令：方法名 Start → XAML 绑 StartCommand =====
        [RelayCommand(CanExecute = nameof(CanStart))]
        private async Task StartAsync()
        {
            await _host.StartAsync();
            Isconnected = true;
        }

        // 已经在跑就不让再点（按钮会自动变灰，同时挡住连点起两套管道）
        private bool CanStart() => !Isconnected;

        [RelayCommand(CanExecute = nameof(CanStop))]
        private async Task StopAsync()
        {
            await _host.StopAsync();
            Isconnected = false;
        }

        private bool CanStop() => Isconnected;

        /// <summary>
        /// 关窗口时调用：确保后台两个循环不会在窗口没了之后还接着跑。
        /// </summary>
        public void Shutdown()
        {
            _host.Dispose();
            Isconnected = false;
        }

        // 后台线程调用。改 double 属性 WPF 自动送回 UI 线程，
        // 所以 Day3 的 Dispatcher.Invoke 不需要了。
        public void UpdateData(Dictionary<string, double> data)
        {
            if (data.TryGetValue("温度", out var t)) Temperature = t;
            if (data.TryGetValue("压力", out var p)) Pressure = p;
            if (data.TryGetValue("流量", out var f)) Flow = f;
        }
    }
}
