using UnityEngine;

public class Player : Entity<Player> // 继承自通用的 Entity<Player> 基类
{
    /// <summary> 玩家输入管理器实例 </summary>
    public PlayerInputManager inputs { get; protected set; }

    /// <summary> 玩家数值管理器实例 </summary>
    public PlayerStatsManager stats { get; protected set; }
    
    protected override void Awake()
    {
        base.Awake();
        InitializeInputs();
        InitializeStats();
    }

    // 初始化输入
    protected virtual void InitializeInputs() => inputs = GetComponent<PlayerInputManager>();
    // 初始化数值
    protected virtual void InitializeStats() => stats = GetComponent<PlayerStatsManager>();
    
    /// <summary>
    /// 在指定方向上平滑移动玩家（加速度控制）
    /// </summary>
    public virtual void Accelerate(Vector3 direction)
    {
        // 根据是否按下 Run 键、是否在地面，决定不同的转向阻尼与加速度
        // var turningDrag = isGrounded && inputs.GetRun() ? stats.current.runningTurningDrag : stats.current.turningDrag;
        // var acceleration = isGrounded && inputs.GetRun() ? stats.current.runningAcceleration : stats.current.acceleration;
        // var finalAcceleration = isGrounded ? acceleration : stats.current.airAcceleration; // 空中与地面不同
        // var topSpeed = inputs.GetRun() ? stats.current.runningTopSpeed : stats.current.topSpeed;
        
        var turningDrag = stats.current.turningDrag;
        var acceleration = stats.current.acceleration;
        var finalAcceleration = acceleration;
        var topSpeed = stats.current.topSpeed;

        // 调用底层 Accelerate(方向, 转向阻尼, 加速度, 最大速度)
        Accelerate(direction, turningDrag, finalAcceleration, topSpeed);
        
    }
}