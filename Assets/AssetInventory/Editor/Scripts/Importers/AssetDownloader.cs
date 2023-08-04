﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AssetInventory
{
    public sealed class AssetDownloader
    {
        private const int STATE_CACHE_PERIOD = 1;
        private const int START_STATE_DELAY_SECONDS = 5;

        public enum State
        {
            Unavailable,
            Unknown,
            Downloading,
            Downloaded,
            UpdateAvailable
        }

        private readonly AssetInfo _asset;
        private State _state;
        private long _bytesDownloaded;
        private DateTime _lastWriteTime;
        private DateTime _startTime;

        // global state since callback does not have object reference
        public static Dictionary<string, bool> DownloadDone
        {
            get
            {
                if (_downloadDone == null) _downloadDone = new Dictionary<string, bool>();
                return _downloadDone;
            }
        }

        private static Dictionary<string, bool> _downloadDone;

        // caching
        private AssetDownloadState _lastState;
        private DateTime _lastStateTime;

        public AssetDownloader(AssetInfo asset)
        {
            _asset = asset;
        }

        public bool IsDownloadSupported()
        {
#if UNITY_2020_1_OR_NEWER
            return true;
#else
            // loading assembly will fail below 2020
            return false;
#endif
        }

        public AssetDownloadState GetState()
        {
            if (_lastState != null && (DateTime.Now - _lastStateTime).Seconds < STATE_CACHE_PERIOD) return _lastState;

            CheckState();

            // start state checking only after minimum download duration has passed to give download time to initialize 
            if ((_state == State.Unknown || _state == State.Unavailable) && (DateTime.Now - _startTime).Seconds < START_STATE_DELAY_SECONDS)
            {
                _state = State.Downloading;
            }

            AssetDownloadState state = new AssetDownloadState
            {
                state = _state
            };
            if (_state == State.Downloading)
            {
                state.bytesTotal = _asset.PackageSize;
                state.bytesDownloaded = _bytesDownloaded;
                state.lastDownloadChange = _lastWriteTime;
                if (_asset.PackageSize > 0) state.progress = (float) _bytesDownloaded / _asset.PackageSize;
            }

            _lastState = state;
            _lastStateTime = DateTime.Now;

            return state;
        }

        private void CheckState()
        {
            string targetFile = _asset.ToAsset().GetCalculatedLocation();
            if (targetFile == null)
            {
                _state = State.Unknown;
                return;
            }

            string folder = Path.GetDirectoryName(targetFile);

            // see if any progress file is there
            FileInfo fileInfo = null;
            string downloadFile = Path.Combine(folder, $".{_asset.SafeName}-{_asset.ForeignId}.tmp");
            if (File.Exists(downloadFile))
            {
                fileInfo = new FileInfo(downloadFile);
            }
            else
            {
                string redownloadFile = Path.Combine(folder, $".{_asset.SafeName}-content__{_asset.ForeignId}.tmp");
                if (File.Exists(redownloadFile)) fileInfo = new FileInfo(redownloadFile);
            }
            if (fileInfo != null)
            {
                _state = State.Downloading;
                _bytesDownloaded = fileInfo.Length;
                _lastWriteTime = fileInfo.LastWriteTime;
                return;
            }

            bool exists = File.Exists(targetFile);

            // update database location once file is downloaded
            if (exists && string.IsNullOrEmpty(_asset.GetLocation(true)))
            {
                _asset.Location = targetFile;
                _asset.Refresh();

                // work directly on db to make sure it's latest state
                DBAdapter.DB.Execute("update Asset set Location=? where Id=?", targetFile, _asset.AssetId);
                _state = State.Downloaded;
                return;
            }

            exists = exists || (!string.IsNullOrEmpty(_asset.GetLocation(true)) && File.Exists(_asset.GetLocation(true)));
            _state = exists ? (_asset.IsUpdateAvailable() ? State.UpdateAvailable : State.Downloaded) : State.Unavailable;
        }

        public void Download()
        {
            if (!IsDownloadSupported()) return;

            Assembly assembly = Assembly.Load("UnityEditor.CoreModule");
            Type asc = assembly.GetType("UnityEditor.AssetStoreUtils");
            MethodInfo download = asc.GetMethod("Download", BindingFlags.Public | BindingFlags.Static);
            Type downloadDone = assembly.GetType("UnityEditor.AssetStoreUtils+DownloadDoneCallback");
            Delegate onDownloadDone = Delegate.CreateDelegate(downloadDone, typeof(AssetDownloaderUtils), "OnDownloadDone");

            string json = new JObject(
                new JProperty("download", new JObject(
                    new JProperty("url", _asset.OriginalLocation),
                    new JProperty("key", _asset.OriginalLocationKey)
                ))).ToString();

            _state = State.Downloading;
            _startTime = DateTime.Now;
            string key = _asset.ForeignId.ToString();
            if (DownloadDone.ContainsKey(key))
            {
                DownloadDone[key] = false;
            }
            else
            {
                DownloadDone.Add(key, false);
            }
            download?.Invoke(null, new object[]
            {
                key, _asset.OriginalLocation,
                new[] {_asset.SafePublisher, _asset.SafeCategory, _asset.SafeName},
                _asset.OriginalLocationKey, json, false, onDownloadDone
            });
            _asset.Refresh(true);
        }
    }

    public sealed class AssetDownloadState
    {
        public AssetDownloader.State state;
        public long bytesDownloaded;
        public long bytesTotal;
        public float progress;
        public DateTime lastDownloadChange;
    }

    public static class AssetDownloaderUtils
    {
        public static void OnDownloadDone(string package_id, string message, int bytes, int total)
        {
            if (message == "ok")
            {
                if (AssetDownloader.DownloadDone.ContainsKey(package_id))
                {
                    AssetDownloader.DownloadDone[package_id] = true;
                }
                else
                {
                    AssetDownloader.DownloadDone.Add(package_id, true);
                }
            }
            else
            {
                Debug.LogError($"Error downloading asset {package_id} at {bytes}/{total}: {message}");
            }
        }
    }
}