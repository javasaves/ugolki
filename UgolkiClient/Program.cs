using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UgolkiClient.Service;

namespace UgolkiClient
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Callback : IUgolkiServiceCallback
    {
        public Dictionary<Move, double> FindMoves(int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first)
        {
            Console.WriteLine(player);
            return new Dictionary<Move, double>();
        }

        public Dictionary<Move, double> FindMovesStart(Move[] startMoves, int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first)
        {
            return Board.FindMoves(startMoves.ToList(),To2D(cells), player, steps, playerPoint, secondPlayer, first);
        }

        static T[,] To2D<T>(T[][] source)
        {
            try
            {
                int firstDim = source.Length;
                int secondDim = source.GroupBy(row => row.Length).Single().Key; // throws InvalidOperationException if source is not rectangular

                var result = new T[firstDim, secondDim];
                for (int i = 0; i < firstDim; ++i)
                    for (int j = 0; j < secondDim; ++j)
                        result[i, j] = source[i][j];

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array is not rectangular.");
            }
        }

        public int Test()
        {
            return 1;
        }
    }

    class Program
    {
        static Callback callback = new Callback();

        static object lockObj = new object();

        static void Main(string[] args)
        {
            var inst = new System.ServiceModel.InstanceContext(callback);
            UgolkiServiceClient client = new UgolkiServiceClient(inst);
            client.Connect();
            Task.Factory.StartNew(() => { while (true) { Task.Delay(10000).Wait(); lock(lockObj) { client.Connect(); } } });
            Console.WriteLine("Connected. Count:");
            Console.WriteLine(client.GetClientsCount());
            string str = "";
            int[][] jaggedArray2 = new int[][]
            {
                new int[] {1,3,5,7,9},
                new int[] {0,2,4,6},
                new int[] {11,22}
            };
            do
            {
                str = Console.ReadLine();
                if (str == "")
                {
                    client.GetMoves(jaggedArray2, 123, 0, new Vector2(), 0, true);
                }
            } while (str != "exit");
            Console.ReadKey();
        }
    }

    public class Board
    {
        private static List<Vector2> FindPlayerPieces(int[,] cells, int player)
        {
            List<Vector2> pieces = new List<Vector2>();
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (cells[i, j] == player)
                    {
                        pieces.Add(new Vector2() { X = i, Y = j });
                    }
                }
            }
            return pieces;
        }

        public static List<Move> FindMoves(int[,] cells, List<Vector2> existingPath, int player)
        {
            List<Move> moves = new List<Move>();
            if (existingPath == null || existingPath.Count == 0)
            {
                existingPath = new List<Vector2>();
                List<Vector2> pieces = FindPlayerPieces(cells, player);
                foreach (var piece in pieces)
                {
                    List<Vector2> path = new List<Vector2>() { piece };
                    moves.AddRange(FindMoves(cells, path, player));
                }
            }
            else
            {
                int width = cells.GetLength(0);
                int height = cells.GetLength(1);
                var piece = existingPath.Last();
                // Top
                if (0 <= piece.Y + 1 && piece.Y + 1 < height && cells[piece.X, piece.Y + 1] == 0 && existingPath.Count == 1)
                {
                    moves.Add(new Move() { Path = new[] { piece, new Vector2() { X = piece.X, Y = piece.Y + 1 } }, Player = player });
                }
                else if (0 <= piece.Y + 2 && piece.Y + 2 < height && cells[piece.X, piece.Y + 2] == 0 && cells[piece.X, piece.Y + 1] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X && v.Y == piece.Y + 2))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2() { X = piece.X, Y = piece.Y + 2 });
                        moves.Add(new Move() { Path = newPath.ToArray(), Player = player });
                        moves.AddRange(FindMoves(cells, newPath, player));
                    }
                }
                // Right
                if (0 <= piece.X + 1 && piece.X + 1 < width && cells[piece.X + 1, piece.Y] == 0 && existingPath.Count == 1)
                {
                    moves.Add(new Move() { Path = new[] { piece, new Vector2() { X = piece.X + 1, Y = piece.Y } }, Player = player });
                }
                else if (0 <= piece.X + 2 && piece.X + 2 < width && cells[piece.X + 2, piece.Y] == 0 && cells[piece.X + 1, piece.Y] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X + 2 && v.Y == piece.Y))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2() { X = piece.X + 2, Y = piece.Y });
                        moves.Add(new Move() { Path = newPath.ToArray(), Player = player });
                        moves.AddRange(FindMoves(cells, newPath, player));
                    }
                }
                // Bottom
                if (0 <= piece.Y - 1 && piece.Y - 1 < height && cells[piece.X, piece.Y - 1] == 0 && existingPath.Count == 1)
                {
                    moves.Add(new Move() { Path = new[] { piece, new Vector2() { X = piece.X, Y = piece.Y - 1 } }, Player = player });
                }
                else if (0 <= piece.Y - 2 && piece.Y - 2 < height && cells[piece.X, piece.Y - 2] == 0 && cells[piece.X, piece.Y - 1] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X && v.Y == piece.Y - 2))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2() { X = piece.X, Y = piece.Y - 2 });
                        moves.Add(new Move() { Path = newPath.ToArray(), Player = player });
                        moves.AddRange(FindMoves(cells, newPath, player));
                    }
                }
                // Left
                if (0 <= piece.X - 1 && piece.X - 1 < width && cells[piece.X - 1, piece.Y] == 0 && existingPath.Count == 1)
                {
                    moves.Add(new Move() { Path = new[] { piece, new Vector2() { X = piece.X - 1, Y = piece.Y } }, Player = player });
                }
                else if (0 <= piece.X - 2 && piece.X - 2 < width && cells[piece.X - 2, piece.Y] == 0 && cells[piece.X - 1, piece.Y] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X - 2 && v.Y == piece.Y))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2() { X = piece.X - 2, Y = piece.Y });
                        moves.Add(new Move() { Path = newPath.ToArray(), Player = player });
                        moves.AddRange(FindMoves(cells, newPath, player));
                    }
                }
            }
            return moves;
        }

        public static Dictionary<Move, double> FindMoves(int[,] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first)
        {
            List<Move> moves = FindMoves(cells, null, first ? player : secondPlayer);
            if (steps == 0)
            {
                return moves.ToDictionary(x => x, x => GetScore(GetCells(cells, x), player, playerPoint));
            }
            else
            {
                var checkWin = moves.ToDictionary(x => x, x => GetScore(GetCells(cells, x), player, playerPoint));
                if (checkWin.Count(x => x.Value == double.MinValue) > 0)
                {
                    return checkWin;
                }
                var avg = 2.0 * checkWin.Average(x => x.Value) - checkWin.Min(x => x.Value);
                moves = checkWin.Where(x => x.Value < avg).Select(x => x.Key).ToList();
                Dictionary<Move, double> result = new Dictionary<Move, double>(moves.Count);
                foreach (Move move in moves)
                {
                    int[,] newCells = GetCells(cells, move);
                    var newMoves = FindMoves(newCells, player, steps - 1, playerPoint, secondPlayer, first);
                    if (newMoves.Count != 0)
                    {
                        result.Add(move, newMoves.Min(x => x.Value));
                    }
                }
                return result;
            }
        }

        public static Dictionary<Move, double> FindMoves(List<Move> startMoves, int[,] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first)
        {
            List<Move> moves = startMoves;//FindMoves(cells, null, first ? player : secondPlayer);
            if (steps == 0)
            {
                return moves.ToDictionary(x => x, x => GetScore(GetCells(cells, x), player, playerPoint));
            }
            else
            {
                var checkWin = moves.ToDictionary(x => x, x => GetScore(GetCells(cells, x), player, playerPoint));
                if (checkWin.Count(x => x.Value == double.MinValue) > 0)
                {
                    return checkWin;
                }
                var avg = 2.0 * checkWin.Average(x => x.Value) - checkWin.Min(x => x.Value);
                moves = checkWin.Where(x => x.Value < avg).Select(x => x.Key).ToList();
                Dictionary<Move, double> result = new Dictionary<Move, double>(moves.Count);
                foreach (Move move in moves)
                {
                    int[,] newCells = GetCells(cells, move);
                    var newMoves = FindMoves(newCells, player, steps - 1, playerPoint, secondPlayer, first);
                    if (newMoves.Count != 0)
                    {
                        result.Add(move, newMoves.Min(x => x.Value));
                    }
                }
                return result;
            }
        }

        public static int[,] GetCells(int[,] source, Move move)
        {
            int width = source.GetLength(0);
            int height = source.GetLength(1);
            int[,] cells = new int[width, height];
            Array.Copy(source, cells, source.Length);

            var to = move.Path.Last();
            var from = move.Path.First();
            var tmp = cells[to.X, to.Y];
            cells[to.X, to.Y] = cells[from.X, from.Y];
            cells[from.X, from.Y] = tmp;
            return cells;
        }


        public static double GetScore(int[,] cells, int playerColor, Vector2 point)
        {
            Vector2 point2 = new Vector2() { X = point.X == 0 ? 7 : 0, Y = point.X == 0 ? 7 : 0 };
            double score = 0.0;
            if (CheckWin(cells, playerColor))
            {
                return double.MinValue;
            }
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (cells[i, j] == playerColor)
                    {
                        score += point.GetDistance(point, i, j);
                    }
                    //if (cells[i, j] == -playerColor)
                    //{
                    //    score -= point.GetDistance(point2, i, j) * 0.5;
                    //}
                }
            }
            score += FindMoves(cells, null, -playerColor).Max(x => x.Path.Length) * score * 0.2;
            return score;
        }

        public static double GetScore(int[,] cells, int playerColor, Vector2 point, Move move)
        {
            double score = GetScore(cells, playerColor, point);
            score -= point.GetDistance(point, move.Path.First());
            score += point.GetDistance(point, move.Path.Last());
            return score;
        }

        public static bool CheckWin(int[,] cells, int player)
        {
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            if (player == playerCellValues[0])
            {
                for (int i = 0; i < PiecesSet.GetLength(0); i++)
                {
                    if (cells[width - 1 - PiecesSet[i, 0], height - 1 - PiecesSet[i, 1]] != player)
                    {
                        return false;
                    }
                }
                return true;
            }
            if (player == playerCellValues[1])
            {
                for (int i = 0; i < PiecesSet.GetLength(0); i++)
                {
                    if (cells[PiecesSet[i, 0], PiecesSet[i, 1]] != player)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }


        #region Fields
        private static int[] playerCellValues = new int[] { 1, -1, 2, -2 };

        private static int[,] PiecesSet = new int[,] { { 0, 0 }, { 0, 1 }, { 0, 2 },
                                                       { 1, 0 }, { 1, 1 }, { 1, 2 },
                                                       { 2, 0 }, { 2, 1 }, { 2, 2 } };
        private Dictionary<int, List<Vector2>> allPieces;
        #endregion

    }

    public static class ExtensionVector
    {
        public static double GetDistance(this Vector2 vector, Vector2 a, Vector2 b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        public static double GetDistance(this Vector2 vector, Vector2 a, int x, int y)
        {
            return Math.Sqrt((a.X - x) * (a.X - x) + (a.Y - y) * (a.Y - y));
        }
    }
}
