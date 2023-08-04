using System.IO;
using System.Net;
using UnityEditor;

namespace Editor
{
    public static class ClickPackageDownloader
    {
        [MenuItem("MaxedOutEntertainment/ClickPackageDownloader")]
        public static void ImportPackage()
        {
            // Set the URL of the package you want to download
            string packageURL =
                "https://s3.amazonaws.com/com.tabtale.repo/android/maven/ttplugins/com/tabtale/tt_plugins/unity/CLIK/Latest%20CLIK%20Package/Latest%20CLIK.unitypackage";

            // Create a temporary directory to store the package
            string tempDirectory = Path.Combine(Path.GetTempPath(), "TempPackage");
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            // Download the package to the temporary directory
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(packageURL, Path.Combine(tempDirectory, "package.unitypackage"));
            }

            // Import the package to the project
            AssetDatabase.ImportPackage(Path.Combine(tempDirectory, "package.unitypackage"), false);

            // Clean up the temporary directory
            Directory.Delete(tempDirectory, true);
        }
    }
}