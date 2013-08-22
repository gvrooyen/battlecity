using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace battlecity
{
    abstract class AI
    {
        protected Board board;
        protected ChallengeService.ChallengeClient client;

        public AI() { }
        public AI(Board board, ChallengeService.ChallengeClient client)
        {
            this.board = board;
            this.client = client;
        }

        public abstract void postEarlyMove();
        public abstract void postFinalMove();

    }

    class AI_Random: AI
    {
        static readonly Random random = new Random(Guid.NewGuid().GetHashCode());

        public AI_Random() : base() { }
        public AI_Random(Board board, ChallengeService.ChallengeClient client) : base(board, client) { }

        public override void postEarlyMove()
        {
            // do nothing
        }

        public override void postFinalMove()
        {
            Array actions = Enum.GetValues(typeof(ChallengeService.action));
            ChallengeService.action A1 = (ChallengeService.action)actions.GetValue(random.Next(actions.Length));
            ChallengeService.action A2 = (ChallengeService.action)actions.GetValue(random.Next(actions.Length));
            System.Console.WriteLine("postFinalMove()");
            System.Console.WriteLine("Tank 1 {0}; Tank 2 {1}", A1, A2);
            lock (client)
            {
                // client.setActions(A1, A2);
                client.setAction(board.playerTank[0].id, A1);
                client.setAction(board.playerTank[1].id, A2);
            }
        }
    }

}
