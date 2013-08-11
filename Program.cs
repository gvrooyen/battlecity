using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace battlecity
{
    class Program
    {
        static Clock clock;

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
            System.Console.WriteLine("handleNewTick()");
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
                long syncTarget = Settings.SYNC_TARGET;

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
                clock.Start(-status.millisecondsToNextTick);

                while (true)
                {
                    // The main program loop handles synchronisation with the server's ticks. 
                    status = client.getStatus();

                    if (inSync)
                    {
                        /* We're supposed to be in sync, so check that the new tick number is exactly
                         * one larger than the last tick value. If it's the same, we should delay a
                         * little bit longer next time. If it's more than one larger, we're missing ticks
                         * (danger!) and should re-sync.
                         */
                        var ms = -status.millisecondsToNextTick;

                        System.Console.WriteLine("-------------------------------------------------");
                        System.Console.WriteLine("Player: {0} | Tick: {1}", status.playerName, status.currentTick);
                        System.Console.WriteLine("{0} ms to next tick", status.millisecondsToNextTick);

                        if (status.currentTick == lastTick + 1)
                        {
                            Diagnostics.Sync.addTickPeriod(ms);
                            if (ms > syncTarget)
                                sleepDelta = 0;
                            else if ((ms <= syncTarget) && (ms > (syncTarget - Settings.SYNC_TARGET_BAND)))
                            {
                                sleepDelta = -Settings.SYNC_DELTA_STEP_LO;
                                System.Console.WriteLine("Synchronisation below target; stepping up by {0} ms.", -sleepDelta);
                            }
                            else if ((ms <= (syncTarget - Settings.SYNC_TARGET_BAND)) && (ms > (syncTarget - 2 * Settings.SYNC_TARGET_BAND)))
                            {
                                sleepDelta = -Settings.SYNC_DELTA_STEP_HI;
                                System.Console.WriteLine("Synchronisation FAR below target; stepping up by {0} ms.", -sleepDelta);
                            }
                            else if (ms > 0)
                            {
                                sleepDelta = ms;
                                System.Console.WriteLine("Near miss; pull back by {0} ms.", sleepDelta);
                            }
                            else
                            {
                                System.Console.WriteLine("Completely out of sync; resynchronising.");
                                inSync = false;
                            }
                        }
                        else if (status.currentTick == lastTick)
                        {
                            Diagnostics.Sync.dupedTicks += 1;
                            // Check again immediately
                            sleepDelta = -Settings.SYNC_TICK;
                            syncTarget -= Settings.SYNC_DELTA_STEP_LO;
                            System.Console.WriteLine("Duped tick! Adjusting target to {0} ms.", syncTarget);
                        }
                        else
                        {
                            inSync = false;
                            Diagnostics.Sync.missedTicks += 1;
                            syncTarget -= Settings.SYNC_DELTA_STEP_HI;
                            if (syncTarget < (syncTarget - 2 * Settings.SYNC_TARGET_BAND))
                                syncTarget = (syncTarget - 2 * Settings.SYNC_TARGET_BAND);
                        }
                    }

                    if (!inSync)
                    {
                        /* If we're not in sync, use the time delta reported by the server (millisecondsToNextTick) to
                         * estimate how long we should wait to synchronise.
                         */
                        sleepDelta = -Settings.SYNC_TICK - status.millisecondsToNextTick + Settings.SYNC_TARGET_BAND / 2;
                        inSync = true;
                    }

                    lastTick = status.currentTick;

                    var sleepTime = Settings.SYNC_TICK + sleepDelta;
                    if (sleepTime > 0)
                        System.Threading.Thread.Sleep((int)sleepTime);
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
