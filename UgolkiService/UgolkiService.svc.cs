using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace UgolkiService
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени класса "Service1" в коде, SVC-файле и файле конфигурации.
    // ПРИМЕЧАНИЕ. Чтобы запустить клиент проверки WCF для тестирования службы, выберите элементы Service1.svc или Service1.svc.cs в обозревателе решений и начните отладку.
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class UgolkiService : IUgolkiService
    {
        IUgolkiCallback Callback
        {
            get
            {
                return OperationContext.Current.GetCallbackChannel<IUgolkiCallback>();
            }
        }
        static List<IUgolkiCallback> clients = new List<IUgolkiCallback>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Connect()
        {
            if (!clients.Contains(Callback))
            {
                clients.Add(Callback);
            }
        }
        
        public Dictionary<Move, double> GetMoves(int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first)
        {
            Dictionary<Move, double> moves = new Dictionary<Move, double>();
            List<IUgolkiCallback> remove = new List<IUgolkiCallback>();
            foreach (var client in clients)
            {
                try
                {
                    if (client.Test() > 0)
                    {

                    }
                }
                catch (Exception ex)
                {
                    remove.Add(client);
                }
            }
            foreach (var item in remove)
            {
                clients.Remove(item);
            }
            if (clients.Count > 0)
            {

                try
                {
                    var startMoves = Board.FindMoves(To2D(cells), null, player);
                    List<List<Move>> movesList = new List<List<Move>>();

                    var count = startMoves.Count / clients.Count;
                    if (count > 0)
                    {
                        for (int i = 0; i + count < startMoves.Count; i += count)
                        {
                            movesList.Add(startMoves.GetRange(i, count));
                        }
                        if (startMoves.Count % clients.Count > 0)
                        {
                            var c = startMoves.Count % clients.Count;
                            for (int i = 0; i < c; i++)
                            {
                                movesList[i].Add(startMoves[startMoves.Count - 1 - i]);
                            }
                            //movesList.Add(startMoves.GetRange(startMoves.Count - 1 - startMoves.Count % clients.Count, startMoves.Count % clients.Count));
                        }
                    }
                    else
                    {
                        movesList = startMoves.Select(x => new List<Move>() { x }).ToList();
                    }
                    List<Task<Dictionary<Move, double>>> tasks = new List<Task<Dictionary<Move, double>>>();
                    for (int i = 0; i < movesList.Count; i++)
                    {
                        var index = i;
                        var task = Task.Factory.StartNew(() => { return clients[index].FindMovesStart(movesList[index], cells, player, steps - 1, playerPoint, secondPlayer, first); });
                        tasks.Add(task);
                    }
                    Task.WaitAll(tasks.ToArray());
                    foreach (var task in tasks)
                    {
                        foreach (var item in task.Result)
                        {
                            moves.Add(item.Key, item.Value);
                        };
                    }
                }
                catch (Exception ex)
                {
                    ex.GetHashCode();
                }
            }
            else
            {
                moves = Board.FindMoves(To2D(cells), player, steps, playerPoint, secondPlayer, first);
            }
            return moves;
        }

        public int GetClientsCount()
        {
            List<IUgolkiCallback> remove = new List<IUgolkiCallback>();
            object locko = new object();
            List<Task> tasks = //new List<Task<int>>();
                clients.Select(x => Task.Factory.StartNew(() => {
                    bool error = false;
                    try
                    {
                        if (!Task.Factory.StartNew(() => { return x.Test(); }).Wait(7000))
                        {
                            error = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = true;
                    }
                    if (error)
                    {
                        lock (locko)
                        {
                            remove.Add(x);
                        }
                    }
                })).ToList();
            Task.WaitAll(tasks.ToArray());
            foreach (var item in remove)
            {
                clients.Remove(item);
            }
            return clients.Count;
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
                        pieces.Add(new Vector2(i, j));
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
                    moves.Add(new Move(piece, new Vector2(piece.X, piece.Y + 1), player));
                }
                else if (0 <= piece.Y + 2 && piece.Y + 2 < height && cells[piece.X, piece.Y + 2] == 0 && cells[piece.X, piece.Y + 1] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X && v.Y == piece.Y + 2))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2(piece.X, piece.Y + 2));
                        moves.Add(new Move(newPath, player));
                        moves.AddRange(FindMoves(cells, newPath, player));
                    }
                }
                // Right
                if (0 <= piece.X + 1 && piece.X + 1 < width && cells[piece.X + 1, piece.Y] == 0 && existingPath.Count == 1)
                {
                    moves.Add(new Move(piece, new Vector2(piece.X + 1, piece.Y), player));
                }
                else if (0 <= piece.X + 2 && piece.X + 2 < width && cells[piece.X + 2, piece.Y] == 0 && cells[piece.X + 1, piece.Y] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X + 2 && v.Y == piece.Y))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2(piece.X + 2, piece.Y));
                        moves.Add(new Move(newPath, player));
                        moves.AddRange(FindMoves(cells, newPath, player));
                    }
                }
                // Bottom
                if (0 <= piece.Y - 1 && piece.Y - 1 < height && cells[piece.X, piece.Y - 1] == 0 && existingPath.Count == 1)
                {
                    moves.Add(new Move(piece, new Vector2(piece.X, piece.Y - 1), player));
                }
                else if (0 <= piece.Y - 2 && piece.Y - 2 < height && cells[piece.X, piece.Y - 2] == 0 && cells[piece.X, piece.Y - 1] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X && v.Y == piece.Y - 2))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2(piece.X, piece.Y - 2));
                        moves.Add(new Move(newPath, player));
                        moves.AddRange(FindMoves(cells, newPath, player));
                    }
                }
                // Left
                if (0 <= piece.X - 1 && piece.X - 1 < width && cells[piece.X - 1, piece.Y] == 0 && existingPath.Count == 1)
                {
                    moves.Add(new Move(piece, new Vector2(piece.X - 1, piece.Y), player));
                }
                else if (0 <= piece.X - 2 && piece.X - 2 < width && cells[piece.X - 2, piece.Y] == 0 && cells[piece.X - 1, piece.Y] != 0)
                {
                    if (!existingPath.Exists(v => v.X == piece.X - 2 && v.Y == piece.Y))
                    {
                        var newPath = new List<Vector2>(existingPath);
                        newPath.Add(new Vector2(piece.X - 2, piece.Y));
                        moves.Add(new Move(newPath, player));
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

            var tmp = cells[move.To.X, move.To.Y];
            cells[move.To.X, move.To.Y] = cells[move.From.X, move.From.Y];
            cells[move.From.X, move.From.Y] = tmp;
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
                        score += Vector2.GetDistance(point, i, j);
                    }
                    if (cells[i, j] == -playerColor)
                    {
                        score -= Vector2.GetDistance(point2, i, j);
                    }
                }
            }
            return score;
        }

        public static double GetScore(int[,] cells, int playerColor, Vector2 point, Move move)
        {
            double score = GetScore(cells, playerColor, point);
            score -= Vector2.GetDistance(point, move.From);
            score += Vector2.GetDistance(point, move.To);
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
}
