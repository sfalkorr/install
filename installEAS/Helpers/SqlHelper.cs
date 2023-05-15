namespace installEAS.Helpers;

public static class Sql
{
    public static void Exex(string query, string DBname = default, int timeout = 5)
    {
        log("Начало SQL запроса > " + query);

        var connectionString = Format($"Data Source={SQLServername};Initial Catalog={DBname};User ID={SQLUsername};Password={SqlPass};Timeout={timeout}");
        var connection       = new SqlConnection(connectionString);
        var cmd              = connection.CreateCommand();
        try
        {
            connection.Open();
            cmd.CommandText = query;
            var result = cmd.ExecuteNonQuery();
            if (result == -1) log("Запрос выполнен успешно");

            connection.Close();
        }
        catch (Exception ex)
        {
            log(ex.Message);
        }
    }

    public static void Read(string DBname, string query, int timeout)
    {
        log(" > Начало запроса");
        var connectionString = Format($"Data Source={SQLServername};Initial Catalog={DBname};User ID={SQLUsername};Password={SqlPass};Timeout={timeout}");
        var connection       = new SqlConnection(connectionString);
        var cmd              = connection.CreateCommand();
        try
        {
            connection.Open();
            cmd.CommandText = query;
            var result = cmd.ExecuteNonQuery();
            if (result == -1) log(" > Запрос выполнен успешно");

            connection.Close();
        }
        catch (Exception ex)
        {
            log(ex.Message);
        }
    }

    public static bool IsSqlPasswordOK(string password)
    {
        var connectionString = Format($"Data Source={SQLServername};Initial Catalog={SqlInitcatalog};User ID={SQLUsername};Password={password};Timeout={10}");
        var connection       = new SqlConnection(connectionString);
        try
        {
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Task<int> ExecuteAsync(string sConnectionString, string sSQL, params SqlParameter[] parameters)
    {
        return Task.Run(() =>
        {
            var newConnection = new SqlConnection(sConnectionString);
            var newCommand    = new SqlCommand(sSQL, newConnection);
            newCommand.CommandType = CommandType.Text;
            if (parameters != null) newCommand.Parameters.AddRange(parameters);
            newConnection.Open();
            return newCommand.ExecuteNonQuery();
        });
    }

    public static Task<DataSet> GetDataSetAsync(string sConnectionString, string sSQL, params SqlParameter[] parameters)
    {
        return Task.Run(() =>
        {
            var newConnection = new SqlConnection(sConnectionString);
            var mySQLAdapter  = new SqlDataAdapter(sSQL, newConnection);
            mySQLAdapter.SelectCommand.CommandType = CommandType.Text;
            if (parameters != null) mySQLAdapter.SelectCommand.Parameters.AddRange(parameters);
            var myDataSet = new DataSet();
            mySQLAdapter.Fill(myDataSet);
            return myDataSet;
        });
    }

    public class CDatabaseResult
    {
        public bool   Success;
        public string Result;
        public object Data;
    }

    public static Task<CDatabaseResult> GetTextDataToCustomClassAsync(string sConnectionString, string sSQL, params SqlParameter[] parameters)
    {
        return Task.Run(() =>
        {
            try
            {
                var sData         = Empty;
                var newConnection = new SqlConnection(sConnectionString);
                newConnection.Open();
                var command = new SqlCommand(sSQL, newConnection);
                var reader  = command.ExecuteReader();
                while (reader.Read())
                {
                    var sLine = Empty;
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (sLine.Length == 0)
                        {
                            sLine += reader[i];
                        }
                        else
                        {
                            sLine += "\t" + reader[i];
                        }
                    }

                    if (sData.Length == 0)
                    {
                        sData += sLine;
                    }
                    else
                    {
                        sData += "\r\n" + sLine;
                    }
                }

                newConnection.Close();

                var result = new CDatabaseResult
                             {
                                 Data    = sData,
                                 Success = true
                             };

                return result;
            }
            catch (Exception ex)
            {
                var result = new CDatabaseResult
                             {
                                 Result  = "GetData Error: " + ex.Message,
                                 Success = false
                             };
                return result;
            }
        });
    }
}