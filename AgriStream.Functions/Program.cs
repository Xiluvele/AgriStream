
using Microsoft.Extensions.Hosting;

namespace AgriStream.Functions
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            host.Run();
        }
    }
}
