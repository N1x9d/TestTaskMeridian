using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;

namespace DataService
{
    public partial class DataService : ServiceBase
    {
        IConfigurationRoot configuration;
        public DataService()
        {
            configuration = new ConfigurationBuilder()
                .AddJsonFile("Config.json", optional: true)
                .Build();
            InitializeComponent();

        }

        protected override void OnStart(string[] args)
        {
            System.Diagnostics.Debugger.Launch();
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

        protected override void OnStop()
        {
        }

        /// <summary>
        /// Запуск сервера данных.
        /// </summary>
        /// <param name="number">Номер сервера.</param>
        /// <param name="data">Данные.</param>
        /// <returns></returns>
        async Task StartServer(int number, string data)
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, number);
            using (Socket listener = new Socket(ipPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp)) {
                var dataSplited = data.Split('\n');
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
                                    await handler.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(dataSplited[i])), SocketFlags.None);
                                }
                                else
                                {
                                    await handler.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("NoData")), SocketFlags.None);

                                }
                            }

                            handler.Shutdown(SocketShutdown.Both);
                        }
                    }
                }
                catch (Exception ex)
                {
                } 
            }
        }
    }
}
