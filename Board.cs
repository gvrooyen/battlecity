using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace battlecity
{

    public class Tank
    {
        public int x { get; set; }
        public int y { get; set; }
        public bool destroyed { get; set; }
        public int id { get; set; }
        public ChallengeService.direction direction { get; set; }

        private Queue<int> xHistory;
        private Queue<int> yHistory;

        // Tanks' positions get updated every tick. If an update is skipped, it means the tank is destroyed.
        public bool updated { get; set; }

        // The bullet fired by this tank (null if no bullet is in play)
        public Bullet bullet { get; set; }

        // The tank's current role in the battle
        public Role.Role role { get; set; }

        // The tank's plans for the future
        public LinkedList<Plan.Plan> plans;

        private void Initialize()
        {
            destroyed = false;
            bullet = null;
            role = null;
            plans = new LinkedList<Plan.Plan>();
            xHistory = new Queue<int>();
            yHistory = new Queue<int>();
        }

        public Tank()
        {
            Initialize();
            x = -1;
            y = -1;
            id = -1;
            updated = false;
            direction = ChallengeService.direction.NONE;
        }

        public Tank(int x, int y, ChallengeService.direction direction, int id)
        {
            Initialize();
            this.x = x;
            this.y = y;
            this.id = id;
            this.direction = direction;
            updated = true;
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

        public bool Watchdog()
        {
            /* Monitor the tank's activity, and reset it if it seems to be stuck.
             */
            int period = 2;

            // Update the history
            if (bullet != null)
            {
                xHistory.Clear();
                xHistory.Enqueue(x);
                yHistory.Clear();
                yHistory.Enqueue(y);
                return true;
            }
            else
            {
                xHistory.Enqueue(x);
                yHistory.Enqueue(y);
            }

            if (xHistory.Count >= period)
            {
                int pastValue = xHistory.Peek();
                foreach (int x in xHistory)
                    if (x != pastValue)
                        return true;
                pastValue = yHistory.Peek();
                foreach (int y in yHistory)
                    if (y != pastValue)
                        return true;
            }
            else
                return true;

            Debug.WriteLine("WARNING: Watchdog triggered for tank (id={0})", id);
            xHistory.Clear();
            yHistory.Clear();
            plans.Clear();
            return false;
        }
    }

    public class Base
    {
        public int x { get; set; }
        public int y { get; set; }

        public Base()
        {
            x = -1;
            y = -1;
        }
    }

    public class Bullet
    {
        public int x { get; set; }
        public int y { get; set; }
        public int id { get; set; }
        public ChallengeService.direction direction { get; set; }
        public Tank owner { get; set; }

        // Bullets' positions get updated every tick. If an update is skipped, it means the bullet is destroyed.
        public bool updated { get; set; }

        public Bullet()
        {
            x = -1;
            y = -1;
            id = -1;
            direction = ChallengeService.direction.NONE;
            owner = null;
            updated = false;
        }

        public Bullet(int x, int y, ChallengeService.direction direction, int id)
        {
            this.x = x;
            this.y = y;
            this.direction = direction;
            this.id = id;
            this.updated = true;
        }
    }

    public class Board
    {
        private static Dictionary<ChallengeService.state?, string> icon;
        public ChallengeService.state?[][] board { get; private set; }
        public int xsize { get; private set; }
        public int ysize { get; private set; }

        public string playerName { get; set; }
        public string opponentName { get; set; }

        private int _playerID;
        public int playerID
        {
            get { return _playerID; }
            set
            {
                this._playerID = value;
                this._opponentID = (value == 0) ? 1 : 0;
            }
        }

        private int _opponentID;
        public int opponentID
        {
            get { return _opponentID; }
            set
            {
                this._opponentID = value;
                this._playerID = (value == 0) ? 1 : 0;
            }
        }

        public Tank[] playerTank { get; set; }
        public Tank[] opponentTank { get; set; }
        public Base playerBase { get; set; }
        public Base opponentBase { get; set; }

        // Bullets are stored in a dictionary, indexed by ID. This makes it easier to track bullets' appearance
        // and disappearance in game client updates.
        public Dictionary<int, Bullet> playerBullet { get; private set; }
        public Dictionary<int, Bullet> opponentBullet { get; private set; }

        private void Initialize()
        {
            icon = new Dictionary<ChallengeService.state?, string>
            {
                {ChallengeService.state.FULL, "#"},
                {ChallengeService.state.EMPTY, " "},
                {ChallengeService.state.NONE, "?"},
                {ChallengeService.state.OUT_OF_BOUNDS, "%"}
            };
            playerTank = new Tank[2];
            opponentTank = new Tank[2];
            playerBase = new Base();
            opponentBase = new Base();
            playerBullet = new Dictionary<int, Bullet>();
            opponentBullet = new Dictionary<int, Bullet>();
        }

        public Board()
        {
            Initialize();
        }

        public Board(ChallengeService.state?[][] board)
        {
            Initialize();
            this.board = board;
            xsize = board.Length;
            if (xsize > 0)
                ysize = board[0].Length;
            else
                ysize = 0;
        }

        public override string ToString()
        {
            return PrintArea();
        }

        public string PrintArea(int x1 = 0, int y1 = 0, int x2 = int.MaxValue, int y2 = int.MaxValue)
        {
            List<StringBuilder> lines = new List<StringBuilder>();

            if (x1 < 0)
                x1 = 0;
            if (x2 > this.xsize)
                x2 = this.xsize;
            if (y1 < 0)
                y1 = 0;
            if (y2 > this.ysize)
                y2 = this.ysize;

            for (int y = y1; y < y2; y++)
            {
                StringBuilder S = new StringBuilder("");
                for (int x = x1; x < x2; x++)
                {
                    if (((x == playerBase.x) && (y == playerBase.y)) || ((x == opponentBase.x) && (y == opponentBase.y)))
                        S.Append("$");
                    else if ((playerTank.Length > 0) &&
                             (((playerTank[0] != null) && !playerTank[0].destroyed &&
                               (Math.Abs(x - playerTank[0].x) <= 2) && (Math.Abs(y - playerTank[0].y) <= 2)) ||
                              ((playerTank[1] != null) && !playerTank[1].destroyed &&
                               (Math.Abs(x - playerTank[1].x) <= 2) && (Math.Abs(y - playerTank[1].y) <= 2))))
                        // This is a bit silly. We should really just paint in the tanks after the main loop.
                        if (((x - playerTank[0].x == 2) && (y == playerTank[0].y) && (playerTank[0].direction == ChallengeService.direction.RIGHT)) ||
                            ((x - playerTank[0].x == -2) && (y == playerTank[0].y) && (playerTank[0].direction == ChallengeService.direction.LEFT)) ||
                            ((y - playerTank[0].y == 2) && (x == playerTank[0].x) && (playerTank[0].direction == ChallengeService.direction.DOWN)) ||
                            ((y - playerTank[0].y == -2) && (x == playerTank[0].x) && (playerTank[0].direction == ChallengeService.direction.UP)) ||
                            ((x - playerTank[1].x == 2) && (y == playerTank[1].y) && (playerTank[1].direction == ChallengeService.direction.RIGHT)) ||
                            ((x - playerTank[1].x == -2) && (y == playerTank[1].y) && (playerTank[1].direction == ChallengeService.direction.LEFT)) ||
                            ((y - playerTank[1].y == 2) && (x == playerTank[1].x) && (playerTank[1].direction == ChallengeService.direction.DOWN)) ||
                            ((y - playerTank[1].y == -2) && (x == playerTank[1].x) && (playerTank[1].direction == ChallengeService.direction.UP)))
                            S.Append("O");
                        else
                            S.Append("X");
                    else if ((opponentTank.Length > 0) &&
                             (((opponentTank[0] != null) && !opponentTank[0].destroyed &&
                               (Math.Abs(x - opponentTank[0].x) <= 2) && (Math.Abs(y - opponentTank[0].y) <= 2)) ||
                              ((opponentTank[1] != null) && !opponentTank[1].destroyed &&
                               (Math.Abs(x - opponentTank[1].x) <= 2) && (Math.Abs(y - opponentTank[1].y) <= 2))))
                        if (((x - opponentTank[0].x == 2) && (y == opponentTank[0].y) && (opponentTank[0].direction == ChallengeService.direction.RIGHT)) ||
                            ((x - opponentTank[0].x == -2) && (y == opponentTank[0].y) && (opponentTank[0].direction == ChallengeService.direction.LEFT)) ||
                            ((y - opponentTank[0].y == 2) && (x == opponentTank[0].x) && (opponentTank[0].direction == ChallengeService.direction.DOWN)) ||
                            ((y - opponentTank[0].y == -2) && (x == opponentTank[0].x) && (opponentTank[0].direction == ChallengeService.direction.UP)) ||
                            ((x - opponentTank[1].x == 2) && (y == opponentTank[1].y) && (opponentTank[1].direction == ChallengeService.direction.RIGHT)) ||
                            ((x - opponentTank[1].x == -2) && (y == opponentTank[1].y) && (opponentTank[1].direction == ChallengeService.direction.LEFT)) ||
                            ((y - opponentTank[1].y == 2) && (x == opponentTank[1].x) && (opponentTank[1].direction == ChallengeService.direction.DOWN)) ||
                            ((y - opponentTank[1].y == -2) && (x == opponentTank[1].x) && (opponentTank[1].direction == ChallengeService.direction.UP)))
                            S.Append("O");
                        else
                            S.Append("Y");
                    else
                        S.Append(icon[this.board[x][y]]);
                }
                lines.Add(S);
            }

            foreach (KeyValuePair<int, Bullet> b in playerBullet)
            {
                if ((b.Value.x >= x1) && (b.Value.x < x2) && (b.Value.y >= y1) && (b.Value.y < y2))
                {
                    lines[b.Value.y - y1][b.Value.x - x1] = '*';
                }
            }
            foreach (KeyValuePair<int, Bullet> b in opponentBullet)
            {
                if ((b.Value.x >= x1) && (b.Value.x < x2) && (b.Value.y >= y1) && (b.Value.y < y2))
                {
                    lines[b.Value.y - y1][b.Value.x - x1] = '*';
                }
            }

            string result = "";
            foreach (StringBuilder line in lines)
            {
                result += line.ToString() + Environment.NewLine;
            }

            return result;
        }

        public void Update(ChallengeService.game status)
        {
            if (playerName == null)     // First time update is called
            {
                playerName = status.playerName;
                if (status.players[0].name == status.playerName)
                    playerID = 0;
                else if (status.players[1].name == status.playerName)
                    playerID = 1;
                else
                    throw new ArgumentException("Player '{0}' not found in player list.", status.playerName);

                playerBase.x = status.players[playerID].@base.x;
                playerBase.y = status.players[playerID].@base.y;
                opponentBase.x = status.players[opponentID].@base.x;
                opponentBase.y = status.players[opponentID].@base.y;

                if ((status.players[playerID].bullets == null) && (status.players[opponentID].bullets == null))
                    Debug.WriteLine("No bullets in play yet.");
                else
                    Debug.WriteLine("WARNING: bullets already in play!");
                int i = 0;
                Debug.WriteLine("");
                Debug.WriteLine("Player base is at ({0},{1}).", playerBase.x, playerBase.y);
                Debug.WriteLine("Opponent base is at ({0},{1}).", opponentBase.x, opponentBase.y);

                foreach (ChallengeService.unit u in status.players[playerID].units)
                {
                    playerTank[i++] = new Tank(u.x, u.y, u.direction, u.id);
                    Debug.WriteLine("Player tank ID {0} starts at ({1},{2}), facing {3}.",
                        u.id, u.x, u.y, u.direction);
                }

                i = 0;
                foreach (ChallengeService.unit u in status.players[opponentID].units)
                {
                    opponentTank[i++] = new Tank(u.x, u.y, u.direction, u.id);
                    Debug.WriteLine("Opponent tank ID {0} starts at ({1},{2}), facing {3}.",
                        u.id, u.x, u.y, u.direction);
                }
            }

            // Update tank positions


            foreach (Tank t in playerTank)
            {
                bool wasDestroyed = t.destroyed;
                t.destroyed = true;
                ChallengeService.unit[] serverUnit = status.players[playerID].units;
                if ((serverUnit.Length > 0) && (serverUnit[0] != null) && (serverUnit[0].id == t.id))
                {
                    t.x = serverUnit[0].x;
                    t.y = serverUnit[0].y;
                    t.direction = serverUnit[0].direction;
                    t.destroyed = false;
                } else if ((serverUnit.Length > 1) && (serverUnit[1] != null) && (serverUnit[1].id == t.id))
                {
                    t.x = serverUnit[1].x;
                    t.y = serverUnit[1].y;
                    t.direction = serverUnit[1].direction;
                    t.destroyed = false;
                } else
                {
                    if (!wasDestroyed)
                        Debug.WriteLine("Player tank (id={0}) destroyed!", t.id);
                }
            }

            foreach (Tank t in opponentTank)
            {
                bool wasDestroyed = t.destroyed;
                t.destroyed = true;
                ChallengeService.unit[] serverUnit = status.players[opponentID].units;
                if ((serverUnit.Length > 0) && (serverUnit[0] != null) && (serverUnit[0].id == t.id))
                {
                    t.x = serverUnit[0].x;
                    t.y = serverUnit[0].y;
                    t.direction = serverUnit[0].direction;
                    t.destroyed = false;
                }
                else if ((serverUnit.Length > 1) && (serverUnit[1] != null) && (serverUnit[1].id == t.id))
                {
                    t.x = serverUnit[1].x;
                    t.y = serverUnit[1].y;
                    t.direction = serverUnit[1].direction;
                    t.destroyed = false;
                }
                else
                {
                    if (!wasDestroyed)
                        Debug.WriteLine("Opponent tank (id={0}) destroyed!", t.id);
                }
            }

            // Update bullet positions, and capture new ones.

            foreach (KeyValuePair<int, Bullet> bullet in playerBullet)
                // For each bullet that we're tracking, clear its updated status. If it doesn't get updated
                // in the latest status report, we know that it's been destroyed.
                bullet.Value.updated = false;

            if (status.players[playerID].bullets != null)
                foreach (ChallengeService.bullet b in status.players[playerID].bullets)
                {
                    Bullet myBullet;
                    if (playerBullet.TryGetValue(b.id, out myBullet))
                    {
                        // This is a bullet we know. Update its position.
                        myBullet.x = b.x;
                        myBullet.y = b.y;
                        if (myBullet.direction != b.direction)
                            Debug.WriteLine("ERROR: Player bullet #{0} changed direction from {1} to {2}!",
                                myBullet.direction, b.direction);
                        myBullet.updated = true;
                    }
                    else
                    {
                        // This is a bullet that has just been fired. Create a new entry, and find out who its daddy is.

                        Bullet newBullet = new Bullet(b.x, b.y, b.direction, b.id);
                        int ownerX, ownerY;
                        switch (b.direction)
                        {
                            case ChallengeService.direction.DOWN:
                                ownerX = b.x;
                                ownerY = b.y - 3;
                                break;
                            case ChallengeService.direction.LEFT:
                                ownerX = b.x + 3;
                                ownerY = b.y;
                                break;
                            case ChallengeService.direction.RIGHT:
                                ownerX = b.x - 3;
                                ownerY = b.y;
                                break;
                            case ChallengeService.direction.UP:
                                ownerX = b.x;
                                ownerY = b.y + 3;
                                break;
                            default:
                                ownerX = b.x;
                                ownerY = b.y;
                                Debug.WriteLine("ERROR: Player bullet #{0} created without firing direction.", b.direction);
                                break;
                        }
                        if (!playerTank[0].destroyed && (playerTank[0].x == ownerX) && (playerTank[0].y == ownerY))
                        {
                            newBullet.owner = playerTank[0];
                            playerTank[0].bullet = newBullet;
                        }
                        else if (!playerTank[1].destroyed && (playerTank[1].x == ownerX) && (playerTank[1].y == ownerY))
                        {
                            newBullet.owner = playerTank[1];
                            playerTank[1].bullet = newBullet;
                        }
                        else if ((Math.Abs(playerTank[0].x - ownerX) <= 2) && (Math.Abs(playerTank[0].y - ownerY) <= 2))
                        {
                            Debug.WriteLine("WARNING: Player bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
                            newBullet.owner = playerTank[0];
                            playerTank[0].bullet = newBullet;
                        }
                        else if ((Math.Abs(playerTank[1].x - ownerX) <= 2) && (Math.Abs(playerTank[1].y - ownerY) <= 2))
                        {
                            Debug.WriteLine("WARNING: Player bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
                            newBullet.owner = playerTank[1];
                            playerTank[1].bullet = newBullet;
                        }
                        // REFACTOR: We're actually violating DRY here, since we're keeping two separate lists of bullets
                        // (one inside the player/opponent bullet lists, another inside the tank objects themselves. It
                        // could perhaps be cleaner just to store the bullet objects in the tank object.
                        playerBullet[b.id] = newBullet;
                    }
                }

            List<int> destroyedBullets = new List<int>();
            foreach (KeyValuePair<int, Bullet> bullet in playerBullet)
                if (bullet.Value.updated == false)
                {
                    // This bullet has not been updated this round, so it must have been destroyed.
                    // Remove the tank's reference to it.
                    if (bullet.Value.owner != null)
                        bullet.Value.owner.bullet = null;
                    // We can't remove it from the dictionary inside the loop, so take note of the destroyed bullets.
                    destroyedBullets.Add(bullet.Key);
                }
            foreach (int key in destroyedBullets)
                playerBullet.Remove(key);

            foreach (KeyValuePair<int, Bullet> bullet in opponentBullet)
                // For each bullet that we're tracking, clear its updated status. If it doesn't get updated
                // in the latest status report, we know that it's been destroyed.
                bullet.Value.updated = false;

            if (status.players[opponentID].bullets != null)
                foreach (ChallengeService.bullet b in status.players[opponentID].bullets)
                {
                    Bullet myBullet;
                    if (opponentBullet.TryGetValue(b.id, out myBullet))
                    {
                        // This is a bullet we know. Update its position.
                        myBullet.x = b.x;
                        myBullet.y = b.y;
                        if (myBullet.direction != b.direction)
                            Debug.WriteLine("ERROR: Opponent bullet #{0} changed direction from {1} to {2}!",
                                myBullet.direction, b.direction);
                        myBullet.updated = true;
                    }
                    else
                    {
                        // This is a bullet that has just been fired. Create a new entry, and find out who its daddy is.

                        Bullet newBullet = new Bullet(b.x, b.y, b.direction, b.id);
                        int ownerX, ownerY;
                        switch (b.direction)
                        {
                            case ChallengeService.direction.DOWN:
                                ownerX = b.x;
                                ownerY = b.y - 3;
                                break;
                            case ChallengeService.direction.LEFT:
                                ownerX = b.x + 3;
                                ownerY = b.y;
                                break;
                            case ChallengeService.direction.RIGHT:
                                ownerX = b.x - 3;
                                ownerY = b.y;
                                break;
                            case ChallengeService.direction.UP:
                                ownerX = b.x;
                                ownerY = b.y + 3;
                                break;
                            default:
                                ownerX = b.x;
                                ownerY = b.y;
                                Debug.WriteLine("ERROR: Opponent bullet #{0} created without firing direction.", b.direction);
                                break;
                        }
                        if (!opponentTank[0].destroyed && (opponentTank[0].x == ownerX) && (opponentTank[0].y == ownerY))
                        {
                            newBullet.owner = opponentTank[0];
                            opponentTank[0].bullet = newBullet;
                        }
                        else if (!opponentTank[1].destroyed && (opponentTank[1].x == ownerX) && (opponentTank[1].y == ownerY))
                        {
                            newBullet.owner = opponentTank[1];
                            opponentTank[1].bullet = newBullet;
                        }
                        else if ((Math.Abs(opponentTank[0].x - ownerX) <= 2) && (Math.Abs(opponentTank[0].y - ownerY) <= 2))
                        {
                            Debug.WriteLine("WARNING: Opponent bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
                            newBullet.owner = opponentTank[0];
                            opponentTank[0].bullet = newBullet;
                        }
                        else if ((Math.Abs(opponentTank[1].x - ownerX) <= 2) && (Math.Abs(opponentTank[1].y - ownerY) <= 2))
                        {
                            Debug.WriteLine("WARNING: Opponent bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
                            newBullet.owner = opponentTank[1];
                            opponentTank[1].bullet = newBullet;
                        }
                        opponentBullet[b.id] = newBullet;
                    }
                }

            destroyedBullets = new List<int>();
            foreach (KeyValuePair<int, Bullet> bullet in opponentBullet)
                if (bullet.Value.updated == false)
                {
                    // This bullet has not been updated this round, so it must have been destroyed.
                    // Remove the tank's reference to it.
                    if (bullet.Value.owner != null)
                        bullet.Value.owner.bullet = null;
                    // We can't remove it from the dictionary inside the loop, so take note of the destroyed bullets.
                    destroyedBullets.Add(bullet.Key);
                }
            foreach (int key in destroyedBullets)
                opponentBullet.Remove(key);
            
            if (status.events == null)
            {
                // Debug.WriteLine("No events.");
                return;
            }
            else
            {
                // Debug.WriteLine(state.events.ToString());
            }
            if (status.events.blockEvents != null)
                foreach (ChallengeService.blockEvent e in status.events.blockEvents)
                {
                    board[e.point.x][e.point.y] = e.newState;
                    Debug.WriteLine("Block at ({0},{1}) changed to {2}.", e.point.x, e.point.y, e.newState);
                }
            if (status.events.unitEvents != null)
                foreach (ChallengeService.unitEvent e in status.events.unitEvents)
                {
                    // These don't seem to do anything, so just ignore it.
                    Debug.WriteLine("WARNING: UNIT EVENT: {0}", e.unit);
                }
        }

        public ChallengeService.state getState(int x, int y)
        {
            return (ChallengeService.state) board[x][y];
        }

        public bool ClearShot(int x1, int y1, int x2, int y2)
        {
            /* Returns true if there is are no filled blocks between the two coordinates.
             * The coordinates must lie on a horizontal or vertical line.
             */
            if (x1 == x2)
            {
                for (int y = y1; y != y2; y += Math.Sign(y2 - y1))
                    if (board[x1][y] == ChallengeService.state.FULL)
                        return false;
            }
            else if (y1 == y2)
            {
                for (int x = x1; x != x2; x += Math.Sign(x2 - x1))
                    if (board[x][y1] == ChallengeService.state.FULL)
                        return false;
            }
            else
                return false;

            return true;
        }
    }
}
