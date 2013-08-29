using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{

    public class Tank
    {
        public int x { get; set; }
        public int y { get; set; }
        public bool destroyed { get; set; }
        public int id { get; set; }
        public ChallengeService.direction direction { get; set; }

        // Tanks' positions get updated every tick. If an update is skipped, it means the tank is destroyed.
        public bool updated { get; set; }

        // The bullet fired by this tank (null if no bullet is in play)
        public Bullet bullet { get; set; }

        public Tank()
        {
            x = -1;
            y = -1;
            destroyed = false;
            id = -1;
            direction = ChallengeService.direction.NONE;
            updated = false;
            bullet = null;
        }

        public Tank(int x, int y, ChallengeService.direction direction, int id)
        {
            this.x = x;
            this.y = y;
            this.id = id;
            this.direction = direction;
            this.destroyed = false;
            this.updated = true;
            this.bullet = null;
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
            List<StringBuilder> lines = new List<StringBuilder>();

            for (int y = 0; y < this.ysize; y++)
            {
                StringBuilder S = new StringBuilder("");
                for (int x = 0; x < this.xsize; x++)
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
                lines[b.Value.y][b.Value.x] = '*';
            }
            foreach (KeyValuePair<int, Bullet> b in opponentBullet)
            {
                lines[b.Value.y][b.Value.x] = '*';
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

                Console.WriteLine("Welcome, {0} (#{1})", playerName, playerID);

                if ((status.players[playerID].bullets == null) && (status.players[opponentID].bullets == null))
                    Console.WriteLine("No bullets in play yet.");
                else
                    Console.WriteLine("WARNING: bullets already in play!");
                int i = 0;
                foreach (ChallengeService.unit u in status.players[playerID].units)
                {
                    playerTank[i++] = new Tank(u.x, u.y, u.direction, u.id);
                    Console.WriteLine("Player tank ID {0} starts at ({1},{2}), facing {3}.",
                        u.id, u.x, u.y, u.direction);
                }

                i = 0;
                foreach (ChallengeService.unit u in status.players[opponentID].units)
                {
                    opponentTank[i++] = new Tank(u.x, u.y, u.direction, u.id);
                    Console.WriteLine("Opponent tank ID {0} starts at ({1},{2}), facing {3}.",
                        u.id, u.x, u.y, u.direction);
                }
            }

            // Update tank positions

            // BUG: These assume that tanks are never destroyed! This is the point where the
            //      number of tanks in the status list should be checked, and if one has disappeared,
            //      its associated board object should be marked as destroyed. It's critical to
            //      track tanks by ID, rather than by their order in the list.

            playerTank[0].x = status.players[playerID].units[0].x;
            playerTank[0].y = status.players[playerID].units[0].y;
            playerTank[0].direction = status.players[playerID].units[0].direction;
            playerTank[1].x = status.players[playerID].units[1].x;
            playerTank[1].y = status.players[playerID].units[1].y;
            playerTank[1].direction = status.players[playerID].units[1].direction;
            opponentTank[0].x = status.players[opponentID].units[0].x;
            opponentTank[0].y = status.players[opponentID].units[0].y;
            opponentTank[0].direction = status.players[opponentID].units[0].direction;
            opponentTank[1].x = status.players[opponentID].units[1].x;
            opponentTank[1].y = status.players[opponentID].units[1].y;
            opponentTank[1].direction = status.players[opponentID].units[1].direction;

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
                            Console.WriteLine("ERROR: Player bullet #{0} changed direction from {1} to {2}!",
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
                                Console.WriteLine("ERROR: Player bullet #{0} created without firing direction.", b.direction);
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
                            Console.WriteLine("WARNING: Player bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
                            newBullet.owner = playerTank[0];
                            playerTank[0].bullet = newBullet;
                        }
                        else if ((Math.Abs(playerTank[1].x - ownerX) <= 2) && (Math.Abs(playerTank[1].y - ownerY) <= 2))
                        {
                            Console.WriteLine("WARNING: Player bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
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
                            Console.WriteLine("ERROR: Opponent bullet #{0} changed direction from {1} to {2}!",
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
                                Console.WriteLine("ERROR: Opponent bullet #{0} created without firing direction.", b.direction);
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
                            Console.WriteLine("WARNING: Opponent bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
                            newBullet.owner = opponentTank[0];
                            opponentTank[0].bullet = newBullet;
                        }
                        else if ((Math.Abs(opponentTank[1].x - ownerX) <= 2) && (Math.Abs(opponentTank[1].y - ownerY) <= 2))
                        {
                            Console.WriteLine("WARNING: Opponent bullet #{0} created too far from closest active tank; taking the best guess.", b.id);
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
                    bullet.Value.owner.bullet = null;
                    // We can't remove it from the dictionary inside the loop, so take note of the destroyed bullets.
                    destroyedBullets.Add(bullet.Key);
                }
            foreach (int key in destroyedBullets)
                opponentBullet.Remove(key);
            
            if (status.events == null)
            {
                // Console.WriteLine("No events.");
                return;
            }
            else
            {
                // Console.WriteLine(state.events.ToString());
            }
            if (status.events.blockEvents != null)
                foreach (ChallengeService.blockEvent e in status.events.blockEvents)
                {
                    board[e.point.x][e.point.y] = e.newState;
                    Console.WriteLine("Block at ({0},{1}) changed to {2}.", e.point.x, e.point.y, e.newState);
                }
            if (status.events.unitEvents != null)
                foreach (ChallengeService.unitEvent e in status.events.unitEvents)
                {
                    // These don't seem to do anything, so just ignore it.
                    Console.WriteLine("WARNING: UNIT EVENT: {0}", e.ToString());
                }
        }

        public ChallengeService.state getState(int x, int y)
        {
            return (ChallengeService.state) board[x][y];
        }
    }
}
