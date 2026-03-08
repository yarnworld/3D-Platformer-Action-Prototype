using UnityEngine;

public class IdlePlayerState : PlayerState
{
    /// <summary>
    /// 进入空闲状态时调用
    /// （此处留空，可用于播放空闲动画/音效）
    /// </summary>
    protected override void OnEnter(Player player) { }

    /// <summary>
    /// 离开空闲状态时调用
    /// （此处留空，可用于清理空闲状态效果）
    /// </summary>
    protected override void OnExit(Player player)
    {
        
    }

    /// <summary>
    /// 每帧更新空闲状态逻辑
    /// </summary>
    protected override void OnStep(Player player)
    {
        player.Gravity();               // 应用重力
        player.SnapToGround();          // 保持贴地
        player.Jump();                  // 允许跳跃
        player.Fall();                  // 检测下落
        player.Spin();                  // 检测旋转动作
        player.PickAndThrow();          // 检测拾取/投掷
        //根据输入要知道往那边走
        // 获取玩家输入方向
        var inputDirection = player.inputs.GetMovementDirection();
        //walk，输入操作---配置数据--速度，方向-----改变状态
        // 如果有移动输入或水平速度 > 0 → 切换到 Walk 状态
        if (inputDirection.sqrMagnitude > 0 || player.lateralVelocity.sqrMagnitude > 0)
        {
            player.states.Change<WalkPlayerState>();
        }
        // 如果按下下蹲/爬行 → 切换到 Crouch 状态
        else if (player.inputs.GetCrouchAndCraw())
        {
            player.states.Change<CrouchPlayerState>();
        }
    }

    /// <summary>
    /// 碰撞检测逻辑
    /// - 空闲状态通常不需要额外碰撞处理
    /// </summary>
    public override void OnContact(Player player, Collider other) { }
}