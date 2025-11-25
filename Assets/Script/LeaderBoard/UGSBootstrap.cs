using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UGSBootstrap : MonoBehaviour
{
    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);
        await InitServicesAsync();
    }

    private async Task InitServicesAsync()
    {
        try
        {
            // เริ่ม Unity Gaming Services
            await UnityServices.InitializeAsync();

            // ถ้ายังไม่ Login → ใช้ Anonymous sign-in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[UGS] Signed in, playerId = {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UGS] Init failed : {e}");
        }
    }
}
