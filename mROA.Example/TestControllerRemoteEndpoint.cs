// using System.Text.Json;
// using mROA.Implementation;
//
// namespace mROA.Example;
//
// public class TestControllerRemoteEndpoint(int id, ISerialisationModule.IFrontendSerialisationModule serialisationModule)
//     : ITestController, IRemoteObject
// {
//     public void A()
//     {
//         serialisationModule.PostCallRequest(new JsonCallRequest { CommandId = 0, ObjectId = id });
//     }
//
//     public async Task AAsync(CancellationToken cancellationToken)
//     {
//         var request = new JsonCallRequest { CommandId = 1, ObjectId = id };
//         serialisationModule.PostCallRequest(request);
//         await serialisationModule.GetNextCommandExecution<FinalCommandExecution>(request.CallRequestId);
//     }
//
//     public int B()
//     {
//         TransmissionConfig.SetupBackendRepository();
//         var request = new JsonCallRequest { CommandId = 2, ObjectId = id };
//         serialisationModule.PostCallRequest(request);
//         var response = serialisationModule.GetNextCommandExecution<FinalCommandExecution>(request.CallRequestId)
//             .GetAwaiter().GetResult();
//         TransmissionConfig.SetupFrontendRepository();
//         var convertation = response.Result! is JsonElement e ? e.Deserialize<int>() : (int)response.Result!;
//         return convertation;
//     }
//
//     public async Task<int> BAsync(CancellationToken cancellationToken)
//     {
//         var request = new JsonCallRequest { CommandId = 3, ObjectId = id };
//         serialisationModule.PostCallRequest(request);
//         var response = await serialisationModule.GetFinalCommandExecution<int>(request.CallRequestId);
//         var convertation = response.Result! is JsonElement e ? e.Deserialize<int>() : (int)response.Result!;
//         return convertation;
//     }
//
//     public TransmittedSharedObject<ITestController> SharedObjectTransmitionTest()
//     {
//         TransmissionConfig.SetupBackendRepository();
//
//         var request = new JsonCallRequest { CommandId = 4, ObjectId = id };
//         serialisationModule.PostCallRequest(request);
//         var response = serialisationModule.GetNextCommandExecution<FinalCommandExecution>(request.CallRequestId)
//             .GetAwaiter().GetResult();
//         TransmissionConfig.SetupFrontendRepository();
//         var convertation = response.Result! is JsonElement e
//             ? e.Deserialize<TransmittedSharedObject<ITestController>>()!
//             : (TransmittedSharedObject<ITestController>)response.Result!;
//         return convertation;
//     }
//
//     public int Parametrized(TestParameter parameter)
//     {
//         var request = new JsonCallRequest { CommandId = 5, ObjectId = id, Parameter = parameter };
//         TransmissionConfig.SetupBackendRepository();
//
//         serialisationModule.PostCallRequest(request);
//         var response = serialisationModule.GetNextCommandExecution<FinalCommandExecution>(request.CallRequestId)
//             .GetAwaiter().GetResult();
//
//         TransmissionConfig.SetupFrontendRepository();
//         var convertation = response.Result! is JsonElement e ? e.Deserialize<int>() : (int)response.Result!;
//         return convertation;
//     }
//
//     public TransmittedSharedObject<ITestParameter> GetTestParameter()
//     {
//         TransmissionConfig.SetupBackendRepository();
//         var request = new JsonCallRequest { CommandId = 6, ObjectId = id };
//         serialisationModule.PostCallRequest(request);
//         var response = serialisationModule.GetNextCommandExecution<FinalCommandExecution>(request.CallRequestId)
//             .GetAwaiter().GetResult();
//         TransmissionConfig.SetupFrontendRepository();
//         var convertation = response.Result! is JsonElement e
//             ? e.Deserialize<TransmittedSharedObject<ITestParameter>>()
//             : (TransmittedSharedObject<ITestParameter>)response.Result!;
//         return convertation;
//     }
//
//     public int Id => id;
// }