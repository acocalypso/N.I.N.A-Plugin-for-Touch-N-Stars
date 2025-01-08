using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Threading;

namespace TouchNStars.Server {
    public class TouchNStarsServer {
        private Thread serverThread;
        private CancellationTokenSource apiToken;
        public WebServer WebServer;
        public readonly int Port = 5000;

        public void CreateServer() {
            WebServer = new WebServer(o => o
                .WithUrlPrefix($"http://*:{Port}")
                .WithMode(HttpListenerMode.EmbedIO))
                .WithWebApi("/", m => m.WithController<Controller>()) // Register the controller, which will be used to handle all the api requests which were previously in server.py
                .WithModule(new RedirectModule("/", "/app", request => request.RequestedPath.Equals("/"))); // Automatically redirect to user to the app, so the user doesn't have to enter /
        }

        public void Start() {
            try {
                Logger.Debug("Creating Webserver");
                CreateServer();
                Logger.Info("Starting Webserver");
                if (WebServer != null) {
                    serverThread = new Thread(() => APITask(WebServer)) {
                        Name = "Touch-N-Stars API Thread"
                    };
                    // serverThread.SetApartmentState(ApartmentState.STA);
                    serverThread.Start();
                }
            } catch (Exception ex) {
                Logger.Error($"failed to start web server: {ex}");
            }
        }

        public void Stop() {
            try {
                apiToken?.Cancel();
                WebServer?.Dispose();
                WebServer = null;
            } catch (Exception ex) {
                Logger.Error($"failed to stop API: {ex}");
            }
        }

        // [STAThread]
        private void APITask(WebServer server) {
            // string ipAdress = CoreUtility.GetLocalNames()["IPADRESS"];
            // Logger.Info($"starting web server, listening at {ipAdress}:{Port}");
            Logger.Info("Touch-N-Stars Webserver starting");

            try {
                apiToken = new CancellationTokenSource();
                server.RunAsync(apiToken.Token).Wait();
            } catch (Exception ex) {
                Logger.Error($"failed to start web server: {ex}");
                Notification.ShowError($"Failed to start web server, see NINA log for details");
            }
        }
    }
}