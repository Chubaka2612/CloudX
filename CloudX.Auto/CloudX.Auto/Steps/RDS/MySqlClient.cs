using System;
using System.Collections.Generic;
using CloudX.Auto.Tests.Models.TestData;
using MySqlConnector;
using Renci.SshNet;

namespace CloudX.Auto.Tests.Steps.RDS
{
    public class MySqlClient
    {
        private ForwardedPortLocal _forwardedPort;
        private SshClient _sshClient;
        private readonly MySqlConnectionModel _connectionModel;

        public MySqlClient( MySqlConnectionModel connectionModel)
        {
            _connectionModel = connectionModel;
        }

        public void Connect()
        {
            var keyPair = new PrivateKeyFile(_connectionModel.PrivateKeyPath);
            _sshClient = new SshClient(_connectionModel.SshServer, _connectionModel.SshPort, _connectionModel.SshUsername, keyPair);//Authenticate with key pair
            _sshClient.Connect();
            _forwardedPort = new ForwardedPortLocal("127.0.0.1", 3306, _connectionModel.MysqlServer, _connectionModel.MysqlPort);
            _sshClient.AddForwardedPort(_forwardedPort);
            _forwardedPort.Start();
        }

        private string GetConnectionString()
        {
            return $"server=localhost;port={_forwardedPort.BoundPort};database={_connectionModel.Database};uid={_connectionModel.Username};pwd={_connectionModel.Password}";
        }

        public void ExecuteNonQuery(string query)
        {
            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<T> ExecuteReader<T>(string query, Func<MySqlDataReader, T> mapRow)
        {
            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<T>();
                        while (reader.Read())
                        {
                            results.Add(mapRow(reader));
                        }
                        return results;
                    }
                }
            }
        }
        
        public void Disconnect()
        {
            if (_forwardedPort != null)
            {
                _forwardedPort.Stop();
            }

            if (_sshClient != null && _sshClient.IsConnected)
            {
                _sshClient.Disconnect();
            }
        }
    }
}