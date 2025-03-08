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

        public decimal Resource { get; set; }

        public string GetName()
        {
            return Name;
        }

        public async Task<IPage> Print(string text, bool some, CancellationToken cancellationToken = default)
        {
            // throw new Exception("The method or operation is not implemented.");
            var page = new Page { Text = text };
            OnPrint?.Invoke(page);
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