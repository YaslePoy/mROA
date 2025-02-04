// using System.Text.Json;
// using mROA.Implementation;
//
// namespace mROA.Example;
//
// public class TestParameterRemoteEndpoint(int id, ISerialisationModule.IFrontendSerialisationModule serialisationModule) : ITestParameter, IRemoteObject{
//     public int Test()
//     {
//         var request = new JsonCallRequest { CommandId = 7, ObjectId = id };
//         serialisationModule.PostCallRequest(request);
//         var response = serialisationModule.GetNextCommandExecution<FinalCommandExecution>(request.CallRequestId)
//             .GetAwaiter().GetResult();
//         TransmissionConfig.SetupFrontendRepository();
//         var convertation = response.Result! is JsonElement e ? e.Deserialize<int>() : (int)response.Result!;
//         TransmissionConfig.SetupBackendRepository();
//         return convertation;
//     }
//
//     public int Id => id;
// }