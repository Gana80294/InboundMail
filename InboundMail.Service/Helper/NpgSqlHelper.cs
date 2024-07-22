using EmamiInboundMail.Service.Static;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmamiInboundMail.Service.Helper
{
    public static class NpgSqlHelper
    {

        public static DataTable ExecuteStoredProcedure(string procedureName, params NpgsqlParameter[] parameters)
        {
            ErrorLog.WriteErrorLog($"Executing the stored procedure {procedureName} using NPG-SQL SERVER");
            DataTable dt = new DataTable();
            using(NpgsqlConnection sqlCon = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["NpgSqlAuthContext"].ConnectionString))
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
