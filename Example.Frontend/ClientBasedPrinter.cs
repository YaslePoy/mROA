using System;
using System.Threading;
using System.Threading.Tasks;
using Example.Shared;
using mROA.Implementation;

namespace Example.Frontend
{
    public class ClientBasedPrinter : IPrinter
    {
        public Task SomeoneIsApproaching(string humanName)
        {
            return Task.CompletedTask;
        }

        public Task SetFingerPrint(int[] fingerPrint)
        {
            return Task.CompletedTask;
        }

        public void OnPrintExternal(IPage p0, RequestContext ro)
        {
        }

        public double Resource { get; set; }

        public string GetName()
        {
            Console.WriteLine("ClientBasedPrinter called from server!!!!!!!!!!! Vova likes that:)");
            DemoCheck.BackwardCall = true;

            return "ClientBasedPrinter from mroa";
        }

        public async Task<IPage> Print(string text, bool some, RequestContext context,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Printed: {text}");
            await Task.Yield();
            return new ClientBasedPage();
        }

        public event Action<IPage, RequestContext>? OnPrint;

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