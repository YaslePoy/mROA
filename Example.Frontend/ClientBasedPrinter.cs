﻿using Example.Shared;
using mROA.Implementation;

namespace Example.Frontend;

public class ClientBasedPrinter : IPrinter
{
    public string GetName()
    {
        Console.WriteLine("ClientBasedPrinter called from server!!!!!!!!!!! Vova likes that:)");
        return "ClientBasedPrinter from mroa";
    }

    public async Task<SharedObject<IPage>> Print(string text, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Printed: {text}");
        await Task.Yield();
        return new ClientBasedPage();
    }
}

public class ClientBasedPage : IPage
{
    public byte[] GetData()
    {
        return [1, 2, 3];
    }
}