using EmamiInboundMail.Service.Static;
using Npgsql;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EmamiInboundMail.Service.Helper
{
    public class SqlHelper
    {
        public static DataTable ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters)
        {
            ErrorLog.WriteErrorLog($"Executing the stored procedure {procedureName} using SQL SERVER");
            DataTable dt = new DataTable();
            using (SqlConnection sqlCon = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlAuthContext"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand())
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
                    SqlDataAdapter sqlDa = new SqlDataAdapter(cmd);
                    sqlDa.Fill(dt);
                    return dt;
                }
            }
        }
    }
}
