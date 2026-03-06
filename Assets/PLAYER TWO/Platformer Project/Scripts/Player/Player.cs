using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Entity<Player>
{
    public PlayerEvents playerEvents; // 玩家事件（受伤、死亡、拾取物品等触发的事件）
    /// <summary> 玩家输入管理器实例 </summary>
    public PlayerInputManager inputs { get; protected set; }

    /// <summary> 玩家数值管理器实例 </summary>
    public PlayerStatsManager stats { get; protected set; }

    /// <summary> 玩家已跳跃的次数（用于多段跳） </summary>
    public int jumpCounter { get; protected set; }

    /// <summary> 玩家已进行的空中冲刺次数 </summary>
    public int airDashCounter { get; protected set; }

    /// <summary> 上一次冲刺的时间 </summary>
    public float lastDashTime { get; protected set; }

    /// <summary> 玩家已进行的空中旋转次数 </summary>
    public int airSpinCounter { get; protected set; }

    /// <summary> 玩家是否在水中 </summary>
    public bool onWater { get; protected set; }

    /// <summary> 生命值实例 </summary>
    public Health health { get; protected set; }


    // 皮肤初始位置与旋转（用于恢复外观）
    protected Vector3 m_skinInitialPosition = Vector3.zero;
    protected Quaternion m_skinInitialRotation = Quaternion.identity;

    public Transform skin;             // 玩家角色皮肤（外观的 Transform，用于重置姿态）

    /// <summary> 玩家是否正在持有物品 </summary>
    public bool holding { get; protected set; }

    /// <summary> 玩家当前攀爬的 Pole（竿子/杆子） </summary>
    public Pole pole { get; protected set; }

    /// <summary> 玩家最后接触到的墙面的法线（用于墙跳等逻辑） </summary>
    public Vector3 lastWallNormal { get; protected set; }

    // 玩家从水中出来时的微小偏移
    protected const float k_waterExitOffset = 0.25f;

    /// <summary> 玩家当前所在的水域碰撞体 </summary>
    public Collider water { get; protected set; }

    protected override void Awake()
    {
        base.Awake();
        InitializeInputs();
        InitializeStats();
        InitializeHealth();
        InitializeTag();
        // 监听落地事件，重置跳跃/空中技能次数
        entityEvents.OnGroundEnter.AddListener(() =>
        {
            ResetJumps();
            ResetAirDash();
            ResetAirSpins();
        });

        // 监听进入轨道事件，重置空中技能并进入滑轨状态
        entityEvents.OnRailsEnter.AddListener(() =>
        {
            ResetJumps();
            ResetAirSpins();
            //ResetAirDash();
        });
    }

    // 初始化输入
    protected virtual void InitializeInputs() => inputs = GetComponent<PlayerInputManager>();
    // 初始化数值
    protected virtual void InitializeStats() => stats = GetComponent<PlayerStatsManager>();

    // 初始化生命
    protected virtual void InitializeHealth() => health = GetComponent<Health>();

    // 初始化标签（标记为 Player）
    protected virtual void InitializeTag() => tag = GameTags.Player;
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
    /// <summary>
    /// 根据相机方向来平滑移动玩家
    /// </summary>
    public virtual void AccelerateToInputDirection()
    {
        var inputDirection = inputs.GetMovementCameraDirection(); // 输入相对于相机的方向
        Accelerate(inputDirection);
    }
    /// <summary>
    /// 平滑朝向某个方向旋转（陆地旋转速度）
    /// </summary>
    public virtual void FaceDirectionSmooth(Vector3 direction) => FaceDirection(direction, stats.current.rotationSpeed);
    /// <summary>
    /// 平滑减速（使用 deceleration 参数）
    /// </summary>
    public virtual void Decelerate() => Decelerate(stats.current.deceleration);

    /// <summary>
    /// 平滑减速（使用摩擦力参数）
    /// </summary>
    public virtual void Friction()
    {
        if (OnSlopingGround())
            Decelerate(stats.current.slopeFriction); // 在斜坡上使用斜坡摩擦
        else
            Decelerate(stats.current.friction);      // 普通摩擦
    }
    /// <summary>
    /// 在指定方向上平滑移动玩家（匍匐状态的参数）
    /// </summary>
    public virtual void CrawlingAccelerate(Vector3 direction) =>
        Accelerate(direction, stats.current.crawlingTurningSpeed, stats.current.crawlingAcceleration,
            stats.current.crawlingTopSpeed);
    /// <summary>
    /// 在空翻动作中平滑移动玩家（后空翻参数）
    /// </summary>
    public virtual void BackflipAcceleration()
    {
        var direction = inputs.GetMovementCameraDirection();
        Accelerate(direction, stats.current.backflipTurningDrag, stats.current.backflipAirAcceleration,
            stats.current.backflipTopSpeed);
    }
    /// <summary>
    /// 在指定方向上平滑移动玩家（水下的参数）
    /// </summary>
    public virtual void WaterAcceleration(Vector3 direction) =>
        Accelerate(direction, stats.current.waterTurningDrag, stats.current.swimAcceleration, stats.current.swimTopSpeed);

    /// <summary>
    /// 冲刺（包括地面冲刺和空中冲刺）
    /// </summary>
    public virtual void Dash()
    {
        // 是否可以空中冲刺
        var canAirDash = stats.current.canAirDash && !isGrounded &&
                         airDashCounter < stats.current.allowedAirDashes;

        // 是否可以地面冲刺（冷却结束）
        var canGroundDash = stats.current.canGroundDash && isGrounded &&
                            Time.time - lastDashTime > stats.current.groundDashCoolDown;
        Debug.Log(inputs.GetDashDown());
        // 如果按下冲刺键，且符合条件
        if (inputs.GetDashDown() && (canAirDash || canGroundDash))
        {
            if (!isGrounded) airDashCounter++; // 空中冲刺计数+1
            lastDashTime = Time.time; // 记录冲刺时间
            states.Change<DashPlayerState>(); // 切换到冲刺状态
        }
    }
    /// <summary>
    /// 施加重力，使玩家下落
    /// </summary>
    public virtual void Gravity()
    {
        if (!isGrounded && verticalVelocity.y > -stats.current.gravityTopSpeed)
        {
            var speed = verticalVelocity.y;
            // 上升时用普通重力，下落时用更强的下落重力
            var force = verticalVelocity.y > 0 ? stats.current.gravity : stats.current.fallGravity;
            speed -= force * gravityMultiplier * Time.deltaTime;

            // 限制最大下落速度
            speed = Mathf.Max(speed, -stats.current.gravityTopSpeed);
            verticalVelocity = new Vector3(0, speed, 0);
        }
    }
    /// <summary>
    /// 踩踏攻击（从空中下踩敌人）
    /// </summary>
    public virtual void StompAttack()
    {
        if (!isGrounded && !holding && stats.current.canStompAttack && inputs.GetStompDown())
        {
            states.Change<StompPlayerState>();
        }
    }
    /// <summary>
    /// 空中俯冲（下劈攻击）
    /// </summary>
    public virtual void AirDive()
    {
        // 必须允许空中俯冲，且不在地面上，没有拿物品，按下了空中俯冲按键
        if (stats.current.canAirDive && !isGrounded && !holding && inputs.GetAirDiveDown())
        {
            states.Change<AirDivePlayerState>(); // 切换到空中俯冲状态
            playerEvents.OnAirDive?.Invoke();
        }
    }
    /// <summary>
    /// 通过 snap 力量强制把玩家贴到地面上
    /// </summary>
    public virtual void SnapToGround() => SnapToGround(stats.current.snapForce);
    /// <summary>
    /// 执行跳跃逻辑（包括多段跳、土狼跳、持物判定）
    /// </summary>
    public virtual void Jump()
    {
        // 是否可以进行二段 / 多段跳
        var canMultiJump = (jumpCounter > 0) && (jumpCounter < stats.current.multiJumps);
        // 土狼跳判定（离地一小段时间内仍然可以跳）
        var canCoyoteJump = (jumpCounter == 0) && (Time.time < lastGroundTime + stats.current.coyoteJumpThreshold);

        // 地面 / 轨道 / 多段跳 / 土狼跳条件满足时才允许跳跃
        if ((isGrounded || canMultiJump || canCoyoteJump))
        {
            if (inputs.GetJumpDown()) // 按下跳跃键
            {
                Jump(stats.current.maxJumpHeight);
            }
        }

        // 松开跳跃键时，如果还在上升，限制为最小跳跃高度（实现“按得短跳得低”的效果）,早松手就早限制
        if (inputs.GetJumpUp() && (jumpCounter > 0) && (verticalVelocity.y > stats.current.minJumpHeight))
        {
            verticalVelocity = Vector3.up * stats.current.minJumpHeight;
        }
    }
    /// <summary>
    /// 执行一个标准的向上跳跃
    /// </summary>
    public virtual void Jump(float height)
    {
        jumpCounter++; // 增加跳跃计数
        verticalVelocity = Vector3.up * height; // 设置垂直速度
        states.Change<FallPlayerState>(); // 切换为下落状态（跳起后最终会落下）
        playerEvents.OnJump?.Invoke(); // 触发跳跃事件
    }
    /// <summary>
    /// 执行带方向的跳跃（比如斜向跳）
    /// </summary>
    public virtual void DirectionalJump(Vector3 direction, float height, float distance)
    {
        jumpCounter++;
        verticalVelocity = Vector3.up * height; // 垂直上升
        lateralVelocity = direction * distance; // 水平方向的推动
        playerEvents.OnJump?.Invoke();
    }
    /// <summary>
    /// 重置空中冲刺计数
    /// </summary>
    public virtual void ResetAirDash() => airDashCounter = 0;

    /// <summary>
    /// 重置空中旋转次数
    /// </summary>
    public virtual void ResetAirSpins() => airSpinCounter = 0;
    /// <summary>
    /// 重置跳跃计数（回到 0，常用于落地时）
    /// </summary>
    public virtual void ResetJumps() => jumpCounter = 0;
    /// <summary>
    /// 设置跳跃计数为指定值（特殊用途）
    /// </summary>
    public virtual void SetJumps(int amount) => jumpCounter = amount;
    /// <summary>
    /// 后空翻
    /// </summary>
    public virtual void Backflip(float force)
    {
        if (stats.current.canBackflip && !holding)
        {
            verticalVelocity = Vector3.up * stats.current.backflipJumpHeight; // 上跳力
            lateralVelocity = -transform.forward * force; // 向后推力
            states.Change<BackflipPlayerState>();
            playerEvents.OnBackflip.Invoke();
        }
    }
    /// <summary>
    /// 执行旋转动作（Spin）
    /// </summary>
    public virtual void Spin()
    {
        // 空中旋转条件：允许空中旋转 && 未超过上限
        var canAirSpin = (isGrounded || stats.current.canAirSpin) && airSpinCounter < stats.current.allowedAirSpins;

        // 满足旋转条件 + 没有持物 + 按下旋转键
        if (stats.current.canSpin && canAirSpin && !holding && inputs.GetSpinDown())
        {
            if (!isGrounded)
            {
                airSpinCounter++; // 空中旋转次数 +1
            }

            states.Change<SpinPlayerState>(); // 切换到旋转状态
            playerEvents.OnSpin?.Invoke(); // 触发旋转事件
        }
    }
    // 让角色立即朝向某个方向（瞬间转向）
    public virtual void FaceDirection(Vector3 direction)
    {
        // 如果方向向量有效（不是零向量）
        if (direction.sqrMagnitude > 0)
        {
            // 生成一个面向 direction 方向的旋转（保持世界Y轴为上）
            var rotation = Quaternion.LookRotation(direction, Vector3.up);

            // 直接设置物体的旋转
            transform.rotation = rotation;
        }
    }
    /// <summary>
    /// 平滑朝向某个方向旋转（水中旋转速度）
    /// </summary>
    public virtual void WaterFaceDirection(Vector3 direction) => FaceDirection(direction, stats.current.waterRotationSpeed);
    /// <summary>
    /// 对玩家造成伤害（带击退与受伤反应）
    /// </summary>
    /// <param name="amount">要扣除的生命值</param>
    /// <param name="origin">伤害来源位置（用于计算击退方向）</param>
    public override void ApplyDamage(int amount, Vector3 origin)
    {
        if (!health.isEmpty && !health.recovering) // 确保玩家未死亡且不在恢复无敌状态
        {
            health.Damage(amount); // 扣血
            var damageDir = origin - transform.position; // 计算受击方向
            damageDir.y = 0; // 忽略垂直方向
            damageDir = damageDir.normalized;
            FaceDirection(damageDir); // 面向攻击方向

            // 受伤时向后击退
            lateralVelocity = -transform.forward * stats.current.hurtBackwardsForce;

            if (!onWater) // 如果不在水中，则会被击飞向上并进入受伤状态
            {
                verticalVelocity = Vector3.up * stats.current.hurtUpwardForce;
                states.Change<HurtPlayerState>();
            }

            playerEvents.OnHurt?.Invoke(); // 触发受伤事件

            // 如果血量空了 -> 死亡
            // if (health.isEmpty)
            // {
            //     Throw(); // 丢掉物品
            //     playerEvents.OnDie?.Invoke(); // 触发死亡事件
            // }
        }
    }
    /// <summary> 玩家是否可以站立（通过 SphereCast 检测头顶是否有障碍物） </summary>
    public virtual bool canStandUp => !SphereCast(Vector3.up, originalHeight);


    /// <summary>
    /// 如果玩家不在地面上，切换到下落状态
    /// </summary>
    public virtual void Fall()
    {
        if (!isGrounded)
        {
            states.Change<FallPlayerState>();
        }
    }
    /// <summary>
    /// 进入水中（切换到游泳状态）
    /// </summary>
    /// <param name="water">水的碰撞体</param>
    public virtual void EnterWater(Collider water)
    {
        if (!onWater && !health.isEmpty)
        {
            //Throw();  // 丢掉手上的物品
            onWater = true;
            this.water = water;
            states.Change<SwimPlayerState>(); // 切换到游泳状态
        }
    }

    /// <summary>
    /// 离开水域
    /// </summary>
    public virtual void ExitWater()
    {
        if (onWater)
        {
            onWater = false;
        }
    } 
    /// <summary>
    /// 触发检测（玩家停留在触发器内）
    /// 用于检测是否进入水体或离开水体
    /// </summary>
    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(GameTags.VolumeWater))
        {
            // 如果当前不在水中，但进入了水体包围盒
            if (!onWater && other.bounds.Contains(unsizedPosition))
            {
                EnterWater(other);
            }
            // 如果已经在水中，则检测是否离开
            else if (onWater)
            {
                // 计算一个向下偏移点，判断是否离开水面
                var exitPoint = position + Vector3.down * k_waterExitOffset;

                if (!other.bounds.Contains(exitPoint))
                {
                    ExitWater();
                }
            }
        }
    }
}