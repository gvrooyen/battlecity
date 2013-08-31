using System;
using System.Diagnostics;
using Games.Pathfinding;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;

namespace battlecity
{
	public class AStarNodeBC: AStarNode
	{
		/* A node class for doing pathfinding on a 2-dimensional map.
		 * This code has been adapted from a license-free public-domain source:
		 * http://www.codeproject.com/Articles/5758/Path-finding-in-C
		 */
		
		// The X-coordinate of the node
		public int X 
		{
			get 
			{
				return FX;
			}
		}
		private int FX;

		// The Y-coordinate of the node
		public int Y
		{
			get
			{
				return FY;
			}
		}
		private int FY;

        public int[,] map { get; set; }

		// Constructor for a node in a 2-dimensional map
		public AStarNodeBC(AStarNode AParent, AStarNode AGoalNode, double ACost, int AX, int AY) : base(AParent, AGoalNode, ACost)
		{
			FX = AX;
			FY = AY;
		}

		// Adds a successor to a list if it is not impassible or the parent node
		private void AddSuccessor(ArrayList ASuccessors, int AX, int AY) 
		{
			int CurrentCost = map[AX, AY];
			if (CurrentCost == -1)
			{
				return;
			}
			AStarNodeBC NewNode = new AStarNodeBC(this, GoalNode, Cost + CurrentCost, AX, AY);
			if (NewNode.IsSameState(Parent)) 
			{
				return;
			}
			ASuccessors.Add(NewNode);
		}

		// Determines wheather the current node is the same state as the on passed.
		public override bool IsSameState(AStarNode ANode)
		{
			if(ANode == null) 
			{
				return false;
			}
			return ((((AStarNodeBC)ANode).X == FX) &&
				(((AStarNodeBC)ANode).Y == FY));
		}
		
		// Calculates the estimated cost for the remaining trip to the goal.
		public override void Calculate()
		{
			if (GoalNode != null) 
			{
				double xd = FX - ((AStarNodeBC)GoalNode).X;
				double yd = FY - ((AStarNodeBC)GoalNode).Y;
				// "Manhattan Distance" - Used when search can only move vertically and horizontally.
				GoalEstimate = Math.Abs(xd) + Math.Abs(yd); 
			}
			else
			{
				GoalEstimate = 0;
			}
		}

		// Gets all successors nodes from the current node and adds them to the successor list
		public override void GetSuccessors(ArrayList ASuccessors)
		{
			ASuccessors.Clear();
			AddSuccessor(ASuccessors, FX-1, FY  );
			AddSuccessor(ASuccessors, FX  , FY-1);
			AddSuccessor(ASuccessors, FX+1, FY  );
			AddSuccessor(ASuccessors, FX  , FY+1);
		}	

		// Prints information about the current node
		public override void PrintNodeInfo()
		{
			Debug.WriteLine("X:\t{0}\tY:\t{1}\tCost:\t{2}\tEst:\t{3}\tTotal:\t{4}",
			                  FX, FY, Cost, GoalEstimate, TotalCost);
		}
	}
	
	public class PathPlanner
	{
		public int[,] kernel;
		private int[,] map;
		
		// Tick number of the last update (to avoid re-scanning the board in the same round).
		private int lastUpdate;
		
		// A vector describing the added routing penalty close to an enemy tank. Element 0
		// is the penalty for moving directly adjacent to an enemy tank, Element 1 the penalty
		// at 1 block clearance, Element 2 at 2 blocks clearance, etc. Clearance is defined as
		// the smallest of the x or the y clearance (i.e. diagonal distance is not considered).
		public int[] enemyClearance;
		
		private void Initialize()
		{
			/* The kernel is used to estimate the movement cost of having the 5x5 tank's center
			 * point move through a specific block in the map. The kernel calculates the weighted
			 * sum of all the obstacles in the 5x5 area around the center point. The cost gives an
			 * indication of how many walls the tank would first have to destroy, before it can move
			 * its center point to the specified square.
			 */
			kernel = new int[,]
				{{3,3,3,3,3},
				 {3,2,2,2,3},
				 {3,2,1,2,3},
				 {3,2,2,2,3},
				 {3,3,3,3,3}};
			
			enemyClearance = new int[] {30,25,20,15,10,5};
			
			lastUpdate = -2;
		}
		
		public PathPlanner ()
		{
            Initialize();
		}
		
		public void mapBoard(Board board, int tick = -1)
		{
			// If tick is -1, force an update. Otherwise, check that we haven't already updated the map
			// this round.
			if ((tick != -1) && (tick <= lastUpdate))
				return;
			
			lastUpdate = tick;
            map = new int[board.xsize - 4, board.ysize - 4];

			/* Scan through the board and apply the kernel, and use that to create a map of movement
			 * costs for the A* algorithm.
			 */
			for (int x = 2; x < board.xsize - 2; x++)
				for (int y = 2; y < board.ysize -2; y++)
                {
                    // Always start with a cost of one (the actual movement cost to enter the square)
                    map[x-2, y-2] = 1;

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
                                map[x-2, y-2] = -1;
                                break;
                            }
                            
                            map[x-2, y-2] += cost * kernel[dx+2, dy+2];
                        }
                        if (map[x-2, y-2] == -1)
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
             * penalised by the elements of this.enemyClearance.
             * 
             */

            foreach (Tank t in board.playerTank)
                if (!t.destroyed)
                    for (int dx = -4; dx <= 4; dx++)
                        for (int dy = -4; dy <= 4; dy++)
                            if ((t.x - 2 + dx >= 0) && (t.x - 2 + dx < map.GetLength(0)) &&
                                (t.y - 2 + dy >= 0) && (t.y - 2 + dy < map.GetLength(1)))
                            {
                                try
                                {
                                    map[t.x - 2 + dx, t.y - 2 + dy] = -1;
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    Debug.WriteLine("ERROR: Player tank ({0},{1}) out of bounds at ({2},{3}) during path planning",
                                        t.x, t.y, t.x - 2 + dx, t.y - 2 + dy);
                                }
                            }

            foreach (Tank t in board.opponentTank)
                if (!t.destroyed)
                    for (int dx = -4 - enemyClearance.Length; dx <= 4 + enemyClearance.Length; dx++)
                        for (int dy = -4 - enemyClearance.Length; dy <= 4 + enemyClearance.Length; dy++)
                        {
                            if ((t.x - 2 + dx >= 0) && (t.x - 2 + dx < map.GetLength(0)) &&
                                (t.y - 2 + dy >= 0) && (t.y - 2 + dy < map.GetLength(1)))
                            {
                                int clearance = Math.Max(Math.Abs(dx) - 5, Math.Abs(dy) - 5);
                                try
                                {
                                    if ((clearance >= 0) && (map[t.x - 2 + dx, t.y - 2 + dy] != -1))
                                        map[t.x - 2 + dx, t.y - 2 + dy] += enemyClearance[clearance];
                                    else if (clearance < 0)
                                        map[t.x - 2 + dx, t.y - 2 + dy] = -1;
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
                    if ((board.playerBase.x + dx - 2 >= 0) && (board.playerBase.x + dx - 2 < map.GetLength(0)) &&
                        (board.playerBase.y + dy - 2 >= 0) && (board.playerBase.y + dy - 2 < map.GetLength(1)))
                        map[board.playerBase.x + dx - 2, board.playerBase.y + dy - 2] = -1;
        }

        public void renderMap(Board board, string filename)
        {
            Debug.WriteLine("Writing image");
            // Create a rendering of the cost map, superimposed onto the board.
            using (Bitmap b = new Bitmap(board.xsize*8, board.ysize*8))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.Clear(Color.FromArgb(255, 0, 0));
                    for (int x = 0; x < map.GetLength(0); x++)
                        for (int y = 0; y < map.GetLength(1); y++)
                        {
                            int green = 0;
                            int blue = 0;
                            int red = map[x,y]*8;
                            if (red > 255)
                                red = 255;
                            else if (red < 0)
                            {
                                red = 255;
                                green = 255;
                                blue = 255;
                            }
                            Rectangle rect = new Rectangle( (x+2)*8, (y+2)*8, 8, 8 );
                            g.FillRectangle(new SolidBrush(Color.FromArgb(red, green, blue)), rect);
                        }
                }
                b.Save(filename, ImageFormat.Png);
            }
        }
	}
}

