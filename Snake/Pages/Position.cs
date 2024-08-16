using System.Diagnostics.CodeAnalysis;

namespace Snake.Pages
{
    public struct Position
    {
        public static Position Up => new Position(0, -1);
        public static Position Down => new Position(0, 1);
        public static Position Left => new Position(-1, 0);
        public static Position Right => new Position(1, 0);

        public float x, y;

        public Position(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Position GetRandomPosition(int rows, int columns)
        {
            Random rand = new Random();
            return new Position(rand.Next(1, rows), rand.Next(1, columns));
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Position position && position.x == x && position.y == y;
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}