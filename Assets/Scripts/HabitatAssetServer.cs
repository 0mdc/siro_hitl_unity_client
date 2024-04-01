/// <summary>
/// This static class configures the Unity addressables system.
/// 
/// The addressables system can reference static variables at initialization.
/// This is done via profile variables. See https://docs.unity3d.com/Packages/com.unity.addressables@1.18/manual/AddressableAssetsProfiles.html. 
/// Therefore, the static variables 'HabitatAssetServer.Address' and 'HabitatAssetServer.Port' must be set before calling any addressables API.
/// See 'AddressablesEditor' for implementation.
/// </summary>
public static class HabitatAssetServer
{
    /// <summary>
    /// Address where to find the remote addressable content.
    /// Must be set before using any addressable API.
    /// </summary>
    public static string Address = "127.0.0.1";
    
    /// <summary>
    /// Port where to find the remote addressable content.
    /// Must be set before using any addressable API.
    /// </summary>
    public static string Port = "9999";
}
