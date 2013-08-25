using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{

    class Tank
    {
        public int x { get; set; }
        public int y { get; set; }
        public bool destroyed { get; set; }
        public int id { get; set; }
        public ChallengeService.direction direction { get; set; }

        public Tank()
        {
            this.x = -1;
            this.y = -1;
            this.destroyed = false;
        }

        public Tank(int x, int y, ChallengeService.direction direction, int id)
        {
            this.x = x;
            this.y = y;
            this.id = id;
            this.direction = direction;
            this.destroyed = false;
        }
    }

    class Base
    {
        public int x { get; set; }
        public int y { get; set; }

        public Base()
        {
            x = -1;
            y = -1;
        }
    }

    class Board
    {
        private static Dictionary<ChallengeService.state?, string> icon;
        private ChallengeService.state?[][] board;
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

        private void Initialize()
        {
            icon = new Dictionary<ChallengeService.state?, string>
            {
                {ChallengeService.state.FULL, "#"},
                {ChallengeService.state.EMPTY, " "},
                {ChallengeService.state.NONE, "?"},
                {ChallengeService.state.OUT_OF_BOUNDS, "%"}
            };
            this.playerTank = new Tank[2];
            this.opponentTank = new Tank[2];
            this.playerBase = new Base();
            this.opponentBase = new Base();
        }

        public Board()
        {
            this.Initialize();
        }

        public Board(ChallengeService.state?[][] board)
        {
            this.Initialize();
            this.board = board;
            this.xsize = board.Length;
            if (this.xsize > 0)
                this.ysize = board[0].Length;
            else
                this.ysize = 0;
        }

        public override string ToString()
        {
            string S = "";

            for (int y = 0; y < this.ysize; y++)
            {
                for (int x = 0; x < this.xsize; x++)
                {
                    if (((x == playerBase.x) && (y == playerBase.y)) || ((x == opponentBase.x) && (y == opponentBase.y)))
                        S += "$";
                    else if ((playerTank.Length > 0) &&
                             (((playerTank[0] != null) && !playerTank[0].destroyed &&
                               (Math.Abs(x - playerTank[0].x) <= 2) && (Math.Abs(y - playerTank[0].y) <= 2)) ||
                              ((playerTank[1] != null) && !playerTank[1].destroyed &&
                               (Math.Abs(x - playerTank[1].x) <= 2) && (Math.Abs(y - playerTank[1].y) <= 2))))
                        if (((x - playerTank[0].x == 2) && (y == playerTank[0].y) && (playerTank[0].direction == ChallengeService.direction.RIGHT)) ||
                            ((x - playerTank[0].x == -2) && (y == playerTank[0].y) && (playerTank[0].direction == ChallengeService.direction.LEFT)) ||
                            ((y - playerTank[0].y == 2) && (x == playerTank[0].x) && (playerTank[0].direction == ChallengeService.direction.DOWN)) ||
                            ((y - playerTank[0].y == -2) && (x == playerTank[0].x) && (playerTank[0].direction == ChallengeService.direction.UP)) ||
                            ((x - playerTank[1].x == 2) && (y == playerTank[1].y) && (playerTank[1].direction == ChallengeService.direction.RIGHT)) ||
                            ((x - playerTank[1].x == -2) && (y == playerTank[1].y) && (playerTank[1].direction == ChallengeService.direction.LEFT)) ||
                            ((y - playerTank[1].y == 2) && (x == playerTank[1].x) && (playerTank[1].direction == ChallengeService.direction.DOWN)) ||
                            ((y - playerTank[1].y == -2) && (x == playerTank[1].x) && (playerTank[1].direction == ChallengeService.direction.UP)))
                            S += "O";
                        else
                            S += "X";
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
                            S += "O";
                        else
                            S += "Y";
                    else
                        S += icon[this.board[x][y]];
                }
                S += Environment.NewLine;
            }
            return S;
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
                    Console.WriteLine("UNIT EVENT: {0}", e.ToString());
                }
        }

        public ChallengeService.state getState(int x, int y)
        {
            return (ChallengeService.state) board[x][y];
        }
    }
}
