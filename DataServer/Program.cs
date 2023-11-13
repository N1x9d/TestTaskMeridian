// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using Microsoft.Extensions.Configuration;

public static class Program
{
    static IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddJsonFile("Config.json", optional: true)
        .Build();  
     
    static void Main(string[] args)
    {   
        Start();

        Console.WriteLine("Press any key to stop...");
        Console.ReadKey(true);
    }

    /// <summary>
    /// Генератор серверов данных.
    /// </summary>
    private static void Start()
    {
        var ServersData = configuration.GetSection("DataServersIp").GetChildren()
            .Select(x => x.Value)
            .ToArray();
        var testData = configuration.GetSection("TestData").GetChildren()
            .Select(x => x.Value)
            .ToArray();

        for (int i = 1; i <= ServersData.Length; i++)
        {
            StartServer(i, testData[i - 1]);
        }
    }

    /// <summary>
    /// Запуск сервера данных.
    /// </summary>
    /// <param name="number">Номер сервера.</param>
    /// <param name="data">Данные.</param>
    /// <returns></returns>
    static async Task StartServer(int number, string data)
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, number);
        using Socket listener = new(
        ipPoint.AddressFamily,
        SocketType.Stream,
        ProtocolType.Tcp);
        var dataSplited = data.Split("\n");
        try
        {
            listener.Bind(ipPoint);
            listener.Listen(10);

            while (true)
            {
                using (Socket handler = await listener.AcceptAsync())
                {
                    string recive = null;

                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    recive += Encoding.UTF8.GetString(bytes, 0, bytesRec);

                    int i;
                    if (int.TryParse(recive, out i))
                    {
                        if (i < dataSplited.Length)
                        {
                            await handler.SendAsync(Encoding.UTF8.GetBytes(dataSplited[i]), SocketFlags.None);
                        }
                        else
                        {
                            //Если индекс не в пределах массива вернем NoData
                            await handler.SendAsync(Encoding.UTF8.GetBytes("NoData"), SocketFlags.None);

                        }
                    }

                    handler.Shutdown(SocketShutdown.Both);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}





 