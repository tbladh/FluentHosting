using System.IO;
using System.Text;

namespace FluentHosting.Contracts
{
    public interface IHandlerResponse
    {
        Stream Stream { get; set; }
        string ContentType { get; set; }
        Encoding Encoding { get; set; }
        int Code { get; set; }
        long ContentLength { get; set; }
    }
}