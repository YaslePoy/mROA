using System;
using System.Threading;
using System.Threading.Tasks;
using Example.Shared;
using mROA.Implementation;

namespace Example.Backend
{
    public class Printer : IPrinter
    {
        public string Name;

        public void OnPrintExternal(IPage p0)
        {
        }

        public double Resource { get; set; } = 100d;

        public string GetName()
        {
            return Name;
        }

        public async Task<IPage> Print(string text, bool some, RequestContext context,
            CancellationToken cancellationToken = default)
        {
            // throw new Exception("The method or operation is not implemented.");
            var page = new Page { Text = text };
            Console.WriteLine($"Request id : :{context.RequestId}");
            OnPrint?.Invoke(page);
            Resource /= 1.5;
            return page;
        }

        public event Action<IPage>? OnPrint;

        public void Dispose()
        {
            Console.WriteLine(
                "Dispose printer with name {0}, Dispose works, Dispose works, Dispose works, Dispose works,", Name);
        }
    }
}