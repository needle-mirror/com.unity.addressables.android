#if UNITY_2023_3_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

[InitializeOnLoad]
public class UpdateAddressablesPackage
{
    const string kAddressablesPackageName = "com.unity.addressables";

    static UpdateAddressablesPackage()
    {
        var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
        var addressablesPackage = Array.Find(packages, p => p.name == kAddressablesPackageName);
        if (addressablesPackage != null && addressablesPackage.version.StartsWith("2.0"))
        {
            return;
        }
        // installing latest compatible addressables package version
        var addRequest = Client.Add($"{kAddressablesPackageName}");
        while (!addRequest.IsCompleted)
        {
            System.Threading.Thread.Sleep(25);
        }
    }
}
#endif
