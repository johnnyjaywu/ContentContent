using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace ContentContent.Core.Editor.Samples
{
    [InitializeOnLoad]
    internal static class ContentContentSamplesPackageManagerRegistrar
    {
        static ContentContentSamplesPackageManagerRegistrar()
        {
            PackageManagerExtensions.RegisterExtension(new ContentContentSamplesPackageManagerExtension());
        }
    }

    /// <summary>
    /// Injects a <b>ContentContent Downloadable Samples</b> panel into Unity's Package Manager details view
    /// whenever a <c>com.contentcontent.*</c> package is selected, allowing one-click remote download/update/removal.
    /// </summary>
    internal sealed class ContentContentSamplesPackageManagerExtension : IPackageManagerExtension
    {
        private const string PackagePrefix = "com.contentcontent.";

        private VisualElement _root;
        private Label _sourceLabel;
        private Label _statusLabel;
        private Button _refreshButton;
        private VisualElement _rowsContainer;

        private string _selectedPackageName = string.Empty;
        private bool _isFetchingManifest;
        private readonly HashSet<string> _busySampleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _sampleProgress = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public VisualElement CreateExtensionUI()
        {
            _root = new VisualElement
            {
                style =
                {
                    marginTop = 10,
                    marginBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 8,
                    paddingBottom = 8,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(0.25f, 0.25f, 0.25f, 0.9f),
                    borderBottomColor = new Color(0.25f, 0.25f, 0.25f, 0.9f),
                    borderLeftColor = new Color(0.25f, 0.25f, 0.25f, 0.9f),
                    borderRightColor = new Color(0.25f, 0.25f, 0.25f, 0.9f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    backgroundColor = new Color(0f, 0f, 0f, 0.12f),
                    display = DisplayStyle.None
                }
            };

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    marginBottom = 6
                }
            };

            var titleGroup = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var title = new Label("Downloadable Samples")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginRight = 8
                }
            };
            titleGroup.Add(title);

            _sourceLabel = new Label(string.Empty)
            {
                style =
                {
                    fontSize = 10,
                    opacity = 0.75f
                }
            };
            titleGroup.Add(_sourceLabel);
            header.Add(titleGroup);

            _refreshButton = new Button(() => _ = FetchAndRebuildAsync(forceRefresh: true))
            {
                text = "Refresh"
            };
            header.Add(_refreshButton);

            _root.Add(header);

            _statusLabel = new Label(string.Empty)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = 11,
                    marginBottom = 4,
                    display = DisplayStyle.None
                }
            };
            _root.Add(_statusLabel);

            _rowsContainer = new VisualElement();
            _root.Add(_rowsContainer);

            return _root;
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (_root == null)
                return;

            if (packageInfo == null ||
                string.IsNullOrEmpty(packageInfo.name) ||
                !packageInfo.name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                _selectedPackageName = string.Empty;
                _root.style.display = DisplayStyle.None;
                return;
            }

            _selectedPackageName = packageInfo.name;
            _root.style.display = DisplayStyle.Flex;

            if (SampleDownloadService.CachedManifest == null)
            {
                _ = FetchAndRebuildAsync(forceRefresh: false);
            }
            else
            {
                RebuildRows();
            }
        }

        public void OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {
            if (packageInfo != null && string.Equals(packageInfo.name, _selectedPackageName, StringComparison.OrdinalIgnoreCase))
            {
                RebuildRows();
            }
        }

        public void OnPackageRemoved(PackageInfo packageInfo)
        {
            if (packageInfo != null && string.Equals(packageInfo.name, _selectedPackageName, StringComparison.OrdinalIgnoreCase))
            {
                _selectedPackageName = string.Empty;
                if (_root != null)
                {
                    _root.style.display = DisplayStyle.None;
                }
            }
        }

        private async System.Threading.Tasks.Task FetchAndRebuildAsync(bool forceRefresh)
        {
            if (_isFetchingManifest)
                return;

            _isFetchingManifest = true;
            if (_refreshButton != null)
                _refreshButton.SetEnabled(false);

            SetStatus("Loading sample catalog...", isError: false);

            try
            {
                await SampleDownloadService.FetchManifestAsync(forceRefresh);
                SetStatus(
                    string.IsNullOrEmpty(SampleDownloadService.LastError)
                        ? string.Empty
                        : $"Catalog fetch notice: {SampleDownloadService.LastError}",
                    isError: !string.IsNullOrEmpty(SampleDownloadService.LastError));
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to load sample catalog: {ex.Message}", isError: true);
            }
            finally
            {
                _isFetchingManifest = false;
                if (_refreshButton != null)
                    _refreshButton.SetEnabled(true);
                RebuildRows();
            }
        }

        private void RebuildRows()
        {
            if (_rowsContainer == null || string.IsNullOrEmpty(_selectedPackageName))
                return;

            _rowsContainer.Clear();

            SampleManifest manifest = SampleDownloadService.CachedManifest;
            if (manifest == null)
            {
                _sourceLabel.text = string.Empty;
                if (!_isFetchingManifest && string.IsNullOrEmpty(_statusLabel.text))
                {
                    SetStatus("Sample catalog unavailable. Check internet connection and click Refresh.", isError: true);
                }
                return;
            }

            _sourceLabel.text = SampleDownloadService.IsUsingLocalFallback
                ? "[Local Samples/]"
                : $"[GitHub Release: {manifest.releaseTag}]";

            List<SampleManifestEntry> entries = SampleDownloadService.GetSamplesForPackage(manifest, _selectedPackageName);
            if (entries.Count == 0)
            {
                _rowsContainer.Add(new Label("No downloadable samples registered for this package.")
                {
                    style = { fontSize = 11, opacity = 0.75f }
                });
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                _rowsContainer.Add(BuildSampleRow(entries[i]));
            }
        }

        private VisualElement BuildSampleRow(SampleManifestEntry entry)
        {
            bool isBusy = _busySampleIds.Contains(entry.sampleId);
            bool isInstalled = SampleDownloadService.IsInstalled(entry, out InstalledSampleMarker marker, out _);
            bool hasUpdate = isInstalled &&
                             marker != null &&
                             (!string.Equals(marker.version, entry.version, StringComparison.Ordinal) ||
                              (!string.IsNullOrEmpty(entry.sha256) &&
                               !string.IsNullOrEmpty(marker.sha256) &&
                               !string.Equals(marker.sha256, entry.sha256, StringComparison.OrdinalIgnoreCase)));

            var card = new VisualElement
            {
                style =
                {
                    marginTop = 4,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    backgroundColor = new Color(0f, 0f, 0f, 0.14f),
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3
                }
            };

            var topRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween
                }
            };

            string sizeText = SampleArchiveInstaller.FormatByteSize(entry.sizeBytes);
            string installedBadge = isInstalled ? (hasUpdate ? "  •  Update available" : "  •  Installed") : string.Empty;
            var nameLabel = new Label($"{entry.displayName}  (v{entry.version}, {sizeText}){installedBadge}")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    flexShrink = 1
                }
            };
            topRow.Add(nameLabel);

            var buttonRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            if (isBusy)
            {
                _sampleProgress.TryGetValue(entry.sampleId, out float pct);
                var progressBtn = new Button { text = $"Downloading {Mathf.RoundToInt(pct * 100f)}%" };
                progressBtn.SetEnabled(false);
                buttonRow.Add(progressBtn);
            }
            else
            {
                string actionLabel = !isInstalled ? "Download" : (hasUpdate ? "Update" : "Re-download");
                var downloadBtn = new Button(() => _ = DownloadEntryAsync(entry))
                {
                    text = actionLabel
                };
                buttonRow.Add(downloadBtn);

                if (isInstalled)
                {
                    var pingBtn = new Button(() => PingSampleFolder(entry))
                    {
                        text = "Ping"
                    };
                    buttonRow.Add(pingBtn);

                    var removeBtn = new Button(() =>
                    {
                        SampleDownloadService.RemoveSample(entry);
                        SetStatus($"Removed sample '{entry.displayName}'.", isError: false);
                        RebuildRows();
                    })
                    {
                        text = "Remove"
                    };
                    buttonRow.Add(removeBtn);
                }
            }

            topRow.Add(buttonRow);
            card.Add(topRow);

            if (!string.IsNullOrEmpty(entry.description))
            {
                card.Add(new Label(entry.description)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal,
                        fontSize = 11,
                        opacity = 0.85f,
                        marginTop = 2
                    }
                });
            }

            return card;
        }

        private async System.Threading.Tasks.Task DownloadEntryAsync(SampleManifestEntry entry)
        {
            if (_busySampleIds.Contains(entry.sampleId))
                return;

            _busySampleIds.Add(entry.sampleId);
            _sampleProgress[entry.sampleId] = 0f;
            SetStatus($"Downloading '{entry.displayName}'...", isError: false);
            RebuildRows();

            var progress = new Progress<float>(p =>
            {
                _sampleProgress[entry.sampleId] = p;
                RebuildRows();
            });

            try
            {
                string assetPath = await SampleDownloadService.DownloadAndInstallAsync(entry, progress);
                SetStatus($"Installed '{entry.displayName}' into {assetPath}", isError: false);
                PingAssetPath(assetPath);
            }
            catch (Exception ex)
            {
                SetStatus($"Download failed for '{entry.displayName}': {ex.Message}", isError: true);
                Debug.LogException(ex);
            }
            finally
            {
                _busySampleIds.Remove(entry.sampleId);
                _sampleProgress.Remove(entry.sampleId);
                RebuildRows();
            }
        }

        private static void PingSampleFolder(SampleManifestEntry entry)
        {
            string assetPath = SampleArchiveInstaller.ToUnityAssetPath(entry.installSubPath);
            PingAssetPath(assetPath);
        }

        private static void PingAssetPath(string assetPath)
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (folder != null)
            {
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            if (_statusLabel == null)
                return;

            if (string.IsNullOrEmpty(message))
            {
                _statusLabel.text = string.Empty;
                _statusLabel.style.display = DisplayStyle.None;
                return;
            }

            _statusLabel.text = message;
            _statusLabel.style.color = isError
                ? new StyleColor(new Color(1f, 0.45f, 0.45f))
                : new StyleColor(StyleKeyword.Null);
            _statusLabel.style.display = DisplayStyle.Flex;
        }
    }
}
