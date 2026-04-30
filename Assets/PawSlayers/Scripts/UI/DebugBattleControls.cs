using UnityEngine;

namespace PawSlayers
{
    public class DebugBattleControls : MonoBehaviour
    {
        public BattleUIManager battleUiManager;
        public RunManager runManager;

        private void Start()
        {
            if (runManager == null)
            {
                runManager = RunManager.Instance;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                KillHeroSlot(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                KillHeroSlot(1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                KillHeroSlot(2);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                HealAllHeroes();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                DrawCard();
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                WinBattle();
            }
        }

        public void KillHeroSlot(int index)
        {
            if (runManager == null || index < 0 || index >= runManager.ActiveHeroesRuntime.Count)
            {
                return;
            }

            runManager.MarkHeroDown(runManager.ActiveHeroesRuntime[index].heroData.heroId);
            battleUiManager.RefreshAllUi();
        }

        public void HealAllHeroes()
        {
            runManager.HealAllHeroes();
            battleUiManager.RefreshAllUi();
        }

        public void DrawCard()
        {
            battleUiManager.DrawOneCard();
        }

        public void WinBattle()
        {
            battleUiManager.WinBattle();
        }
    }
}
