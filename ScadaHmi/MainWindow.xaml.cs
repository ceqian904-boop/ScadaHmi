using System;
using System.Windows;
using ScadaHmi.Comm;
using ScadaHmi.Services;

namespace ScadaHmi
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // ---- 今天的全部接线：就这 4 行 ----
            var svc = new PollingService(new MockDriver());          // 假设备塞给轮询服务
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)   // 每秒一次 timer.Interval：就是定时器的“闹钟间隔”
            };                                       //TimeSpan.FromSeconds(1)：就是告诉它“间隔 1 秒钟”。（C# 里表示时间，直接用 FromSeconds(秒数) 就行）。
            timer.Tick += (s, e) => svc.PollOnce();
            timer.Start();
        }
    }
}
