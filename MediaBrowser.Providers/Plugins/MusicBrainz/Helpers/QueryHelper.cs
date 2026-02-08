using System;
using System.Net;
using System.Net.Http;
using MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;
using MetaBrainz.MusicBrainz;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.MusicBrainz.Helpers
{
    internal class QueryHelper
    {
        public static Query Create(PluginConfiguration configuration, ILogger logger)
        {
            if (Uri.TryCreate(configuration.Server, UriKind.Absolute, out var server))
            {
                Query.DefaultServer = server.DnsSafeHost;
                Query.DefaultPort = server.Port;
                Query.DefaultUrlScheme = server.Scheme;
            }
            else
            {
                // Fallback to official server
                logger.LogWarning("Invalid MusicBrainz server specified, falling back to official server");
                var defaultServer = new Uri(PluginConfiguration.DefaultServer);
                Query.DefaultServer = defaultServer.Host;
                Query.DefaultPort = defaultServer.Port;
                Query.DefaultUrlScheme = defaultServer.Scheme;
            }

            Query.DelayBetweenRequests = configuration.RateLimit;
            var musicBrainzQuery = new Query();
            musicBrainzQuery.ConfigureClientCreation(() =>
            {
                return new HttpClient(new SocketsHttpHandler()
                {
                    Proxy = configuration.UseProxy
                        ? new WebProxy(configuration.ProxyUrl)
                        : null
                });
            });

            return musicBrainzQuery;
        }
    }
}
