using DouglasDwyer.Dozer;

namespace TestProject
{
    public class Baz
    {
        public bool Car;
    }

    [DozerIncludeFields(Accessibility.All, FieldMutability.All)]
    [DozerIncludeProperties(Accessibility.All, PropertyMutability.All)]
    public sealed class Cyclic
    {
        public string Foo { get; init; }
        public Cyclic?[] Next { get; init; }

        [DozerExclude]
        public int DoNotSerialize;

        private int PleaseSerialize = 23;

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

            var ppp = new Cyclic(28) { Foo = "pepee", Next = new Cyclic?[2], DoNotSerialize = 59 };
            //var ppp = new[] { new OneTwoOatmeal { Ex = 2, Hi = 42, Why = 'L' } };

            var ser = serializer.Serialize<Kirby>(Kirby.Oatmeal);
            var deser = serializer.Deserialize<Kirby>(ser);

            Console.WriteLine("Hello, World!");
        }
    }
}
