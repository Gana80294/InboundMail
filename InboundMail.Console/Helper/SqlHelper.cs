using EmamiInboundMail.Console.Static;
using Npgsql;
using System.Data;
using System.Data.SqlClient;

namespace EmamiInboundMail.Console.Helper
{
    public class SqlHelper
    {
        public static DataTable ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            using (SqlConnection sqlCon = new SqlConnection(StaticVariables.SqlConnectionString))
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
