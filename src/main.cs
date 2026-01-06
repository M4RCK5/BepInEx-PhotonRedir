using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Photon.Realtime;

namespace PhotonAppIDRedirector
{
    [BepInPlugin("photon-redirect", "Photon Redirector", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        public static string PhotonAppid { get; private set; }
        public static string PhotonNameServer { get; private set; }
        public static string PhotonRegion { get; private set; }

        private void Awake()
        {

            PhotonAppid = Config.Bind("General", "PhotonAppID", "PASTE_HERE", "Set your Photon Realtime AppID here.").Value;
            PhotonNameServer = Config.Bind("General", "PhotonNameServer", "", "Set your NameServer Host here.").Value;
            PhotonRegion = Config.Bind("General", "PhotonRegion", "", "Set your Photon region here (e.g., us, eu, tr), see https://doc.photonengine.com/pun/current/connection-and-authentication/regions#photon-cloud-for-gaming for more.").Value;

            if (string.IsNullOrWhiteSpace(PhotonAppid) || PhotonAppid == "PASTE_HERE")
            {
                Logger.LogError("PhotonAppID is missing or invalid!!");
                Logger.LogInfo("Skipping Redirection.");
                return;
            }

            Harmony harmony = new Harmony("auth.patch");
            harmony.PatchAll();

            Logger.LogInfo("Plugin Loaded!");
            Logger.LogInfo("Using AppID: " + PhotonAppid);

            if (!string.IsNullOrWhiteSpace(PhotonNameServer))
            {
                Logger.LogInfo("Using NameServer: " + PhotonNameServer);
            }

            if (string.IsNullOrWhiteSpace(PhotonRegion))
            {
                Logger.LogWarning("PhotonRegion is not set. Using default region.");
            }
            else
            {
                Logger.LogInfo("Using Region: " + PhotonRegion);
            }
        }
    }

    [HarmonyPatch(typeof(LoadBalancingClient), "ConnectToRegionMaster")]
    public class RegionPatch
    {
        static bool Prefix(LoadBalancingClient __instance, ref string region)
        {
            if (!string.IsNullOrWhiteSpace(Plugin.PhotonRegion))
            {
                BepInEx.Logging.Logger.CreateLogSource("Photon Redirector").LogWarning($"Overriding region from '{region}' to '{Plugin.PhotonRegion}'");
                region = Plugin.PhotonRegion; 
            }
            return true; 
        }
    }
    
    [HarmonyPatch(typeof(LoadBalancingClient), "GetNameServerAddress")]
    public class NameServerPatch
    {
        static void Prefix(LoadBalancingClient __instance)
        {
            if (!string.IsNullOrWhiteSpace(Plugin.PhotonNameServer))
            {
                BepInEx.Logging.Logger.CreateLogSource("Photon Redirector").LogInfo($"Overriding NameServer from {__instance.NameServerHost} to {Plugin.PhotonNameServer}");
                __instance.NameServerHost = Plugin.PhotonNameServer;
            }
        }
    }

    [HarmonyPatch(typeof(LoadBalancingPeer), "OpAuthenticate")]
    public class PhotonPatch
    {
        static void Prefix(ref string appId, ref AuthenticationValues authValues)
        {
            appId = Plugin.PhotonAppid;
            BepInEx.Logging.Logger.CreateLogSource("Photon Redirector").LogInfo($"Redirecting Photon Traffic");
            if (authValues != null)
            {
                authValues.AuthType = CustomAuthenticationType.None;
            }
        }
    }
}
