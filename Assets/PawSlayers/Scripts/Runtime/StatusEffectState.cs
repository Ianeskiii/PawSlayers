using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawSlayers
{
    [Serializable]
    public class StatusEffectState
    {
        public int weak;
        public int vulnerable;
        public int strength;
        public int bleed;
        public int poison;
        public int taunt;
        public int stun;
        public int silence;

        public void AddWeak(int amount) => weak += Math.Max(0, amount);
        public void AddVulnerable(int amount) => vulnerable += Math.Max(0, amount);
        public void AddStrength(int amount) => strength += Math.Max(0, amount);
        public void AddBleed(int amount) => bleed += Math.Max(0, amount);
        public void AddPoison(int amount) => poison += Math.Max(0, amount);
        public void AddTaunt(int amount) => taunt += Math.Max(0, amount);
        public void AddStun(int amount) => stun += Math.Max(0, amount);
        public void AddSilence(int amount) => silence += Math.Max(0, amount);

        public void ClearAll()
        {
            weak = 0;
            vulnerable = 0;
            strength = 0;
            bleed = 0;
            poison = 0;
            taunt = 0;
            stun = 0;
            silence = 0;
        }

        public string GetStatusSummaryText()
        {
            List<string> parts = new List<string>();

            if (weak > 0) parts.Add($"Weak {weak}");
            if (vulnerable > 0) parts.Add($"Vuln {vulnerable}");
            if (strength > 0) parts.Add($"Str {strength}");
            if (bleed > 0) parts.Add($"Bleed {bleed}");
            if (poison > 0) parts.Add($"Poison {poison}");
            if (taunt > 0) parts.Add($"Taunt {taunt}");
            if (stun > 0) parts.Add($"Stun {stun}");
            if (silence > 0) parts.Add($"Silence {silence}");

            return string.Join(" | ", parts);
        }

        public bool HasAnyActiveStatus()
        {
            return weak > 0 ||
                vulnerable > 0 ||
                strength > 0 ||
                bleed > 0 ||
                poison > 0 ||
                taunt > 0 ||
                stun > 0 ||
                silence > 0;
        }

        public void TickEndOfTurnCore()
        {
            weak = Mathf.Max(0, weak - 1);
            vulnerable = Mathf.Max(0, vulnerable - 1);
            stun = Mathf.Max(0, stun - 1);
            silence = Mathf.Max(0, silence - 1);
        }
    }
}
