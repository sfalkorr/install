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

    public static async void Read(string DBname, string query, int timeout)
    {
        await log(" > Начало запроса");
        var connectionString = Format($"Data Source={SQLServername};Initial Catalog={DBname};User ID={SQLUsername};Password={SqlPass};Timeout={timeout}");
        var connection       = new SqlConnection(connectionString);
        var cmd              = connection.CreateCommand();
        try
        {
            connection.Open();
            cmd.CommandText = query;
            var result = cmd.ExecuteNonQuery();
            if (result == -1) await log(" > Запрос выполнен успешно");

            connection.Close();
        }
        catch (Exception ex)
        {
            await log(ex.Message);
        }
    }

    public static async Task<bool> IsSqlAvaible()
    {
        var connection = new SqlConnection(Format($"Data Source={SQLServername};Initial Catalog={SqlInitcatalog};User ID={SQLUsername};Password={SqlPass};Timeout={SQLTimeout}"));
        try
        {
            await Task.Delay(100);
            connection.Open();
        }
        catch (SqlException ex)
        {
            await Task.Delay(100);
            if (ex.State == 0)
                return false;
        }

        await Task.Delay(100);
        return true;
    }

    public static async Task<bool> IsSqlPasswordOK(string password)
    {
        var connectionString = Format($"Data Source={SQLServername};Initial Catalog={SqlInitcatalog};User ID={SQLUsername};Password={password};Timeout={10}");
        var connection       = new SqlConnection(connectionString);
        try
        {
            connection.Open();
            await Task.Delay(100);
            return true;
        }
        catch (SqlException ex)
        {
            await Task.Delay(100);
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    // public static Task<int> ExecuteAsync(string sConnectionString, string sSQL, params SqlParameter[] parameters)
    // {
    //     return Task.Run(() =>
    //     {
    //         var newConnection = new SqlConnection(sConnectionString);
    //         var newCommand    = new SqlCommand(sSQL, newConnection);
    //         newCommand.CommandType = CommandType.Text;
    //         if (parameters != null) newCommand.Parameters.AddRange(parameters);
    //         newConnection.Open();
    //         return newCommand.ExecuteNonQuery();
    //     });
    // }

    public static Task<int> ExecuteAsync(string query, params SqlParameter[] parameters)
    {
        return Task.Run(() =>
        {
            var connectionString = Format($"Data Source={SQLServername};Initial Catalog={SqlInitcatalog};User ID={SQLUsername};Password={SqlPass};Timeout={5}");


            var connection = new SqlConnection(connectionString);


            var newCommand = new SqlCommand(query, connection);
            newCommand.CommandType = CommandType.Text;
            if (parameters != null) newCommand.Parameters.AddRange(parameters);
            connection.Open();
            return newCommand.ExecuteNonQuery();
        });
    }

    public static async Task<int> ExecuteAsync2(string query, string database)
    {
        var connectionString = Format($"Data Source={SQLServername};Initial Catalog={database};User ID={SQLUsername};Password={SqlPass};Timeout={5}");
        using var connection = new SqlConnection(connectionString);
        var       command    = new SqlCommand(query, connection);
        connection.Open();
        var total1 = (int)await command.ExecuteScalarAsync();
        Console.WriteLine(total1.ToString());
        await log(total1.ToString());
        return total1;
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
                        if (sLine.Length == 0)
                            sLine += reader[i];
                        else
                            sLine += "\t" + reader[i];

                    if (sData.Length == 0)
                        sData += sLine;
                    else
                        sData += "\r\n" + sLine;
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