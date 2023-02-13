using System.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using installEAS.Helpers;
using installEAS.MessageBoxCustom;
using installEAS.Themes;
using installEAS.Controls;
using static installEAS.Helpers.Log;
using static installEAS.Helpers.Animate;
using static installEAS.Variables;

namespace installEAS.Helpers;

public static class Sql
{
    public static void Exex(string query, string DBname = default, int timeout = 5)
    {
        log("Начало SQL запроса > " + query);

        var connectionString = string.Format($"Data Source={SQLServername};Initial Catalog={DBname};User ID={SQLUsername};Password={SqlPass};Timeout={timeout}");
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
        catch (Exception ex) { log(ex.Message); }
    }

    public static void Read(string DBname, string query, int timeout)
    {
        log(" > Начало запроса");
        var connectionString = string.Format($"Data Source={SQLServername};Initial Catalog={DBname};User ID={SQLUsername};Password={SqlPass};Timeout={timeout}");
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
        catch (Exception ex) { log(ex.Message); }
    }

    public static bool IsServerConnected()
    {
        var connectionString = string.Format($"Data Source={SQLServername};Initial Catalog={SqlInitcatalog};User ID={SQLUsername};Password={SqlPass};Timeout={10}");
        var connection       = new SqlConnection(connectionString);

        try
        {
            connection.Open();
            return true;
        }
        catch (SqlException e)
        {
            log(e.Message);
            return false;
        }
    }

    public static bool IsPasswordOK(string password)
    {
        var connectionString = string.Format($"Data Source={SQLServername};Initial Catalog={SqlInitcatalog};User ID={SQLUsername};Password={password};Timeout={10}");
        var connection       = new SqlConnection(connectionString);
        log("Connecting...", false);
        try
        {
            connection.Open();
            return true;
        }
        catch { return false; }
        finally { log("OK", Brushes.GreenYellow); }
    }
}