using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ArmySurvivor.Saving;

namespace ArmySurvivor.Army
{
    // 재도전권(기획서 8) 확인·재도전 창. 패널과 문구는 PlayScene에 배치하고 여기서는 동작만 연결한다.
    public class CampaignRetry : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text message;
        [SerializeField] private TMP_Text tokens;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelLabel;
        [SerializeField] private string villageSceneName = "VillageScene";
        [SerializeField] private string startSceneName = "StartScene";

        [Header("중도 복귀 확인")]
        [SerializeField] private string retreatTitle = "중도 복귀";
        [SerializeField, TextArea] private string retreatMessage = "재도전권 1장을 사용하고\nDay {0} 아침으로 돌아갑니다.";
        [SerializeField, TextArea] private string retreatLastMessage = "남은 재도전권이 없습니다.\n복귀하면 이번 회차가 종료됩니다.";
        [SerializeField] private string retreatConfirm = "복귀";
        [SerializeField] private string retreatCancel = "계속 싸우기";

        [Header("패배 후 재도전")]
        [SerializeField] private string retryTitle = "전멸";
        [SerializeField] private string retreatDoneTitle = "중도 복귀";
        [SerializeField, TextArea] private string retryMessage = "재도전권 1장을 사용했습니다.\nDay {0} 아침부터 다시 시작합니다.";
        [SerializeField] private string retryConfirm = "다시 도전";
        [SerializeField] private string retryCancel = "시작 화면으로";

        [SerializeField] private string tokensFormat = "남은 재도전권 {0} / {1}";

        private Action confirmAction;
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Cancel);
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Cancel);
        }

        public string TokensLabel(int remaining) => string.Format(tokensFormat, remaining, CampaignState.MaxRetryTokens);

        // 일시정지 메뉴의 '준비로' 버튼. 확인 시 onConfirm(재도전권 소모)을 실행한다.
        public void ShowRetreatConfirm(int day, Action onConfirm)
        {
            int remaining = CampaignState.RetryTokens;
            Open(retreatTitle, remaining > 0 ? string.Format(retreatMessage, day) : retreatLastMessage,
                remaining, retreatConfirm, retreatCancel, onConfirm, null);
        }

        // 패배 직후. 재도전권은 이미 파일에 기록되어 있으므로 취소해도 되돌아오지 않는다.
        public void ShowRetry(int day, int remaining, bool retreat)
        {
            Open(retreat ? retreatDoneTitle : retryTitle, string.Format(retryMessage, day), remaining, retryConfirm, retryCancel,
                LoadMorning, ReturnToStart);
        }

        private Action cancelAction;

        private void Open(string titleText, string messageText, int remaining, string confirmText, string cancelText,
            Action onConfirm, Action onCancel)
        {
            title.text = titleText;
            message.text = messageText;
            tokens.text = TokensLabel(remaining);
            confirmLabel.text = confirmText;
            cancelLabel.text = cancelText;
            confirmAction = onConfirm;
            cancelAction = onCancel;
            confirmButton.interactable = cancelButton.interactable = true;
            // 일시정지 메뉴 위에 보이도록 마지막 순서로 올린다.
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        private void Confirm()
        {
            var action = confirmAction;
            confirmAction = cancelAction = null;
            panel.SetActive(false);
            action?.Invoke();
        }

        private void Cancel()
        {
            var action = cancelAction;
            confirmAction = cancelAction = null;
            panel.SetActive(false);
            action?.Invoke();
        }

        private void LoadMorning()
        {
            if (GameSave.LoadAutoSave(villageSceneName) == GameSave.ContinueResult.Missing)
            {
                Debug.LogError("자동 저장을 불러오지 못해 시작 화면으로 돌아갑니다.", this);
                ReturnToStart();
            }
        }

        private void ReturnToStart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
        }
    }
}
