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

    // ใช้ cache Task เพื่อกันการเรียกซ้ำพร้อมกัน
    private static Task<AuthState> _authTask;

    /// <summary>
    /// ให้ทุกที่ในโปรเจกต์เรียกเมธอดนี้เท่านั้นเวลาอยากให้ Auth
    /// </summary>
    public static Task<AuthState> DoAuth(int maxRetries = 5)
    {
        // ถ้าเคย Auth สำเร็จแล้วก็คืนค่าเลย
        if (AuthState == AuthState.Authenticated)
        {
            return Task.FromResult(AuthState);
        }

        // ถ้ามี Task เก่าที่กำลังรันอยู่ ให้รออันเดิม
        if (_authTask == null || _authTask.IsCompleted)
        {
            _authTask = DoAuthInternal(maxRetries);
        }

        return _authTask;
    }

    // ===== ภายใน =====

    private static async Task<AuthState> DoAuthInternal(int maxRetries)
    {
        // กันไม่ให้หลายที่เรียกพร้อมกันโดยไม่จำเป็น
        if (AuthState == AuthState.Authenticated)
            return AuthState;

        // 1) Initialize Unity Services ถ้ายังไม่เคยทำ
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        // 2) ถ้ามีคนอื่น sign-in ไปแล้ว ให้ใช้สถานะนั้นเลย
        if (AuthenticationService.Instance.IsSignedIn &&
            AuthenticationService.Instance.IsAuthorized)
        {
            AuthState = AuthState.Authenticated;
            Debug.Log("[AuthWrapper] Already signed in (from other system).");
            return AuthState;
        }

        AuthState = AuthState.Authenticating;

        int retries = 0;

        while (retries < maxRetries && AuthState == AuthState.Authenticating)
        {
            try
            {
                // ถ้ายังไม่ signed-in จริง ๆ ค่อยเรียก SignIn
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log($"[AuthWrapper] SignIn attempt {retries + 1} ...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                if (AuthenticationService.Instance.IsSignedIn &&
                    AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated;
                    Debug.Log($"[AuthWrapper] Signed in. PlayerId = {AuthenticationService.Instance.PlayerId}");
                    break;
                }
            }
            catch (AuthenticationException ex)
            {
                // เคสสำคัญ: Player is already signing in → แปลว่ามีคนอื่นเรียกอยู่แล้ว
                if (ex.Message != null &&
                    ex.Message.ToLower().Contains("already signing in"))
                {
                    Debug.LogWarning("[AuthWrapper] Already signing in somewhere else, waiting...");

                    // รอให้การ sign-in ที่อื่นเสร็จแทน
                    int waitMs = 0;
                    while (!AuthenticationService.Instance.IsSignedIn &&
                           waitMs < 5000)   // รอสูงสุด ~5 วิ
                    {
                        await Task.Delay(200);
                        waitMs += 200;
                    }

                    if (AuthenticationService.Instance.IsSignedIn &&
                        AuthenticationService.Instance.IsAuthorized)
                    {
                        AuthState = AuthState.Authenticated;
                        Debug.Log("[AuthWrapper] Sign-in finished while waiting.");
                        break;
                    }
                }
                else
                {
                    Debug.LogError(ex);
                    AuthState = AuthState.Error;
                }
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError(ex);
                AuthState = AuthState.Error;
            }

            if (AuthState == AuthState.Authenticated)
                break;

            retries++;
            await Task.Delay(1000);
        }

        // สรุปผลรอบสุดท้าย
        if (AuthState != AuthState.Authenticated)
        {
            if (AuthenticationService.Instance.IsSignedIn &&
                AuthenticationService.Instance.IsAuthorized)
            {
                AuthState = AuthState.Authenticated;
            }
            else
            {
                Debug.LogWarning($"[AuthWrapper] Player was not signed in successfully after {retries} retries");
                AuthState = AuthState.TimeOut;
            }
        }

        return AuthState;
    }
}
