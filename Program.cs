using System;
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
            binding.MaxReceivedMessageSize = Settings.MAX_SOAP_MSG_SIZE;
            System.ServiceModel.EndpointAddress remoteAddress = new System.ServiceModel.EndpointAddress(endpoint);
            ChallengeService.ChallengeClient client = new ChallengeService.ChallengeClient(binding, remoteAddress);

            try
            {
                ChallengeService.state?[][] result = client.login();
                System.Console.WriteLine(result.Length);
                board = new Board(result);
                System.Console.WriteLine(board.ToString());

                bool inSync = false;
                int lastTick = 0;
                long sleepDelta = 0;
                int syncTarget = 2850;
                int missedTickCount = 0;

                while (true)
                {
                    // The main program loop handles synchronisation with the server's ticks. 
                    ChallengeService.game status = client.getStatus();

                    if (inSync)
                    {
                        /* We're supposed to be in sync, so check that the new tick number is exactly
                         * one larger than the last tick value. If it's the same, we should delay a
                         * little bit longer next time. If it's more than one larger, we're missing ticks
                         * (danger!) and should re-sync.
                         */
                        var ms = -status.millisecondsToNextTick;
                        if (status.currentTick == lastTick + 1)
                        {
                            Diagnostics.Sync.addTickPeriod(ms);
                            if (ms > 2850)
                                sleepDelta = 0;
                            else if ((ms <= 2850) && (ms > 2500))
                                sleepDelta = -10;
                            else if ((ms <= 2500) && (ms > 2000))
                                sleepDelta = -100;
                            else
                                inSync = false;
                        }
                        else if (status.currentTick == lastTick)
                            Diagnostics.Sync.dupedTicks += 1;
                        else
                        {
                            inSync = false;
                            Diagnostics.Sync.missedTicks += 1;
                            missedTickCount += 1;
                            if (missedTickCount >= 3)
                            {
                                missedTickCount = 0;
                                syncTarget -= 50;   // BUG: What happens when it falls below 2500?
                            }
                        }
                    }

                    if (!inSync)
                    {
                        /* If we're not in sync, use the time delta reported by the server (millisecondsToNextTick) to
                         * estimate how long we should wait to synchronise.
                         */
                        sleepDelta = -Settings.SYNC_TICK - status.millisecondsToNextTick + 200;
                        inSync = true;
                    }

                    System.Console.WriteLine("-------------------------------------------------");
                    System.Console.WriteLine("Player: {0} | Tick: {1}", status.playerName, status.currentTick);
                    System.Console.WriteLine("{0} ms to next tick", status.millisecondsToNextTick);

                    lastTick = status.currentTick;

                    System.Threading.Thread.Sleep((int)(Settings.SYNC_TICK + sleepDelta));
                }
            }
            finally
            {
                System.Console.WriteLine("-------------------------------------------------");
                System.Console.WriteLine("DIAGNOSTICS -- SYNCHRONISATION");
                System.Console.WriteLine("Average usable tick period: {0}", Diagnostics.Sync.avgTickPeriod);
                System.Console.WriteLine("Number of duped ticks: {0}", Diagnostics.Sync.dupedTicks);
                System.Console.WriteLine("Number of missed ticks: {0}", Diagnostics.Sync.missedTicks);
            }
        }
    }
}
