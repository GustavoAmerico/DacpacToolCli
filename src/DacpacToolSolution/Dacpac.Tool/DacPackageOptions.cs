﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dacpac.Tool
{
    internal class DacPackageOptions
    {
        private const string DefaultPassword = "IFromBrazilian";
        private bool _connectionIsLoad = false;
        private string _dataBaseNames = "";

        public IDictionary<string, string> Connections { get; set; } = new Dictionary<string, string>();

        /// <summary>Obtém e envia a connection string que deve ser utilizada para publicar o dacpac</summary>
        public string ConnectionString { get; set; }

        /// <summary>Lista de banco de dados que deve ser atualizada/criado</summary>
        public string DataBaseNames
        {
            get { return _dataBaseNames; }
            set
            {
                _connectionIsLoad = false;
                _dataBaseNames = value;


            }
        }

        public string NamePattern { get; set; } = @"*.dacpac";

        /// <summary>Obtém e envia a senha necessária para acessar o banco de dados</summary>
        public String Password { get; set; } = DefaultPassword;

        /// <summary>Obtém e envia o diretorio que o dacpac está armazenado</summary>
        public string DacPath { get; set; }

        /// <summary>Obtém o endereço do servidor que a base de dados deve ser publicada</summary>
        public string Server { get; set; }

        /// <summary>
        /// Obtém e envia um valor indicando se a connection string deve considerar a autenticação do windows
        /// </summary>
        public bool UseSspi { get; set; } = true;

        public string UserId { get; set; } = "";


        /// <summary>Find by dacpac file in directory</summary>
        public Microsoft.SqlServer.Dac.DacPackage FindDacPackage()
        {

            if (TryGetFiles(out string[] files) && files.Any())
            {

                try
                {
                    Console.WriteLine("Found file: {0}", files[0]);
                    var packge = Microsoft.SqlServer.Dac.DacPackage.Load(files[0]);
                    LoadConnectionStrins();
                    return packge;

                    //TODO: write message
                }
                catch (TypeInitializationException typeInitializationException)
                {
                    Console.Error.WriteLine("An error on read dacpac file: {0}/{1}", DacPath, NamePattern);
                    Console.Error.WriteLine("Exception Details: {0}", typeInitializationException);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("An error on read dacpac file: {0}/{1}", DacPath, NamePattern);
                    Console.Error.WriteLine("Exception Details: {0}", exception);
                }
            }
            return null;
        }

        /// <summary>Carrega as conexões com base nas configurações do servidor e do banco selecionado</summary>
        private void LoadConnectionStrins()
        {

            if (_connectionIsLoad) return;
            TryDeserializerConnectionString();
            if (!string.IsNullOrWhiteSpace(_dataBaseNames) && !string.IsNullOrWhiteSpace(Server))
            {
                foreach (var dbName in _dataBaseNames.Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Connections[dbName] = string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(UserId) || UseSspi
                        ? BuildConnectionString(Server, null, null)
                        : BuildConnectionString(Server, UserId, Password);
                }
            }

            if (!Connections.Any())
            {
                Console.Error.WriteLine("Your must be define the connection string, see example:");
                Console.Error.WriteLine("1. --server=<ip or damain> --databasenames=<database name or multi db name separated by ;> --userId=<user with permission> --password=<user password>;TrustServerCertificate=true");
                Console.Error.WriteLine("2. --ConnectionString='Data Source={Server};User Id={UserId};Password={Password}';TrustServerCertificate=true");

            }

            _connectionIsLoad = true;
        }

        private void TryDeserializerConnectionString()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString)) return;
            try
            {
                var connString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionString.Trim());
                Connections[connString.InitialCatalog] = BuildConnectionString(connString.DataSource, connString.UserID, connString.Password);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e}");
            }


        }

        private string BuildConnectionString(string dataSource, string? userId, string? password)
        {
            string userInfo = string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password)
                ? "Integrated Security=SSPI"
                : $"User Id={userId};Password={password};Integrated Security=False";

            return $"Data Source={dataSource};Application Name=SqlPackageUpdate;TrustServerCertificate=true;{userInfo};";
        }

        ///<summary>Run the command</summary>
        /// <summary>Gets the files match with sended file pattern</summary>
        /// <param name="path">The path.</param>
        /// <param name="filePattern">The file pattern.</param>
        /// <param name="files"></param>
        /// <returns></returns>
        private bool TryGetFiles(out string[] files)
        {
            string path = DacPath, fileNamePattern = NamePattern;

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
                //TODO: write the throw code
                files = Directory.GetFiles(path, fileNamePattern, new EnumerationOptions()
                {
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true
                })
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
