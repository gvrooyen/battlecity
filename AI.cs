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

        protected bool FireObstructionsExist(int x1, int y1, int x2, int y2)
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

        protected bool MoveObstructionsExit(int x1, int y1, int x2, int y2, out int xo, out int yo)
        {
            // default "not found" values for the output variables
            xo = -1; 
            yo = -1;

            if (y1 == y2)
            {
                int dx = (x2 > x1) ? 1 : -1;
                for (int x = x1; x <= x2; x += dx)
                    // TODO: It's worthwhile here to inspect coordinates in the order dy = {0, \pm1, \pm2}, because
                    //       that implies the lowest movement cost to remove obstructions.
                    for (int dy = -2; dy <= 2; dy++)
                        if (board.getState(x, y1 + dy) == ChallengeService.state.FULL)
                        {
                            xo = x;
                            xo = y1 + dy;
                            return true;
                        }
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

        protected ChallengeService.action MoveOrFire(Tank tank, ChallengeService.direction direction)
        {
            /* Return an action which instructs a tank to fire in the specified direction,
             * first performing a move in that direction, if necessary.
             */

            if (tank.direction == direction)
                return ChallengeService.action.FIRE;
            else
                return MoveDirection(direction);
        }
		
		protected ChallengeService.action RunAndGun(Tank tank, int destX, int destY)
		{
			/* Move towards the destination coordinates (L-shaped path, longest edge first).
			 * If any obstructions are found en route, destroy them.
			 * 
			 * The function just returns the next move required in order to act on this
			 * strategy. The path is searched along the longest edge, to determine whether
			 * any obstructions exist along that path. If not, the tank simply moves in that
			 * direction.
			 * 
			 * For the first obstruction found along the path, the tank will attempt to move
			 * laterally to come in line with the obstruction. Of course, that movement may
			 * come across obstacles as well, which must be removed first, possibly requiring
			 * another lateral movement. This search is done recursively until a valid move
			 * or firing direction is found.
			 * 
			 * It is possible for tanks to get trapped, e.g. in the following map:
			 *
			 * ·········
             * ··#···#··
             * ·#XXXXX#·
             * ··XXXXX··
             * ··XXXXO··
             * ··XXXXX··
             * ·#XXXXX#·
             * ··#···#··
             * ·········
             * 
             * No combination of movement or firing can let the tank move. However, this
             * condition can only occur if the tank is placed in such a position at the
             * start of the game -- if you can move in somewhere, you can move out again.
             * 
             * Nonetheless, this strategy should detect if no combination of actions can get
             * the tank to the desired destination, and return NONE as resulting action.
			 */
			
			int Lx = destX - tank.x;
			int Ly = destX - tank.y;
			int dx = 0;
			int dy = 0;
			int[] scanOrder = {0,1,-1,-2,2};
			
			if ((Lx == 0) && (Ly == 0))
				return ChallengeService.action.NONE;
			
			if (Math.Abs(Lx) > Math.Abs(Ly))
				dx = Math.Sign(Lx);
		    else
				dy = Math.Sign(Ly);
			
			
			// Do a search along the 5-blocks-wide path for obstacles
			
			int obstX = -1;
			int obstY = -1;
			
			for (int x = tank.x + dx*3, y = tank.y + dy*3; 
			     (x <= destX + dx*2) && (destY <= destY + dy*2);
			     x += dx, y += dy)
			{
				/* Depending on whether we're scanning horizontally or vertically, we look for
				 * obstacles in a vertical or horizontal line. We start looking for obstacles at
				 * the center point first, because these are easiest to destroy (and will take out
				 * any neighbouring blocks).
				 */
				
				if (dx == 0)
				{
					// We're running vertically, so look for obstacles in a horizontal line.
					foreach (int i in scanOrder)
					{
						if (board.board[x+scanOrder[i]][y] == ChallengeService.state.FULL)
						{
							obstX = x + scanOrder[i];
							obstY = y;
							break;
						}
					}
				}
				else
				{
					// We're running horizontally, so look for obstacles in a vertical line.
					foreach (int i in scanOrder)
					{
						if (board.board[x][y+scanOrder[i]] == ChallengeService.state.FULL)
						{
							obstX = x;
							obstY = y + scanOrder[i];
							break;
						}
					}
				}
				
			}
						
			if ((obstX == -1) || (obstY == -1))
				// The path's clear!
				if (dx == -1)
					return ChallengeService.action.LEFT;
				else if (dx == 1)
					return ChallengeService.action.RIGHT;
				else if (dy == -1)
					return ChallengeService.action.UP;
				else if (dy == 1)
					return ChallengeService.action.DOWN;
				else
					return ChallengeService.action.NONE;
			
			if (((dx == 0) && (obstY == tank.y)) || (dy == 0) && (obstX == tank.x))
				// The next obstacle is perfectly lined up with the tank. If we're facing
				// the right direction, fire. Otherwise, move into the right direction
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

            A1 = ChallengeService.action.NONE;
            A2 = ChallengeService.action.NONE;

            // If both tanks exist, one goes for the base, and one goes for an enemy tank
            if (!tank0.destroyed && !tank1.destroyed)
            {
                // Tank 0 goes for the base
                dx = board.opponentBase.x - tank0.x;
                dy = board.opponentBase.y - tank0.y;

                // If we're in line, start firing
                if (dx == 0)
                {
                    if (dy < 0)
                        A1 = MoveOrFire(tank0, ChallengeService.direction.UP);
                    else
                        A1 = MoveOrFire(tank0, ChallengeService.direction.DOWN);
                }
                else if (dy == 0)
                {
                    if (dx < 0)
                        A1 = MoveOrFire(tank0, ChallengeService.direction.LEFT);
                    else
                        A1 = MoveOrFire(tank0, ChallengeService.direction.RIGHT);
                }
                else
                {
                    // Not in line yet. Take the shortest path (even if it has obstructions)
                    if (Math.Abs(dx) >= Math.Abs(dy))
                    {
                        if (board.opponentBase.y > tank0.y)
                            targetDirection = ChallengeService.direction.DOWN;
                        else
                            targetDirection = ChallengeService.direction.UP;
                    }
                    else
                    {
                        if (board.opponentBase.x > tank0.x)
                            targetDirection = ChallengeService.direction.RIGHT;
                        else
                            targetDirection = ChallengeService.direction.LEFT;
                    }

                    if (tank0.direction != targetDirection)
                        // Always move or at least point our guns in the right direction.
                        A1 = MoveDirection(targetDirection);
                    else
                    {
                        if (FireObstructionsExist(tank0.x, tank0.y, board.opponentBase.x, tank0.y))
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
                System.Console.WriteLine("Tank 1 {0}; Tank 2 {1}", A1, A2);
                if (!board.playerTank[0].destroyed)
                    lock (client)
                        client.setAction(board.playerTank[0].id, A1);
                if (!board.playerTank[1].destroyed)
                    lock (client)
                        client.setAction(board.playerTank[1].id, A2);
            }
        }

    }

}
