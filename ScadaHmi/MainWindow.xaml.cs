using System;
using System.Windows;
using ScadaHmi.ViewModels;

namespace ScadaHmi
{
    public partial class MainWindow : Window
    {
        private readonly RealTimeViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;   // 业务全在 VM，这行就够
        }

        // 关窗口 → 通知 VM 把两个后台循环停掉。
        // Day3 这段逻辑本来就在，MVVM 瘦身时被一起删掉了，现在收在 VM 的 Shutdown 里。
        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Shutdown();
            base.OnClosed(e);
        }
    }
}
