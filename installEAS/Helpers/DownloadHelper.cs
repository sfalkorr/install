using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;


namespace installEAS.Helpers;

public class DownloadHelper

        {
        private static string CalculateMD5(string filename)
        {
            using var mD5            = MD5.Create();
            using var fileStream     = File.OpenRead(filename);
            var       lowerInvariant = BitConverter.ToString(mD5.ComputeHash(fileStream)).Replace("-", Empty).ToLowerInvariant();

            return lowerInvariant;
        }

        public async Task DownloadFileAsync(Uri fromUrl, string toFile, CancellationToken token, int count = 3)
        {
            await DownloadFileInnerAsync(fromUrl, toFile, token);
            try
            {
                count--;
                File.Delete(toFile);
                await DownloadFileAsync(fromUrl, toFile, token, count);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        private static async Task DownloadFileInnerAsync(Uri fromUrl, string toFile, CancellationToken token)
        {
            if (!File.Exists(toFile))
            {
                var flag  = false;
                var flag1 = false;
                var empty = Empty;
                Console.WriteLine(Concat("Скачивание файла ", Path.GetFileName(toFile), "\t"));
                await Task.Run(() =>
                {
                    var webClient   = new WebClient();
                    var progressBar = new ProgressBar();
                    webClient.DownloadProgressChanged += (_, e) =>
                    {
                        var bytesReceived = e.BytesReceived;
                        var num           = double.Parse(bytesReceived.ToString());
                        bytesReceived = e.TotalBytesToReceive;
                        var num1 = num / double.Parse(bytesReceived.ToString());
                            //progressBar.Report(num1);

                    };
                    webClient.DownloadFileCompleted += (_, e) =>
                    {
                        if (e.Error != null)
                        {
                            flag1 = true;
                            empty = e.Error.Message;
                        }

                        flag = true;
                        //progressBar.Dispose();
                    };
                    webClient.DownloadFileAsync(fromUrl, toFile, token);
                    Console.WriteLine($"Скачивание файла {Path.GetFileName(toFile)} с адреса {fromUrl} с сохранением в {toFile}");
                    while (!flag)
                    {
                        Thread.Sleep(50);
                    }
                }, token);
                if (flag)
                {
                    var fileName = Path.GetFileName(toFile);
                    var str      = (flag1 ? "завершено c ошибкой" : "завершено успешно");
                    Console.WriteLine(Concat("Скачивание файла ", fileName, " ", str));
                }

                if (flag1)
                {
                    Console.WriteLine(Concat("Ошибка при скачивании файла файла: ", Path.GetFileName(toFile), " ", empty));
                }
            }
            else
            {
                Console.WriteLine(Concat("Файл ", Path.GetFileName(toFile), " уже скачан!"));
            }
        }
}