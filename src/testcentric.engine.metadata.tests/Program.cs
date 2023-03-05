using System.Reflection;
using NUnitLite;

namespace TestCentric.Metadata
{
    class Program
    {
        static int Main(string[] args)
        {
#if NETFRAMEWORK
            return new AutoRun().Execute(args);
#else
            return new TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
#endif
        }
    }
}
