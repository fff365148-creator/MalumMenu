using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
public static class TextBoxTMP_Update
{
    public static void Postfix(TextBoxTMP __instance)
    {
        if (!CheatToggles.chatJailbreak) return;
        if (!__instance.hasFocus) return;

        // Ctrl+C: 복사
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
        {
            ClipboardHelper.PutClipboardString(__instance.text);
        }

        // Ctrl+V: 붙여넣기 (네모 없는 텍스트만 추가)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
        {
            string paste = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(paste))
            {
                // 네모 문자 필터링: □ (U+25A1) 제거 후 추가
                paste = paste.Replace("\u25A1", "").Replace("\ufffd", ""); // □ & � (replacement char) 제거
                __instance.text += paste;
            }
        }

        // IME 지연 우회: 포커스 시 텍스트 강제 업데이트 (네모 방지)
        if (Input.inputString.Length > 0) // 입력 이벤트 감지 시
        {
            __instance.ForceMeshUpdate(); // TMP 강제 리렌더 (네모 원인: 폰트 캐시 미업데이트)
        }
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
public static class TextBoxTMP_IsCharAllowed
{
    public static bool Prefix(TextBoxTMP __instance, char i, ref bool __result)
    {
        if (!CheatToggles.chatJailbreak) return true;

        // IME 필수 제어 문자: 한글 조합 중 백스페이스/엔터 무조건 허용 (입력 안 됨 해결!)
        if (i == '\b' || i == '\r' || i == '\n' || i == '\t')
        {
            __result = true;
            return false;
        }

        // 한글 IME 조합 전용: 자모 + 완성형 + 중간 상태 무조건 허용 (네모 방지 핵심!)
        if ((i >= 0x1100 && i <= 0x11FF) ||      // 한글 자음/모음 (초/중/종성 – 네모 원인 1위)
            (i >= 0x3130 && i <= 0x318F) ||      // 호환 자모
            (i >= 0xAC00 && i <= 0xD7AF) ||      // 완성형 한글 음절
            (i >= 0x7F && i <= 0x9F) ||          // C1 제어 (IME 지연)
            (i >= 0xD800 && i <= 0xDBFF))        // Surrogate (Unicode 조합)
        {
            __result = true;
            return false;
        }

        // 기존 블록 문자 유지 (> < ] [ 만)
        if (i == '>' || i == '<' || i == ']' || i == '[')
        {
            __result = false;
            return false;
        }

        // 일반 + 러시아/기타 언어 지원 (네모 아닌 문자만)
        if (char.IsLetterOrDigit(i) || char.IsWhiteSpace(i) || char.IsPunctuation(i) || char.IsSymbol(i) || i <= 0x1F)
        {
            __result = true;
            return false;
        }

        // 안전: 나머지 허용 (네모 필터링은 Update에서)
        __result = true;
        return false;
    }
}

// TMP 렌더링 패치: 네모 방지 (TextMeshProUGUI 강제 폰트 업데이트)
[HarmonyPatch(typeof(TextMeshProUGUI), nameof(TextMeshProUGUI.ForceMeshUpdate))]
public static class TextMeshProUGUI_ForceMeshUpdate
{
    public static void Prefix(TextMeshProUGUI __instance)
    {
        if (CheatToggles.chatJailbreak && __instance.text.Contains("\u25A1")) // 네모 감지 시
        {
            // 폰트 재로드를 시도 (Among Us 기본 폰트: Noto Sans KR 지원 가정)
            __instance.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // 대체 폰트 (한글 지원)
            __instance.ForceMeshUpdate(true); // 즉시 업데이트
        }
    }
}
