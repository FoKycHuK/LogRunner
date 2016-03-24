using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CVARC.V2;

namespace LogRunner
{
    class Program
    {
        // запуск логов всегда только на локальной машине.
        const string ip = "127.0.0.1";

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("usage: LogRunner.exe path_to_log_file");
                return;
            }
            var log = Log.Load(args[0]);
            var port = 14002;
            var playPort = 14001;
            var tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);
            var client = new CvarcClient(tcpClient);

            client.Write(log.Configuration);
            client.Write(log.WorldState);
            client.Close();
            Thread.Sleep(500);

            var threads = new List<Thread>();
            foreach (var key in log.Commands)
            {
                var curKey = key;
                var tcpPlayerClient = new TcpClient();
                tcpPlayerClient.Connect(ip, playPort);
                var playerClient = new CvarcClient(tcpPlayerClient);
                var configProposal = new ConfigurationProposal()
                {
                    LoadingData = log.Configuration.LoadingData,
                    SettingsProposal = new SettingsProposal {CvarcTag = "log_runner"}
                };
                playerClient.Write(configProposal);
                playerClient.Write(log.WorldState);
                var thread = new Thread(() => ClientPlay(curKey.Item2, playerClient));
                thread.Start();
                threads.Add(thread);
                Thread.Sleep(500);
            }

            foreach (var thread in threads)
                thread.Join();
        }

        static void ClientPlay(IEnumerable<ICommand> commands, CvarcClient client)
        {
            try
            {
                foreach (var command in commands)
                {
                    client.Write(command);
                    client.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }
    }
}
