using EmamiInboundMail.Console.Static;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmamiInboundMail.Console.Helper
{
    public static class NpgSqlHelper
    {

        public static DataTable ExecuteStoredProcedure(string procedureName, params NpgsqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            using(NpgsqlConnection sqlCon = new NpgsqlConnection(StaticVariables.NpgSqlConnectionString))
            {
                using(NpgsqlCommand cmd = new NpgsqlCommand())
                {
                    cmd.Connection = sqlCon;
                    cmd.CommandText = procedureName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (sqlCon.State != System.Data.ConnectionState.Open)
                        sqlCon.Open();

                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    NpgsqlDataAdapter sqlDa = new NpgsqlDataAdapter(cmd);
                    sqlDa.Fill(dt);
                    return dt;
                }
            }
        } 
    }
}
