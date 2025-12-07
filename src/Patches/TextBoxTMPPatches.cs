using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
public static class TextBoxTMP_Update
{
    // Postfix patch of TextBoxTMP.Update to allow copying from the chatbox + Ctrl+V + 실시간 편집 허용
    public static void Postfix(TextBoxTMP __instance)
    {
        if (!CheatToggles.chatJailbreak) return;
        if (!__instance.hasFocus) return;

        // 실시간 편집 허용: 텍스트 직접 수정 가능하게 함 (readOnly 우회)
        // characterLimit 없으므로 SetText 없이 무제한

        // Ctrl+C: 복사
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
        {
            ClipboardHelper.PutClipboardString(__instance.text);
        }

        // Ctrl+V: 붙여넣기 (inputField 없이 text 직접 추가 + 커서 끝 이동)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
        {
            string paste = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(paste))
            {
                __instance.text += paste;
                // 커서 끝으로 이동 (TextBoxTMP의 stringPosition 없으므로 간접 처리: 다음 Update에서 자연 이동)
            }
        }
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
public static class TextBoxTMP_IsCharAllowed
{
    // Prefix patch of TextBoxTMP.IsCharAllowed to allow all characters (한글 IME 완벽 지원)
    public static bool Prefix(TextBoxTMP __instance, char i, ref bool __result)
    {
        if (!CheatToggles.chatJailbreak) return true; // 원본 실행

        // IME 필수 제어 문자: 한글 조합 중 백스페이스/엔터 무조건 허용 (기존 블록 해제!)
        if (i == '\b' || i == '\r' || i == '\n' || i == '\t')
        {
            __result = true;
            return false;
        }

        // 한글 IME 조합 전용: 자모(초/중/종성) + 완성형 범위 무조건 허용
        if ((i >= 0x1100 && i <= 0x11FF) ||  // 한글 자음/모음 (조합용)
            (i >= 0x3130 && i <= 0x318F) ||  // 호환 자모
            (i >= 0xAC00 && i <= 0xD7AF))    // 완성형 한글 음절
        {
            __result = true;
            return false;
        }

        // 기존 블록 문자 제거: > < ] [ 만 블록 (문제 발생 문자만)
        // 러시아/기타 언어 + 일반 문자 지원 유지
        if (i == '>' || i == '<' || i == ']' || i == '[')
        {
            __result = false;
            return false;
        }

        // C1 제어 + 일반 문자 전부 허용 (안전)
        if ((i >= 0x7F && i <= 0x9F) ||  // IME 중간 상태
            char.IsLetterOrDigit(i) || 
            char.IsWhiteSpace(i) || 
            char.IsPunctuation(i) || 
            char.IsSymbol(i) || 
            i <= 0x1F)
        {
            __result = true;
            return false;
        }

        // 나머지 전부 허용
        __result = true;
        return false;
    }
}
