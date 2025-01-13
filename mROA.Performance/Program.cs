using System.Diagnostics;
using System.Reflection;
using mROA.Implementation;
using mROA.Test;

namespace mROA.Performance;

class Program
{
    private static ProgramlyInteractionChanel _interactionModule;
    private static ISerialisationModule _serialisationModule;
    private static IExecuteModule _executeModule;
    private static IMethodRepository _methodRepository;
    private static IContextRepository _contextRepository;

    static void Main(string[] args)
    {
        _interactionModule = new ProgramlyInteractionChanel();
        _serialisationModule = new JsonSerialisationModule(_interactionModule);
        var repo = new MethodRepository();
        repo.CollectForAssembly(Assembly.GetAssembly(typeof(ITestController)));
        _methodRepository = repo;
        var repo2 = new ContextRepository();
        repo2.FillSingletons(Assembly.GetAssembly(typeof(ITestController)));
        _contextRepository = repo2;

        _executeModule = new PrepairedExecutionModule(_methodRepository, _serialisationModule, _contextRepository);
        var sw = Stopwatch.StartNew();

        _interactionModule.PassCommand(132, """
                                            {
                                              "RequestTypeId": 0,
                                              "CommandId": 3
                                            }
                                            """);
        while (_interactionModule.OutputBuffer.Count != 2) ;


        // _interactionModule.PassCommand(132, """
        //                                     {
        //                                       "RequestTypeId": 0,
        //                                       "CommandId": 3
        //                                     }
        //                                     """);
        // while (_interactionModule.OutputBuffer.Count() != 4) ;

        var time = sw.Elapsed;
        Console.WriteLine(time.TotalMilliseconds.ToString());
    }
}