using ArmySurvivor.Army;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArmySurvivor.UI
{
    // ESC 한 키로 설정 창과 전투 일시정지를 연다/닫는다.
    // 다른 창(보급소 등)이 ESC로 먼저 닫히는 것과 겹치지 않게, 그 창들의 Update보다 먼저 실행해 열린 창이 있으면 양보한다.
    [DefaultExecutionOrder(-200)]
    public sealed class EscapeMenu : MonoBehaviour
    {
        [SerializeField] private SettingsPanel settings;
        [Tooltip("전투 씬에서만 연결. 전투 중 ESC는 설정 대신 일시정지 메뉴를 연다.")]
        [SerializeField] private BattleHUD battleHud;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            // 모험 모드는 마을 씬 위에 전투 씬을 추가로 불러온다. 지금 활성 씬의 메뉴만 ESC를 처리해 두 메뉴가 동시에 열리지 않게 한다.
            if (gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) return;
            if (settings.IsOpen) { settings.Close(); return; }
            // 튜토리얼 안내가 시간을 멈춘 중에 일시정지를 겹치면, 안내가 끝난 뒤 멈춘 시간으로 되돌아가 게임이 멈춘 것처럼 보인다.
            if (ArmySurvivor.Tutorial.TutorialManager.PausingGame) return;
            if (OtherWindowHandlesEscape()) return;
            if (battleHud != null && battleHud.HandleEscape()) return;
            settings.Open();
        }

        private static bool OtherWindowHandlesEscape()
        {
            var equipment = FindFirstObjectByType<ArmySurvivor.Equipment.EquipmentUI>();
            return equipment != null && equipment.IsOpen;
        }
    }
}
