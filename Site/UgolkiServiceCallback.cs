using System;
using System.Collections.Generic;
using Site.Service;

namespace Site
{
    [System.ServiceModel.CallbackBehavior(ConcurrencyMode = System.ServiceModel.ConcurrencyMode.Multiple)]
    internal class UgolkiServiceCallback : Service.IUgolkiServiceCallback
    {
        public UgolkiServiceCallback()
        {
        }

        public Dictionary<Move, double> FindMoves(int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first)
        {
            return new Dictionary<Move, double>();
        }

        public Dictionary<Move, double> FindMovesStart(Move[] startMoves, int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first)
        {
            return new Dictionary<Move, double>();
        }

        public int Test()
        {
            return 0;
        }

        public static UgolkiServiceCallback Callback { get; private set; } = new UgolkiServiceCallback();
    }
}