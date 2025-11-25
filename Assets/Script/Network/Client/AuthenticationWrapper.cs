using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public enum AuthState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    TimeOut
}

public static class AuthenticationWrapper
{
    public static AuthState AuthState { get; private set; } = AuthState.NotAuthenticated;

    // กันไม่ให้ DoAuth() ถูกเรียกซ้อนพร้อมกันหลายครั้ง
    private static Task<AuthState> _currentAuthTask;

    public static Task<AuthState> DoAuth(int maxRetries = 5)
    {
        // ถ้าเคย sign-in แล้วก็รีเทิร์นเลย
        if (AuthState == AuthState.Authenticated)
            return Task.FromResult(AuthState);

        // ถ้ามี task Auth กำลังรันอยู่ ให้ใช้ task เดิม
        if (_currentAuthTask != null && !_currentAuthTask.IsCompleted)
            return _currentAuthTask;

        _currentAuthTask = DoAuthInternal(maxRetries);
        return _currentAuthTask;
    }

    private static async Task<AuthState> DoAuthInternal(int maxRetries)
    {
        AuthState = AuthState.Authenticating;

        try
        {
            // --- Initialize UGS ---
            if (UnityServices.State == ServicesInitializationState.Uninitialized ||
                UnityServices.State == ServicesInitializationState.Initializing)
            {
                await UnityServices.InitializeAsync();
            }

            // ถ้า sign-in อยู่แล้วไม่ต้องทำอะไร
            if (AuthenticationService.Instance.IsSignedIn &&
                AuthenticationService.Instance.IsAuthorized)
            {
                Debug.Log($"[Auth] Already signed in as {AuthenticationService.Instance.PlayerId}");
                AuthState = AuthState.Authenticated;
                return AuthState;
            }

            int retries = 0;

            while (retries < maxRetries &&
                   !(AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized))
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                catch (AuthenticationException ex)
                {
                    // เคสคลาสสิก: "The player is already signing in."
                    Debug.LogWarning($"[Auth] AuthenticationException: {ex.Message}");

                    // ถ้าข้อความบอกว่า already signing in ให้รอเฉย ๆ แล้วคอยเช็คสถานะ
                    if (ex.Message != null && ex.Message.Contains("already signing in"))
                    {
                        // รอให้การ sign-in ที่ไหนสักแห่งเสร็จก่อน
                        int waitLoops = 0;
                        while (!AuthenticationService.Instance.IsSignedIn &&
                               waitLoops < 20) // รอสูงสุด ~2 วินาที
                        {
                            await Task.Delay(100);
                            waitLoops++;
                        }

                        if (AuthenticationService.Instance.IsSignedIn &&
                            AuthenticationService.Instance.IsAuthorized)
                        {
                            AuthState = AuthState.Authenticated;
                            Debug.Log($"[Auth] Signed in (other caller). PlayerId={AuthenticationService.Instance.PlayerId}");
                            return AuthState;
                        }
                    }

                    AuthState = AuthState.Error;
                }
                catch (RequestFailedException ex)
                {
                    Debug.LogError($"[Auth] RequestFailedException: {ex}");
                    AuthState = AuthState.Error;
                }

                if (AuthenticationService.Instance.IsSignedIn &&
                    AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated;
                    Debug.Log($"[Auth] Signed in, playerId = {AuthenticationService.Instance.PlayerId}");
                    return AuthState;
                }

                retries++;
                await Task.Delay(1000);
            }

            if (AuthenticationService.Instance.IsSignedIn &&
                AuthenticationService.Instance.IsAuthorized)
            {
                AuthState = AuthState.Authenticated;
                Debug.Log($"[Auth] Signed in, playerId = {AuthenticationService.Instance.PlayerId}");
            }
            else
            {
                Debug.LogWarning($"[Auth] Player was not signed in successfully after {retries} retries");
                AuthState = AuthState.TimeOut;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Auth] Unexpected error: {e}");
            AuthState = AuthState.Error;
        }

        return AuthState;
    }
}
