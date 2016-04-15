using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace UgolkiService
{
    public interface IUgolkiCallback
    {
        [OperationContract]
        Dictionary<Move, double> FindMoves(int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first);

        [OperationContract]
        Dictionary<Move, double> FindMovesStart(List<Move> startMoves, int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first);

        [OperationContract]
        int Test();
    }

    [ServiceContract(Namespace = "http://Microsoft.ServiceModel.Samples", SessionMode = SessionMode.Required,
                 CallbackContract = typeof(IUgolkiCallback))]
    public interface IUgolkiService
    {

        [OperationContract]
        int GetClientsCount();

        [OperationContract]
        Dictionary<Move, double> GetMoves(int[][] cells, int player, int steps, Vector2 playerPoint, int secondPlayer, bool first);

        [OperationContract]
        void Connect();
    }


    // Используйте контракт данных, как показано в примере ниже, чтобы добавить составные типы к операциям служб.
    [DataContract]
    public class Move
    {
        [DataMember]
        public int Player { get; private set; }

        [DataMember]
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

    [DataContract]
    public struct Vector2
    {
        [DataMember]
        public int X { get; set; }

        [DataMember]
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
}
