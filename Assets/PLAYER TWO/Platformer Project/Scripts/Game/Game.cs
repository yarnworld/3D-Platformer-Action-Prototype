using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 游戏核心管理类，单例模式。
/// 负责游戏的整体状态管理，如关卡列表、重试次数、保存与加载等。
/// </summary>
[AddComponentMenu("PLAYER TWO/Platformer Project/Game/Game")]
public class Game : Singleton<Game>
{
    /// <summary>
    /// 设置鼠标指针的锁定和显示状态。
    /// 仅在 Standalone 和 WebGL 平台生效。
    /// </summary>
    /// <param name="value">为 true 时隐藏并锁定光标，为 false 时显示并释放锁定。</param>
    public static void LockCursor(bool value = true)
    {
#if UNITY_STANDALONE || UNITY_WEBGL
        Cursor.visible = value;
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
#endif
    }
}