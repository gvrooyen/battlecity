using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{

    class Board
    {
        static Dictionary<ChallengeService.state?, string> icon;

        private void Initialize()
        {
            icon = new Dictionary<ChallengeService.state?, string>
            {
                {ChallengeService.state.FULL, "#"},
                {ChallengeService.state.EMPTY, " "},
                {ChallengeService.state.NONE, "%"},
                {ChallengeService.state.OUT_OF_BOUNDS, "@"}
            };
        }

        private ChallengeService.state?[][] board;

        public Board()
        {
            this.Initialize();
        }

        public Board(ChallengeService.state?[][] board)
        {
            this.Initialize();
            this.board = board;
        }

        public string ToString()
        {
            string S = "";
            foreach (ChallengeService.state?[] row in board)
            {
                foreach (ChallengeService.state? element in row)
                {
                    S += icon[element];
                }
                S += "\n";
            }
            return S;
        }

        public void Update(ChallengeService.events event_list)
        {
            if (event_list == null)
                return;
            if (event_list.blockEvents != null)
                foreach (ChallengeService.blockEvent e in event_list.blockEvents)
                {
                    board[e.point.x][e.point.y] = e.newState;
                }
            if (event_list.unitEvents != null)
                foreach (ChallengeService.unitEvent e in event_list.unitEvents)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
