using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trimmel_MCTG.db
{
    public class Database
    {
        NpgsqlConnection conn;
        string connection = "Host=localhost;Port=5432;Username=postgres;Password=#;Database=mctg_trimmel";
        public Database()
        {
            conn = new NpgsqlConnection(connection);
            conn.Open();
        }
    }
}
