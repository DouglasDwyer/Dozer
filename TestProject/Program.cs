using DouglasDwyer.Dozer;

namespace TestProject
{
    public class Baz
    {
        public bool Car;
    }

    [DozerConfig]
    public sealed class Cyclic
    {
        public string Foo { get; set; }
        public Cyclic?[] Next { get; set; }

        [DozerExclude]
        public int DoNotSerialize;

        [DozerInclude]
        private readonly int PleaseSerialize = 23;

        public Cyclic() { }

        public Cyclic(int k) { PleaseSerialize = k; }
    }

    public struct OneTwoOatmeal
    {
        public byte Ex;
        public byte Hi;
        public char Why;
    }

    public enum Kirby
    {
        One,
        Two,
        Oatmeal
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var options = new DozerSerializerOptions();
            options.KnownAssemblies.Add(typeof(Program).Assembly);

            var serializer = new DozerSerializer(options);

            //var ppp = new Cyclic { Foo = "pepee", Next = new Cyclic?[2] };
            var ppp = new[] { new OneTwoOatmeal { Ex = 2, Hi = 42, Why = 'L' } };

            var ser = serializer.Serialize<OneTwoOatmeal[]>(ppp);
            var deser = serializer.Deserialize<OneTwoOatmeal[]>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
