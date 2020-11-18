using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace FluentHosting
{
    public class JsonResponse<T>: HandlerResponse
    {
        public JsonResponse(T value, Encoding encoding = null) : this(value, 200, encoding)
        {
        }

        public JsonResponse(T value, int code, Encoding encoding = null)
        {
            Encoding = encoding ?? Encoding.UTF8;
            ContentType = "application/json";
            Code = code;
            var json = JsonConvert.SerializeObject(value);
            var contentBytes = Encoding.GetBytes(json);
            ContentLength = contentBytes.Length;
            Stream = new MemoryStream(contentBytes);
        }
    }
}
