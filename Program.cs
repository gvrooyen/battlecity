using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace battlecity
{
    class Program
    {
        static Clock clock;
        static ChallengeService.ChallengeClient client;

        static void handleNewTick()
        {
            /* At the start of a new clock cycle, the following should happen:
             * 1. Check the server status, and make sure we've actually advanced a
             *    tick. If not (we're still at the same tick), we're slightly out of
             *    sync, so bump the clock ahead. If we completely missed a tick,
             *    reset the clock.
             * 2. Process all received events.
             * 3. Check that the executed moves correspond to the posted moves, and
             *    output a warning if this is not the case.
             */
            ChallengeService.game status;
            long ms;

            Console.WriteLine("handleNewTick()");
            lock (client)
            {
                status = client.getStatus();
            }

            // TODO: Add recovery logic for when ticks are duped

            ms = -status.millisecondsToNextTick;
            Console.WriteLine("- TICK {0}, {1} ms to next tick", status.currentTick, ms);
            if (ms < Settings.SYNC_TARGET - 2 * Settings.SYNC_DELTA_STEP_LO)
            {
                Console.WriteLine("Pulling clock");
                clock.Push();
            }
            else if (ms > Settings.SYNC_TARGET + 2 * Settings.SYNC_DELTA_STEP_LO)
            {
                Console.WriteLine("Pushing clock");
                clock.Pull();
            }
        }

        static void postEarlyMove()
        {
            /* Post the best move found so far. This is done in case the postFinalMove()
             * handler does not get serviced before the current tick ends.
             */
            System.Console.WriteLine("postEarlyMove()");
        }

        static void postFinalMove()
        {
            /* Post the final best move found.
             */
            System.Console.WriteLine("postFinalMove()");
        }

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
            client = new ChallengeService.ChallengeClient(binding, remoteAddress);

            try
            {
                ChallengeService.state?[][] result = client.login();
                System.Console.WriteLine(result.Length);
                board = new Board(result);
                System.Console.WriteLine(board.ToString());

                // Set up the clock, and add all the tasks that should execute each cycle.
                clock = new Clock(Settings.SYNC_TICK, Settings.SYNC_DELTA_STEP_LO);

                // At the start of each cycle, process all events and prune the game trees.
                clock.AddTask(0, handleNewTick);

                // Some time through the cycle, post a preliminary move as backup.
                clock.AddTask(Settings.SCHEDULE_EARLY_MOVE, postEarlyMove);

                // Just before the end of the cycle, post the final best move found.
                clock.AddTask(Settings.SCHEDULE_FINAL_MOVE, postFinalMove);

                // In the first tick, the server status is read, and the clock is started based
                // on the reported millisecondsToNextTick.
                ChallengeService.game status = client.getStatus();
                // TODO: Process first batch of events.

                Console.WriteLine(Settings.SYNC_INITIAL_DELAY);
                clock.Start(-status.millisecondsToNextTick + 200);

                while (true)
                {
                    // Now everything runs in the background.
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
