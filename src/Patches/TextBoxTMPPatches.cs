using HarmonyLib;
using UnityEngine;
using TMPro; // TMP_InputField 접근

namespace MalumMenu;

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
public static class TextBoxTMP_Update_Postfix
{
    public static void Postfix(TextBoxTMP __instance)
    {
        if (!CheatToggles.chatJailbreak || !__instance.hasFocus) return;

        // Ctrl+C: 복사
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
        {
            ClipboardHelper.PutClipboardString(__instance.text);
        }

        // Ctrl+V: 붙여넣기
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
        {
            string paste = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(paste))
            {
                __instance.text += paste;
                if (__instance.inputField != null)
                {
                    __instance.inputField.stringPosition = __instance.text.Length;
                    __instance.inputField.MoveTextEnd(false);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
public static class TextBoxTMP_IsCharAllowed_Prefix
{
    public static bool Prefix(char i, ref bool __result)
    {
        if (!CheatToggles.chatJailbreak) return true; // 원본 실행

        // IME 필수 제어 문자: 한글 조합 중 백스페이스/엔터 무조건 허용 (이게 핵심!)
        if (i == '\b' || i == '\r' || i == '\n' || i == '\t')
        {
            __result = true;
            return false;
        }

        // 한글 IME 조합 전용: 자모(초/중/종성) + 완성형 범위 무조건 허용
        if ((i >= 0x1100 && i <= 0x11FF) ||  // 한글 자음/모음 (조합용)
            (i >= 0x3130 && i <= 0x318F) ||  // 호환 자모
            (i >= 0xAC00 && i <= 0xD7AF) ||  // 완성형 한글 음절
            (i >= 0x7F && i <= 0x9F))        // C1 제어 문자 (IME 중간 상태)
        {
            __result = true;
            return false;
        }

        // 일반 문자 + 러시아/기타 언어 지원 (기존 픽스 유지)
        if (char.IsLetterOrDigit(i) || char.IsWhiteSpace(i) || char.IsPunctuation(i) || char.IsSymbol(i) || i <= 0x1F)
        {
            __result = true;
            return false;
        }

        // 안전: 나머지 전부 허용
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
public static class TextBoxTMP_SetText_Prefix
{
    public static void Prefix(TextBoxTMP __instance)
    {
        if (CheatToggles.chatJailbreak)
            __instance.characterLimit = 0; // 무제한 입력
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.OnEnable))]
public static class TextBoxTMP_OnEnable_Postfix
{
    public static void Postfix(TextBoxTMP __instance)
    {
        if (CheatToggles.chatJailbreak && __instance.inputField != null)
        {
            __instance.inputField.readOnly = false;
            __instance.readOnly = false;
            __instance.characterLimit = 0;
        }
    }
}
