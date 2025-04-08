using System.Text;
using Example.Shared;

namespace Example.Backend
{
    public class Page : IPage
    {
        public string Text;

        public byte[] GetData()
        {
            return Encoding.UTF8.GetBytes(Text);
        }
    }
}