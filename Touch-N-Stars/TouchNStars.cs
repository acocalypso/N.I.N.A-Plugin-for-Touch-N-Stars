using ASCOM.Com;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TouchNStars.Server;
using Settings = TouchNStars.Properties.Settings;

namespace TouchNStars {

    public class Mediators(IDeepSkyObjectSearchVM DeepSkyObjectSearchVM, IImageDataFactory ImageDataFactory, IFramingAssistantVM framingAssistantVM, IProfileService profile, IGuiderMediator guider) {
        public readonly IDeepSkyObjectSearchVM DeepSkyObjectSearchVM = DeepSkyObjectSearchVM;
        public readonly IImageDataFactory ImageDataFactory = ImageDataFactory;
        public readonly IFramingAssistantVM FramingAssistantVM = framingAssistantVM;
        public readonly IProfileService Profile = profile;
        public readonly IGuiderMediator Guider = guider;
    }

    [Export(typeof(IPluginManifest))]
    public class TouchNStars : PluginBase, INotifyPropertyChanged {
        private TouchNStarsServer server;

        public static Mediators Mediators { get; private set; }


        [ImportingConstructor]
        public TouchNStars(IProfileService profileService, IDeepSkyObjectSearchVM DeepSkyObjectSearchVM, IImageDataFactory imageDataFactory, IFramingAssistantVM framingAssistantVM, IGuiderMediator guider) {
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }
            Mediators = new Mediators(DeepSkyObjectSearchVM, imageDataFactory, framingAssistantVM, profileService, guider);
            server = new TouchNStarsServer();
            server.Start();
        }

        public override Task Teardown() {
            // Make sure to unregister an event when the object is no longer in use. Otherwise garbage collection will be prevented.
            server.Stop();
            return base.Teardown();
        }

        public string DefaultNotificationMessage {
            get {
                return Settings.Default.DefaultNotificationMessage;
            }
            set {
                Settings.Default.DefaultNotificationMessage = value;
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
