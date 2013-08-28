using System;
using Games.Pathfinding;

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

		// Constructor for a node in a 2-dimensional map
		public AStarNodeBC(AStarNode AParent, AStarNode AGoalNode, double ACost, int AX, int AY) : base(AParent, AGoalNode, ACost)
		{
			FX = AX;
			FY = AY;
		}

		// Adds a successor to a list if it is not impassible or the parent node
		private void AddSuccessor(ArrayList ASuccessors, int AX, int AY) 
		{
			int CurrentCost = MainClass.GetMap(AX, AY);
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
				double xd = FX - ((AStarNode2D)GoalNode).X;
				double yd = FY - ((AStarNode2D)GoalNode).Y;
				// "Euclidean distance" - Used when search can move at any angle.
				//GoalEstimate = Math.Sqrt((xd*xd) + (yd*yd));
				// "Manhattan Distance" - Used when search can only move vertically and 
				// horizontally.
				GoalEstimate = Math.Abs(xd) + Math.Abs(yd); 
				// "Diagonal Distance" - Used when the search can move in 8 directions.
				//GoalEstimate = Math.Max(Math.Abs(xd),Math.Abs(yd));
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
			// AddSuccessor(ASuccessors,FX-1,FY-1);
			AddSuccessor(ASuccessors, FX  , FY-1);
			// AddSuccessor(ASuccessors,FX+1,FY-1);
			AddSuccessor(ASuccessors, FX+1, FY  );
			// AddSuccessor(ASuccessors,FX+1,FY+1);
			AddSuccessor(ASuccessors, FX  , FY+1);
			// AddSuccessor(ASuccessors,FX-1,FY+1);
		}	

		// Prints information about the current node
		public override void PrintNodeInfo()
		{
			Console.WriteLine("X:\t{0}\tY:\t{1}\tCost:\t{2}\tEst:\t{3}\tTotal:\t{4}",
			                  FX, FY, Cost, GoalEstimate, TotalCost);
		}
	}
	
	public class PathPlanner
	{
		public int[,] kernel;
		private int[,] map;
		
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
		}
		
		public PathPlanner ()
		{			
		}
		
		public void mapBoard(Board board)
		{
			/* Scan through the board and apply the kernel, and use that to create a map of movement
			 * costs for the A* algorithm.
			 */
			for (int x = 2; x < board.xsize - 2; x++)
				for (int y = 2; y < board.ysize -2; x++)
				{
				}
		}
	}
}

