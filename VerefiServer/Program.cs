// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

public static class Program
{
    static IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddJsonFile("Config.json", optional: true)
        .Build();

    static List<string> ServersMessagesBuffer = new List<string>();

    static void Main(string[] args)
    {
        // running as console app
        Start();
    }

    /// <summary>
    /// Запуск основного обработчика с клиента
    /// </summary>
    /// <returns></returns>
    private static async Task Start()
    {
        var verefyIp = configuration.GetSection("VerifiServerIp").Value;
        var conData = verefyIp.Split(':');

        var ServersData = configuration.GetSection("DataServersIp").GetChildren()
            .Select(x => x.Value)
            .ToArray();

        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, Int32.Parse(conData[1]));
        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(ipPoint);
        socket.Listen();
        Console.WriteLine("Сервер запущен. Ожидание подключений...");
        // получаем входящее подключение
        using Socket client = await socket.AcceptAsync();
        int i = 0;
        while (ServersMessagesBuffer.Count == 0)
        {
            ServersMessagesBuffer.Clear();
            await GetData(ServersData, (i).ToString());
            if (ServersMessagesBuffer.First() != "NoData")
            {
                await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("end")), SocketFlags.None);
                break;
            }
            i++;
            var stringToSend = await AnalyzeData();
            await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"meassage number {i} " + stringToSend)), SocketFlags.None);
        }
    }

    /// <summary>
    /// Считатать данные со всех серверов.
    /// </summary>
    /// <param name="serversData">Список серверов данных.</param>
    /// <param name="message">Номер сообщения.</param>
    /// <returns></returns>
    static async Task GetData(string[] serversData, string message)
    {
        foreach (var serverData in serversData)
        {
            await GetDataFromServer(serverData, message);
        }
    }

    /// <summary>
    /// Получить сообщение с сервера данных.
    /// </summary>
    /// <param name="conString">Адрес.</param>
    /// <param name="message">Номер сообщения.</param>
    /// <returns></returns>
    static async Task GetDataFromServer(string conString, string message)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var conData = conString.Split(':');
        try
        {
            await socket.ConnectAsync(conData[0], int.Parse(conData[1]));
            // буфер для получения данных
            var responseBytes = new byte[512];
            socket.SendAsync(Encoding.UTF8.GetBytes(message), SocketFlags.None);
            int n;
            if (int.TryParse(message, out n))
            {
                var response = "";
                // получаем данные
                var bytes = await socket.ReceiveAsync(responseBytes, SocketFlags.None);
                // преобразуем полученные данные в строку
                response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
                // выводим данные на консоль
                ServersMessagesBuffer.Add(response);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }             
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Не удалось установить подключение с {socket.RemoteEndPoint}");
        }
    }

    /// <summary>
    /// Сравнивание сообщения со всех серверов.
    /// </summary>
    /// <returns>Оригинальную строку если все данные совпали, иначе отмечена не совпавшая дата.</returns>
    static async Task<string> AnalyzeData()
    {
        var clearData = new List<string>();
        bool date1Trigger = false;
        bool date2Trigger = false;
        var sb = new StringBuilder();
        foreach (var message in ServersMessagesBuffer)
        {
            clearData.Add(ClearData(message));
        }

        for (int i = 1; i < clearData.Count; i++)
        {
            var prevDates = clearData[i - 1].Split(";");
            var curDates = clearData[i].Split(";");
            //Не совпала первая дата.
            date1Trigger = prevDates[0] != curDates[0];
            //Не совпала вторая дата.
            date2Trigger = prevDates[1] != curDates[1];

            if (date1Trigger || date2Trigger)
            {
                sb.Append("#90#010102#27");
                sb.Append(date1Trigger ? "NoRead;" : curDates[0] + ';');
                sb.Append(date2Trigger ? "NoRead" : curDates[1]);
                sb.Append("#91");
                return sb.ToString();
            }
        }

        return ServersMessagesBuffer.First();
    }

    /// <summary>
    /// Извлечь даты из строки.
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns></returns>
    static string ClearData(string input)
    {
        var substring = Regex.Replace(input, @"#90#010102#27", "");
        return Regex.Replace(substring, @"#91", "");
    }
}


