using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{
    abstract class AI
    {
        protected Board board;
        protected ChallengeService.ChallengeClient client;

        public AI() { }
        public AI(Board board, ChallengeService.ChallengeClient client)
        {
            this.board = board;
            this.client = client;
        }

        public abstract void postEarlyMove();
        public abstract void postFinalMove();

        protected bool ObstructionsExist(int x1, int y1, int x2, int y2)
        {
            if (y1 == y2)
            {
                int dx = (x2 > x1) ? 1 : -1;
                for (int x = x1; x <= x2; x += dx)
                    if (board.getState(x,y1) == ChallengeService.state.FULL)
                        return true;
            }
            else if (x1 == x2)
            {
                int dy = (y2 > y1) ? 1 : -1;
                for (int y = y1; y <= y2; y += dy)
                    if (board.getState(x1, y) == ChallengeService.state.FULL)
                        return true;
            }
            else
            {
                Console.WriteLine("WARNING: Can only check for obstructions in a straight horizontal/vertical line.");
            }
            return false;
        }

        protected ChallengeService.action MoveDirection(ChallengeService.direction direction)
        {
            /* Return an action which instructs a tank to move in the specified direction.
             */

            switch (direction)
            {
                case ChallengeService.direction.DOWN: return ChallengeService.action.DOWN;
                case ChallengeService.direction.LEFT: return ChallengeService.action.LEFT;
                case ChallengeService.direction.RIGHT: return ChallengeService.action.RIGHT;
                case ChallengeService.direction.UP: return ChallengeService.action.UP;
                default: return ChallengeService.action.NONE;
            }
        }

    }

    class AI_Random: AI
    {
        /* A simple bot that just issues random commands to both tanks.
         * No checks are done to see whether a tank has been destroyed.
         */

        static readonly Random random = new Random(Guid.NewGuid().GetHashCode());

        public AI_Random() : base() { }
        public AI_Random(Board board, ChallengeService.ChallengeClient client) : base(board, client) { }

        public override void postEarlyMove()
        {
            // do nothing
        }

        public override void postFinalMove()
        {
            Array actions = Enum.GetValues(typeof(ChallengeService.action));
            ChallengeService.action A1 = (ChallengeService.action)actions.GetValue(random.Next(actions.Length));
            ChallengeService.action A2 = (ChallengeService.action)actions.GetValue(random.Next(actions.Length));
            System.Console.WriteLine("Tank 1 {0}; Tank 2 {1}", A1, A2);
            lock (client)
            {
                // client.setActions(A1, A2);
                client.setAction(board.playerTank[0].id, A1);
                client.setAction(board.playerTank[1].id, A2);
            }
        }
    }

    class AI_Aggro : AI
    {
        /* The first tank takes the shortest route to get in line with the base (blowing up anything in
         * the way, if need be), and then fires towards the base until all obstructions are gone and the
         * base destroyed. The second tank gets in line with the closest enemy tank (clearing the route,
         * if need be), fires until it is destroyed, and then targets the second tank.
         */

        ChallengeService.action A1, A2;

        public AI_Aggro() : base() { }
        public AI_Aggro(Board board, ChallengeService.ChallengeClient client) : base(board, client)
        {
            A1 = ChallengeService.action.NONE;
            A2 = ChallengeService.action.NONE;
        }

        public override void postEarlyMove()
        {
            // We'll do most of our calculations here, and just post the actions in the final move.

            int dx, dy;
            Tank tank0 = board.playerTank[0];
            Tank tank1 = board.playerTank[1];
            ChallengeService.direction targetDirection;

            // If both tanks exist, one goes for the base, and one goes for an enemy tank
            if (!tank0.destroyed && !tank1.destroyed)
            {
                // Tank 0 goes for the base
                dx = board.opponentBase.x - tank0.x;
                dy = board.opponentBase.y - tank0.y;

                // If we're in line, start firing

                // TODO: Add firing logic

                // Take the shortest path (even if it has obstructions)
                if (Math.Abs(dx) >= Math.Abs(dy))
                {
                    if (board.opponentBase.x > tank0.x)
                        targetDirection = ChallengeService.direction.RIGHT;
                    else
                        targetDirection = ChallengeService.direction.LEFT;

                    if (tank0.direction != targetDirection)
                        // Always move or at least point our guns in the right direction.
                        A1 = MoveDirection(targetDirection);
                    else
                    {
                        if (ObstructionsExist(tank0.x, tank0.y, board.opponentBase.x, tank0.y))
                            // Get rid of the blocks in the way first
                            A1 = ChallengeService.action.FIRE;
                        else
                            // The line is clear, go for it!
                            A1 = MoveDirection(targetDirection);
                    }
                }
            }
        }

        public override void postFinalMove()
        {
            lock (client)
            {
                if (!board.playerTank[0].destroyed)
                    client.setAction(board.playerTank[0].id, A1);
                if (!board.playerTank[1].destroyed)
                    client.setAction(board.playerTank[1].id, A2);
            }
        }

    }

}
