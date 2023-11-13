// See https://aka.ms/new-console-template for more information

using System;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public static class Program
{
    static IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddJsonFile("Config.json", optional: true)
        .Build();



    static void Main(string[] args)
    {
       Start();
        Console.ReadLine();
    }

    /// <summary>
    /// Основной обработчик.
    /// </summary>
    /// <returns></returns>
    private static async Task Start()
    {
        var verifyIp = configuration.GetSection("VerifiServerIp").Value;
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var conData = verifyIp.Split(':');
        try
        {
            var response = "";
            await socket.ConnectAsync(conData[0], int.Parse(conData[1]));
            while (response != "end")
            {                
                var responseBytes = new byte[512];
                // получаем данные
                var bytes = await socket.ReceiveAsync(responseBytes, SocketFlags.None);
                // преобразуем полученные данные в строку
                response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
                // выводим данные на консоль
                if(response != "")
                    Console.WriteLine(response);
            }
        }
        catch (SocketException e)
        {
            //Попытаемся перезапустить сервис.
            StartServiceWorker(e);
            Console.WriteLine($"Не удалось установить подключение с {socket.RemoteEndPoint}");
        }
    }

    /// <summary>
    /// Запустить сервис анализатора.
    /// </summary>
    /// <returns></returns>
    private static async Task StartServiceWorker(Exception e)
    {
        ServiceController controller = new ServiceController(configuration.GetSection("VerifyServiceName").Value);
        //если сервис не запущен то запускаем его.
        if (controller.Status==ServiceControllerStatus.Stopped)
        {
            controller.Start();
            Start();
        }
        else
        {// если сервис работает но всеравно ошибки при подключении выдаем исключение.
            Console.WriteLine($"Сервис не найден, {e.Message}"); 
            throw e;
        }
    }
}
