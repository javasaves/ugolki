using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Xml.Serialization;

namespace MessageClient
{

    class Program
    {
        static MessageQueue CreateOrGetMessageQueue(string name)
        {
            string path = @".\Private$\" + name;
            if (!MessageQueue.Exists(path))
            {
                return MessageQueue.Create(path);
            }
            else
            {
                return new MessageQueue(path);
            }
        }

        static void Main(string[] args)
        {


            MessageQueue requestQueue = CreateOrGetMessageQueue("UgolkiRequest");
            MessageQueue responseQueue = CreateOrGetMessageQueue("UgolkiResponse");
            ((XmlMessageFormatter)requestQueue.Formatter).TargetTypes = new Type[] { typeof(BoardXml) };
            ((XmlMessageFormatter)responseQueue.Formatter).TargetTypes = new Type[] { typeof(MoveItem[]) };
            while (true)
            {
                Console.WriteLine("Ready");
                var requestMessage = requestQueue.Receive();
                Console.WriteLine("Busy");
                BoardXml board = (BoardXml)requestMessage.Body;
                var startMoves = board.Moves.Select(x => new Move(x.Path.Select(y => new Vector2(y.X, y.Y)).ToList(), x.Player)).ToList();
                var cells = board.GetCells2d();
                var player = board.Player;
                var steps = board.Steps;
                var playerPoint = new Vector2(board.PlayerX, board.PlayerY);
                var secondPlayer = board.SecondPlayer;
                var first = board.First;
                Dictionary<Move, double> moves = Board.FindMoves(startMoves, cells, player, steps, playerPoint, secondPlayer, first);
                var obj = moves.Select(x => new MoveItem() { Move = new MoveXml() { Path = x.Key.Path.Select(y => new Vector2Xml() { X = y.X, Y = y.Y }).ToArray(), Player = x.Key.Player }, Score = x.Value }).ToArray();
                responseQueue.Send(obj, requestMessage.Label);
            }








            //    MessageQueue messageQueue = new MessageQueue(@".\Private$\Ugolki");
            //((XmlMessageFormatter)messageQueue.Formatter).TargetTypes = new Type[] { typeof(BoardXml) };
            //Message[] messages = messageQueue.GetAllMessages();
            
            //foreach (Message message in messages)
            //{
            //    //message.Formatter = new XmlMessageFormatter(new string[] { "BoardXml" });
            //    //XmlSerializer serializer = new XmlSerializer(typeof(BoardXml));
            //    BoardXml board = (BoardXml)message.Body;
            //    //using (TextReader reader = new StringReader(message.Body.ToString()))
            //    //{
            //    //    board = (BoardXml)serializer.Deserialize(reader);
            //    //}

            //    //Board board = message
            //    Console.WriteLine(message.Body.ToString());
            //}
            //// after all processing, delete all the messages
            //Console.ReadKey();
            ////messageQueue.
            //messageQueue.Purge();
        }
    }

    [XmlRoot("MoveItem")]
    public class MoveItem
    {
        [XmlElement("Move")]
        public MoveXml Move { get; set; }
        [XmlElement("Score")]
        public double Score { get; set; }
    }

    //int player, int steps, Vector2 playerPoint, int secondPlayer, bool first
    [XmlRoot("Board")]
    public class BoardXml
    {
        [XmlElement("Moves")]
        public MoveXml[] Moves { get; set; }

        [XmlElement("Cells")]
        public int[] Cells { get; set; }
        [XmlElement("Width")]
        public int Width { get; set; }
        [XmlElement("Height")]
        public int Height { get; set; }
        [XmlElement("Player")]
        public int Player { get; set; }
        [XmlElement("Steps")]
        public int Steps { get; set; }
        [XmlElement("PlayerX")]
        public int PlayerX { get; set; }
        [XmlElement("PlayerY")]
        public int PlayerY { get; set; }
        [XmlElement("SecondPlayer")]
        public int SecondPlayer { get; set; }
        [XmlElement("First")]
        public bool First { get; set; }

        public int[,] GetCells2d()
        {
            int[,] cells = new int[Width, Height];
            for (int i = 0, k = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    cells[i, j] = Cells[k++];
                }
            }
            return cells;
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

    [XmlRoot("Move")]
    public class MoveXml
    {
        public int Player { get; set; }

        public Vector2Xml[] Path { get; set; }
    }

    [XmlRoot("Vector2")]
    public class Vector2Xml
    {
        [XmlElement("X")]
        public int X { get; set; }
        [XmlElement("Y")]
        public int Y { get; set; }
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
}
