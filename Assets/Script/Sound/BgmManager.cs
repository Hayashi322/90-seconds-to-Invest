using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmManager : MonoBehaviour
{
    public static BgmManager Instance { get; private set; }

    [Header("Audio Source (ตัวเล่นเพลง)")]
    [SerializeField] private AudioSource audioSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip menuBgm;   // เพลง bg 1 (เมนู / ลอบบี้ / Leaderboard)
    [SerializeField] private AudioClip gameBgm;   // เพลง bg 2 (GameSceneNet)

    private enum BgmType { None, Menu, Game }
    private enum SceneGroup { None, MenuGroup, Game, GameOver }

    private BgmType currentType = BgmType.None;
    private SceneGroup lastGroup = SceneGroup.None;

    private void Awake()
    {
        // ทำเป็น Singleton + ไม่ถูกทำลายเมื่อเปลี่ยนซีน
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneGroup newGroup = GetSceneGroup(scene.name);

        // กลุ่มเมนู (MainMenu / LobbyScene / LeaderboardScene)
        if (newGroup == SceneGroup.MenuGroup)
        {
            // ถ้าตอนนี้ยังไม่ได้เล่นเพลงเมนู → เล่น
            // หรือเพิ่งมาจาก GameOver → บังคับเริ่มเพลงใหม่
            if (currentType != BgmType.Menu || lastGroup == SceneGroup.GameOver)
            {
                PlayMenuBgm(restart: true); // เริ่มใหม่จากต้นเพลง
            }
            // ถ้าเดิมก็อยู่ใน MenuGroup อยู่แล้ว → ปล่อยให้เล่นต่อ
        }
        // กลุ่มเกมหลัก
        else if (newGroup == SceneGroup.Game)
        {
            PlayGameBgm();
        }
        // GameOver → ปิดเพลงทั้งหมด
        else if (newGroup == SceneGroup.GameOver)
        {
            StopBgm();
        }

        lastGroup = newGroup;
    }

    // ====== ฟังก์ชันควบคุม BGM ======

    public void PlayMenuBgm(bool restart)
    {
        PlayClip(menuBgm, BgmType.Menu, restart);
    }

    public void PlayGameBgm()
    {
        // เกมซีนให้เริ่มเพลงใหม่ทุกครั้ง
        PlayClip(gameBgm, BgmType.Game, restart: true);
    }

    public void StopBgm()
    {
        if (audioSource)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        currentType = BgmType.None;
    }

    private void PlayClip(AudioClip clip, BgmType type, bool restart)
    {
        if (!clip || !audioSource) return;

        // ถ้าเพลงเดียวกันกำลังเล่นอยู่ และไม่ต้องการ restart → ไม่ทำอะไร
        if (!restart && audioSource.clip == clip && audioSource.isPlaying)
        {
            currentType = type;
            return;
        }

        audioSource.clip = clip;
        audioSource.time = 0f;       // เริ่มจากต้นเพลง
        audioSource.loop = true;
        audioSource.Play();

        currentType = type;
    }

    private SceneGroup GetSceneGroup(string sceneName)
    {
        // Debug ดูชื่อจริงเวลาโหลดก็ได้
        // Debug.Log("[BGM] Scene loaded: " + sceneName);

        switch (sceneName)
        {
            // ✅ กลุ่มเมนู
            case "MainMenu":
            case "LobbyScene":
            case "LeaderboardScene":
                return SceneGroup.MenuGroup;

            // ✅ เกมหลัก
            case "GameSceneNet":
                return SceneGroup.Game;

            // ✅ เกมโอเวอร์
            case "GameOver":
                return SceneGroup.GameOver;

            // NetBootstrap หรือซีนอื่น ๆ ไม่ต้องมี BGM
            default:
                return SceneGroup.None;
        }
    }
}
