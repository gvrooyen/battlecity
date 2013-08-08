﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{
    class Program
    {
        static void Main(string[] args)
        {
            var info = new Microsoft.VisualBasic.Devices.ComputerInfo();
            Board board;

            System.Console.WriteLine(info.OSFullName);
            System.Console.WriteLine("Memory usage: {0}%", info.AvailablePhysicalMemory*100/info.TotalPhysicalMemory);

            var endpoint = "http://localhost:7070/Challenge/ChallengeService";
            if (args.Length > 0)
            {
                endpoint = args[0];
            }

            System.ServiceModel.BasicHttpBinding binding = new System.ServiceModel.BasicHttpBinding();
            binding.MaxReceivedMessageSize = 1024*1024;
            System.ServiceModel.EndpointAddress remoteAddress = new System.ServiceModel.EndpointAddress(endpoint);
            ChallengeService.ChallengeClient client = new ChallengeService.ChallengeClient(binding, remoteAddress);

            try
            {
                ChallengeService.state?[][] result = client.login();
                System.Console.WriteLine(result.Length);
                board = new Board(result);
                System.Console.WriteLine(board.ToString());
                while (true)
                {
                    ChallengeService.game status = client.getStatus();
                    System.Console.WriteLine("-------------------------------------------------");
                    System.Console.WriteLine("Player: {0} | Tick: {1}", status.playerName, status.currentTick);
                    System.Console.WriteLine("{0} ms to next tick", status.millisecondsToNextTick);
                    if (status.millisecondsToNextTick < 0)
                        System.Threading.Thread.Sleep((int)(-status.millisecondsToNextTick)+100);
                }
            }
            finally { }
        }
    }
}
