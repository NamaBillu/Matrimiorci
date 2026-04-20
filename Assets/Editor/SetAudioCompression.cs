using UnityEditor;
using UnityEngine;

public static class SetAudioCompression
{
    [MenuItem("Tools/Set All Audio to CompressedInMemory")]
    public static void SetAllAudioToCompressedInMemory()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");

        if (guids.Length == 0)
        {
            Debug.Log("[SetAudioCompression] No AudioClips found.");
            return;
        }

        int count = 0;
        string[] mobileTargets = { "iPhone", "Android" };

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer == null)
                continue;

            // Update default sample settings — only loadType, preserve compressionFormat and quality
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            importer.defaultSampleSettings = settings;

            // Update platform overrides for iOS and Android only if overrides already exist
            foreach (string platform in mobileTargets)
            {
                if (importer.ContainsSampleSettingsOverride(platform))
                {
                    AudioImporterSampleSettings platformSettings = importer.GetOverrideSampleSettings(platform);
                    platformSettings.loadType = AudioClipLoadType.CompressedInMemory;
                    importer.SetOverrideSampleSettings(platform, platformSettings);
                }
            }

            importer.SaveAndReimport();
            Debug.Log($"[SetAudioCompression] Processed: {path}");
            count++;
        }

        Debug.Log($"[SetAudioCompression] Done: set {count} AudioClips to CompressedInMemory.");
    }
}
