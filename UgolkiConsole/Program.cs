using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Site.Classes;

namespace Site
{
    class Program
    {
        static void Main(string[] args)
        {
            var game = new Game();

            var rand = new Random();
            List<double> avgList = new List<double>();
            double[] distances;
            double eps = 0;//0.000000000005;
            for (int i = 0; i < 80; i++)
            {
                //var moves = game.GetGoodMoves(0);
                //var tmp = game.Board.GetDistance(game.Players[0].Color, game.Players[0].EndPosition);
                //distances = moves.Select(m => game.Board.GetDistance(game.Players[0].Color, game.Players[0].EndPosition, m)).ToArray();

                //var avg = distances.Average();
                //avgList.Add(avg);

                // game.ApplyMove(moves.First(m => game.Board.GetDistance(game.Players[0].Color, game.Players[0].EndPosition, m) == distances.Min()));//(moves[rand.Next(0, moves.Count - 1)]);

                //game.AutoMove(0);
                int steps = 3;

                if (!Board.CheckWin(game.Board.Cells, game.Players[0].Color))
                {
                    //var moves0 = Board.FindMoves(game.Board.Cells, game.Players[0].Color, steps, game.Players[0].EndPosition, game.Players[1].Color, true);
                    //double max0 = moves0.Min(x => x.Value);
                    //var moves = moves0.Where(m => Math.Abs(m.Value - max0) <= eps).Select(x => x.Key).ToArray();
                    //var move = moves[rand.Next(0, moves.Length - 1)];
                    //game.ApplyMove(move);
                    Game.ApplyGoodMove(game, 0, 1, steps);

                    Console.Clear();
                    Console.WriteLine("============ " + i.ToString("D2") + " ============");
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("============ " + i.ToString("D2") + " ============ WIN 1");
                }
                Console.WriteLine(game.Board.ToString());
                Console.ReadKey();

                //game.AutoMove(1);
                if (!Board.CheckWin(game.Board.Cells, game.Players[1].Color))
                {
                    //var moves1 = Board.FindMoves(game.Board.Cells, game.Players[1].Color, steps, game.Players[1].EndPosition, game.Players[0].Color, true);
                    //double max1 = moves1.Min(x => x.Value);
                    //var moves = moves1.Where(m => Math.Abs(m.Value - max1) <= eps).Select(x => x.Key).ToArray();
                    //var move = moves[rand.Next(0, moves.Length - 1)];
                    //game.ApplyMove(move);

                    Game.ApplyGoodMove(game, 1, 0, steps);

                    Console.Clear();
                    Console.WriteLine("============ " + i.ToString("D2") + " ============");
                }
                else
                {

                    Console.Clear();
                    Console.WriteLine("============ " + i.ToString("D2") + " ============ WIN 2");
                }
                Console.WriteLine(game.Board.ToString());
                Console.ReadKey();
            }

            
        }
    }
}
