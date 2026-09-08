using System;
using System.Windows;
using ScadaHmi.Comm;
using ScadaHmi.Services;
using ScadaHmi.Learning;   // 引用刚才的 demo（第 4 步用）

namespace ScadaHmi
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // ★ 先运行一下学习 demo，看看事件推送效果
            EventDemo.Run();

            var svc = new PollingService(new MockDriver());

            // ── 订阅者 1：控制台（数据每到一个，打印一条）──
            svc.DataReceived += d =>
            {
                foreach (var kvp in d)
                    System.Diagnostics.Debug.WriteLine($"[控制台] {kvp.Key} = {kvp.Value:F1}");

            };

            // ── 订阅者 2：界面（把窗口标题改成最新温度，你能亲眼看到在动）──
            svc.DataReceived += d =>
            {
                if (d.ContainsKey("温度"))
                    this.Title = $"实时温度: {d["温度"]:F1} ℃";
            };

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) => svc.PollOnce();
            timer.Start();
        }
    }
}
