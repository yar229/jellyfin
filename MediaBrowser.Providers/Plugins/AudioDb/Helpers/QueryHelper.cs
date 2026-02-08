using System.Net;
using System.Net.Http;
using System.Text;
using Jellyfin.Networking.HappyEyeballs;
using MediaBrowser.Common.Net;

namespace MediaBrowser.Providers.Plugins.AudioDb.Helpers
{
    internal class QueryHelper
    {
        public static HttpClient CreateClient(PluginConfiguration pluginConfig, IHttpClientFactory clientFactory)
        {
            var proxy = pluginConfig.UseProxy
                ? new WebProxy(pluginConfig.ProxyUrl)
                : null;

            var client = new HttpClient(new SocketsHttpHandler()
            {
                Proxy = proxy,
                AutomaticDecompression = DecompressionMethods.All,
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8,
                ConnectCallback = HttpClientExtension.OnConnect
            });
            AddDefaultHeaders(clientFactory, client);
            return client;
        }

        private static void AddDefaultHeaders(IHttpClientFactory clientFactory, HttpClient client)
        {
            var defaultClient = clientFactory.CreateClient(NamedClient.Default);

            foreach (var userAgent in defaultClient.DefaultRequestHeaders.UserAgent)
            {
                client.DefaultRequestHeaders.UserAgent.Add(userAgent);
            }

            foreach (var accept in defaultClient.DefaultRequestHeaders.Accept)
            {
                client.DefaultRequestHeaders.Accept.Add(accept);
            }
        }
    }
}
