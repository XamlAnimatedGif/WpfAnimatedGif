using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TestWaitSplash
{
    public static class ServerTransmitter
    {
        internal static string GetFromResources(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();

            var resource = asm.GetManifestResourceNames().First(res => res.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            using (var stream = asm.GetManifestResourceStream(resource))
            {
                if (stream == null) return string.Empty;

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }



        public static async Task CreateDatabaseAsync()
        {
            using (var sqlConn = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
            {
                using (var cmd = sqlConn.CreateCommand())
                {
                    //
                    // Set the command object so it knows to execute a stored procedure
                    cmd.CommandType = CommandType.Text;

                    cmd.CommandText = GetFromResources("DatabaseCreatorQuery.sql");
                    //
                    // execute the command
                    try
                    {
                        await sqlConn.OpenAsync();

                        await cmd.ExecuteNonQueryAsync();
                    }
                    finally
                    {
                        sqlConn.Close();
                    }

                }
            }
        }
    }
}
