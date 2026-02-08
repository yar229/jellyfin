#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.AudioDb.Helpers;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbAlbumImageProvider : IRemoteImageProvider, IHasOrder, IDisposable
    {
        private readonly IServerConfigurationManager _config;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        public AudioDbAlbumImageProvider(IServerConfigurationManager config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = QueryHelper.CreateClient(AudioDb.Plugin.Instance!.Configuration, httpClientFactory);
        }

        /// <inheritdoc />
        public string Name => "TheAudioDB";

        /// <inheritdoc />
        // After embedded and fanart
        public int Order => 2;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
            yield return ImageType.Disc;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            if (item.TryGetProviderId(MetadataProvider.MusicBrainzReleaseGroup, out var id))
            {
                await AudioDbAlbumProvider.Current.EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = AudioDbAlbumProvider.GetAlbumInfoPath(_config.ApplicationPaths, id);

                FileStream jsonStream = AsyncFile.OpenRead(path);
                await using (jsonStream.ConfigureAwait(false))
                {
                    var obj = await JsonSerializer.DeserializeAsync<AudioDbAlbumProvider.RootObject>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

                    if (obj is not null && obj.album is not null && obj.album.Count > 0)
                    {
                        return GetImages(obj.album[0]);
                    }
                }
            }

            return [];
        }

        private List<RemoteImageInfo> GetImages(AudioDbAlbumProvider.Album item)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(item.strAlbumThumb))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strAlbumThumb,
                    Type = ImageType.Primary
                });
            }

            if (!string.IsNullOrWhiteSpace(item.strAlbumCDart))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = item.strAlbumCDart,
                    Type = ImageType.Disc
                });
            }

            return list;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetAsync(url, cancellationToken);
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => item is MusicAlbum;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose all resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
        }
    }
}
