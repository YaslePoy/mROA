using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Example.Shared;
using mROA.Implementation;

namespace Example.Backend
{
    public class Printer : IPrinter
    {
        public string Name;
        public string GetName()
        {
            return Name;
        }

        public async Task<SharedObject<IPage>> Print(string text, CancellationToken cancellationToken = default)
        {
            // throw new Exception("The method or operation is not implemented.");
            return new Page {Text = text};
        }

        public void Dispose()
        {
            Console.WriteLine("Dispose printer with name {0}, Dispose works, Dispose works, Dispose works, Dispose works,", Name);
        }
    }
}