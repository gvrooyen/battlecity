﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace battlecity
{
    /* The planner uses a queue of Plan objects that represent future plans, at various levels of
     * abstraction. The base class, Plan is completely abstract. Derived classes may represent
     * certain "classes" of actions (e.g. move or fire in an unspecified direction), goals that may
     * later expand into several actions (e.g. "move to (x,y)"), or atomic actions (e.g. "move left").
     * 
     * Plans can include sub-plans. For example, a "RunAndGun" plan might have the overarching goal
     * of reaching a certain destination, moving horizontally first. The RunAndGun object has a list
     * of child Plans, which may break down the goal into smaller, more concrete actions.
     */

    namespace Plan
    {
        public abstract class Plan
        {
            public static Dictionary<ChallengeService.direction, ChallengeService.action> moveDirection
                = new Dictionary<ChallengeService.direction,ChallengeService.action>
            {
                {ChallengeService.direction.LEFT, ChallengeService.action.LEFT},
                {ChallengeService.direction.RIGHT, ChallengeService.action.RIGHT},
                {ChallengeService.direction.UP, ChallengeService.action.UP},
                {ChallengeService.direction.DOWN, ChallengeService.action.DOWN},
                {ChallengeService.direction.NONE, ChallengeService.action.NONE}
            };

            public LinkedList<Plan> subplans { get; set; }
            public string description { get; set; }

            public Plan()
            {
                subplans = new LinkedList<Plan>();
            }

            public virtual string PrintParameters()
            {
                return null;
            }

            public override string ToString()
            {
                StringBuilder result = new StringBuilder(this.GetType().ToString());

                string parameters = PrintParameters();
                if (parameters != null)
                {
                    result.Append(" - ").Append(parameters);
                }

                if (description != null)
                {
                    result.Append(": ").Append(description);
                }
                result.Append(Environment.NewLine);

                if (subplans.First != null)
                {
                    foreach (Plan plan in subplans)
                    {
                        StringBuilder subResult = new StringBuilder(plan.ToString());
                        // Indent all lines in the subplan description
                        subResult.Insert(0, "    ");
                        subResult.Replace(Environment.NewLine, Environment.NewLine + "    ", 0, subResult.Length - 1);
                        result.Append(subResult);
                    }
                }

                return result.ToString();
            }
        }

        public class Separator : Plan
        {
            /* This class is used as a separator when planning sequences of independent actions. It is
             * transparantly removed when executing plans, and ignored (never replaced) when adding
             * new plans to a planning list.
             */
            public Separator() : base() { }
        }

        public class Navigate : Plan
        {
            public int destX { get; set; }
            public int destY { get; set; }
            public ChallengeService.direction currentDirection { get; set; }

            public Navigate() : base()
            {
                destX = -1;
                destY = -1;
            }

            public Navigate(int destX, int destY) : base()
            {
                this.destX = destX;
                this.destY = destY;
            }

            public override string PrintParameters()
            {
                StringBuilder result = new StringBuilder(base.PrintParameters());
                if (result.Length > 0)
                    result.Append(", ");
                if (currentDirection != ChallengeService.direction.NONE)
                    result.Append(String.Format("heading {0} towards ({1},{2})", currentDirection, destX, destY));
                else
                    result.Append(String.Format("heading towards ({0},{1})", destX, destY));
                return result.ToString();
            }
        }

        public class RunAndGun : Navigate
        {
            private void Initialize()
            {
                currentDirection = ChallengeService.direction.NONE;
            }

            public RunAndGun() : base() { }

            public RunAndGun(int destX, int destY) : base(destX, destY) { }
        }

        public class ConcreteAction : Plan
        {
            public ChallengeService.action action { get; protected set; }
            public ConcreteAction() : base() { }
        }

        public class Left : ConcreteAction
        {
            public Left() : base()
            {
                action = ChallengeService.action.LEFT;
            }
        }

        public class Right : ConcreteAction
        {
            public Right() : base()
            {
                action = ChallengeService.action.RIGHT;
            }
        }

        public class Up : ConcreteAction
        {
            public Up() : base()
            {
                action = ChallengeService.action.UP;
            }
        }

        public class Down : ConcreteAction
        {
            public Down() : base()
            {
                action = ChallengeService.action.DOWN;
            }
        }

        public class Move : ConcreteAction
        {
            public Move() : base()
            {
                action = ChallengeService.action.NONE;
            }

            public Move(ChallengeService.direction direction) : base()
            {
                action = Plan.moveDirection[direction];
            }
        }

        public class Fire : ConcreteAction
        {
            public Fire() : base() { }
        }
    }

    /* Player units can be assigned roles, which helps them to differentiate their actions on the
     * battlefield. Opponents' roles can also be inferred, in order to help player units to plan
     * appropriate responses.
     */

    namespace Role
    {
        abstract class Role { }

        class DefendBase : Role { /* Defend the player base */ }
        class AttackBase : Role { /* Attack the opponent's base */ }
        class Sniper : Role { /* Target the closest enemy unit */ }

        class Assassin : Role /* Hunt down and kill the specified enemy unit */
        {
            public Tank target { get; set; }

            public Assassin() { }
            public Assassin(Tank target) { this.target = target; }
        }
    }

    /*********************************************************************************************/

    /* AI class
     */

    abstract class AI
    {
        protected Board board;
        protected ChallengeService.ChallengeClient client;
        protected LinkedList<Plan.Plan> plans;
        protected Role.Role role;

        private void Initialize()
        {
            plans = new LinkedList<Plan.Plan>();
        }

        public AI()
        {
            Initialize();
        }

        public AI(Board board, ChallengeService.ChallengeClient client)
        {
            Initialize();
            this.board = board;
            this.client = client;
        }

        public abstract void newTick();
        public abstract void postEarlyMove();
        public abstract void postFinalMove();

        protected void directionToXY(ChallengeService.direction direction, out int dx, out int dy)
        {
            switch (direction)
            {
                case ChallengeService.direction.LEFT:
                    dx = -1;
                    dy = 0;
                    break;
                case ChallengeService.direction.RIGHT:
                    dx = 1;
                    dy = 0;
                    break;
                case ChallengeService.direction.UP:
                    dx = 0;
                    dy = -1;
                    break;
                case ChallengeService.direction.DOWN:
                    dx = 0;
                    dy = 1;
                    break;
                default:
                    dx = 0;
                    dy = 0;
                    break;
            }
        }

        protected ChallengeService.direction xyToDirection(int dx, int dy)
        {
            if ((dx < 0) && (dy == 0))
                return ChallengeService.direction.LEFT;
            else if ((dx > 0) && (dy == 0))
                return ChallengeService.direction.RIGHT;
            else if ((dx == 0) && (dy < 0))
                return ChallengeService.direction.UP;
            else if ((dx == 0) && (dy > 0))
                return ChallengeService.direction.DOWN;
            else
                return ChallengeService.direction.NONE;
        }

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
                Debug.WriteLine("WARNING: Can only check for obstructions in a straight horizontal/vertical line.");
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
                Debug.WriteLine("WARNING: Can only check for obstructions in a straight horizontal/vertical line.");
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

        public string PrintPlans()
        {
            StringBuilder result = new StringBuilder("");
            foreach (var plan in plans)
            {
                result.Append(plan.ToString());
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

		protected ChallengeService.action RunAndGun(Tank tank, int destX, int destY, int recursionDepth = 0)
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

            Plan.RunAndGun plan;

            if (recursionDepth > 0)
            {
                // Subplans are in play, so we don't need to do anything here.
            }
            if ((plans.First == null) || (plans.First.Value.GetType() == typeof(Plan.Separator)))
            {
                // Option 1: We have no plans yet. This is the first one. Remember it.
                // Option 2: We should start executing a new RunAndGun plan, before executing the next plan.
                plan = new Plan.RunAndGun(destX, destY);
                plans.AddFirst(plan);
            }
            else if (plans.First.Value.GetType() == typeof(Plan.RunAndGun))
            {
                // We're currently executing a run-and-gun plan, but the destination may have changed.
                plan = (Plan.RunAndGun)plans.First.Value;
                if ((plan.destX != destX) || (plan.destY != destY))
                {
                    // A new destination has been specified, so ditch the previous plan and create a new one.
                    plans.RemoveFirst();
                    plan = new Plan.RunAndGun(destX, destY);
                    plans.AddFirst(plan);
                }
            } else {
                // Replace the old plan with a new one.
                plans.RemoveFirst();
                plan = new Plan.RunAndGun(destX, destY);
                plans.AddFirst(plan);
            }

            while (plan.subplans.First != null)
            {
                // We already know what to do. Just do it.

                Plan.Plan subplan = plan.subplans.First.Value;
                plan.subplans.RemoveFirst();

                if (subplan.GetType() == typeof(Plan.ConcreteAction))
                {
                    return ((Plan.ConcreteAction)plan.subplans.First.Value).action;
                }
                else
                {
                    Debug.WriteLine("ERROR: Subplan action {0} is not a concrete action in RunAndGun(); removing it.",
                        plan.subplans.First.GetType());
                }
            }

			int Lx = destX - tank.x;
			int Ly = destX - tank.y;
			int dx = 0;
			int dy = 0;
			int[] scanOrder = {0,1,-1,-2,2};
			
			if ((Lx == 0) && (Ly == 0))
            {
                // We've reached our goal. Move on to the next part of the plan, or wait for new instructions.
                plans.RemoveFirst();
                if (plans.First == null)
				    return ChallengeService.action.NONE;
                else if (plans.First.Value.GetType() == typeof(Plan.RunAndGun))
                {
                    Plan.RunAndGun nextPlan = (Plan.RunAndGun)plans.First.Value;

                    // We effectively retain rather than increase the recursion depth in the next call,
                    // because we've popped out of a subplan.
                    return RunAndGun(tank, nextPlan.destX, nextPlan.destY, recursionDepth);
                }
            }

            if (Lx == 0)
            {
                // We're in horizontally in line with the destination, only the vertical leg is left.
                // This overrides any previous direction we might have had in our plan.
                dy = Math.Sign(Ly);
            }
            else if (Ly == 0)
            {
                // We're vertically in line with the destination, only the horizontal leg is left
                // This overrides any previous direction we might have had in our plan.
                dx = Math.Sign(Ly);
            }
            else if (plan.currentDirection != ChallengeService.direction.NONE)
            {
                // We have both the horizontal and vertical leg left to execute. Stick with what we
                // were doing in the last tick.
                directionToXY(plan.currentDirection, out dx, out dy);
            }
            else
            {
                // We haven't moved on this plan yet, so start with the longest leg of the L first.
                if (Math.Abs(Lx) > Math.Abs(Ly))
                    dx = Math.Sign(Lx);
                else
                    dy = Math.Sign(Ly);
            }

            // Save the current target and the direction we're moving in, in the plan.
            plan.destX = destX;
            plan.destY = destY;
            plan.currentDirection = xyToDirection(dx, dy);
			
			// Do a search along the 5-blocks-wide path for obstacles
			
			int obstX = -1;
			int obstY = -1;

            /* Depending on whether we're scanning horizontally or vertically, we look for
             * obstacles in a vertical or horizontal line. We start looking for obstacles at
             * the center point first, because these are easiest to destroy (and will take out
             * any neighbouring blocks).
             */

            if ((dx == 0) && (dy == -1))
            {
                int x = tank.x;
                // We're running up, so look for obstacles in a horizontal line upwards.
                for (int y = tank.y - 3; y >= destY - 2; y--)
                {
                    foreach (int i in scanOrder)
                    {
                        if ((x + i >= 0) && (x + i < board.xsize) && (y >= 0) && (y < board.ysize) && (board.board[x + i][y] == ChallengeService.state.FULL))
                        {
                            obstX = x + i;
                            obstY = y;
                            break;
                        }
                    }
                }
            }
            else if ((dx == 0) && (dy == 1))
            {
                // We're running down, so look for obstacles in a horizontal line downwards.
                int x = tank.x;
                for (int y = tank.y + 3; y <= destY + 2; y++)
                {
                    foreach (int i in scanOrder)
                    {
                        if ((x + i >= 0) && (x + i < board.xsize) && (y >= 0) && (y < board.ysize) && (board.board[x + i][y] == ChallengeService.state.FULL))
                        {
                            obstX = x + i;
                            obstY = y;
                            break;
                        }
                    }
                }
            }
            else if ((dy == 0) && (dx == -1))
            {
                // We're running left, so look for obstacles in a vertical line leftwards.
                int y = tank.y;
                for (int x = tank.x - 3; x >= destX - 2; x--)
                {
                    foreach (int i in scanOrder)
                    {
                        if ((y + i >= 0) && (y + i < board.ysize) && (x >= 0) && (x < board.xsize) && (board.board[x][y + i] == ChallengeService.state.FULL))
                        {
                            obstX = x;
                            obstY = y + i;
                            break;
                        }
                    }
                }
            }
            else if ((dy == 0) && (dx == 1))
            {
                // We're running right, so look for obstacles in a vertical line rightwards.
                int y = tank.y;
                for (int x = tank.x - 3; x >= destX - 2; x--)
                {
                    foreach (int i in scanOrder)
                    {
                        if ((y + i >= 0) && (y + i < board.ysize) && (x >= 0) && (x < board.xsize) && (board.board[x][y + i] == ChallengeService.state.FULL))
                        {
                            obstX = x;
                            obstY = y + i;
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
            {
                // The next obstacle is perfectly lined up with the tank. If we're facing
                // the right direction and no bullet is in play, fire. Otherwise, move into the right direction
                if (dx == -1)
                {
                    if ((tank.direction == ChallengeService.direction.LEFT) && (tank.bullet == null))
                        return ChallengeService.action.FIRE;
                    return ChallengeService.action.LEFT;
                }
                if (dx == +1)
                {
                    if ((tank.direction == ChallengeService.direction.RIGHT) && (tank.bullet == null))
                        return ChallengeService.action.FIRE;
                    return ChallengeService.action.RIGHT;
                }
                if (dy == -1)
                {
                    if ((tank.direction == ChallengeService.direction.UP) && (tank.bullet == null))
                        return ChallengeService.action.FIRE;
                    return ChallengeService.action.UP;
                }
                if (dy == +1)
                {
                    if ((tank.direction == ChallengeService.direction.DOWN) && (tank.bullet == null))
                        return ChallengeService.action.FIRE;
                    return ChallengeService.action.DOWN;
                }
            }
            else
            {
                /* We need to move a bit to get into firing position. This is where we need to do
                 * some longer-term planning (for the previous cases considered, a next call to
                 * RunAndGun after the returned action, with the same destination, would just have
                 * calculated the next move or the next obstacle to destroy. Now, however, we *at least*
                 * need to ensure that the current obstacle does, in fact, get destroyed -- assuming
                 * that we keep heading towards the original target.
                 * 
                 * The following need to happen to destroy the obstacle:
                 * 1. Move (recursive RunAndGun) laterally to get in line with the obstacle.
                 * 2. Move in the direction of the obstacle to point the turret
                 * 3. Fire to destroy the obstacle
                 * 4. Move back to the current position
                 * and continue with the main plan.
                 * 
                 * The above actions are added pushed into the planning queue in reverse order, so that
                 * they are popped in the order described above.
                 */

                // 4. Move back to the current position
                plan.subplans.AddFirst(new Plan.RunAndGun(tank.x, tank.y));

                // 3. Fire to destroy the obstacle
                plan.subplans.AddFirst(new Plan.Fire());

                // 2. Move in the direction of the obstacle to point the turret
                plan.subplans.AddFirst(new Plan.Move(plan.currentDirection));

                // 1. Move (recursive RunAndGun) laterally to get in line with the obstacle.

                plans.AddFirst(new Plan.RunAndGun());   // We're going to recurse
                if (dx != 0)
                {
                    // We're moving horizontally, so get in line with the obstacle by moving vertically
                    return RunAndGun(tank, tank.x, obstY, recursionDepth+1);
                }
                else
                {
                    // We're moving vertically, so get in line with the obstacle by moving horizontally
                    return RunAndGun(tank, obstX, tank.y, recursionDepth+1);
                }

            }

            Debug.WriteLine("ERROR: RunAndGun() found no valid action to execute.");
            return ChallengeService.action.NONE;
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

        public override void newTick()
        {
            // do nothing
        }

        public override void postEarlyMove()
        {
            // do nothing
        }

        public override void postFinalMove()
        {
            Array actions = Enum.GetValues(typeof(ChallengeService.action));
            ChallengeService.action A1 = (ChallengeService.action)actions.GetValue(random.Next(actions.Length));
            ChallengeService.action A2 = (ChallengeService.action)actions.GetValue(random.Next(actions.Length));
            Debug.WriteLine("Tank 1 {0}; Tank 2 {1}", A1, A2);
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

        public override void newTick()
        {
            // We'll do most of our calculations here, and just post the actions in the final move.

            Tank tank0 = board.playerTank[0];
            Tank tank1 = board.playerTank[1];

            A1 = ChallengeService.action.NONE;
            A2 = ChallengeService.action.NONE;

            // If both tanks exist, one goes for the base, and one goes for an enemy tank
            if (!tank0.destroyed && !tank1.destroyed)
            {
                // Tank 0 goes for the base
                Debug.WriteLine("Calculating next move...");
                A1 = RunAndGun(tank0, board.opponentBase.x, board.opponentBase.y);
                Debug.WriteLine("Settled on action {0}", A1);
            }
        }

        public override void postEarlyMove()
        {
            // Do nothing
        }

        public override void postFinalMove()
        {
            lock (client)
            {
                Debug.WriteLine("Tank 1 {0}; Tank 2 {1}", A1, A2);
                if (!board.playerTank[0].destroyed)
                    lock (client)
                        client.setAction(board.playerTank[0].id, A1);
                if (!board.playerTank[1].destroyed)
                    lock (client)
                        client.setAction(board.playerTank[1].id, A2);
            }

            Debug.WriteLine(PrintPlans());
        }

    }

}
