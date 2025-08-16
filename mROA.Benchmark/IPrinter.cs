using mROA.Abstract;
using mROA.Implementation;
using mROA.Implementation.Attributes;

namespace mROA.Benchmark
{
    [SharedObjectInterface]
    public partial interface IPrinter : IDisposable, IShared
    {
        double Resource { get; set; }
        string GetName();
        Task<IPage> Print(string text, bool someParameter, RequestContext context, CancellationToken cancellationToken);
        event Action<IPage, RequestContext> OnPrint;

        [Untrusted]
        Task SomeoneIsApproaching(string humanName);

        Task SetFingerPrint(int[] fingerPrint);
        Task IntTest(MyData data);
    }

    public class MyData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Score { get; set; }

        public override string ToString()
        {
            return $"{{{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Score)}: {Score}}}";
        }

        protected bool Equals(MyData other)
        {
            return Id == other.Id && Name == other.Name && Score.Equals(other.Score);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MyData)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Score);
        }
    }
}