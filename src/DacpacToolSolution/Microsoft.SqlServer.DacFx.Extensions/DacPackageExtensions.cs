using System;
using System.IO;
using System.Linq;

namespace Microsoft.SqlServer.Dac
{
    public static class DacPackageExtensions
    {
        /// <summary>Find by dacpac file in directory</summary>
        public static void Deploy(string dacPath, string namePattern, string connectionString
            , DacDeployOptions options = null
            , EventHandler<DacProgressEventArgs> progressChanged = null
            , EventHandler<DacMessageEventArgs> messageEvent = null)
        {
            var package = FindDacPackage(dacPath, namePattern);
            package.Deploy(connectionString, options, progressChanged, messageEvent);
        }

        /// <summary>Find by dacpac file in directory</summary>
        public static void Deploy(this DacPackage package, string connectionString
            , DacDeployOptions options = null
            , EventHandler<DacProgressEventArgs> progressChanged = null
            , EventHandler<DacMessageEventArgs> messageEvent = null)
        {
            var connString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString.Trim());

            var dacService = new DacServices(connString.ToString());
            if (progressChanged != null) dacService.ProgressChanged += progressChanged;
            if (messageEvent != null) dacService.Message += messageEvent;
            dacService.Deploy(package, connString.InitialCatalog, true, options);
            //dacService.Publish(package, connString.InitialCatalog, options);
        }

        /// <summary>Find by dacpac file in directory</summary>
        public static (string DataBaseScript, string DeploymentReport, string masterDbScript) Publish(string dacPath, string namePattern, string connectionString
            , PublishOptions options = null
            , EventHandler<DacProgressEventArgs> progressChanged = null
            , EventHandler<DacMessageEventArgs> messageEvent = null)
        {
            var package = FindDacPackage(dacPath, namePattern);
            return package.Publish(connectionString, options, progressChanged, messageEvent);
        }

        /// <summary>Find by dacpac file in directory</summary>
        public static (string DataBaseScript, string DeploymentReport, string masterDbScript) Publish(this DacPackage package, string connectionString
            , PublishOptions options = null
            , EventHandler<DacProgressEventArgs> progressChanged = null
            , EventHandler<DacMessageEventArgs> messageEvent = null)
        {
            var connString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString.Trim());
            var dacService = new DacServices(connString.ToString());
            if (progressChanged != null) dacService.ProgressChanged += progressChanged;
            if (messageEvent != null) dacService.Message += messageEvent;
            var result = dacService.Publish(package, connString.InitialCatalog, options);
            return (result.DatabaseScript, result.DeploymentReport, result.MasterDbScript);
        }

        /// <summary>Find by dacpac file in directory</summary>
        public static Microsoft.SqlServer.Dac.DacPackage FindDacPackage(string dacPath, string namePattern)
        {
            if (!TryGetFiles(dacPath, namePattern, out string[] files) || !files.Any()) return null;
            try
            {
                var packge = Microsoft.SqlServer.Dac.DacPackage.Load(files[0]);
                return packge;
            }
            catch (TypeInitializationException typeInitializationException)
            {
                Console.Error.WriteLine("An error on read dacpac file: {0}/{1}", dacPath, namePattern);
                Console.Error.WriteLine("Exception Details: {0}", typeInitializationException);
                throw;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("An error on read dacpac file: {0}/{1}", dacPath, namePattern);
                Console.Error.WriteLine("Exception Details: {0}", exception);
                throw;
            }
        }

        ///<summary>Run the command</summary>
        /// <summary>Gets the files match with sended file pattern</summary>
        /// <param name="path">The path.</param>
        /// <param name="filePattern">The file pattern.</param>
        /// <param name="files"></param>
        /// <returns></returns>
        private static bool TryGetFiles(string dacPath, string namePattern, out string[] files)
        {
            string path = dacPath, fileNamePattern = namePattern;

            // Read the arguments sended by user
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Environment.CurrentDirectory;
                Console.WriteLine("Your no send the dacpac path, we will search in {0}", path);
            }

            if (string.IsNullOrWhiteSpace(fileNamePattern))
            {
                fileNamePattern = @"*.dacpac";
                Console.WriteLine("Your no send the dacpac pattern name, we will search by {0}", fileNamePattern);
            }

            files = new string[0];
            try
            {
                var searchOptions = new EnumerationOptions()
                {
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true
                };

                files = Directory.GetFiles(path, fileNamePattern, searchOptions)
                    .Distinct()
                    .ToArray();


                if (files.Length == 0)
                {
                    //TODO: write help
                    Console.Error.WriteLine("No file find: {0}/{1}", path, fileNamePattern);
                    return false;
                }
                else if (files.Length > 1)
                {
                    //TODO: write help
                    Console.Error.WriteLine("No can exists multiple files per pattern in directory: {0}/{1}", path, fileNamePattern);
                    return false;
                }

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex.Message);
                return false;
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex.Message);
                return false;
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}