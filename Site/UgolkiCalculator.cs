using Site.Classes;
using Site.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Web;
using System.Xml.Serialization;

namespace Site
{
    public class UgolkiCalculator
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

        public static Dictionary<Service.Move, double> GetMovesMsmq(int[,] cells, int player, int steps, Service.Vector2 playerPoint, int secondPlayer, bool first)
        {
            MessageQueue messageQueue = CreateOrGetMessageQueue("Ugolki");
            if (MessageQueue.Exists(@".\Private$\UgolkiResult")) MessageQueue.Delete(@".\Private$\UgolkiResult");
            MessageQueue messageResultQueue = CreateOrGetMessageQueue("UgolkiResult");
            messageQueue.Send(new BoardXml()
            {
                Cells = BoardXml.GetCells1d(cells),
                Moves = null,
                Player = player,
                First = first,
                Height = cells.GetLength(1),
                Width = cells.GetLength(0),
                PlayerX = playerPoint.X,
                PlayerY = playerPoint.Y,
                SecondPlayer = secondPlayer,
                Steps = steps
            });

            ((XmlMessageFormatter)messageResultQueue.Formatter).TargetTypes = new Type[] { typeof(MoveItem[]) };

            var message = messageResultQueue.Receive();
            Dictionary<Service.Move, double> moves = ((MoveItem[])message.Body).ToDictionary(x => new Service.Move() { Path = x.Move.Path.Select(y => new Service.Vector2() { X = y.X, Y = y.Y }).ToArray(), Player = x.Move.Player }, x => x.Score);
            return FilterMoves(moves, player);
        }

        public static Dictionary<Service.Move, double> GetMovesLocal(int[,] cells, int player, int steps, Service.Vector2 playerPoint, int secondPlayer, bool first)
        {
            Dictionary<Service.Move, double> moves = Board.FindMoves(cells, player, steps, new Classes.Vector2(playerPoint.X, playerPoint.Y), secondPlayer, first).ToDictionary(x => new Service.Move() { Path = x.Key.Path.Select(y => new Service.Vector2() { X = y.X, Y = y.Y }).ToArray(), Player = x.Key.Player }, x => x.Value);
            return FilterMoves(moves, player);
        }

        public static Dictionary<Service.Move, double> GetMovesWcf(int[,] gameCells, int player, int steps, Service.Vector2 playerPoint, int secondPlayer, bool first)
        {
            Service.UgolkiServiceClient client = new Service.UgolkiServiceClient(new System.ServiceModel.InstanceContext(UgolkiServiceCallback.Callback));
            int[][] cells = new int[gameCells.GetLength(0)][];
            for (int i = 0; i < gameCells.GetLength(0); i++)
            {
                cells[i] = new int[gameCells.GetLength(1)];
                for (int j = 0; j < gameCells.GetLength(1); j++)
                {
                    cells[i][j] = gameCells[i, j];
                }
            }
            var moves = client.GetMoves(cells, player, 4, Classes.Game.GetPlayerPoint(player), -player, true);
            return FilterMoves(moves, player);
        }

        public static Dictionary<Service.Move, double> FilterMoves(Dictionary<Service.Move, double> moves, int player)
        {
            int val = 5;
            if (moves.Count(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val) > 0)
            {
                moves = moves.Where(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val).ToDictionary(x => x.Key, x => x.Value);
                val = 7;
                if (moves.Count(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val) > 0)
                {
                    moves = moves.Where(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val).ToDictionary(x => x.Key, x => x.Value);
                    val = 9;
                    if (moves.Count(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val) > 0)
                    {
                        moves = moves.Where(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val).ToDictionary(x => x.Key, x => x.Value);
                        val = 11;
                        if (moves.Count(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val) > 0)
                        {
                            moves = moves.Where(x => ((-player) * (x.Key.Path.First().X - x.Key.Path.Last().X) + (-player) * (x.Key.Path.First().Y - x.Key.Path.Last().Y)) > val).ToDictionary(x => x.Key, x => x.Value);
                        }
                    }
                }
            }
            return moves;
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

        public static int[] GetCells1d(int[,] cells)
        {
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            int[] cells1d = new int[width * height];
            for (int i = 0, k = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    cells1d[k++] = cells[i, j];
                }
            }
            return cells1d;
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
    
}