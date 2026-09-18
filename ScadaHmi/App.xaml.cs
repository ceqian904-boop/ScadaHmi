using Microsoft.Extensions.DependencyInjection;
using ScadaHmi.Communication;
using ScadaHmi.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace ScadaHmi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //静态属性让 MainWindow能取到容器
        //作用是让程序任何地方都能通过 App.Services.GetService<T>() 拿到依赖注入的服务实例。
        public static IServiceProvider Services { get; private set; } = null!;

        //重写OnStartup 
        //这是虚方法重写，虚方法是基类默认实现，子类可改写，也可以不实现
        protected override void OnStartup(StartupEventArgs e)
        {
            //ServiceCollection = 菜单（登记有哪些菜）
            //ServiceProvider = 厨房（真正做菜给你吃）。
            var sc = new ServiceCollection();

            // ② 驱动：Singleton + 工厂写法
            //    为什么要 lambda？ModbusTcpDriver 构造函数要 4 个参数，
            //    容器靠反射 new 不出来，必须手写怎么造。
            //工厂注册 = 你自己动手 new 对象，容器只负责在需要时调用你的工厂函数。
            //适用于需要传自定义参数、根据条件选不同实现、或者做复杂初始化的场景。
            sc.AddSingleton<ICommDriver>(_ => new MockDriver());


            //Transient = 一次性筷子，每次用都拿一根新的，用完就扔。
            sc.AddTransient<RealTimeViewModel>();
            //BuildServiceProvider() = 把菜单（ServiceCollection）交给厨房
            //厨房正式开张，开始做菜（创建对象）。
            Services = sc.BuildServiceProvider();


            // ④ 开窗口
            // 方案 A（Step 8 删了 StartupUri）：用这行手动开
            base.OnStartup(e);   // ← 先调用基类，完成框架内部初始化
            new MainWindow().Show();  // ← 再创建并显示窗口
        }

        //⑤ 点位表：温度/压力/流量 → 地址 0/1/2，Scale 都是 10
        private static List<ModbusPoint> BuildPointMap() => new()
        {
            new ModbusPoint {Tag = "温度", Address = 0 , Scale = 10},
            new ModbusPoint { Tag = "压力", Address = 1, Scale = 10 },
            new ModbusPoint { Tag = "流量", Address = 2, Scale = 10 },
        };
    }

}
