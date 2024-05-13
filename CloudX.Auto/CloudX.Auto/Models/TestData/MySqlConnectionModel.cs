
namespace CloudX.Auto.Tests.Models.TestData
{
    public class MySqlConnectionModel
    {
        public string SshServer { get; set; }

        public int SshPort { get; set; }

        public string SshUsername { get; set; }

        public string PrivateKeyPath { get; set; }

        public string MysqlServer { get; set; }
        
        public uint MysqlPort { get; set; }

        public string Database { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
