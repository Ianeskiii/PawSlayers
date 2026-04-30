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

            if (Input.GetKeyDown(KeyCode.B))
            {
                ForceBossBattle();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                DamageBossHalf();
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                KillBoss();
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                AddWeakToEnemy1();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                AddVulnerableToEnemy1();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                AddStrengthToHero1();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                AddPoisonToEnemy1();
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                AddBleedToHero1();
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                StunHero1();
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                SilenceHero1();
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                StunEnemy1();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                SilenceEnemy1();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                ClearAllStatuses();
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

        public void ForceBossBattle()
        {
            battleUiManager.ForceBossBattleDebug();
        }

        public void DamageBossHalf()
        {
            battleUiManager.DamageBossHalfDebug();
        }

        public void KillBoss()
        {
            battleUiManager.KillBossDebug();
        }

        public void AddWeakToEnemy1()
        {
            battleUiManager.AddWeakToEnemy1Debug();
        }

        public void AddVulnerableToEnemy1()
        {
            battleUiManager.AddVulnerableToEnemy1Debug();
        }

        public void AddStrengthToHero1()
        {
            battleUiManager.AddStrengthToHero1Debug();
        }

        public void AddPoisonToEnemy1()
        {
            battleUiManager.AddPoisonToEnemy1Debug();
        }

        public void AddBleedToHero1()
        {
            battleUiManager.AddBleedToHero1Debug();
        }

        public void StunHero1()
        {
            battleUiManager.StunHero1Debug();
        }

        public void SilenceHero1()
        {
            battleUiManager.SilenceHero1Debug();
        }

        public void StunEnemy1()
        {
            battleUiManager.StunEnemy1Debug();
        }

        public void SilenceEnemy1()
        {
            battleUiManager.SilenceEnemy1Debug();
        }

        public void ClearAllStatuses()
        {
            battleUiManager.ClearAllStatusesDebug();
        }
    }
}
