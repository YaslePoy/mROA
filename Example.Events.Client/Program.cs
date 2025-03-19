using System.Diagnostics;
using System.Net;
using Example.Events.Shared;
using mROA.Cbor;
using mROA.Codegen;
using mROA.Implementation;
using mROA.Implementation.Backend;
using mROA.Implementation.Bootstrap;
using mROA.Implementation.Frontend;

namespace Example.Events.Client;

class Program
{
    static void Main(string[] args)
    {
        var builder = new FullMixBuilder();
        new RemoteTypeBinder();
        // builder.Modules.Add(new JsonSerializationToolkit());
        builder.Modules.Add(new CborSerializationToolkit());

        builder.Modules.Add(new RemoteContextRepository());
        builder.Modules.Add(new NextGenerationInteractionModule());
        builder.Modules.Add(new RepresentationModule());
        builder.Modules.Add(new NetworkFrontendBridge(new IPEndPoint(IPAddress.Parse("95.105.78.72"), 6000)));
        builder.Modules.Add(new StaticRepresentationModuleProducer());
        builder.Modules.Add(new RequestExtractor());
        builder.Modules.Add(new BasicExecutionModule());
        builder.Modules.Add(new CoCodegenMethodRepository());
        builder.UseCollectableContextRepository();
        builder.Modules.Add(new CancellationRepository());

        builder.Build();


        TransmissionConfig.RealContextRepository = builder.GetModule<ContextRepository>();
        TransmissionConfig.RemoteEndpointContextRepository = builder.GetModule<RemoteContextRepository>();

        builder.GetModule<NetworkFrontendBridge>()!.Connect();
        _ = builder.GetModule<RequestExtractor>()!.StartExtraction();

        var chatFactory =
            TransmissionConfig.RemoteEndpointContextRepository.GetSingleObject(typeof(IChatFactory), 0) as IChatFactory;
        var chat = chatFactory.GetChat(Guid.Empty);
        chat.OnCharPosted += (s, context) => { Console.Write(s); };
        while (true)
        {
            var input = Console.ReadKey(true);
            if (input.Key == ConsoleKey.Escape)
                return;

            var symb = "";

            if (input.Key == ConsoleKey.Backspace)
            {
                symb = "\b \b";
            }
            else
                symb = input.KeyChar.ToString();
#if TRACE
            Console.Write(symb);
            var sw = Stopwatch.StartNew();
            chat.PostSymbol(symb);
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds}ms");
#else
            Console.Write(symb);
            chat.PostSymbol(symb);
#endif
        }
    }
}