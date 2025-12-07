using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TMP_InputField 접근을 위해 필요할 수 있음

namespace MalumMenu;

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
public static class TextBoxTMP_Update
{
    /// <summary>
    /// Postfix patch of TextBoxTMP.Update to allow Ctrl+C copy and Ctrl+V paste
    /// </summary>
    public static void Postfix(TextBoxTMP __instance)
    {
        if (!CheatToggles.chatJailbreak) return;
        if (!__instance.hasFocus) return;

        // Ctrl+C: Copy text to clipboard
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
        {
            ClipboardHelper.PutClipboardString(__instance.text);
            return;
        }

        // Ctrl+V: Paste from clipboard (GUIUtility.systemCopyBuffer 사용)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
        {
            string pasteText = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(pasteText))
            {
                __instance.text += pasteText;
                __instance.inputField.stringPosition = __instance.text.Length; // 커서 끝으로 이동
                __instance.inputField.MoveTextEnd(false); // TMP_InputField 커서 고정
            }
            return;
        }
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
public static class TextBoxTMP_IsCharAllowed
{
    /// <summary>
    /// Prefix patch: 모든 문자 허용 (한글 조합 완벽 지원 + 일본어/중국어 IME)
    /// </summary>
    public static bool Prefix(TextBoxTMP __instance, char i, ref bool __result)
    {
        if (!CheatToggles.chatJailbreak) return true; // 원본 실행

        // 필수 제어 문자 허용 (백스페이스, 엔터, 탭 등)
        if (i == '\b' || i == '\r' || i == '\n' || i == '\t')
        {
            __result = true;
            return false;
        }

        // IME 조합용 한글 자모 (초성/중성/종성 완전 범위)
        if ((i >= 0x1100 && i <= 0x11FF) ||  // 한글 자음/모음
            (i >= 0x3131 && i <= 0x318F) ||  // 한글 호환 자모
            (i >= 0xAC00 && i <= 0xD7AF))    // 완성형 한글
        {
            __result = true;
            return false;
        }

        // 일반 문자 + 공백 + 기호 + 숫자 + 제어 문자 전체 허용
        if (char.IsLetterOrDigit(i) ||
            char.IsWhiteSpace(i) ||
            char.IsPunctuation(i) ||
            char.IsSymbol(i) ||
            i <= 0x1F ||  // C0 제어 문자
            (i >= 0x7F && i <= 0x9F))        // C1 제어 문자 (IME 필수)
        {
            __result = true;
            return false;
        }

        // 그 외 모든 문자 무조건 허용 (안전)
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
public static class TextBoxTMP_SetText
{
    /// <summary>
    /// Prefix: 채팅 길이 제한 해제 (1000자 이상 입력 가능)
    /// </summary>
    public static void Prefix(TextBoxTMP __instance)
    {
        if (CheatToggles.chatJailbreak)
        {
            __instance.characterLimit = 0; // 무제한 (또는 10000 등으로 설정)
        }
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Awake))]
public static class TextBoxTMP_Awake
{
    /// <summary>
    /// Awake에서 초기화: inputField 읽기 전용 해제 및 읽기모드 OFF
    /// </summary>
    public static void Postfix(TextBoxTMP __instance)
    {
        if (CheatToggles.chatJailbreak && __instance.inputField != null)
        {
            __instance.inputField.readOnly = false;
            __instance.readOnly = false;
        }
    }
}
