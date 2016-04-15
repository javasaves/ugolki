using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Site.Service;

namespace Site.Classes
{
    public struct Vector2
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static double GetDistance(Vector2 a, Vector2 b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
            //return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        public static double GetDistance(Vector2 a, int x, int y)
        {
            return Math.Sqrt((a.X - x) * (a.X - x) + (a.Y - y) * (a.Y - y));
            //return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", X, Y);
        }
    }

    public class Game
    {
        public Board Board { get; private set; }

        public List<Player> Players { get; private set; }

        public Game()
        {
            Board = new Board();
            Players = new List<Player>() { new Player() { Color = 1, StartPosition = new Vector2(0, 0), EndPosition = new Vector2(Board.Width - 1, Board.Height - 1) },
                                           new Player() { Color = -1, StartPosition = new Vector2(Board.Width - 1, Board.Height - 1), EndPosition = new Vector2(0, 0) } };
        }

        public static void ApplyMove(int[,] cells, Service.Move move)
        {
            var to = move.Path.Last();
            var from = move.Path.First();
            var tmp = cells[to.X, to.Y];
            cells[to.X, to.Y] = cells[from.X, from.Y];
            cells[from.X, from.Y] = tmp;
        }

        public void AutoMove(int playerIndex)
        {
            var moves = GetGoodMoves(playerIndex);
            var distances = moves.Select(m => Board.GetDistance(Players[playerIndex].Color, Players[playerIndex].EndPosition, m)).ToArray();
            ApplyMove(moves.First(m => Board.GetDistance(Players[playerIndex].Color, Players[playerIndex].EndPosition, m) == distances.Min()));
        }

        public void ApplyMove(Move move)
        {
            Board.ApplyMove(move);
        }
        
        public List<Move> GetMoves(int playerIndex)
        {
            return Board.FindMoves(Players[playerIndex].Color);
        }

        public List<Move> GetGoodMoves(List<Move> moves, int playerIndex)
        {
            Player player = Players[playerIndex];
            double distance = Board.GetDistance(player.Color, player.EndPosition);
            return moves.Where(x => Board.GetDistance(player.Color, player.EndPosition, x) < distance).ToList();
        }

        public List<Move> GetGoodMoves(int playerIndex)
        {
            var moves = Board.FindMoves(Players[playerIndex].Color);
            Player player = Players[playerIndex];
            double distance = Board.GetDistance(player.Color, player.EndPosition);
            return moves.Where(x => Board.GetDistance(player.Color, player.EndPosition, x) < distance).ToList();
        }

        public static void ApplyGoodMove(Game game, int playerIndex, int secondPlayerIndex, int steps)
        {
            Move move = null;
            for (int i = 0; i <= steps && move == null; i++)
            {
                var moves = Board.FindMoves(game.Board.Cells,
                                             game.Players[playerIndex].Color,
                                             i,
                                             game.Players[playerIndex].EndPosition,
                                             game.Players[secondPlayerIndex].Color,
                                             true);
                if (moves.ContainsValue(double.MinValue) && i < steps)
                {
                    move = moves.First(m => m.Value == double.MinValue).Key;
                    game.ApplyMove(move);
                }
                else if (i == steps)
                {
                    try
                    {
                        double max0 = moves.Min(x => x.Value);
                        move = moves.First(m => m.Value == max0).Key;
                        game.ApplyMove(move);
                    }
                    catch (Exception) { }
                }
            }
            //var moves = moves0.Where(m => Math.Abs(m.Value - max0) <= eps).Select(x => x.Key).ToArray();
            //var move = moves[rand.Next(0, moves.Length - 1)];
            //game.ApplyMove(move);
        }

        internal static Service.Vector2 GetPlayerPoint(int player)
        {
            if (player == 1)
            {
                return new Service.Vector2() { X = 8 - 1, Y = 8 - 1 };
            }
            else
            {
                return new Service.Vector2() { X = 0, Y = 0 };
            }
        }
    }

    public class Player
    {
        public Vector2 StartPosition { get; set; }
        public Vector2 EndPosition { get; set; }
        public int Color { get; set; }
    }

    public class Move
    {
        public int Player { get; private set; }

        public List<Vector2> Path { get; private set; }

        public Vector2 From
        {
            get
            {
                return Path.First();
            }
        }
        public Vector2 To
        {
            get
            {
                return Path.Last();
            }
        }

        public Move(List<Vector2> path, int player)
        {
            Path = path;
            Player = player;
        }

        public Move(Vector2 from, Vector2 to, int player)
        {
            Path = new List<Vector2>() { from, to };
            Player = player;
        }

        public Move(int fromX, int fromY, int toX, int toY, int player)
        {
            Path = new List<Vector2>() { new Vector2(fromX, fromY), new Vector2(toX, toY) };
            Player = player;
        }
    }

    public enum Colors { White = 1, Black = -1 }

    public class Board
    {
        #region Static Methods
        /// <summary>
        /// Returns new field, which is obtained from the source after applying the move.
        /// </summary>
        /// <param name="source">Cells of the source board.</param>
        /// <param name="move">Move to apply.</param>
        /// <returns></returns>
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

        public static double GetScore(int[,] cells, int playerColor, Vector2 point)
        {
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

        private static void SetPieces(int[,] cells, bool reverse = false)
        {
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            int v = playerCellValues[reverse ? 1 : 0];

            for (int i = 0; i < PiecesSet.GetLength(0); i++)
            {
                if (!reverse)
                {
                    cells[PiecesSet[i, 0], PiecesSet[i, 1]] = v;
                }
                else
                {
                    cells[width - 1 - PiecesSet[i, 0], height - 1 - PiecesSet[i, 1]] = v;
                }
            }
        }

        public static int[,] StartCells()
        {
            int[,] cells = null;
            var width = 8;
            var height = 8;
            cells = new int[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    cells[i, j] = 0;
                }
            }
            SetPieces(cells);
            SetPieces(cells, true);
            return cells;
        }
        #endregion

        #region Fields
        private static int[] playerCellValues = new int[] { 1, -1, 2, -2 };

        private static int[,] PiecesSet = new int[,] { { 0, 0 }, { 0, 1 }, { 0, 2 },
                                                       { 1, 0 }, { 1, 1 }, { 1, 2 },
                                                       { 2, 0 }, { 2, 1 }, { 2, 2 } };
        private Dictionary<int, List<Vector2>> allPieces;
        #endregion

        #region Properties
        public int PiecesCount { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int[,] Cells { get; private set; }

        public List<Move> Moves { get; private set; }

        public double Distance { get; private set; }
        #endregion
        
        public double GetDistance(int player, Vector2 cell)
        {
            var pieces = allPieces[player];
            double distance = 0.0;
            foreach (var piece in pieces)
            {
                distance += Vector2.GetDistance(piece, cell);
            }
            return distance;
        }

        public double GetDistance(int player, Vector2 cell, Move move)
        {
            double distance = GetDistance(player, cell);
            distance -= Vector2.GetDistance(move.From, cell);
            distance += Vector2.GetDistance(move.To, cell);
            return distance;
        }

        public List<Move> FindMoves(List<Vector2> existingPath, int player)
        {
            var piece = existingPath.Last();
            List<Move> moves = new List<Move>();
            // Top
            if (0 < piece.Y + 1 && piece.Y + 1 < Height && Cells[piece.X, piece.Y + 1] == 0 && existingPath.Count == 1)
            {
                moves.Add(new Move(piece, new Vector2(piece.X, piece.Y + 1), player));
            }
            else if (0 < piece.Y + 2 && piece.Y + 2 < Height && Cells[piece.X, piece.Y + 2] == 0 && Cells[piece.X, piece.Y + 1] != 0)
            {
                if (!existingPath.Exists(v => v.X == piece.X && v.Y == piece.Y + 2))
                {
                    var newPath = new List<Vector2>(existingPath);
                    newPath.Add(new Vector2(piece.X, piece.Y + 2));
                    moves.Add(new Move(newPath, player));
                    moves.AddRange(FindMoves(newPath, player));
                }
            }
            // Right
            if (0 < piece.X + 1 && piece.X + 1 < Width && Cells[piece.X + 1, piece.Y] == 0 && existingPath.Count == 1)
            {
                moves.Add(new Move(piece, new Vector2(piece.X + 1, piece.Y), player));
            }
            else if (0 < piece.X + 2 && piece.X + 2 < Width && Cells[piece.X + 2, piece.Y] == 0 && Cells[piece.X + 1, piece.Y] != 0)
            {
                if (!existingPath.Exists(v => v.X == piece.X + 2 && v.Y == piece.Y))
                {
                    var newPath = new List<Vector2>(existingPath);
                    newPath.Add(new Vector2(piece.X + 2, piece.Y));
                    moves.Add(new Move(newPath, player));
                    moves.AddRange(FindMoves(newPath, player));
                }
            }
            // Bottom
            if (0 < piece.Y - 1 && piece.Y - 1 < Height && Cells[piece.X, piece.Y - 1] == 0 && existingPath.Count == 1)
            {
                moves.Add(new Move(piece, new Vector2(piece.X, piece.Y - 1), player));
            }
            else if (0 < piece.Y - 2 && piece.Y - 2 < Height && Cells[piece.X, piece.Y - 2] == 0 && Cells[piece.X, piece.Y - 1] != 0)
            {
                if (!existingPath.Exists(v => v.X == piece.X && v.Y == piece.Y - 2))
                {
                    var newPath = new List<Vector2>(existingPath);
                    newPath.Add(new Vector2(piece.X, piece.Y - 2));
                    moves.Add(new Move(newPath, player));
                    moves.AddRange(FindMoves(newPath, player));
                }
            }
            // Left
            if (0 < piece.X - 1 && piece.X - 1 < Width && Cells[piece.X - 1, piece.Y] == 0 && existingPath.Count == 1)
            {
                moves.Add(new Move(piece, new Vector2(piece.X - 1, piece.Y), player));
            }
            else if (0 < piece.X - 2 && piece.X - 2 < Width && Cells[piece.X - 2, piece.Y] == 0 && Cells[piece.X - 1, piece.Y] != 0)
            {
                if (!existingPath.Exists(v => v.X == piece.X - 2 && v.Y == piece.Y))
                {
                    var newPath = new List<Vector2>(existingPath);
                    newPath.Add(new Vector2(piece.X - 2, piece.Y));
                    moves.Add(new Move(newPath, player));
                    moves.AddRange(FindMoves(newPath, player));
                }
            }
            return moves;
        }

        private void FindPieces(int player)
        {
            List<Vector2> pieces = new List<Vector2>(PiecesCount);
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (Cells[i, j] == player)
                    {
                        pieces.Add(new Vector2(i, j));
                    }
                }
            }
            allPieces[player] = pieces;
        }

        public List<Vector2> GetPieces(int player)
        {
            return allPieces[player];
        }

        public bool CheckMove(Move move)
        {
            return true;
        }

        public void ApplyMove(Move move)
        {
            if (!CheckMove(move))
            {
                return;
            }
            var tmp = Cells[move.To.X, move.To.Y];
            Cells[move.To.X, move.To.Y] = Cells[move.From.X, move.From.Y];
            Cells[move.From.X, move.From.Y] = tmp;
            //var pieces = GetPieces(move.Player);
            //var from = pieces.First(v => v.X == move.From.X && v.Y == move.From.Y);
            //from.X = move.To.X;
            //from.Y = move.To.Y;
            Moves.Add(move);
        }

        public List<Move> FindMoves(int player)
        {
            List<Move> moves = new List<Move>();
            FindPieces(1);
            FindPieces(-1);
            List<Vector2> pieces = GetPieces(player);            
            foreach (var piece in pieces)
            {
                List<Vector2> path = new List<Vector2>() { piece };
                moves.AddRange(FindMoves(path, player));
            }
            List<Move> remove = new List<Move>();
            
            foreach (var move in moves)
            {
                if (moves.Exists(x => x.From.X == move.From.X &&
                                      x.From.Y == move.From.Y &&
                                      x.To.X == move.To.X &&
                                      x.To.Y == move.To.Y &&
                                      x != move))
                {
                    remove.Add(move);
                }
            }
            foreach (var item in remove)
            {
                moves.Remove(item);
            }
            return moves;
        }

        private void SetPieces(bool reverse = false)
        {
            int v = playerCellValues[reverse ? 1 : 0];

            for (int i = 0; i < PiecesSet.GetLength(0); i++)
            {
                if (!reverse)
                {
                    Cells[PiecesSet[i, 0], PiecesSet[i, 1]] = v;
                }
                else
                {
                    Cells[Width - 1 - PiecesSet[i, 0], Height - 1 - PiecesSet[i, 1]] = v;
                }
            }
        }

        private void ResetCells()
        {
            if (Cells == null)
            {
                Cells = new int[Width, Height];
            }
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    Cells[i, j] = 0;
                }
            }
            SetPieces();
            SetPieces(true);
        }

        public Board()
        {
            Moves = new List<Move>();

            Width = 8;
            Height = 8;
            PiecesCount = PiecesSet.Length;

            ResetCells();
            allPieces = new Dictionary<int, List<Vector2>>();
            FindPieces(1);
            FindPieces(-1);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    builder.Append(" ").Append((Cells[i, j] < 0 ? Cells[i, j] + 5 : Cells[i, j]).ToString());//.Append(" ");
                }
                builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }
    }
}
