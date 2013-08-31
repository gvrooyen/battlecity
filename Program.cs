using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace battlecity
{
    class Program
    {
        static bool debug = true;

        static Clock clock;
        static ChallengeService.ChallengeClient client;
        static int lastTick = 0;
        static Board board;
        static AI bot;

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

            lock(client)
            {
                status = client.getStatus();
                board.Update(status);
            }

            ms = status.millisecondsToNextTick;
            Console.WriteLine("- TICK {0}, {1} ms to next tick", status.currentTick, ms);

            if (status.currentTick == lastTick)
            {
                // We've got a duped tick, so push the clock ahead
                Console.WriteLine("Duped tick; pulling clock.");
                Diagnostics.Sync.dupedTicks += 1;
                clock.Pull();
                
                // A duped tick means the expected tick is just around the corner; wait a moment and try again
                // TODO: Make sure the events from all status requests are handled correctly.
                while (status.currentTick == lastTick)
                {
                    Thread.Sleep((int)Settings.SYNC_DELTA_STEP_LO);
                    Console.WriteLine("Polling again for a new tick...");
                    lock (client)
                    {
                        status = client.getStatus();
                        board.Update(status);
                    }
                }
            }
            else if (status.currentTick - lastTick > 1)
            {
                // We've completely missed a tick, so reset the clock
                Console.WriteLine("Missed a tick! Pulling back hard.");
                Diagnostics.Sync.missedTicks += 1;
                // clock.Reset(status.millisecondsToNextTick + Settings.SYNC_INITIAL_DELAY);
                clock.Pull(5);
            }
            else if (status.millisecondsToNextTick <= 0)
            {
                Console.WriteLine("Borderline synchronisation; pulling clock.");
                clock.Pull();
            }
            else if ((status.currentTick > 1) && (status.millisecondsToNextTick < Settings.SYNC_TICK / 2))
            {
                // We're completely out of sync, so reset the clock
                Console.WriteLine("Out of sync! Pulling back hard.");
                // clock.Reset(status.millisecondsToNextTick + Settings.SYNC_INITIAL_DELAY);
                clock.Pull(5);
            }
            else if (ms < Settings.SYNC_TARGET - 2 * Settings.SYNC_DELTA_STEP_LO)
            {
                Console.WriteLine("Pushing clock");
                clock.Push();
            }
            else if (ms > Settings.SYNC_TARGET + 2 * Settings.SYNC_DELTA_STEP_LO)
            {
                Console.WriteLine("Pulling clock");
                clock.Pull();
            }

            lastTick = status.currentTick;

            if (ms > 0)
                Diagnostics.Sync.addTickPeriod(ms);

            if (debug)
            {
                // System.IO.File.WriteAllText(board.playerName + ".txt", board.ToString());
                StreamWriter file = new StreamWriter(File.Open(board.playerName + ".txt", FileMode.Open, FileAccess.Write, FileShare.ReadWrite));
                file.WriteLine(board.ToString());
                file.Close();
            }
        }

        static void postEarlyMove()
        {
            /* Post the best move found so far. This is done in case the postFinalMove()
             * handler does not get serviced before the current tick ends.
             */
            bot.postEarlyMove();
        }

        static void postFinalMove()
        {
            /* Post the final best move found.
             */
            bot.postFinalMove();
        }

        static void Main(string[] args)
        {
            // var info = new Microsoft.VisualBasic.Devices.ComputerInfo();
            string botname = "random";

            Console.BufferHeight = 4096;
            Console.WindowWidth = 132;
            Console.WindowHeight = 40;

            // System.Console.WriteLine(info.OSFullName);
            // System.Console.WriteLine("Memory usage: {0}%", info.AvailablePhysicalMemory*100/info.TotalPhysicalMemory);

            var endpoint = "http://localhost:7070/Challenge/ChallengeService";
            if (args.Length > 0)
            {
                endpoint = args[0];
            }
            if (args.Length > 1)
            {
                botname = args[1].ToLower();
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
                board.Update(status);
                board.playerName = status.playerName;
                if (status.players[0].name == status.playerName)
                    board.playerID = 0;
                else if (status.players[1].name == status.playerName)
                    board.playerID = 1;
                else
                    throw new ArgumentException("Player '{0}' not found in player list.", status.playerName);

                board.playerBase.x = status.players[board.playerID].@base.x;
                board.playerBase.y = status.players[board.playerID].@base.y;
                board.opponentBase.x = status.players[board.opponentID].@base.x;
                board.opponentBase.y = status.players[board.opponentID].@base.y;

                Console.WriteLine("Welcome, {0} (#{1})", board.playerName, board.playerID);

                if ((status.players[board.playerID].bullets == null) && (status.players[board.opponentID].bullets == null))
                    Console.WriteLine("No bullets in play yet.");
                else
                    Console.WriteLine("WARNING: bullets already in play!");

                int i = 0;
                foreach (ChallengeService.unit u in status.players[board.playerID].units)
                {
                    board.playerTank[i++] = new Tank(u.x, u.y, u.direction, u.id);
                    Console.WriteLine("Player tank ID {0} starts at ({1},{2}), facing {3}.",
                        u.id, u.x, u.y, u.direction);
                }

                i = 0;
                foreach (ChallengeService.unit u in status.players[board.opponentID].units)
                {
                    board.opponentTank[i++] = new Tank(u.x, u.y, u.direction, u.id);
                    Console.WriteLine("Opponent tank ID {0} starts at ({1},{2}), facing {3}.",
                        u.id, u.x, u.y, u.direction);
                }

                Console.WriteLine("Testing path planner");
                PathPlanner planner = new PathPlanner();
                planner.mapBoard(board);
                planner.renderMap(board, board.playerName + ".png");

                if (board.playerName == "Player One")
                    botname = "aggro";

                switch (botname)
                {
                    case "random":
                        bot = new AI_Random(board, client);
                        break;
                    case "aggro":
                        bot = new AI_Aggro(board, client);
                        break;
                    default:
                        bot = new AI_Random(board, client);
                        break;
                }

                Console.WriteLine(Settings.SYNC_INITIAL_DELAY);
                clock.Start(status.millisecondsToNextTick + Settings.SYNC_INITIAL_DELAY);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void ExceptionHandler(Task t)
        {
            AggregateException e = t.Exception;
            if (e.InnerException.GetType() == typeof(System.ServiceModel.EndpointNotFoundException))
            {
                Console.WriteLine(Environment.NewLine + "GAME OVER!");
                Program.clock.Abort();
            }
            else
            {
                Console.WriteLine("CRITICAL: Caught {0}", e.InnerException.GetType());
            }
        }

    }
}
