using UnityEngine;

// 限制此组件必须挂载的依赖组件
// [RequireComponent(typeof(EnemyStatsManager))] // 敌人的属性管理器
// [RequireComponent(typeof(EnemyStateManager))] // 敌人的状态机管理器
// [RequireComponent(typeof(WaypointManager))] // 路径点管理器（巡逻用）
// [RequireComponent(typeof(Health))] // 血量组件
[AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/Enemy")] // 添加到组件菜单
public class Enemy : Entity<Enemy>
{
    /// <summary>
    /// 对敌人造成伤害，并根据血量状态触发相应事件
    /// </summary>
    /// <param name="amount">伤害值</param>
    /// <param name="origin">伤害来源位置</param>
    public override void ApplyDamage(int amount, Vector3 origin)
    {
    }
}