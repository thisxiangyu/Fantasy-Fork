using System.Runtime.Loader;
using Fantasy.Helper;
using Fantasy.Platform.Net;

namespace Fantasy
{
    public static class AssemblyHelper
    {
        private const string HotfixDll = "Hotfix";
        private static AssemblyLoadContext? _assemblyLoadContext = null;

        public static void Initialize()
        {
            LoadEntityAssembly();
            LoadHotfixAssembly();
        }
        
        private static void LoadEntityAssembly()
        {
            // .NET 运行时采用延迟加载机制，如果代码中不使用程序集的类型，程序集不会被加载
            // 执行一下，触发运行时强制加载从而自动注册到框架中
            // 因为AssemblyHelper代码在Entity项目里，所以需要获取这个项目的Assembly
            // 然后调用EnsureLoaded方法强制加载一下
            typeof(AssemblyHelper).Assembly.EnsureLoaded();
        }
        
        public static System.Reflection.Assembly LoadHotfixAssembly()
        {
            if (_assemblyLoadContext != null)
            {
                _assemblyLoadContext.Unload();
                System.GC.Collect();
            }
            string baseDirectory = AppContext.BaseDirectory;
            // 如果是EFCore设计时, 需要指定特定路径
            if (Entry.IsEFCoreDesignTime()) 
            { 
                Console.WriteLine("EFCore Design Time ...");
                baseDirectory = "..\\..\\..\\examples\\Bin\\Debug\\net9.0\\";
            }
            _assemblyLoadContext = new AssemblyLoadContext(HotfixDll, true);
            var dllBytes = File.ReadAllBytes(Path.Combine(baseDirectory, $"{HotfixDll}.dll"));
            var pdbBytes = File.ReadAllBytes(Path.Combine(baseDirectory, $"{HotfixDll}.pdb"));
            var assembly = _assemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
            // 强制触发 Initializer 执行
            // AssemblyLoadContext.LoadFromStream 只加载程序集到内存，不会自动触发 Initializer
            // 必须访问程序集中的类型才能触发初始化，这里通过反射调用生成的方法
            // 注意：此方法仅用于热重载场景（JIT），Native AOT 不支持动态加载
            // 拿到Assembly就用EnsureLoaded()方法强制触发
            assembly.EnsureLoaded();
            return assembly;
        }
    }
}