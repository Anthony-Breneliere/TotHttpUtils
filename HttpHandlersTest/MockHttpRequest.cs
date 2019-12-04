using IMAUtils.Extension;
using Microsoft.AspNetCore.Http;
using Moq;
using System.IO;

namespace HttpHandlersTest
{
    /// <summary>
    /// Initialise un moq de HttpRequest avec un body par défaut.
    /// </summary>
    public class MockHttpRequest : Mock<HttpRequest>
    {
        internal Stream CreateStream(object obj) => CreateStream(obj.Json());

        internal Stream CreateStream(string json)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public MockHttpRequest(object body)
        {
            this.SetupGet(r => r.Body).Returns(CreateStream(body));
        }

        public MockHttpRequest(string body)
        {
            this.SetupGet(r => r.Body).Returns(CreateStream(body));
        }
    }
}
