using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

// 1. Update → Ctrl+C / Ctrl+V
[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
public static class TextBoxTMP_Update_Postfix
{
    public static void Postfix(TextBoxTMP __instance)
    {
        if (!CheatToggles.chatJailbreak || !__instance.hasFocus) return;

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                ClipboardHelper.PutClipboardString(__instance.text);
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                string paste = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(paste))
                {
                    __instance.text += paste;
                    __instance.inputField.stringPosition = __instance.text.Length;
                    __instance.inputField.MoveTextEnd(false);
                }
            }
        }
    }
}

// 2. 모든 문자 허용 (한글 완벽 조합)
[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
public static class TextBoxTMP_IsCharAllowed_Prefix
{
    public static bool Prefix(char i, ref bool __result)
    {
        if (!CheatToggles.chatJailbreak) return true;

        // 백스페이스, 엔터, 탭은 무조건 허용
        if (i == '\b' || i == '\r' || i == '\n' || i == '\t')
        {
            __result = true;
            return false;
        }

        // 한글 자모 + 완성형 + IME 중간 문자 전부 허용
        if ((i >= 0x1100 && i <= 0x11FF) ||   // 조합용 자모
            (i >= 0x3130 && i <= 0x318F) ||   // 호환 자모
            (i >= 0xAC00 && i <= 0xD7AF))     // 완성형 한글
        {
            __result = true;
            return false;
        }

        // 나머지는 전부 허용 (제일 간단하고 확실한 방법)
        __result = true;
        return false;
    }
}

// 3. 글자 수 제한 해제
[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
public static class TextBoxTMP_SetText_Prefix
{
    public static void Prefix(TextBoxTMP __instance)
    {
        if (CheatToggles.chatJailbreak)
            __instance.characterLimit = 0; // 무제한
    }
}

// 4. 읽기전용 강제 해제 (Awake 대신 OnEnable 사용!)
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
