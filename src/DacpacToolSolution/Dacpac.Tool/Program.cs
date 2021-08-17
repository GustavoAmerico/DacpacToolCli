using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Dac;
using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Threading;
using Dacpac.Tool.Commands;

namespace Dacpac.Tool
{
    internal class Program
    {
        private static IConfiguration _configuration;

        public const string DacDeployOptionsKey = "DacDeployOptions";

        private static int Main(string[] args)
        {
            args = args?.SkipWhile(string.IsNullOrWhiteSpace).ToArray();

            if (args?.Any() == true && args[0].Equals("publish", StringComparison.CurrentCultureIgnoreCase))
            {
                ConfigurationBuild(args);
                var option = _configuration.GetSection(DacDeployOptionsKey)
                                 .Get<DacDeployOptions>() ?? new DacDeployOptions();
                var dacPackageOptions = _configuration
                                            .Get<DacPackageOptions>() ?? new DacPackageOptions();
                PublishCommand.Run(dacPackageOptions, option).GetAwaiter().GetResult();
                Finished();
                return 0;
            }

            var root = new RootCommand();
            root.AddCommand(new PublishCommand());

            return root.Invoke(args);
        }

        private static void Finished()
        {
            Console.WriteLine("Good bye!");
            Thread.Sleep(200);
        }


        private static void ConfigurationBuild(string[] args)
        {
            _configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                  .AddEnvironmentVariables()
                  .AddCommandLine(args)
                  .Build();
        }
    }
}