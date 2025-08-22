using UnityEngine;

public class WalkPlayerState : PlayerState
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
    protected override void OnExit(Player player) { }

    /// <summary>
    /// 每帧更新空闲状态逻辑
    /// </summary>
    protected override void OnStep(Player player)
    {
        // 获取玩家输入方向（相机方向）
        var inputDirection = player.inputs.GetMovementCameraDirection();

        if (inputDirection.sqrMagnitude > 0)
        {
            // 输入方向与当前水平速度的点乘，用于判断刹车阈值
            var dot = Vector3.Dot(inputDirection, player.lateralVelocity);
        
            if (dot >= player.stats.current.brakeThreshold)
            {
                // 超过刹车阈值 → 正常加速与面向方向
                player.Accelerate(inputDirection);
                //player.FaceDirectionSmooth(player.lateralVelocity);
            }
        }
    }
    
    /// <summary>
    /// 碰撞检测逻辑
    /// - 空闲状态通常不需要额外碰撞处理
    /// </summary>
    public override void OnContact(Player player, Collider other) { }
}