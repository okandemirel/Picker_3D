using System;
using System.IO;
using System.Text;
using Unity.SharpZipLib.GZip;
using Unity.SharpZipLib.Tar;
using UnityEngine;

namespace AssetInventory
{
    public static class TarUtil
    {
        public static void ExtractGz(string fileName, string destinationFolder)
        {
            Stream rawStream = File.OpenRead(fileName);
            GZipInputStream gzipStream = new GZipInputStream(rawStream);

            try
            {
                TarArchive tarArchive = TarArchive.CreateInputTarArchive(IsZipped(fileName) ? gzipStream : rawStream, Encoding.Default);
                tarArchive.ExtractContents(destinationFolder);
                tarArchive.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not extract archive '{fileName}'. The process was either interrupted or the file is corrupted: {e.Message}");
            }

            gzipStream.Close();
            rawStream.Close();
        }

        private static bool IsZipped(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[2];
                fs.Read(buffer, 0, buffer.Length);
                return buffer[0] == 0x1F && buffer[1] == 0x8B;
            }
        }
    }
}