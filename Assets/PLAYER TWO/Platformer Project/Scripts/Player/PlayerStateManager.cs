using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))] // 强制要求该组件所在的物体必须挂有 Player 组件
public class PlayerStateManager : EntityStateManager<Player>
{
    /// <summary>
    /// 玩家状态类的字符串数组。
    /// 使用 ClassTypeName 特性，让 Unity Inspector 面板中可以通过下拉/输入选择继承自 PlayerState 的类。
    /// 
    /// 示例：
    /// states = { "IdlePlayerState", "RunPlayerState", "JumpPlayerState", "SpinPlayerState" }
    /// </summary>
    [ClassTypeName(typeof(PlayerState))]
    public string[] states;
    
    /// <summary>
    /// 重写基类方法，获取该玩家的状态列表。
    /// 会将 states 中的字符串类名数组转换为真正的状态对象列表。
    /// </summary>
    /// <returns>返回一个包含所有状态的 List<EntityState<Player>>。</returns>
    protected override List<EntityState<Player>> GetStateList()
    {
        // 调用 PlayerState 的辅助方法，把字符串数组转换为状态对象集合
        // 例如： "RunPlayerState" → new RunPlayerState()
        return PlayerState.CreateListFromStringArray(states);
    }
}