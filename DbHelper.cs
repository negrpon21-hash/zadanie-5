using System.Configuration;
using MySql.Data.MySqlClient;

namespace DoctorMagApp
{
    internal static class DbHelper
    {
        public static MySqlConnection GetConnection()
        {
            string cs = ConfigurationManager.ConnectionStrings["DoctorMagDB"].ConnectionString;
            return new MySqlConnection(cs);
        }
    }
}
