// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using mROA.Benchmark;

Console.WriteLine("Hello, Performance!");

BenchmarkRunner.Run<MethodAccess>();