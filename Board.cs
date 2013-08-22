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
            this.x = 0;
            this.y = 0;
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
                    S += icon[this.board[x][y]];
                S += "\n";
            }
            return S;
        }

        public void Update(ChallengeService.events event_list)
        {
            if (event_list == null)
            {
                // Console.WriteLine("No events.");
                return;
            }
            else
            {
                // Console.WriteLine(event_list.ToString());
            }
            if (event_list.blockEvents != null)
                foreach (ChallengeService.blockEvent e in event_list.blockEvents)
                {
                    board[e.point.x][e.point.y] = e.newState;
                    Console.WriteLine("Block at ({0},{1}) changed to {2}.", e.point.x, e.point.y, e.newState);
                }
            if (event_list.unitEvents != null)
                foreach (ChallengeService.unitEvent e in event_list.unitEvents)
                {
                    Console.WriteLine("UNIT EVENT: {0}", e.ToString());
                }
        }
    }
}
