using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

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

            public static Dictionary<ChallengeService.action, ChallengeService.direction> actionDirection
                = new Dictionary<ChallengeService.action, ChallengeService.direction>
            {
                {ChallengeService.action.LEFT, ChallengeService.direction.LEFT},
                {ChallengeService.action.RIGHT, ChallengeService.direction.RIGHT},
                {ChallengeService.action.UP, ChallengeService.direction.UP},
                {ChallengeService.action.DOWN, ChallengeService.direction.DOWN},
                {ChallengeService.action.NONE, ChallengeService.direction.NONE}
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

            public override string PrintParameters()
            {
                StringBuilder result = new StringBuilder(base.PrintParameters());
                if (result.Length > 0)
                    result.Append(", ");
                ChallengeService.direction direction = actionDirection[action];
                if (direction != ChallengeService.direction.NONE)
                    result.Append(direction.ToString());
                else
                    result.Append("standing still");
                return result.ToString();
            }
        }

        public class Fire : ConcreteAction
        {
            public Fire() : base()
            {
                action = ChallengeService.action.FIRE;
            }
        }

        public class FireToDestroy : Fire
        {
            /* Fire to destroy the block at the specified coordinate. If that block is no
             * longer FULL, this plan is ignored.
             */

            int targetX;
            int targetY;

            public FireToDestroy() : base()
            {
                action = ChallengeService.action.FIRE;
                targetX = -1;
                targetY = -1;
            }

            public FireToDestroy(int targetX, int targetY) : base()
            {
                action = ChallengeService.action.FIRE;
                this.targetX = targetX;
                this.targetY = targetY;
            }

            public override string PrintParameters()
            {
                StringBuilder result = new StringBuilder(base.PrintParameters());
                if (result.Length > 0)
                    result.Append(", ");
                if ((targetX == -1) || (targetY == -1))
                    result.Append(String.Format("no target specified"));
                else
                    result.Append(String.Format("target at ({0},{1})", targetX, targetY));
                return result.ToString();
            }

        }
    }

    /* Player units can be assigned roles, which helps them to differentiate their actions on the
     * battlefield. Opponents' roles can also be inferred, in order to help player units to plan
     * appropriate responses.
     */

    namespace Role
    {
        public abstract class Role { }

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

        static public readonly Random random = new Random(Guid.NewGuid().GetHashCode());

        private void Initialize()
        {
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

		protected ChallengeService.action RunAndGun(Tank tank, int destX, int destY, int recursionDepth = 0, bool checkFinalGoal = true)
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

            if (recursionDepth > 32)
            {
                // Maximum recursion depth reached; bailing out.
                Debug.WriteLine("ERROR: Maximum recursion depth reached, bailing out.");
                tank.plans.Clear();

                // This typically happens when we start inside the scenery. Taking a step back is one workaround.
                if (tank.direction == ChallengeService.direction.UP)
                    return ChallengeService.action.DOWN;
                else if (tank.direction == ChallengeService.direction.DOWN)
                    return ChallengeService.action.UP;
                else if (tank.direction == ChallengeService.direction.LEFT)
                    return ChallengeService.action.RIGHT;
                else if (tank.direction == ChallengeService.direction.RIGHT)
                    return ChallengeService.action.LEFT;
                else
                    return ChallengeService.action.NONE;

            }
            else if (recursionDepth > 0)
            {
                // Subplans are in play, so we don't need to do much here.
                plan = (Plan.RunAndGun)tank.plans.First.Value;
                plan.description = "Moving into subplans";
            }
            else if ((tank.plans.First == null) || (tank.plans.First.Value.GetType() == typeof(Plan.Separator)))
            {
                // Option 1: We have no plans yet. This is the first one. Remember it.
                // Option 2: We should start executing a new RunAndGun plan, before executing the next plan.
                plan = new Plan.RunAndGun(destX, destY);
                plan.description = "New plan";
                tank.plans.AddFirst(plan);
            }
            else if (tank.plans.First.Value.GetType() == typeof(Plan.RunAndGun))
            {
                // We're currently executing a run-and-gun plan, but the destination may have changed.
                // Here we inspect the LAST plan in the chain, because that represents the ultimate goal.
                // TODO: Add support for separators; the last plain in the chain is the one just before the first separator.
                plan = (Plan.RunAndGun)tank.plans.Last.Value;
                if (checkFinalGoal && ((plan.destX != destX) || (plan.destY != destY)))
                {
                    // A new destination has been specified, so ditch the previous plans and create a new one.
                    tank.plans.Clear();
                    plan = new Plan.RunAndGun(destX, destY);
                    plan.description = "New destination specified";
                    tank.plans.AddFirst(plan);
                }
                else
                {
                    // We've now established that we're still chasing the same goal. Now we need to execute on
                    // the FIRST plan in the list.
                    plan.description = "Continuing the current plan";
                    plan = (Plan.RunAndGun)tank.plans.First.Value;
                    destX = plan.destX;
                    destY = plan.destY;
                }
            } else {
                // Replace the old plans with a new one.
                tank.plans.Clear();
                plan = new Plan.RunAndGun(destX, destY);
                plan.description = "Replacing obsolete plan";
                tank.plans.AddFirst(plan);
            }

            while (plan.subplans.First != null)
            {
                // We already know what to do. Just do it.

                Plan.Plan subplan = plan.subplans.First.Value;
                plan.subplans.RemoveFirst();

                if (subplan.GetType().IsSubclassOf(typeof(Plan.ConcreteAction)))
                {
                    return ((Plan.ConcreteAction)subplan).action;
                }
                else if (subplan.GetType() == typeof(Plan.RunAndGun))
                {
                    // This is typically to get us to move back to a specified location.
                    // Move it to the front of the main list of plans, and execute it.
                    tank.plans.AddFirst(subplan);
                    plan = (Plan.RunAndGun)subplan;
                    destX = plan.destX;
                    destY = plan.destY;
                }
                else
                {
                    Debug.WriteLine("ERROR: Subplan action {0} invalid in RunAndGun(); removing it.",
                        subplan.GetType());
                }
            }

			int Lx = destX - tank.x;
			int Ly = destY - tank.y;
			int dx = 0;
			int dy = 0;
			int[] scanOrder = {0,1,-1,-2,2};
			
			if ((Lx == 0) && (Ly == 0))
            {
                // We've reached our goal. Move on to the next part of the plan, or wait for new instructions.
                tank.plans.RemoveFirst();
                if (tank.plans.First == null)
				    return ChallengeService.action.NONE;
                else if (tank.plans.First.Value.GetType() == typeof(Plan.RunAndGun))
                {
                    Plan.RunAndGun nextPlan = (Plan.RunAndGun)tank.plans.First.Value;

                    // We effectively reset the recursion depth in the next call,
                    // because we've popped out of a subplan.
                    return RunAndGun(tank, nextPlan.destX, nextPlan.destY, checkFinalGoal: false);
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
                dx = Math.Sign(Lx);
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
			
			// Do a search along the 5-blocks-wide path for all obstacles

            LinkedList<Tuple<int, int>> obstacles = new LinkedList<Tuple<int, int>>();
			
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
                    foreach (int i in scanOrder)
                        if ((x + i >= 0) && (x + i < board.xsize) && (y >= 0) && (y < board.ysize) && (board.board[x + i][y] == ChallengeService.state.FULL))
                            obstacles.AddLast(new Tuple<int, int>(x + i, y));
            }
            else if ((dx == 0) && (dy == 1))
            {
                // We're running down, so look for obstacles in a horizontal line downwards.
                int x = tank.x;
                for (int y = tank.y + 3; y <= destY + 2; y++)
                    foreach (int i in scanOrder)
                        if ((x + i >= 0) && (x + i < board.xsize) && (y >= 0) && (y < board.ysize) && (board.board[x + i][y] == ChallengeService.state.FULL))
                            obstacles.AddLast(new Tuple<int, int>(x + i, y));
            }
            else if ((dy == 0) && (dx == -1))
            {
                // We're running left, so look for obstacles in a vertical line leftwards.
                int y = tank.y;
                for (int x = tank.x - 3; x >= destX - 2; x--)
                    foreach (int i in scanOrder)
                        if ((y + i >= 0) && (y + i < board.ysize) && (x >= 0) && (x < board.xsize) && (board.board[x][y + i] == ChallengeService.state.FULL))
                            obstacles.AddLast(new Tuple<int, int>(x, y + i));
            }
            else if ((dy == 0) && (dx == 1))
            {
                // We're running right, so look for obstacles in a vertical line rightwards.
                int y = tank.y;
                for (int x = tank.x + 3; x <= destX + 2; x++)
                    foreach (int i in scanOrder)
                        if ((y + i >= 0) && (y + i < board.ysize) && (x >= 0) && (x < board.xsize) && (board.board[x][y + i] == ChallengeService.state.FULL))
                            obstacles.AddLast(new Tuple<int, int>(x, y + i));
            }
									
			if (obstacles.Count == 0)
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

            if (((dx == 0) && (obstacles.First.Value.Item1 == tank.x)) || (dy == 0) && (obstacles.First.Value.Item2 == tank.y))
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
                 * 2. Move in the direction of the obstacle to point the turret.
                 * 3. Fire to destroy the obstacle, and all other obstacles that happen to be conveniently
                 *    reachable from where we are.
                 * 4. Move back to the current position.
                 * and continue with the main plan.
                 * 
                 * The above actions are added pushed into the planning queue in reverse order, so that
                 * they are popped in the order described above.
                 */

                Plan.Plan subplan;

                int newX = -1;
                int newY = -1;

                if (dx != 0)
                {
                    // We're moving horizontally, so get in line with the obstacle by moving vertically
                    newX = tank.x;
                    newY = obstacles.First.Value.Item2;
                }
                else
                {
                    // We're moving vertically, so get in line with the obstacle by moving horizontally
                    newX = obstacles.First.Value.Item1;
                    newY = tank.y;
                }

                // 4. Move back to the current position
                //    Note that we most likely had to move one step ahead in our desired direction, in
                //    order to turn the tank to fire it. So rather move back one step ahead of the current
                //    position.

                subplan = new Plan.RunAndGun(tank.x + dx, tank.y + dy);
                subplan.description = "Move back in position";
                plan.subplans.AddFirst(subplan);

                // 3. Fire to destroy the obstacle

                // Add the primary obstacle first

                int firstObstX = obstacles.First.Value.Item1;
                int firstObstY = obstacles.First.Value.Item2;
                subplan = new Plan.FireToDestroy(firstObstX, firstObstY);
                subplan.description = "Remove obstacle";
                plan.subplans.AddFirst(subplan);
                obstacles.RemoveFirst();

                LinkedListNode<Plan.Plan> index = plan.subplans.First;

                /* Next, we need to figure out what other obstacles we can opportunistically remove while
                 * we're at this vantage point. From just beyond the first obstacle, scan through the board
                 * in a straight line, looking for other FULL squares. For each one found, cycle through
                 * the list of obstacles. Remove all obstacles that are "behind" this bullet trace
                 * from the list. If an obstacle would be destroyed by firing at the target square, add
                 * a FireToDestroy plan on the target square.
                 * 
                 * It can also happen that no obstacles would be destroyed by firing at a specific FULL
                 * square, but that subsequent squares could destroy obstacles (i.e. useful targets are
                 * hidden behind a useless one). For this reason, useless targets are added to a "tentative"
                 * list. As soon as a useful target is found, all targets in the tentative list are also
                 * added.
                 */

                LinkedList<Plan.Plan> tentativePlans = new LinkedList<Plan.Plan>();

                // There's probably an elegant way to do this is a single loop, but let's rather be
                // verbose and avoid bugs.

                if (dx == 1)
                {
                    int y = firstObstY;
                    for (int x = firstObstX+1; x <= destX; x++)
                    {
                        if (board.board[x][y] == ChallengeService.state.FULL)
                        {
                            // There's a block to destroy. Let's see if it will take out any obstacles with it.
                            LinkedListNode<Tuple<int,int>> obst = obstacles.First;
                            LinkedListNode<Tuple<int,int>> nextObst = null;
                            LinkedListNode<Plan.Plan> newNode = new LinkedListNode<Plan.Plan>(new Plan.FireToDestroy(x, y));
                            while (obst != null)
                            {
                                nextObst = obst.Next;
                                if (obst.Value.Item1 < x)
                                    obstacles.Remove(obst);
                                else if ((obst.Value.Item1 == x) && (Math.Abs(obst.Value.Item2 - y) <= 2))
                                {
                                    // This block should be blasted! Add any blocks on the tentative list first.
                                    foreach (Plan.Plan p in tentativePlans)
                                    {
                                        plan.subplans.AddAfter(index, p);
                                        index = index.Next;
                                    }
                                    tentativePlans.Clear();
                                    plan.subplans.AddAfter(index, newNode);
                                    index = index.Next;
                                    break;
                                }
                                if (nextObst == null)
                                {
                                    // We've cycled through the obstacles without finding one that will be removed by
                                    // destroying this block. So add this block to the tentative list.
                                    tentativePlans.AddLast(newNode);
                                }
                                obst = nextObst;
                            }
                        }
                    }
                }
                if (dx == -1)
                {
                    int y = firstObstY;
                    for (int x = firstObstX - 1; x >= destX; x--)
                    {
                        if (board.board[x][y] == ChallengeService.state.FULL)
                        {
                            // There's a block to destroy. Let's see if it will take out any obstacles with it.
                            LinkedListNode<Tuple<int, int>> obst = obstacles.First;
                            LinkedListNode<Tuple<int, int>> nextObst = null;
                            LinkedListNode<Plan.Plan> newNode = new LinkedListNode<Plan.Plan>(new Plan.FireToDestroy(x, y));
                            while (obst != null)
                            {
                                nextObst = obst.Next;
                                if (obst.Value.Item1 > x)
                                    obstacles.Remove(obst);
                                else if ((obst.Value.Item1 == x) && (Math.Abs(obst.Value.Item2 - y) <= 2))
                                {
                                    // This block should be blasted! Add any blocks on the tentative list first.
                                    foreach (Plan.Plan p in tentativePlans)
                                    {
                                        plan.subplans.AddAfter(index, p);
                                        index = index.Next;
                                    }
                                    tentativePlans.Clear();
                                    plan.subplans.AddAfter(index, newNode);
                                    index = index.Next;
                                    break;
                                }
                                if (nextObst == null)
                                {
                                    // We've cycled through the obstacles without finding one that will be removed by
                                    // destroying this block. So add this block to the tentative list.
                                    tentativePlans.AddLast(newNode);
                                }
                                obst = nextObst;
                            }
                        }
                    }
                }
                if (dy == 1)
                {
                    int x = firstObstX;
                    for (int y = firstObstY + 1; y <= destY; y++)
                    {
                        if (board.board[x][y] == ChallengeService.state.FULL)
                        {
                            // There's a block to destroy. Let's see if it will take out any obstacles with it.
                            LinkedListNode<Tuple<int, int>> obst = obstacles.First;
                            LinkedListNode<Tuple<int, int>> nextObst = null;
                            LinkedListNode<Plan.Plan> newNode = new LinkedListNode<Plan.Plan>(new Plan.FireToDestroy(x, y));
                            while (obst != null)
                            {
                                nextObst = obst.Next;
                                if (obst.Value.Item2 < y)
                                    obstacles.Remove(obst);
                                else if ((obst.Value.Item2 == y) && (Math.Abs(obst.Value.Item1 - x) <= 2))
                                {
                                    // This block should be blasted! Add any blocks on the tentative list first.
                                    foreach (Plan.Plan p in tentativePlans)
                                    {
                                        plan.subplans.AddAfter(index, p);
                                        index = index.Next;
                                    }
                                    tentativePlans.Clear();
                                    plan.subplans.AddAfter(index, newNode);
                                    index = index.Next;
                                    break;
                                }
                                if (nextObst == null)
                                {
                                    // We've cycled through the obstacles without finding one that will be removed by
                                    // destroying this block. So add this block to the tentative list.
                                    tentativePlans.AddLast(newNode);
                                }
                                obst = nextObst;
                            }
                        }
                    }
                }
                if (dy == -1)
                {
                    int x = firstObstX;
                    for (int y = firstObstY - 1; y >= destY; y--)
                    {
                        if ((x >= 0) && (x < board.xsize) && (y >= 0) && (y < board.ysize) && (board.board[x][y] == ChallengeService.state.FULL))
                        {
                            // There's a block to destroy. Let's see if it will take out any obstacles with it.
                            LinkedListNode<Tuple<int, int>> obst = obstacles.First;
                            LinkedListNode<Tuple<int, int>> nextObst = null;
                            LinkedListNode<Plan.Plan> newNode = new LinkedListNode<Plan.Plan>(new Plan.FireToDestroy(x, y));
                            while (obst != null)
                            {
                                nextObst = obst.Next;
                                if (obst.Value.Item2 > y)
                                    obstacles.Remove(obst);
                                else if ((obst.Value.Item2 == y) && (Math.Abs(obst.Value.Item1 - x) <= 2))
                                {
                                    // This block should be blasted! Add any blocks on the tentative list first.
                                    foreach (Plan.Plan p in tentativePlans)
                                    {
                                        plan.subplans.AddAfter(index, p);
                                        index = index.Next;
                                    }
                                    tentativePlans.Clear();
                                    plan.subplans.AddAfter(index, newNode);
                                    index = index.Next;
                                    break;
                                }
                                if (nextObst == null)
                                {
                                    // We've cycled through the obstacles without finding one that will be removed by
                                    // destroying this block. So add this block to the tentative list.
                                    tentativePlans.AddLast(newNode);
                                }
                                obst = nextObst;
                            }
                        }
                    }
                }

                // 2. Move in the direction of the obstacle to point the turret
                subplan = new Plan.Move(plan.currentDirection);
                subplan.description = "Point turret";
                plan.subplans.AddFirst(subplan);

                // 1. Move (recursive RunAndGun) laterally to get in line with the obstacle.
                subplan = new Plan.RunAndGun();
                subplan.description = "Recurse to get in line with the obstacle";

                tank.plans.AddFirst(subplan);   // We're going to recurse

                return RunAndGun(tank, newX, newY, recursionDepth + 1, checkFinalGoal: false);
            }

            Debug.WriteLine("ERROR: RunAndGun() found no valid action to execute.");
            return ChallengeService.action.NONE;
		}

        protected bool Occupied(int x, int y)
        {
            // For now, just check for FULL blocks and our own base.
            // TODO: Check for enemy tanks too
            return !((x >= 0) && (x < board.xsize) && (y >= 0) && (y < board.ysize) &&
                (board.board[x][y] == ChallengeService.state.EMPTY) &&
                (x != board.playerBase.x) && (y != board.playerBase.y));
        }

        protected bool FlankClear(Tank tank, ChallengeService.direction direction, int distance)
        {
            /* true if there are no FULL blocks or other tanks in the specified direction,
             * for the number of blocks specified by 'distance'.
             */

            int dx = 0;
            int dy = 0;
            int y0 = tank.x;
            int x0 = tank.y;

            if (direction == ChallengeService.direction.UP)
            {
                dy = -1;
                y0 = tank.y - 3;
            }
            else if (direction == ChallengeService.direction.DOWN)
            {
                dy = 1;
                y0 = tank.y + 3;
            }
            else if (direction == ChallengeService.direction.LEFT)
            {
                dx = -1;
                x0 = tank.x - 3;
            }
            else if (direction == ChallengeService.direction.RIGHT)
            {
                dx = 1;
                x0 = tank.x + 3;
            }

            if (dx == 0)
            {
                for (int y = y0; Math.Abs(y - y0) <= distance; y += dy)
                    for (int x = x0 - 2; x <= x0 + 2; x++)
                        if (Occupied(x, y))
                            return false;
            }
            else
            {
                for (int x = x0; Math.Abs(x - x0) <= distance; x += dx)
                    for (int y = y0 - 2; y <= y0 + 2; y++)
                        if (Occupied(x, y))
                            return false;
            }

            return true;
        }

        protected ChallengeService.action Dodge(Tank tank)
        {
            /* Check for incoming bullets, and take evasive action, if necessary.
             * 
             * There are a number of possible reactions, in order of preference:
             *   1. Move laterally in the direction that requires the fewest moves to avoid the bullet, if there's space
             *   2. Move laterally in the other direction, if there's space
             *   3. Run away from the bullet, if there's time
             *   4. Do nothing, and hope we can take a shot at the bullet
             * The order of (3) and (4) is significant: If we default to pot-shots, the chances are higher that a
             * permanent stand-off occurs. If we run away, the chances are higher that we'll be able to do a lateral
             * movement later.
             * 
             * At the moment, just a single action is taken. A more effective approach might be to add plans to the
             * tank's queue.
             */

            ChallengeService.direction incomingFrom = ChallengeService.direction.NONE;
            ChallengeService.action result = ChallengeService.action.NONE;
            Bullet bullet = null;

            foreach (KeyValuePair<int, Bullet> b in board.opponentBullet)
                if ((b.Value.direction == ChallengeService.direction.LEFT) && (tank.x < b.Value.x) &&
                    (Math.Abs(tank.y - b.Value.y) <= 2) && board.ClearShot(b.Value.x, b.Value.y, tank.x, b.Value.y))
                {
                    incomingFrom = ChallengeService.direction.RIGHT;
                    bullet = b.Value;
                    break;
                }
                else if ((b.Value.direction == ChallengeService.direction.RIGHT) && (tank.x > b.Value.x) &&
                    (Math.Abs(tank.y - b.Value.y) <= 2) && board.ClearShot(b.Value.x, b.Value.y, tank.x, b.Value.y))
                {
                    incomingFrom = ChallengeService.direction.LEFT;
                    bullet = b.Value;
                    break;
                }
                else if ((b.Value.direction == ChallengeService.direction.UP) && (tank.y < b.Value.y) &&
                    (Math.Abs(tank.x - b.Value.x) <= 2) && board.ClearShot(b.Value.x, b.Value.y, b.Value.x, tank.y))
                {
                    incomingFrom = ChallengeService.direction.DOWN;
                    bullet = b.Value;
                    break;
                }
                else if ((b.Value.direction == ChallengeService.direction.DOWN) && (tank.y > b.Value.y) &&
                    (Math.Abs(tank.x - b.Value.x) <= 2) && board.ClearShot(b.Value.x, b.Value.y, b.Value.x, tank.y))
                {
                    incomingFrom = ChallengeService.direction.UP;
                    bullet = b.Value;
                    break;
                }

            if (incomingFrom == ChallengeService.direction.NONE)
                return result;

            Debug.WriteLine("Incoming bullet!");

            if ((incomingFrom == ChallengeService.direction.LEFT) || (incomingFrom == ChallengeService.direction.RIGHT))
            {
                // Try to dodge vertically
                if (tank.y - bullet.y < 0)
                {
                    // Move UP, if there's space
                    int dy = 3 + tank.y - bullet.y;
                    if (tank.y > dy)
                        if (FlankClear(tank, ChallengeService.direction.UP, tank.y - dy))
                            return ChallengeService.action.UP;
                        else if (FlankClear(tank, ChallengeService.direction.DOWN, 6 - tank.y + dy))
                            return ChallengeService.action.DOWN;
                }
                else if (tank.y == bullet.y)
                {
                    bool clearUp = FlankClear(tank, ChallengeService.direction.UP, 3);
                    bool clearDown = FlankClear(tank, ChallengeService.direction.DOWN, 3);
                    if (clearUp)
                    {
                        if (clearDown)
                        {
                            if (random.Next(2) == 0)
                                return ChallengeService.action.UP;
                            else
                                return ChallengeService.action.DOWN;
                        }
                        else
                            return ChallengeService.action.LEFT;
                    }
                    else if (clearDown)
                    {
                        return ChallengeService.action.DOWN;
                    }
                }
                else
                {
                    // Move DOWN, if there's space
                    int dy = 3 - tank.y + bullet.y;
                    if ((board.ysize - tank.y) > dy)
                    {
                        if (FlankClear(tank, ChallengeService.direction.DOWN, (board.ysize - tank.y) - dy))
                            return ChallengeService.action.DOWN;
                    }
                    else
                    {
                        if (FlankClear(tank, ChallengeService.direction.UP, 6 - board.ysize + tank.y + dy))
                            return ChallengeService.action.UP;
                    }
                }
            }
            else
            {
                // Try to dodge horizontally
                if (tank.x - bullet.x < 0)
                {
                    // Move LEFT, if there's space
                    int dx = 3 + tank.x - bullet.x;
                    if (tank.x > dx)
                    {
                        if (FlankClear(tank, ChallengeService.direction.LEFT, tank.x - dx))
                            return ChallengeService.action.LEFT;
                    }
                    else
                    {
                        if (FlankClear(tank, ChallengeService.direction.RIGHT, 6 - tank.x + dx))
                            return ChallengeService.action.RIGHT;
                    }
                }
                else if (tank.x == bullet.x)
                {
                    bool clearLeft = FlankClear(tank, ChallengeService.direction.LEFT, 3);
                    bool clearRight = FlankClear(tank, ChallengeService.direction.RIGHT, 3);
                    if (clearLeft)
                    {
                        if (clearRight)
                        {
                            if (random.Next(2) == 0)
                                return ChallengeService.action.LEFT;
                            else
                                return ChallengeService.action.RIGHT;
                        }
                        else
                            return ChallengeService.action.LEFT;
                    }
                    else if (clearRight)
                    {
                        return ChallengeService.action.RIGHT;
                    }
                }
                else
                {
                    // Move RIGHT, if there's space
                    int dx = 3 - tank.x + bullet.x;
                    if ((board.xsize - tank.x) > dx)
                    {
                        if (FlankClear(tank, ChallengeService.direction.RIGHT, board.xsize - tank.x - dx))
                            return ChallengeService.action.RIGHT;
                    }
                    else
                    {
                        if (FlankClear(tank, ChallengeService.direction.LEFT, 6 - board.xsize + tank.x + dx))
                            return ChallengeService.action.LEFT;
                    }
                }
            }

            // We can't move laterally, rather run away and hope for the best next tick.

            switch (incomingFrom)
            {
                case ChallengeService.direction.LEFT:
                    return ChallengeService.action.RIGHT;
                case ChallengeService.direction.RIGHT:
                    return ChallengeService.action.LEFT;
                case ChallengeService.direction.UP:
                    return ChallengeService.action.DOWN;
                case ChallengeService.direction.DOWN:
                    return ChallengeService.action.UP;
            }

            return ChallengeService.action.NONE;
        }

        protected ChallengeService.action FireOrTurn(Tank tank, int tx, int ty, int targetSize = 0)
        {
            /* If the tank is horizontally or vertically in line with the target and we have a clear shot,
             * either prepare to fire by moving in the target direction (if necessary) or just fire.
             * If the tank is not in line with the target, the NONE action is returned.
             */
            if ((Math.Abs(tx - tank.x) <= targetSize) && board.ClearShot(tank.x, tank.y, tank.x, ty))
            {
                if (ty > tank.y)
                {
                    if (tank.direction == ChallengeService.direction.DOWN)
                        return ChallengeService.action.FIRE;
                    else
                        return ChallengeService.action.DOWN;
                }
                else
                {
                    if (tank.direction == ChallengeService.direction.UP)
                        return ChallengeService.action.FIRE;
                    else
                        return ChallengeService.action.UP;
                }
            }
            else if ((Math.Abs(ty - tank.y) <= targetSize) && board.ClearShot(tank.x, tank.y, tx, tank.y))
            {
                if (tx > tank.x)
                {
                    if (tank.direction == ChallengeService.direction.RIGHT)
                        return ChallengeService.action.FIRE;
                    else
                        return ChallengeService.action.RIGHT;
                }
                else
                {
                    if (tank.direction == ChallengeService.direction.LEFT)
                        return ChallengeService.action.FIRE;
                    else
                        return ChallengeService.action.LEFT;
                }
            }
            else
                return ChallengeService.action.NONE;
        }

        protected ChallengeService.action PotShot(Tank tank)
        {
            /* Check whether enemies are within range, and fire at them.
             */
            ChallengeService.action result = ChallengeService.action.NONE;

            if (tank.bullet != null)
                return result;

            result = FireOrTurn(tank, board.opponentBase.x, board.opponentBase.y);
            if (result != ChallengeService.action.NONE)
            {
                Debug.WriteLine("All your base are belong to us!");
                return result;
            }

            foreach (KeyValuePair<int,Bullet> bullet in board.opponentBullet)
            {
                // TODO: Check that the bullet is actually being fired towards the tank
                result = FireOrTurn(tank, bullet.Value.x, bullet.Value.y);
                if (result != ChallengeService.action.NONE)
                {
                    Debug.WriteLine("Attempting countershot!");
                    return result;
                }
            }
        
            foreach (Tank opponent in board.opponentTank)
            {
                if (!opponent.destroyed)
                {
                    result = FireOrTurn(tank, opponent.x, opponent.y);
                    if (result != ChallengeService.action.NONE)
                    {
                        Debug.WriteLine("Die, Piggy-Piggy, die die!");
                        return result;
                    }
                }
            }

            return ChallengeService.action.NONE;
        }

    }

    class AI_Random: AI
    {
        /* A simple bot that just issues random commands to both tanks.
         * No checks are done to see whether a tank has been destroyed.
         */

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
                if (!board.playerTank[0].destroyed)
                    client.setAction(board.playerTank[0].id, A1);
                if (!board.playerTank[1].destroyed)
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

            Tank baseKiller = null;
            Tank tankKiller = null;

            if (!board.playerTank[0].destroyed && !board.playerTank[1].destroyed)
            {
                baseKiller = board.playerTank[0];
                tankKiller = board.playerTank[1];
            }
            else if (!board.playerTank[0].destroyed)
                baseKiller = board.playerTank[0];
            else if (!board.playerTank[1].destroyed)
                baseKiller = board.playerTank[1];

            A1 = ChallengeService.action.NONE;
            A2 = ChallengeService.action.NONE;

            // One tank goes for the base
            if (!baseKiller.destroyed)
                baseKiller.Watchdog();
            A1 = Dodge(baseKiller);
            if (A1 == ChallengeService.action.NONE)
                A1 = PotShot(baseKiller);
            if (A1 == ChallengeService.action.NONE)
                A1 = RunAndGun(baseKiller, board.opponentBase.x, board.opponentBase.y);

            // The other tank (if we still have two) goes for the closest enemy
            if (!tankKiller.destroyed)
                tankKiller.Watchdog();
            A2 = Dodge(tankKiller);
            if (A2 == ChallengeService.action.NONE)
                A2 = PotShot(tankKiller);
            if (A2 == ChallengeService.action.NONE)
            {
                int o1 = Math.Abs(board.opponentTank[0].x - tankKiller.x) + Math.Abs(board.opponentTank[0].y - tankKiller.y);
                int o2 = Math.Abs(board.opponentTank[1].x - tankKiller.x) + Math.Abs(board.opponentTank[1].y - tankKiller.y);
                if ((o1 < o2) && !board.opponentTank[0].destroyed)
                    A2 = RunAndGun(tankKiller, board.opponentTank[0].x, board.opponentTank[0].y);
                else
                    A2 = RunAndGun(tankKiller, board.opponentTank[1].x, board.opponentTank[1].y);
            }

            // If the original base killer is destroyed, appoint his successor.
            if (baseKiller.destroyed)
                A2 = A1;

            if (!baseKiller.destroyed && !tankKiller.destroyed)
            {
                Debug.WriteLine("- - - - - - - - - - - - - - - - - - -");
                Debug.WriteLine("Sniper's minimap:");
                Debug.WriteLine(board.PrintArea(tankKiller.x - 7, tankKiller.y - 7, tankKiller.x + 8, tankKiller.y + 8));
                Debug.WriteLine("Sniper's move: {0}", A1);
            }
            Debug.WriteLine("- - - - - - - - - - - - - - - - - - -");
            Debug.WriteLine("Base attacker's minimap:");
            Debug.WriteLine(board.PrintArea(baseKiller.x - 7, baseKiller.y - 7, baseKiller.x + 8, baseKiller.y + 8));
            Debug.WriteLine("Base attacker's move: {0}", A1);
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
                {
                    lock (client)
                        client.setAction(board.playerTank[0].id, A1);
                    Debug.WriteLine("Tank 1's plans:");
                    Debug.WriteLine(board.playerTank[0].PrintPlans());
                }
                if (!board.playerTank[1].destroyed)
                {
                    lock (client)
                        client.setAction(board.playerTank[1].id, A2);
                    Debug.WriteLine("Tank 2's plans:");
                    Debug.WriteLine(board.playerTank[1].PrintPlans());
                }
            }
        }

    }

    class AI_CTF : AI
    {
        /* "Capture The Flag". One bot randomly chooses to take the AttackBase role, and the other one takes the
         * DefendBase role. The attacker uses the path planner to find the shortest route to a point from where
         * the enemy base can be shot. He then runs and guns down that route, dodging enemy bullets and taking
         * opportunistic pot shots at enemies when possible. The defender attempts to obstruct enemy tanks. He
         * uses the path planner to stay at a point one-third of the way along a straight line from the player
         * base to the enemy tank which is closest to the player base.
         * 
         * If one of the bots is destroyed, the surviving bot automatically assumes the AttackBase role.
         * 
         * If the opponent assumes a completely defensive strategy, the defender quits his DefendBase role, and
         * also takes the AttackBase role. This is triggered based on the distance L between the player base and
         * the enemy base. If our first attacker reaches a distance L/3 from the enemy base, but all surviving
         * opponents are also still within L/3 from the enemy base.
         * 
         * The schedule is handled as follows:
         * - At a new tick, basic checks for tactical moves are done (pot shot at enemy, dodge a bullet, etc.).
         *   If no tactical moves have been scheduled, the path planner tasks are started, and the default
         *   "run-and-gun" actions are calculated and submitted to the server. These serve as backup actions in
         *   case the path planner does not complete in time, or the final actions aren't posted in time.
         * - At the early-move tick, the results from the path planner are set as actions.
         * - At the final-move tick, the queued actions are posted.
         */

        private ChallengeService.action[] A;
        private List<Tuple<int, int>>[] route;
        private CancellationTokenSource cancel;
        private List<Task> taskPool;
        private PathPlanner[] planner;

        public AI_CTF() : base() { }
        public AI_CTF(Board board, ChallengeService.ChallengeClient client)
            : base(board, client)
        {
            A = new ChallengeService.action[2];
            A[0] = ChallengeService.action.NONE;
            A[1] = ChallengeService.action.NONE;
            route = new List<Tuple<int, int>>[2];
            taskPool = new List<Task>();
            planner = new PathPlanner[2];
        }

        public override void newTick()
        {
            #region Set up roles
            if ((board.playerTank[0].role == null) || (board.playerTank[1].role == null))
            {
                // We haven't assigned roles yet, so randomly assign the AttackBase and DefendBase roles.
                if (random.Next(2) == 0)
                {
                    board.playerTank[0].role = new Role.AttackBase();
                    board.playerTank[1].role = new Role.DefendBase();
                }
                else
                {
                    board.playerTank[1].role = new Role.AttackBase();
                    board.playerTank[0].role = new Role.DefendBase();
                }
            }
            else if ((board.playerTank[0].destroyed && (board.playerTank[1].role.GetType() == typeof(Role.DefendBase))))
            {
                // We're not going to win if only a defender survives. Geronimo!
                Debug.WriteLine("Switching surviving player tank to assault mode.");
                board.playerTank[1].role = new Role.AttackBase();
                board.playerTank[1].plans.Clear();
                route[1].Clear();
            }
            else if ((board.playerTank[1].destroyed && (board.playerTank[0].role.GetType() == typeof(Role.DefendBase))))
            {
                Debug.WriteLine("Switching surviving player tank to assault mode.");
                board.playerTank[0].role = new Role.AttackBase();
                board.playerTank[0].plans.Clear();
                route[0].Clear();
            }
            else if (!board.opponentTank[0].destroyed && !board.opponentTank[1].destroyed)
            {
                /* Calculate whether all other tanks are within one-third radius of the enemy base. If so, let
                 * the defender switch to offense. Only do this if both the enemy tanks still survive (i.e. our
                 * single attacker would be outnumbered).
                 */
                int L = Math.Abs(board.playerBase.x - board.opponentBase.x)
                    + Math.Abs(board.playerBase.y - board.opponentBase.y);

                int defender = -1;
                int attacker = -1;

                if (board.playerTank[0].role.GetType() == typeof(Role.DefendBase))
                {
                    defender = 0;
                    attacker = 1;
                }
                else if (board.playerTank[1].role.GetType() == typeof(Role.DefendBase))
                {
                    defender = 1;
                    attacker = 0;
                }

                if (defender != -1)
                {
                    int O1 = Math.Abs(board.opponentBase.x - board.opponentTank[0].x)
                        + Math.Abs(board.opponentBase.y - board.opponentTank[0].y);
                    int O2 = Math.Abs(board.opponentBase.x - board.opponentTank[1].x)
                        + Math.Abs(board.opponentBase.y - board.opponentTank[1].y);
                    int Pa = Math.Abs(board.opponentBase.x - board.playerTank[attacker].x)
                        + Math.Abs(board.opponentBase.y - board.playerTank[attacker].y);
                    if ( (O1 < L/3) && (O2 < L/3) && (Pa < L/3) )
                    {
                        Debug.WriteLine("Switching player's defending tank over to assault.");
                        board.playerTank[defender].role = new Role.AttackBase();
                        board.playerTank[defender].plans.Clear();
                        route[defender].Clear();
                    }
                }
            }
            #endregion

            #region Schedule tasks
            for (int i = 0; i < 2; i++)
            {
                // We create separate planners for each tank, so that they can run as parallel asynchronous
                // tasks. We need this, because they need to work on different maps,
                // so that the current tank can be excluded from the map (otherwise its own blocks look
                // like no-go areas).

                Tank t = board.playerTank[i];
                if (t.destroyed)
                    continue;

                A[i] = ChallengeService.action.NONE;

                planner[i] = new PathPlanner();

                planner[i].mapBoard(board, t);
#if DEBUG
                planner[i].renderMap(board, String.Format("map{0}.png", t.id));
#endif

                // See if there's a bullet we need to dodge
                ChallengeService.action dodgeAction = Dodge(board.playerTank[i]);

                // See if there's an enemy to take a pot shot at
                ChallengeService.action potshotAction = PotShot(board.playerTank[i]);

                // See if we're stuck in a rut
                ChallengeService.action recoveryAction = t.Watchdog();

                if (dodgeAction != ChallengeService.action.NONE)
                    A[i] = dodgeAction;
                else if (potshotAction != ChallengeService.action.NONE)
                    A[i] = potshotAction;
                else if (recoveryAction != ChallengeService.action.NONE)
                    A[i] = recoveryAction;
                else if (t.role.GetType() == typeof(Role.AttackBase))
                {
                    // For now, just target the actual coordinates of the base (gun at it and run over it).
                    // TODO: Scan the horizontal and vertical lines from the base, to find good sniping spots.
                    //       Point in case would be the centerpoint of the board-center-counter board, which
                    //       has a long vertical corridor heading straight towards the base.

                    /* Next, we start an asynchronous task running the path planner. We'll check its results
                     * in postEarlyMove(), and cancel it if it's not done by postFinalMove().
                     */

                    cancel = new CancellationTokenSource();

                    // Make a local copy of the loop variable, since the latter can change while the task
                    // executes.
                    int _i = i;

                    Task task = new Task(() =>
                        {
                            route[_i] = planner[_i].GetPath(t.x, t.y, board.opponentBase.x, board.opponentBase.y, cancel);
                        }, cancel.Token);

                    task.ContinueWith(battlecity.Program.ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
                    taskPool.Add(task);
                    task.Start();
                }
                else if (t.role.GetType() == typeof(Role.DefendBase))
                {
                    int destX;
                    int destY;

                    // Find out which of the opponents is closest to our base.

                    int closestDist = int.MaxValue;
                    Tank closestOpponent = null;

                    for (int j = 0; j < 2; j++)
                    {
                        if (board.opponentTank[j].destroyed)
                            break;
                        int dist = Math.Abs(board.playerBase.x - board.opponentTank[j].x)
                            + Math.Abs(board.playerBase.y - board.opponentTank[j].y);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestOpponent = board.opponentTank[j];
                        }
                    }

                    if (closestOpponent != null)
                    {
                        // Target a point one third of the way between the opponent and the base.
                        int dx = (closestOpponent.x - board.playerBase.x) / 3;
                        int dy = (closestOpponent.y - board.playerBase.y) / 3;

                        if ((Math.Abs(dx) < 3) && (Math.Abs(dy) < 3))
                        {
                            /* At this point, the opponent has moved too close to the base to physically
                             * block him. Rather move to flank him.
                             * 
                             * There are four possible flanking destinations. Cycle through all four of
                             * them, and move towards the closest one which doesn't imply that we're too
                             * close to the base.
                             */
                            Debug.WriteLine("Attempting to flank opponent.");

                            int[,] flanks = new int[,] { {closestOpponent.x - 5, closestOpponent.y    },
                                                         {closestOpponent.x    , closestOpponent.y - 5},
                                                         {closestOpponent.x + 5, closestOpponent.y    },
                                                         {closestOpponent.x    , closestOpponent.y + 5}};
                            int bestFlank = -1;
                            int bestFlankDist = int.MaxValue;

                            for (int j = 0; j < 4; j++)
                            {
                                int flankMarginX = Math.Abs(flanks[j, 0] - board.playerBase.x);
                                int flankMarginY = Math.Abs(flanks[j, 1] - board.playerBase.y);
                                if ((flankMarginX >= 3) || (flankMarginY >= 3))
                                {
                                    int flankDist = Math.Abs(flanks[j, 0] - t.x) + Math.Abs(flanks[j, 1] - t.y);
                                    if (flankDist < bestFlankDist)
                                    {
                                        bestFlank = j;
                                        bestFlankDist = flankDist;
                                    }
                                }
                            }

                            destX = flanks[bestFlank, 0];
                            destY = flanks[bestFlank, 1];
                        }
                        else
                        {
                            destX = board.playerBase.x + dx;
                            destY = board.playerBase.y + dy;
                        }

                        /* Next, we start an asynchronous task running the path planner. We'll check its results
                         * in postEarlyMove(), and cancel it if it's not done by postFinalMove().
                         */

                        cancel = new CancellationTokenSource();

                        // Make a local copy of the loop variable, since the latter can change while the task
                        // executes.
                        int _i = i;

                        Task task = new Task(() =>
                        {
                            route[_i] = planner[_i].GetPath(t.x, t.y, destX, destY, cancel);
                        }, cancel.Token);

                        task.ContinueWith(battlecity.Program.ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
                        taskPool.Add(task);
                        task.Start();
                    }
                }
                else
                    Debug.WriteLine("ERROR: Player tank (id={0}) has unsupported role {1}", t.id, t.role);
            }
            #endregion

            #region Post default moves
            for (int i = 0; i < 2; i++)
            {
                Tank t = board.playerTank[i];
                if (t.destroyed)
                    continue;

                // We don't want to overwrite A[i], because we later use it to check whether a tactical move
                // has been taken.
                ChallengeService.action action = A[i];

                if (action == ChallengeService.action.NONE)
                    action = RunAndGun(board.playerTank[i], planner[i].destX, planner[i].destY);

                try
                {
                    lock (client)
                        client.setAction(board.playerTank[i].id, action);
                }
                catch (System.ServiceModel.EndpointNotFoundException)
                {
                    Debug.WriteLine("ERROR: Server seems to have shut down.");
                }
            }
            #endregion
        }

        public override void postEarlyMove()
        {
            cancel.Cancel();
            foreach (Task task in taskPool)
               task.Wait(100);
            taskPool.Clear();
#if DEBUG
            if (route[0] != null)
                planner[0].renderRoute(board, route[0], String.Format("route{0}.png", 0));
            if (route[1] != null)
                planner[1].renderRoute(board, route[1], String.Format("route{0}.png", 1));
#endif
            // Now act on the (hopefully updated) routes.
            for (int i = 0; i < 2; i++)
            {
                if (!board.playerTank[i].destroyed)
                {
                    // If we already assigned a dodge or a pot shot action, nothing remains to be done here
                    if (A[i] == ChallengeService.action.NONE)
                    {
                        board.playerTank[i].plans.Clear();
                        if ((route[i] == null) || (route.Length < 4))
                        {
                            // The pathfinder hasn't been able to find a valid route yet, or we're practically
                            // at the target. Just bash ahead.
                            if (route[i] == null)
                                Debug.WriteLine("WARNING: No suitable route found for tank (ID={0}); bashing ahead.", board.playerTank[i].id);
                            else
                                Debug.WriteLine("Tank (ID={0}) going straight for the target.", board.playerTank[i].id);
                            // A[i] = RunAndGun(board.playerTank[i], planner[i].destX, planner[i].destY);
                            A[i] = ChallengeService.action.NONE;
                        }
                        else
                        {
                            // Check the proposed route, and target the end of the first straight line
                            // segment in the route (this works well, because RunAndGun handles straight
                            // lines perfectly: correctly alternating between moving and firing).
                            int dx = route[i][1].Item1 - route[i][0].Item1;
                            int dy = route[i][1].Item2 - route[i][0].Item2;
                            int destItem = 1;
                            for (int j = 2; j < route[i].Count; j++)
                            {
                                if (((route[i][j].Item1 - route[i][j].Item1) == dx) &&
                                    ((route[i][j].Item2 - route[i][j].Item2) == dy))
                                {
                                    destItem = j;
                                }
                                else
                                    break;
                            }
                            A[i] = RunAndGun(board.playerTank[i], route[i][destItem].Item1, route[i][destItem].Item2);
                        }
                    }
                }
            }

        }

        public override void postFinalMove()
        {
            lock (client)
            {
                Debug.WriteLine("Tank 1 {0}; Tank 2 {1}", A[0], A[1]);
                try
                {
                    // A NONE action typically means a default action has already been posted in the NewTick() phase.
                    if (!board.playerTank[0].destroyed && (A[0] != ChallengeService.action.NONE))
                    {
                        lock (client)
                            client.setAction(board.playerTank[0].id, A[0]);
                        Debug.WriteLine("Tank 1's plans:");
                        Debug.WriteLine(board.playerTank[0].PrintPlans());
                    }
                    if (!board.playerTank[1].destroyed && (A[1] != ChallengeService.action.NONE))
                    {
                        lock (client)
                            client.setAction(board.playerTank[1].id, A[1]);
                        Debug.WriteLine("Tank 2's plans:");
                        Debug.WriteLine(board.playerTank[1].PrintPlans());
                    }
                }
                catch (System.ServiceModel.EndpointNotFoundException)
                {
                    Debug.WriteLine("ERROR: Server seems to have shut down.");
                }
            }
        }

    }


}
