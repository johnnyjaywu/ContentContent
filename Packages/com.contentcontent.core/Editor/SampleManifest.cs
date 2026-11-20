using System;

namespace ContentContent.Core.Editor.Samples
{
    [Serializable]
    public sealed class SampleManifest
    {
        public int schemaVersion;
        public string generatedAtUtc;
        public string releaseTag;
        public SampleManifestEntry[] samples;
    }

    [Serializable]
    public sealed class SampleManifestEntry
    {
        public string packageName;
        public string sampleId;
        public string displayName;
        public string description;
        public string version;
        public string installSubPath;
        public string archiveFileName;
        public string downloadUrl;
        public string sha256;
        public long sizeBytes;

        [NonSerialized]
        public string LocalSourceDirectory;
    }

    [Serializable]
    public sealed class LocalSampleCatalog
    {
        public int schemaVersion;
        public string repository;
        public string defaultReleaseTag;
        public LocalSampleCatalogEntry[] samples;
    }

    [Serializable]
    public sealed class LocalSampleCatalogEntry
    {
        public string packageName;
        public string sampleId;
        public string displayName;
        public string description;
        public string version;
        public string sourcePath;
        public string installSubPath;
    }

    [Serializable]
    public sealed class InstalledSampleMarker
    {
        public string packageName;
        public string sampleId;
        public string version;
        public string sha256;
        public string installedAtUtc;
    }
}
