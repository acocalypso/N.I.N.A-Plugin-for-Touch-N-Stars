using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] The following GUID is used as a unique identifier of the plugin. Generate a fresh one for your plugin!
[assembly: Guid("5af0afae-cab5-4fde-9fcd-3302c0d59686")]

// [MANDATORY] The assembly versioning
//Should be incremented for each new release build of a plugin
[assembly: AssemblyVersion("1.0.0.2")]
[assembly: AssemblyFileVersion("1.0.0.2")]

// [MANDATORY] The name of your plugin
[assembly: AssemblyTitle("Touch 'N' Stars")]
// [MANDATORY] A short description of your plugin
[assembly: AssemblyDescription("A WebApp to control NINA")]

// The following attributes are not required for the plugin per se, but are required by the official manifest meta data

// Your name
[assembly: AssemblyCompany("Johannes Maier, Christian Palm, Christian Wöhrle")]
// The product name that this plugin is part of
[assembly: AssemblyProduct("Touch 'N' Stars")]
[assembly: AssemblyCopyright("Copyright © 2025 Johannes Maier, Christian Palm, Christian Wöhrle")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.1.2.9001")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "GPL-3.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.gnu.org/licenses/gpl-3.0.en.html#license-text")]
// The repository where your plugin is hosted
[assembly: AssemblyMetadata("Repository", "https://github.com/JohannesWorks/N.I.N.A-Plugin-for-Touch-N-Stars")]

// The following attributes are optional for the official manifest meta data

//[Optional] Your plugin homepage URL - omit if not applicaple
// [assembly: AssemblyMetadata("Homepage", "")]

//[Optional] Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "Web,App")]

//[Optional] A link that will show a log of all changes in between your plugin's versions
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/JohannesWorks/N.I.N.A-Plugin-for-Touch-N-Stars/blob/main/CHANGELOG.md")]

//[Optional] The url to a featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://github.com/JohannesWorks/Touch-N-Stars/blob/master/Logo/Logo_TouchNStars.png?raw=true")]
//[Optional] A url to an example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "")]
//[Optional] An additional url to an example example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
//[Optional] An in-depth description of your plugin
[assembly: AssemblyMetadata("LongDescription", @"
# Touch'N'Stars: WebApp for Mobile Control of NINA

### 🚀 **Current Status: Beta Version**  
**Android Build:** Available at [GitHub Releases](https://github.com/Touch-N-Stars/Touch-N-Stars/releases)

This web application is in **development phase** and is aimed at astronomy enthusiasts who want to conveniently set up their equipment using the **NINA** (Nighttime Imaging 'N' Astronomy) software via touch devices.

### 🏁 **Purpose of the WebApp**  
The application aims to make controlling and adjusting already configured profiles easier - directly through a tablet or smartphone. This makes handling more mobile and convenient, especially when setting up equipment and starting imaging sessions.

### 🧩 **Important Notes**
- This web app **requires a fully configured and running installation of NINA**.
- It requires the **Advanced API** plugin in the latest beta version.
- The API port must be set to 1888 and V2 must be enabled.
- For Three Point Polar Alignment, version 2.2.2.0 or newer is required.
- It is intended as a complement to the desktop software and provides mobile support for basic functions.
- I assume no liability
  
### 🔧 **Installation**
- After installing the plugin, Touch'N'Stars can be accessed via http://<NINA - PC - IP>:5000.

### 🧪 **What does the Alpha Version offer?**  
- **Mobile Operation**: Easily access NINA through your smartphone or tablet.
- **User - Friendly Design**: Simple and intuitive interface specifically optimized for mobile devices.
- **Focus on Practicality**: Support for essential steps in setting up your equipment.

### 💙 **Acknowledgements**  
My thanks go to the entire **NINA** development team, whose great work makes this web app possible.
A special thank you to **Christian**, the developer of the **Advanced API**, for his efforts and support. His work has significantly enabled the development of this web app.
")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
// [Unused]
[assembly: AssemblyConfiguration("")]
// [Unused]
[assembly: AssemblyTrademark("")]
// [Unused]
[assembly: AssemblyCulture("")]