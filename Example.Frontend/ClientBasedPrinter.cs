using System;
using System.Threading;
using System.Threading.Tasks;
using Example.Shared;
using mROA.Implementation;

namespace Example.Frontend
{
    public class ClientBasedPrinter : IPrinter
    {
        public double Resource { get; set; }

        public string GetName()
        {
            Console.WriteLine("ClientBasedPrinter called from server!!!!!!!!!!! Vova likes that:)");
            
            return "ClientBasedPrinter from mroa";
        }

        public async Task<IPage> Print(string text, bool some, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Printed: {text}");
            await Task.Yield();
            return new ClientBasedPage();
        }

        public event Action<IPage>? OnPrint;

        public void Dispose()
        {
            
        }
    }

    public class ClientBasedPage : IPage
    {
        public byte[] GetData()
        {
            return new byte[] { 1, 2, 3 };
        }
    }
}