using EmbedIO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TouchNStars.Server {
    public class Proxy {
        private WebServer server;
        private Thread proxyThread;
        private CancellationTokenSource token;

        public Proxy() {

        }

        public void Start() {
            server = new WebServer(o => o
                .WithUrlPrefix($"http://*:6001")
                .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ProxyModule());

            token = new CancellationTokenSource();

            proxyThread = new Thread(() => ProxyThread()) {
                Name = "Proxy Thread"
            };
            proxyThread.Start();
        }

        public void Stop() {
            token?.Cancel();
            server.Dispose();
        }

        public void ProxyThread() {
            Task t = server.RunAsync();
            while (!t.IsCompleted && !token.IsCancellationRequested) {
                Thread.Sleep(100);
            }
        }
    }

    public class ProxyModule : WebModuleBase {
        internal ProxyModule() : base("/") {
        }

        public override bool IsFinalHandler => true;

        protected override async Task OnRequestAsync(IHttpContext context) {
            string url = await context.GetRequestBodyAsStringAsync();
            HttpClient client = new HttpClient();
            (await client.GetStreamAsync(url)).CopyTo(context.Response.OutputStream);
            client.Dispose();
        }
    }
}