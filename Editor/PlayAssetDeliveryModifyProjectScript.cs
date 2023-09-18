#if UNITY_ANDROID
using System.IO;
using System.Linq;
using UnityEditor.Android;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Android;

namespace UnityEditor.AddressableAssets.Android
{
    /// <summary>
    /// When building for Android with asset packs support copies generated addressables asset bundles to asset packs inside gradle project.
    /// </summary>
    public class PlayAssetDeliveryModifyProjectScript : AndroidProjectFilesModifier
    {
        void AddBundlesFilesToContext(AndroidProjectFilesModifierContext projectFilesContext, string postfix)
        {
            var buildProcessorDataPath = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{postfix}", CustomAssetPackUtility.kBuildProcessorDataFilename);
            if (!File.Exists(buildProcessorDataPath))
            {
                return;
            }
            var contents = File.ReadAllText(buildProcessorDataPath);
            var data = JsonUtility.FromJson<BuildProcessorData>(contents);
            foreach (BuildProcessorDataEntry entry in data.Entries)
            {
                projectFilesContext.AddFileToCopy(entry.BundleBuildPath, entry.AssetPackPath);
            }
        }

        static string CreateAddressableAssetPackAssetsPath(string postfix)
        {
            return Path.Combine(CustomAssetPackUtility.kAddressablesAssetPackName, $"{CustomAssetPackUtility.CustomAssetPacksAssetsPath}{postfix}");
        }

        static void AddInstallTimeFilesToContext(AndroidProjectFilesModifierContext projectFilesContext, string postfix)
        {
            var targetPath = CreateAddressableAssetPackAssetsPath(postfix);
            var sourcePath = $"{Addressables.BuildPath}{postfix}";
            if (!Directory.Exists(sourcePath))
            {
                // using default texture compression variant
                sourcePath = Addressables.BuildPath;
            }
            foreach (var mask in CustomAssetPackUtility.InstallTimeFilesMasks)
            {
                var files = Directory.EnumerateFiles(sourcePath, mask, SearchOption.AllDirectories).ToList();
                foreach (var f in files)
                {
                    var dest = Path.Combine(targetPath, Path.GetRelativePath(sourcePath, f));
                    projectFilesContext.AddFileToCopy(f, dest);
                }
            }
        }

        static void AddCustomAssetPackDataContext(AndroidProjectFilesModifierContext projectFilesContext, string postfix)
        {
            var targetPath = Path.Combine(CreateAddressableAssetPackAssetsPath(postfix), CustomAssetPackUtility.kCustomAssetPackDataFilename);
            var sourcePath = Path.Combine(CustomAssetPackUtility.BuildRootDirectory, $"{Addressables.StreamingAssetsSubFolder}{postfix}", CustomAssetPackUtility.kCustomAssetPackDataFilename);
            projectFilesContext.AddFileToCopy(sourcePath, targetPath);
        }

        /// <summary>
        /// Setup copy operations for addressables asset bundles to the asset packs inside gradle project.
        /// Stores information required for new build.gradle files and for modifying existing gradle files.
        /// </summary>
        public override AndroidProjectFilesModifierContext Setup()
        {
            var projectFilesContext = new AndroidProjectFilesModifierContext();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
                !TextureCompressionTargetingHelper.UseAssetPacks ||
                PlayAssetDeliverySetup.PlayAssetDeliveryNotInitialized() ||
                !File.Exists(CustomAssetPackUtility.CustomAssetPacksDataEditorPath) ||
                (TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting && !TextureCompressionTargetingHelper.IsCurrentTextureCompressionDefault))
            {
                // gradle project must be modified only when using asset packs, play asset delivery is supported and addressables are generated for PAD,
                // this should be done during the last (or the only) texture compression related iteration
                projectFilesContext.SetData("UseAssetPacks", false);
                return projectFilesContext;
            }

            var contents = File.ReadAllText(CustomAssetPackUtility.CustomAssetPacksDataEditorPath);
            var customPackData = JsonUtility.FromJson<CustomAssetPackData>(contents);
            foreach (CustomAssetPackDataEntry entry in customPackData.Entries)
            {
                projectFilesContext.Outputs.AddBuildGradleFile(Path.Combine(entry.AssetPackName, "build.gradle"));
            }
            projectFilesContext.SetData("AssetPacks", customPackData);
            projectFilesContext.SetData("UseAssetPacks", true);

            AddCustomAssetPackDataContext(projectFilesContext, "");
            AddBundlesFilesToContext(projectFilesContext, "");
            AddInstallTimeFilesToContext(projectFilesContext, "");
            if (TextureCompressionTargetingHelper.EnabledTextureCompressionTargeting)
            {
                foreach (var textureCompression in PlayerSettings.Android.textureCompressionFormats)
                {
                    var postfix = TextureCompressionTargetingHelper.TcfPostfix(textureCompression);
                    AddCustomAssetPackDataContext(projectFilesContext, postfix);
                    AddBundlesFilesToContext(projectFilesContext, postfix);
                    AddInstallTimeFilesToContext(projectFilesContext, postfix);
                }
            }

            projectFilesContext.Dependencies.DependencyFiles = new[]
            {
                CustomAssetPackUtility.CustomAssetPacksDataEditorPath
            };

            return projectFilesContext;
        }

        /// <summary>
        /// Create build.gradle files for the new asset packs. Adds required dependencies to the existing gradle files.
        /// </summary>
        /// <param name="projectFiles">An object representing gradle project files</param>
        public override void OnModifyAndroidProjectFiles(AndroidProjectFiles projectFiles)
        {
            if (!projectFiles.GetData<bool>("UseAssetPacks"))
            {
                return;
            }

            var customPackData = projectFiles.GetData<CustomAssetPackData>("AssetPacks");

            var assetPackString = projectFiles.LauncherBuildGradle.Android.AssetPacks.GetRaw();
            foreach (var entry in customPackData.Entries)
            {
                var buildGradle = new ModuleBuildGradleFile();
                buildGradle.ApplyPluginList.AddPluginByName("com.android.asset-pack");
                buildGradle.AddElement(new Block("assetPack", $"{{\n\tpackName = \"{entry.AssetPackName}\"\n\tdynamicDelivery {{\n\t\tdeliveryType = \"{CustomAssetPackUtility.DeliveryTypeToGradleString(entry.DeliveryType)}\"\n\t}}\n}}"));
                projectFiles.SetBuildGradleFile(Path.Combine(entry.AssetPackName, "build.gradle"), buildGradle);
                projectFiles.GradleSettings.IncludeList.AddPluginByName($":{entry.AssetPackName}");
                if (string.IsNullOrEmpty(assetPackString))
                {
                    assetPackString = $"\":{entry.AssetPackName}\"";
                }
                else
                {
                    assetPackString += $", \":{entry.AssetPackName}\"";
                }
            }
            projectFiles.LauncherBuildGradle.Android.AssetPacks.SetRaw(assetPackString);
        }
    }
}
#endif
