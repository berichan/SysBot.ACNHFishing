using Discord;
using NHSE.Core;
using System.Net;
using System.Threading.Tasks;

namespace SysBot.ACNHFishing
{
    public static class NetUtil
    {
        private static readonly WebClient webClient = new WebClient();

        public static async Task<byte[]> DownloadFromUrlAsync(string url)
        {
            return await webClient.DownloadDataTaskAsync(url).ConfigureAwait(false);
        }

        public static byte[] DownloadFromUrlSync(string url)
        {
            return webClient.DownloadData(url);
        }
    }

    public sealed class Download<T> where T : class
    {
        public bool Success;
        public T? Data;
        public string? SanitizedFileName;
        public string? ErrorMessage;
    }
}