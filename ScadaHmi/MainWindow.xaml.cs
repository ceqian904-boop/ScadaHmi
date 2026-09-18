using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScadaHmi.ViewModels;

namespace ScadaHmi
{
    public partial class MainWindow : Window
    {
        // 字段不再自己 new，改由构造函数从容器取
        private readonly RealTimeViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<RealTimeViewModel>();
            DataContext = _viewModel;
        }

        // 关窗口 → 通知 VM 收工，VM 内部会 Dispose 掉 AcquisitionHost，
        // 进而停掉两个后台循环。
        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Shutdown();
            base.OnClosed(e);
        }
    }
}