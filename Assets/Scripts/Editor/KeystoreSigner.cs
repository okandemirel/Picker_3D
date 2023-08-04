using System;
using UnityEditor;

namespace Editor
{
    public static class KeystoreSigner
    {
        [MenuItem("MaxedOutEntertainment/Sign Keystore")]
        static void SignKeystore()
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName =
                $"{Environment.CurrentDirectory}/maxedoutentertainment.keystore";
            PlayerSettings.Android.keyaliasName = "maxedoutentertainment";
            PlayerSettings.Android.keystorePass = "whm*1xgO0r1*!ctM";
            PlayerSettings.Android.keyaliasPass = "whm*1xgO0r1*!ctM";
        }
    }
}