using System.Runtime.InteropServices;

namespace mROA.Example;

public class MainX
{
    public static void Main(string[] args)
    {
        // CoCodegenMethodRepository a = new CoCodegenMethodRepository();
        unsafe
        {
            
        }
    }

    public unsafe byte[] Convert<T>(T input)
    {
        var size = Marshal.SizeOf(typeof(T));
        var place = Marshal.AllocHGlobal(size);
        var pt = (T*)place;
        return [];
    }
}