using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;

namespace Dacpac.Tool.Commands
{
    /// <summary>Command use for publish an dacpac on database server</summary>
    class PublishCommand : Command
    {
        public PublishCommand() : base("pub", Properties.Resources.PublishDescription)
        {
            AddDacpacPath();
            AddOptionFile();


            AddCommand(new PublishWithConnectionStringCommand());
            AddCommand(new PublishOnServerCommand());
        }

        private static void AddOptionDacDeployOptions(Command command)
        {
            var properties = typeof(Microsoft.SqlServer.Dac.DacDeployOptions)
                .GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsPrimitive)
                .OrderBy(p => p.Name)
                .ToArray();

            foreach (var propertyInfo in properties)
            {
                var description = Properties.Resources.ResourceManager.GetString(propertyInfo.Name);
                command.AddGlobalOption(new Option(propertyInfo.Name, description, propertyInfo.PropertyType));
            }
        }

        private void AddOptionFile()
        {
            var option = new Option<string>("-f", Properties.Resources.NamePatternDescription)
            {
                Name = "NamePattern",
                IsRequired = true,
                AllowMultipleArgumentsPerToken = false

            };

            AddGlobalOption(option);
        }

        private Command AddDacpacPath()
        {
            var option = new Option<DirectoryInfo>("-d", Properties.Resources.DacpathDescription)
            {
                Name = "dacpath",
                IsRequired = true
            };

            AddGlobalOption(option);
            return this;
        }

        private class PublishWithConnectionStringCommand : Command
        {
            public PublishWithConnectionStringCommand() : base("db")
            {
                AddOptionConnectionString();
                AddOptionDacDeployOptions(this);
                this.Handler = CommandHandler.Create<DacPackageOptions, DacDeployOptions>(Run);
            }

            private Command AddOptionConnectionString()
            {
                var option = new Option<string>("-c", Properties.Resources.ConnectionStringDescription)
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = true,
                    Name = "connectionString"
                };

                this.AddOption(option);
                return this;

            }

        }

        private class PublishOnServerCommand : Command
        {
            public PublishOnServerCommand()
                : base("server")
            {
                AddOptionServer();
                AddOptionDatabaseNames();
                AddOptionUserId();
                AddOptionPassword();
                var useSspi = new Option<bool>("useSspi", () => false) { IsHidden = true };
                AddOption(useSspi);
                AddOptionDacDeployOptions(this);
                this.Handler = CommandHandler.Create<DacPackageOptions, DacDeployOptions>(Run);

                var comm = new Command("sspi", Properties.Resources.SspiDescription);
                comm.AddOption(new Option<bool>("useSspi", () => true)
                {
                    IsHidden = true

                });
                AddOptionDacDeployOptions(comm);
                comm.Handler = this.Handler = CommandHandler.Create<DacPackageOptions, DacDeployOptions>(Run);
            }

            private void AddOptionUserId()
            {
                var option = new Option<string>("-u", Properties.Resources.UserIdDescription)
                {
                    Name = "userid",
                    IsRequired = true

                };
                AddOption(option);
            }

            private void AddOptionServer()
            {
                var option = new Option<string>("-s", Properties.Resources.ServerDescription)
                {
                    Name = "server",
                    IsRequired = true

                };
                AddGlobalOption(option);
            }

            private void AddOptionPassword()
            {
                var option = new Option<string>("-p", Properties.Resources.PasswordDescription)
                {
                    Name = "password",
                    IsRequired = true

                };
                AddOption(option);
            }

            private void AddOptionDatabaseNames()
            {
                var option = new Option<string>("--dbs", Properties.Resources.DataBaseNamesDescription)
                {
                    Name = "DataBaseNames",
                    IsRequired = true
                };
                AddGlobalOption(option);

            }

        }

        internal static Task Run(DacPackageOptions settings, DacDeployOptions option)
        {
            //  new DacPackageOptions();

            var package = settings.FindDacPackage();
            foreach (var connection in settings.Connections)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Starting deploy on {0}", connection.Key);
                var dacService = new DacServices(connection.Value);
                dacService.ProgressChanged += (object sender, DacProgressEventArgs e) =>
                {
                    Console.WriteLine($"{e.Message}: {DateTimeOffset.Now:HH:mm:sss tt zzzz}");
                    if (e.Status == DacOperationStatus.Completed)
                    {
                        Console.WriteLine("-".PadRight(15, '-'));
                    }
                };
                dacService.Deploy(package, connection.Key, true, option);
                Console.WriteLine("Finished {0}", connection.Key);
                Console.ResetColor();
            }
            return Task.CompletedTask;
        }
    }


}
