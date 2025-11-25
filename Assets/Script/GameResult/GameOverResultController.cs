using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class GameOverResultController : MonoBehaviour
{
    [Header("Result UI")]
    [SerializeField] private Transform resultsParent;       // Content ของ Vertical Layout
    [SerializeField] private PlayerResultUI playerResultPrefab;

    [Header("Character Sprites (เต็มตัว)")]
    [SerializeField] private Sprite[] characterFullBodySprites; // index ตรงกับ CharacterIndex

    [Header("Rank Badges")]
    [SerializeField] private Sprite rank1Badge;
    [SerializeField] private Sprite rank2Badge;
    [SerializeField] private Sprite rank3Badge;

    [Header("Lottery Popup")]
    [SerializeField] private LotteryPopupUI lotteryPopup;

    [Header("Lottery Prize (ควรให้เท่ากับ GameResultManager.lotteryPrize)")]
    [SerializeField] private int prizePerWinner = 6_000_000;

    [Header("Menu / Exit Button")]
    [SerializeField] private GameObject exitButtonRoot;     // ปุ่มออก/กลับเมนูในฉาก GameOver

    private readonly Dictionary<ulong, PlayerResultUI> uiByClientId =
        new Dictionary<ulong, PlayerResultUI>();

    private void Start()
    {
        // ตอนเข้า Scene ให้ซ่อนปุ่มออกไว้ก่อน
        if (exitButtonRoot != null)
            exitButtonRoot.SetActive(false);

        StartCoroutine(FlowRoutine());
    }

    private IEnumerator FlowRoutine()
    {
        // ✅ รอจนกว่า GameResultManager สร้าง LastResults เสร็จ
        while (GameResultManager.LastResults == null ||
               GameResultManager.LastResults.Count == 0)
        {
            Debug.Log("[ResultCtrl] Waiting for LastResults...");
            yield return null;
        }

        var list = new List<GameResultManager.PlayerFinalResult>(
            GameResultManager.LastResults);

        // เรียงจาก networth มาก -> น้อย
        list.Sort((a, b) => b.finalNetworth.CompareTo(a.finalNetworth));

        // ✅ ใช้เลขรางวัลที่ 1 จาก GameResultManager.LastWinningNumber เสมอ
        int winningTicket = GameResultManager.LastWinningNumber;
        Debug.Log($"[ResultCtrl] WinningTicket = {winningTicket:000000}");

        // ⭐ หา local player (ตาม LocalClientId ของเครื่องนี้)
        ulong localId = 0;
        bool hasNetMgr = (NetworkManager.Singleton != null);
        if (hasNetMgr)
            localId = NetworkManager.Singleton.LocalClientId;
        else
            Debug.LogWarning("[ResultCtrl] NetworkManager.Singleton == null on GameOver scene.");

        GameResultManager.PlayerFinalResult? myResult = null;
        if (hasNetMgr)
        {
            foreach (var data in list)
            {
                if (data.clientId == localId)
                {
                    myResult = data;
                    break;
                }
            }
        }

        // 1) สร้าง UI สรุปผล (ก่อนหวย) + วิ่งเลขก่อนหวย
        int rank = 1;
        foreach (var data in list)
        {
            Sprite avatar = null;
            if (data.characterIndex >= 0 &&
                data.characterIndex < characterFullBodySprites.Length)
            {
                avatar = characterFullBodySprites[data.characterIndex];
            }

            long cashAfter = (int)Math.Round(data.finalNetworth); ;
            long cashBefore = data.lotteryWin ? (cashAfter - prizePerWinner) : cashAfter;

            var ui = Instantiate(playerResultPrefab, resultsParent);
            ui.Setup(avatar, data.playerName, cashBefore, rank);

            uiByClientId[data.clientId] = ui;

            yield return ui.ShowWithSlideAndMoney(cashBefore);
            yield return new WaitForSeconds(0.3f);

            rank++;
        }

        // 2) ลุ้นหวย — "ทุกเครื่องต้องเห็น popup แน่นอน"
        if (lotteryPopup != null)
        {
            int ticketNumber = -1;
            long prize = 0;

            if (myResult.HasValue)
            {
                var mine = myResult.Value;
                ticketNumber = mine.hasLottery ? mine.lotteryNumber : -1;
                prize = mine.lotteryWin ? prizePerWinner : 0;

                Debug.Log($"[ResultCtrl] Lottery popup for {mine.playerName}, " +
                          $"hasTicket={mine.hasLottery}, ticket={ticketNumber}, prize={prize}");
            }
            else
            {
                Debug.LogWarning("[ResultCtrl] myResult == null → จะโชว์เฉพาะเลขรางวัลที่ 1, " +
                                 "ตั๋วผู้เล่นจะถูกซ่อนเพราะ ticketNumber = -1");
            }

            // ✅ เรียก popup เสมอ (ทุกเครื่องเห็นเลขที่ออกเหมือนกัน)
            yield return lotteryPopup.ShowTicketAndResult(
                ticketNumber: ticketNumber,
                winningNumber: winningTicket,
                prize: prize
            );
        }
        else
        {
            Debug.LogWarning("[ResultCtrl] lotteryPopup == null → อย่าลืมลากอ้างอิงใน Inspector");
        }

        // 3) อัปเดตเงินจากก่อนหวย -> หลังหวย
        foreach (var data in list)
        {
            if (!uiByClientId.TryGetValue(data.clientId, out var ui))
                continue;

            long cashAfter = (int)Math.Round(data.finalNetworth);
            yield return ui.UpdateMoneyTo(cashAfter);
            yield return new WaitForSeconds(0.2f);
        }

        // 4) ติดเหรียญ rank 1 / 2 / 3
        rank = 1;
        foreach (var data in list)
        {
            if (!uiByClientId.TryGetValue(data.clientId, out var ui))
            {
                rank++;
                continue;
            }

            if (rank == 1 && rank1Badge) ui.SetRankBadge(rank1Badge);
            else if (rank == 2 && rank2Badge) ui.SetRankBadge(rank2Badge);
            else if (rank == 3 && rank3Badge) ui.SetRankBadge(rank3Badge);

            rank++;
        }

        // ✅ สรุปผลเสร็จแล้ว → ค่อยให้กดปุ่มออก/กลับเมนูได้
        if (exitButtonRoot != null)
        {
            yield return new WaitForSeconds(2f);
            exitButtonRoot.SetActive(true);
        }
    }
}
