using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Swole
{

    /// <summary>
    /// adapted from source: https://stackoverflow.com/a/43169927
    /// </summary>
    public class ResourceDownloader : IDisposable
    {

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler ProgressChanged;

        protected HttpClient client;
        protected InternalWebClient webClient;

        public int progressUpdateStepByteSize;

        protected class InternalWebClient : WebClient
        {
            public int Timeout { get; set; }

            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest lWebRequest = base.GetWebRequest(uri);
                lWebRequest.Timeout = Timeout;
                if (lWebRequest is HttpWebRequest httpRequest) httpRequest.ReadWriteTimeout = Timeout;
                return lWebRequest;
            }
        }

        public ResourceDownloader()
        {
            client = new HttpClient();
            webClient = new InternalWebClient();
        }

        #region Http(s)

        public void StartCancellableDownloadHTTP(CancellationTokenSource token, string downloadUrl, Stream writeStream, double timeoutMS = 60000) => StartCancellableDownloadHTTPInternal(true, token, downloadUrl, writeStream, timeoutMS).GetAwaiter().GetResult();
        public Task StartCancellableDownloadHTTPAsync(CancellationTokenSource token, string downloadUrl, Stream writeStream, double timeoutMS = 60000) => StartCancellableDownloadHTTPInternal(false, token, downloadUrl, writeStream, timeoutMS);
        private async Task StartCancellableDownloadHTTPInternal(bool sync, CancellationTokenSource token, string downloadUrl, Stream writeStream, double timeoutMS = 60000)
        {
            if (client == null) throw new ObjectDisposedException("HttpDownloader has been disposed and can no longer be used.");

            if (sync) StartDownloadHTTP(downloadUrl, writeStream, timeoutMS, token.Token); else await StartDownloadHTTPAsync(downloadUrl, writeStream, timeoutMS, token.Token);
        }

        public void StartDownloadHTTP(string downloadUrl, Stream writeStream, double timeoutMS = 60000, CancellationToken cancellationToken = default) => StartDownloadHTTPInternal(true, downloadUrl, writeStream, timeoutMS, cancellationToken).GetAwaiter().GetResult();
        public Task StartDownloadHTTPAsync(string downloadUrl, Stream writeStream, double timeoutMS = 60000, CancellationToken cancellationToken = default) => StartDownloadHTTPInternal(false, downloadUrl, writeStream, timeoutMS, cancellationToken);
        private async Task StartDownloadHTTPInternal(bool sync, string downloadUrl, Stream writeStream, double timeoutMS = 60000, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ObjectDisposedException("HttpDownloader has been disposed and can no longer be used.");

            cancellationToken.ThrowIfCancellationRequested();

            client.Timeout = TimeSpan.FromMilliseconds(timeoutMS);

            if (sync)
            {
                using (var response = client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    DownloadFileFromHttpResponseMessage(true, response.Result, writeStream, cancellationToken).GetAwaiter().GetResult(); 
            } 
            else
            {
                using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    await DownloadFileFromHttpResponseMessage(false, response, writeStream, cancellationToken);
            } 

        }

        private async Task DownloadFileFromHttpResponseMessage(bool sync, HttpResponseMessage response, Stream writeStream, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
             
            if (sync)
            {
                using (var contentStream = response.Content.ReadAsStreamAsync())
                    ProcessContentStream(true, totalBytes, contentStream.Result, writeStream, progressUpdateStepByteSize, cancellationToken).GetAwaiter().GetResult();
            } 
            else
            {
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                    await ProcessContentStream(false, totalBytes, contentStream, writeStream, progressUpdateStepByteSize, cancellationToken);
            }
        }

        #endregion

        #region URI

        public void StartCancellableDownloadURI(CancellationTokenSource token, Uri downloadUri, Stream writeStream, double timeoutMS = 60000) => StartCancellableDownloadURIInternal(true, token, downloadUri, writeStream, timeoutMS).GetAwaiter().GetResult();
        public Task StartCancellableDownloadURIAsync(CancellationTokenSource token, Uri downloadUri, Stream writeStream, double timeoutMS = 60000) => StartCancellableDownloadURIInternal(false, token, downloadUri, writeStream, timeoutMS);
        private async Task StartCancellableDownloadURIInternal(bool sync, CancellationTokenSource token, Uri downloadUri, Stream writeStream, double timeoutMS = 60000)
        {
            if (webClient == null) throw new ObjectDisposedException("HttpDownloader has been disposed and can no longer be used.");

            if (sync) StartDownloadURI(downloadUri, writeStream, timeoutMS, token.Token); else await StartDownloadURIAsync(downloadUri, writeStream, timeoutMS, token.Token);
        }

        public void StartDownloadURI(Uri downloadUri, Stream writeStream, double timeoutMS = 60000, CancellationToken cancellationToken = default) => StartDownloadURIInternal(true, downloadUri, writeStream, timeoutMS, cancellationToken).GetAwaiter().GetResult();
        public Task StartDownloadURIAsync(Uri downloadUri, Stream writeStream, double timeoutMS = 60000, CancellationToken cancellationToken = default) => StartDownloadURIInternal(false, downloadUri, writeStream, timeoutMS, cancellationToken);
        private async Task StartDownloadURIInternal(bool sync, Uri downloadUri, Stream writeStream, double timeoutMS = 60000, CancellationToken cancellationToken = default)
        {
            if (webClient == null) throw new ObjectDisposedException("HttpDownloader has been disposed and can no longer be used.");

            cancellationToken.ThrowIfCancellationRequested();

            webClient.Timeout = (int)timeoutMS;
            webClient.UseDefaultCredentials = true;

            if (sync)
            {
                using (var contentStream = webClient.OpenRead(downloadUri))
                    ProcessContentStream(true, 0, contentStream, writeStream, progressUpdateStepByteSize, cancellationToken).GetAwaiter().GetResult(); 
            }
            else
            {
                using (var contentStream = await webClient.OpenReadTaskAsync(downloadUri))
                    await ProcessContentStream(false, 0, contentStream, writeStream, progressUpdateStepByteSize, cancellationToken);
            }

        }

        #endregion

        private async Task ProcessContentStream(bool sync, long? totalDownloadSize, Stream contentStream, Stream writeStream, int progressUpdateStep = 0, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
             
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            if (sync)
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesRead = contentStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    writeStream.Write(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (progressUpdateStep <= 0 || readCount % progressUpdateStep == 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);
            }
            else
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        continue;
                    }

                    await writeStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (progressUpdateStep <= 0 || readCount % progressUpdateStep == 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);
            }

        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        public void Dispose()
        {
            client?.Dispose();
            client = null;
            webClient?.Dispose();
            webClient = null;
            ProgressChanged = null;
        }
    }
}
