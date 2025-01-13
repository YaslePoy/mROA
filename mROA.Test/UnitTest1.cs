﻿using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using mROA.Implementation;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace mROA.Test;

public class Tests
{
    private ProgramlyInteractionChanel _interactionModule;
    private ISerialisationModule _serialisationModule;
    private IExecuteModule _executeModule;
    private IMethodRepository _methodRepository;
    private IContextRepository _contextRepository;

    private ITestController _testController;

    [SetUp]
    public void Setup()
    {
        _interactionModule = new ProgramlyInteractionChanel();
        _serialisationModule = new JsonSerialisationModule(_interactionModule);
        var repo = new MethodRepository();
        repo.CollectForAssembly(Assembly.GetExecutingAssembly());
        _methodRepository = repo;
        var repo2 = new ContextRepository();
        repo2.FillSingletons(Assembly.GetExecutingAssembly());
        _contextRepository = repo2;

        _executeModule = new PrepairedExecutionModule(_methodRepository, _serialisationModule, _contextRepository);
        TransmissionConfig.DefaultContextRepository = _contextRepository;
    }

    [Test]
    public void CommandPipelineTest()
    {
        var sw = Stopwatch.StartNew();

        _interactionModule.PassCommand(132, """
                                            {
                                              "RequestTypeId": 0,
                                              "CommandId": 2
                                            }
                                            """);
        Assert.Pass(_interactionModule.OutputBuffer.Last());
    }

    [Test]
    public void CommandPipelineTestAsync()
    {
        var sw = Stopwatch.StartNew();
        _interactionModule.PassCommand(132, """
                                            {
                                              "RequestTypeId": 0,
                                              "CommandId": 3
                                            }
                                            """);
        while (_interactionModule.OutputBuffer.Count != 2) ;

        Assert.Pass(_interactionModule.OutputBuffer.Last());
    }

    [Test]
    public void MethodRegistrationTest()
    {
        var repo = new MethodRepository();
        repo.CollectForAssembly(Assembly.GetExecutingAssembly());
        Assert.That(repo.GetMethods().ToList().Count == 5);
    }

    [Test]
    public void ContextSupplyTest()
    {
        var repo = new ContextRepository();
        repo.FillSingletons(Assembly.GetExecutingAssembly());
        var singleObject = repo.GetSingleObject(typeof(ITestController)) as ITestController;
        singleObject.B();
        Assert.That(singleObject.B() == 6);
    }

    [Test]
    public void TransmissionTest()
    {
        _interactionModule.PassCommand(132, """
                                            {
                                              "CommandId": 4
                                            }
                                            """);
        var response =
            JsonSerializer.Deserialize<TransmittedSharedObject<IContextRepository>>(
                JsonSerializer.Deserialize<FinalCommandExecution>(_interactionModule.OutputBuffer.Last())
                    ?.Result.ToString()
            );

        _interactionModule.PassCommand(132,
            JsonSerializer.Serialize(new JsonCallRequest { CommandId = 2, ObjectId = response.ContextId }));

        var firstFull = JsonDocument.Parse(_interactionModule.OutputBuffer.Last()).RootElement.GetProperty("Result")
            .GetInt32();
        
        _interactionModule.PassCommand(132,
            JsonSerializer.Serialize(new JsonCallRequest { CommandId = 4, ObjectId = response.ContextId }));

        response =
            JsonSerializer.Deserialize<TransmittedSharedObject<IContextRepository>>(
                JsonSerializer.Deserialize<FinalCommandExecution>(_interactionModule.OutputBuffer.Last())
                    ?.Result.ToString()
            );
        
        _interactionModule.PassCommand(132,
            JsonSerializer.Serialize(new JsonCallRequest { CommandId = 2, ObjectId = response.ContextId }));
        _interactionModule.PassCommand(132,
            JsonSerializer.Serialize(new JsonCallRequest { CommandId = 2, ObjectId = response.ContextId }));
        
        var secondFull = JsonDocument.Parse(_interactionModule.OutputBuffer.Last()).RootElement.GetProperty("Result")
            .GetInt32();
        
        Assert.That(firstFull == 456789 && secondFull == 6);

    }
}