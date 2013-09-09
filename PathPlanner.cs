using System;
using System.Diagnostics;
using Games.Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace battlecity
{
	public class AStarNodeBC: AStarNode
	{
		/* A node class for doing pathfinding on a 2-dimensional map.
		 * This code has been adapted from a license-free public-domain source:
		 * http://www.codeproject.com/Articles/5758/Path-finding-in-C
		 */

        public PathPlanner Planner { get; set; }
        int Scale { get; set; }
		
		// The X-coordinate of the node
		public int X 
		{
			get 
			{
				return fX;
			}
		}
        private readonly int fX;

		// The Y-coordinate of the node
		public int Y
		{
			get
			{
				return fY;
			}
		}
        private readonly int fY;

		// Constructor for a node in a 2-dimensional map
		public AStarNodeBC(AStarNode aParent, AStarNode aGoalNode, double aCost, int aX, int aY, PathPlanner owner, int scale = 0) : base(aParent, aGoalNode, aCost)
		{
			fX = aX;
			fY = aY;
            Planner = owner;
            this.Scale = scale;
		}

		// Adds a successor to a list if it is not impassible or the parent node
		private void AddSuccessor(ArrayList aSuccessors, int aX, int aY) 
		{
			double currentCost = Planner.GetMap(aX, aY, Scale);
			if (currentCost < 0.0)
			{
				return;
			}
			AStarNodeBC newNode = new AStarNodeBC(this, GoalNode, Cost + currentCost, aX, aY, Planner, Scale);
			if (newNode.IsSameState(Parent)) 
			{
				return;
			}
			aSuccessors.Add(newNode);
		}

		// Determines wheather the current node is the same state as the on passed.
		public override bool IsSameState(AStarNode aNode)
		{
			if(aNode == null) 
			{
				return false;
			}
			return ((((AStarNodeBC)aNode).X == fX) &&
				(((AStarNodeBC)aNode).Y == fY));
		}
		
		// Calculates the estimated cost for the remaining trip to the goal.
		public override void Calculate()
		{
			if (GoalNode != null) 
			{
				double xd = fX - ((AStarNodeBC)GoalNode).X;
				double yd = fY - ((AStarNodeBC)GoalNode).Y;
				// "Manhattan Distance" - Used when search can only move vertically and horizontally.
				GoalEstimate = 1.0 * (Math.Abs(xd) + Math.Abs(yd)); 
			}
			else
			{
				GoalEstimate = 0;
			}
		}

		// Gets all successors nodes from the current node and adds them to the successor list
		public override void GetSuccessors(ArrayList aSuccessors)
		{
			aSuccessors.Clear();
            AddSuccessor(aSuccessors, fX - (1 << Scale), fY);
			AddSuccessor(aSuccessors, fX, fY - (1 << Scale));
			AddSuccessor(aSuccessors, fX + (1 << Scale), fY);
			AddSuccessor(aSuccessors, fX, fY + (1 << Scale));
		}	

		// Prints information about the current node
		public override void PrintNodeInfo()
		{
			Debug.WriteLine("X:\t{0}\tY:\t{1}\tCost:\t{2}\tEst:\t{3}\tTotal:\t{4}",
			                  fX, fY, Cost, GoalEstimate, TotalCost);
		}
	}
	
	public class PathPlanner
	{
        public double[,,] Map { get; set; }

        public int DestX { get; set; }
        public int DestY { get; set; }

        public double GetMap(int x, int y, int scale)
        {
            /* Return the target coordinates, of a map scaled down by the specified power.
             * scale = 0 returns the coordinates of the map as specified.
             * Otherwise the map is scaled down by a factor (2^scale).
             */
            if ((x < 0) || (x >= Map.GetLength(1)) || (y < 0) || (y >= Map.GetLength(2)))
                return -1;

            return Map[scale, x >> scale, y >> scale];
        }

        public double[,] Kernel;
		
		// Tick number of the last update (to avoid re-scanning the board in the same round).
		private int lastUpdate;
		
		// A vector describing the added routing penalty close to an enemy tank. Element 0
		// is the penalty for moving directly adjacent to an enemy tank, Element 1 the penalty
		// at 1 block clearance, Element 2 at 2 blocks clearance, etc. Clearance is defined as
		// the smallest of the x or the y clearance (i.e. diagonal distance is not considered).
		public double[] EnemyClearance;
		
		private void Initialize()
		{
			/* The kernel is used to estimate the movement cost of having the 5x5 tank's center
			 * point move through a specific block in the map. The kernel calculates the weighted
			 * sum of all the obstacles in the 5x5 area around the center point. The cost gives an
			 * indication of how many walls the tank would first have to destroy, before it can move
			 * its center point to the specified square.
			 */
			
            /*
            Kernel = new int[,]
				{{3,3,3,3,3},
				 {3,2,2,2,3},
				 {3,2,1,2,3},
				 {3,2,2,2,3},
				 {3,3,3,3,3}};
             */

            Kernel = new double[,]
                {{0.03,0.03,0.03,0.03,0.03},
                 {0.03,0.02,0.02,0.02,0.03},
                 {0.03,0.02,0.01,0.02,0.03},
                 {0.03,0.02,0.02,0.02,0.03},
                 {0.03,0.03,0.03,0.03,0.03}};

            //Kernel = new double[,]
            //    {{0.04,0.04,0.04,0.04,0.04},
            //     {0.04,0.03,0.03,0.03,0.04},
            //     {0.04,0.03,0.02,0.03,0.04},
            //     {0.04,0.03,0.03,0.03,0.04},
            //     {0.04,0.04,0.04,0.04,0.04}};
			
			EnemyClearance = new double[] {0.05, 0.04, 0.03, 0.02, 0.01, 0.01};
			
			lastUpdate = -2;
            DestX = -1;
            DestY = -1;
		}
		
		public PathPlanner ()
		{
            Initialize();
		}
		
		public void MapBoard(Board board, Tank excludeTank = null, int tick = -1, int scaleSpace = 0)
		{
			// If tick is -1, force an update. Otherwise, check that we haven't already updated the map
			// this round.
			if ((tick != -1) && (tick <= lastUpdate))
				return;
			
			lastUpdate = tick;
            Map = new double[scaleSpace+1, board.xsize - 4, board.ysize - 4];

			/* Scan through the board and apply the kernel, and use that to create a map of movement
			 * costs for the A* algorithm.
			 */

            int scale = 0;

			for (int x = 2; x < board.xsize - 2; x++)
				for (int y = 2; y < board.ysize -2; y++)
                {
                    // Always start with a cost of one (the actual movement cost to enter the square)
                    Map[scale, x - 2, y - 2] = 1;

                    // Next, calculate the estimated movement penalty due to obstructions
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
						    // First, calculate map movement cost just based on the blocks in the terrain
							// BUG: This should use 0 when the terrain is clear, 1 when it's a wall, and
						    //      add 1 for actual movement cost.

                            int cost = 0;

                            switch (board.board[x + dx][y + dy])
                            {
                                case (ChallengeService.state.FULL):
                                    cost = 1;
                                    break;
                                case (ChallengeService.state.NONE):
                                case (ChallengeService.state.OUT_OF_BOUNDS):
                                    cost = -1;
                                    break;
                            }

                            if (cost == -1)
                            {
                                Map[scale, x - 2, y - 2] = -1;
                                break;
                            }

                            Map[scale, x - 2, y - 2] += cost * Kernel[dx + 2, dy + 2];
                        }
                        if (Map[scale, x - 2, y - 2] == -1)
                            // Once it's impassible, there's no way to recover
                            break;
                    }

                }

            // Next, we can paint in "no-go" areas, e.g. where we don't want to collide with our
            // own base or a friendly tank, or to keep a safe distance from an enemy tank.

            /* TODO: It may be possible to adapt A* to dodge bullets.
             *       If there are any bullets currently in flight, coordinates through which a bullet
             *       will pass should be no-go areas i.t.o. routing *for that tick*. This would require
             *       a way to annotate the map with movement-number-dependent weights, and the
             *       algorithm would have to be adapted to take these into account.
             */

            /* Calculating collisions:
             * 
             * ············
             * ·XXXXXYYYYY·
             * ·XXXXXYYYYY·
             * ·XXAXXYYBYY·
             * ·XXXXXYYYYY·
             * ·XXXXXYYYYY·
             * ············
             * 
             * X is the player's tank, and Y the opponent's. Their respective centerpoints are at
             * A and B. Abs(A.x - B.x) == 5 is the point where the tanks just touch.
             * Abs(A.x - B.x) - 5 gives the clearance between the tanks.
             * 
             * Everything in a radius of 4 units (9x9 square) around a tank is a collision (no-go)
             * for another tank. For enemy tanks, concentric squares around this 9x9 area may also be
             * penalised by the elements of this.EnemyClearance.
             * 
             */

            foreach (Tank t in board.playerTank)
                if (!t.destroyed && ((excludeTank == null) || (excludeTank.id != t.id)))
                    for (int dx = -4; dx <= 4; dx++)
                        for (int dy = -4; dy <= 4; dy++)
                            if ((t.x - 2 + dx >= 0) && (t.x - 2 + dx < Map.GetLength(1)) &&
                                (t.y - 2 + dy >= 0) && (t.y - 2 + dy < Map.GetLength(2)))
                            {
                                try
                                {
                                    Map[scale, t.x - 2 + dx, t.y - 2 + dy] = -1;
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    Debug.WriteLine("ERROR: Player tank ({0},{1}) out of bounds at ({2},{3}) during path planning",
                                        t.x, t.y, t.x - 2 + dx, t.y - 2 + dy);
                                }
                            }

            foreach (Tank t in board.opponentTank)
                if (!t.destroyed && ((excludeTank == null) || (excludeTank.id != t.id)))
                    for (int dx = -4 - EnemyClearance.Length; dx <= 4 + EnemyClearance.Length; dx++)
                        for (int dy = -4 - EnemyClearance.Length; dy <= 4 + EnemyClearance.Length; dy++)
                        {
                            if ((t.x - 2 + dx >= 0) && (t.x - 2 + dx < Map.GetLength(1)) &&
                                (t.y - 2 + dy >= 0) && (t.y - 2 + dy < Map.GetLength(2)))
                            {
                                int clearance = Math.Max(Math.Abs(dx) - 5, Math.Abs(dy) - 5);
                                try
                                {
                                    if ((clearance >= 0) && (Map[scale, t.x - 2 + dx, t.y - 2 + dy] != -1))
                                        Map[scale, t.x - 2 + dx, t.y - 2 + dy] += EnemyClearance[clearance];
                                    else if (clearance < 0)
                                        Map[scale, t.x - 2 + dx, t.y - 2 + dy] = -1;
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    Debug.WriteLine("ERROR: Opponent tank ({0},{1}) out of bounds at ({2},{3}) during path planning, clearance {4}",
                                        t.x, t.y, t.x - 2 + dx, t.y - 2 + dy, clearance);
                                }
                            }
                        }

            /* Don't crash into our own base:
             * 
             * ········
             * ·XXXXX··
             * ·XXXXX··
             * ·XXAXXB·
             * ·XXXXX··
             * ·XXXXX··
             * ········
             * 
             * X is the player's tank (with centerpoint A) and B is the player's base.
             * The safe clearance needs to be at least 3 (just touching the base). Everything
             * in a radius of 2 units (5x5 square) around the base is a collision (no-go) for
             * the player's tanks.
             */

            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if ((board.playerBase.x + dx - 2 >= 0) && (board.playerBase.x + dx - 2 < Map.GetLength(1)) &&
                        (board.playerBase.y + dy - 2 >= 0) && (board.playerBase.y + dy - 2 < Map.GetLength(2)))
                        Map[scale, board.playerBase.x + dx - 2, board.playerBase.y + dy - 2] = -1;

            // Build the rest of the scale space
            for (int s = 1; s <= scaleSpace; s++)
            {
                for (int x = 0; x < ((board.xsize-4) >> s); x++)
                {
                    int mapX = x << 1;
                    for (int y = 0; y < ((board.ysize-4) >> s); y++)
                    {
                        int mapY = y << 1;
                        Map[s, x, y] = 0;
                        bool impassable = false;
                        for (int dx = 0; dx < 2; dx++)
                        {
                            for (int dy = 0; dy < 2; dy++)
                            {
                                if ((mapX + dx < (board.xsize-4)) && (mapY + dy < (board.ysize-4)))
                                    Map[s, x, y] += Map[s - 1, mapX + dx, mapY + dy];
                                else
                                {
                                    impassable = true;
                                    break;
                                }
                            }
                            if (impassable)
                                break;
                        }
                        if (impassable)
                            Map[s, x, y] = -1;
                        else
                            Map[s, x, y] /= 4.0;
                    }
                }
            }
        }

        private Bitmap DrawMap(Board board, int scale = 0)
        {
            Bitmap b = new Bitmap(board.xsize * 8, board.ysize * 8);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.FromArgb(255, 0, 0));
                for (int x = 0; x < Map.GetLength(1); x++)
                    for (int y = 0; y < Map.GetLength(2); y++)
                    {
                        int green = 0;
                        int blue = 0;
                        int red = (int)Math.Round((Map[scale, x >> scale, y >> scale] - 1.0) * 200);
                        if (red > 255)
                            red = 255;
                        else if (red < 0)
                        {
                            red = 255;
                            green = 255;
                            blue = 255;
                        }
                        Rectangle rect = new Rectangle((x + 2) * 8, (y + 2) * 8, 8, 8);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(red, green, blue)), rect);
                    }
            }
            return b;
        }

        public void RenderMap(Board board, string filename, int scale = 0)
        {
            // Create a rendering of the cost map, superimposed onto the board.
            using (Bitmap b = DrawMap(board, scale))
            {
                b.Save(filename, ImageFormat.Png);
            }
        }

        public void RenderRoute(Board board, List<Tuple<int,int>> route, string filename, int scale = 0)
        {
            // Create a rendering of the cost map, with the specified route superimposed on it.
            using (Bitmap b = DrawMap(board, scale))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    foreach (Tuple<int, int> p in route)
                    {
                        Rectangle rect = new Rectangle( (p.Item1 - 2) * 8 + 2, (p.Item2 - 2) * 8 + 2, 4, 4);
                        g.FillEllipse(new SolidBrush(Color.FromArgb(0, 255, 0)), rect);
                    }
                }
                b.Save(filename, ImageFormat.Png);
            }
        }

        public List<Tuple<int, int>> GetPath(int x1, int y1, int x2, int y2, CancellationTokenSource cancel, int scaleSpace = 0)
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();

            // Save the destination on which the path was planned. This is useful if the planning gets cancelled
            // and we later want to re-run the plan (or substitute it with an alternative).
            DestX = x2;
            DestY = y2;
            int scale = scaleSpace;

            int sx1 = ((x1 - 2) >> scale) << scale;
            int sy1 = ((y1 - 2) >> scale) << scale;
            int sx2 = ((x2 - 2) >> scale) << scale;
            int sy2 = ((y2 - 2) >> scale) << scale;

            {
                Games.Pathfinding.AStar astar = new Games.Pathfinding.AStar();

                AStarNodeBC goalNode = new AStarNodeBC(null, null, 0, sx2, sy2, this, scale);
                AStarNodeBC startNode = new AStarNodeBC(null, goalNode, 0, sx1, sy1, this, scale);
                startNode.GoalNode = goalNode;
                astar.FindPath(startNode, goalNode, cancel);

                foreach (AStarNodeBC n in astar.Solution)
                    result.Add(new Tuple<int, int>(n.X + 2, n.Y + 2));
            }

            if (!cancel.IsCancellationRequested && (scaleSpace > 0))
            {
                /* If we've been working on a coarse first estimate, now connect the dots to find a finer
                 * path for the first leg of the coarse path.
                 */
                int replaceNode = Math.Min(2, result.Count - 1);
                Games.Pathfinding.AStar astar = new Games.Pathfinding.AStar();

                // In the area of the coarse node that we're targeting, find the map coordinate with the
                // lowest cost.

                double minCost = double.PositiveInfinity;
                for (int dx = 0; dx < (1 << scale); dx++)
                    for (int dy = 0; dy < (1 << scale); dy++)
                    {
                        double thisCost = double.PositiveInfinity;
                        int thisX = (result[replaceNode].Item1 - 2) + dx;
                        int thisY = (result[replaceNode].Item2 - 2) + dy;
                        if ((thisX > 0) && (thisX < Map.GetLength(1)) && (thisY > 0) && (thisY < Map.GetLength(2)))
                            thisCost = Map[0, thisX, thisY];
                        if (thisCost < minCost)
                        {
                            sx2 = thisX;
                            sy2 = thisY;
                            minCost = thisCost;
                        }
                    }

                AStarNodeBC goalNode = new AStarNodeBC(null, null, 0, sx2, sy2, this, 0);
                AStarNodeBC startNode = new AStarNodeBC(null, goalNode, 0, x1 - 2 , y1 - 2, this, 0);
                startNode.GoalNode = goalNode;
                astar.FindPath(startNode, goalNode, cancel);

                List<Tuple<int, int>> finePath = new List<Tuple<int, int>>();

                int skip = 0;
                foreach (AStarNodeBC n in astar.Solution)
                {
                    if (skip++ >= 0)
                        finePath.Add(new Tuple<int, int>(n.X + 2, n.Y + 2));
                }

                if (finePath.Count > 0)
                {
                    // Not particularly efficient; both operations are O(n)
                    result.RemoveRange(0, replaceNode+1);
                    result.InsertRange(0, finePath);
                }
            }

            return result;
        }
	}
}

