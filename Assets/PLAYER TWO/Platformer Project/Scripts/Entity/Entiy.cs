using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

public abstract class EntityBase : MonoBehaviour 
{
    protected Collider[] m_contactBuffer = new Collider[10];    // 碰撞检测缓冲区，用于存储接触的碰撞体
    public EntityEvents entityEvents;
    // 忽略碰撞器缩放的实体位置
    public Vector3 unsizedPosition => position - transform.up * height * 0.5f + transform.up * originalHeight * 0.5f;

    protected readonly float m_groundOffset = 0.1f;              // 地面检测偏移

    public bool isGrounded { get; protected set; } = true;        // 是否在地面上
    public SplineContainer rails { get; protected set; }           // 当前轨道（Spline轨迹）
    public bool onRails { get; set; }                              // 是否处于轨道（Spline轨迹）上

    public CharacterController controller { get; protected set; } // 角色控制器组件

    public float originalHeight { get; protected set; }            // 初始碰撞器高度
    public float lastGroundTime { get; protected set; }           // 上一次处于地面时间

    public float groundAngle { get; protected set; }               // 当前地面角度

    public Vector3 groundNormal { get; protected set; }            // 当前地面法线

    protected CapsuleCollider m_collider;                          // 自定义胶囊碰撞器（用于自定义碰撞）

    protected Rigidbody m_rigidbody;                                // 刚体组件，用于物理模拟

    public float positionDelta { get; protected set; }            // 当前位置和上一帧位置的距离

    public Vector3 lastPosition { get; set; }                     // 上一帧的位置
    public Vector3 velocity { get; set; }                          // 当前速度

    // 当前水平速度（XZ平面速度）
    public Vector3 lateralVelocity
    {
        get { return new Vector3(velocity.x, 0, velocity.z); }
        set { velocity = new Vector3(value.x, velocity.y, value.z); }
    }

    // 当前垂直速度（Y轴速度）
    public Vector3 verticalVelocity
    {
        get { return new Vector3(0, velocity.y, 0); }
        set { velocity = new Vector3(velocity.x, value.y, velocity.z); }
    }
    public Vector3 localSlopeDirection { get; protected set; }     // 当前地面的局部斜坡方向

    public RaycastHit groundHit;                                   // 当前地面检测的碰撞信息

    public float height => controller.height;                      // 碰撞器当前高度

    public float radius => controller.radius;                      // 碰撞器半径

    public Vector3 center => controller.center;                    // 碰撞器中心点
    // 实体当前位置（角色控制器中心加位置）
    public Vector3 position => transform.position + center;

    // 脚步位置（底部位置，考虑了stepOffset）
    public Vector3 stepPosition => position - transform.up * (height * 0.5f - controller.stepOffset);

    // 受到伤害（空实现，子类重写）
    public virtual void ApplyDamage(int damage, Vector3 origin) { }
    // 判断实体是否在斜坡上
    public virtual bool OnSlopingGround()
    {
        return false;
    }
    // 调整角色控制器碰撞器高度
    public virtual void ResizeCollider(float height)
    {
        // 计算新的高度和当前高度的差值
        var delta = height - this.height;
        // 修改角色控制器的高度
        controller.height = height;
        // 调整角色控制器的中心位置，使其根据高度变化自动平移
        controller.center += Vector3.up * delta * 0.5f;
    }

    // 判断一个点是否在实体脚步位置下方（用于踩踏检测）
    public virtual bool IsPointUnderStep(Vector3 point) => stepPosition.y > point.y;

    // 球形检测（无返回检测信息，只返回是否检测到碰撞）
    public virtual bool SphereCast(Vector3 direction, float distance, int layer = Physics.DefaultRaycastLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        // 调用带有返回检测信息的方法，并忽略返回的hit信息
        return SphereCast(direction, distance, out _, layer, queryTriggerInteraction);
    }

    // 球形检测（返回检测信息，检测是否与其他物体发生碰撞）
    public virtual bool SphereCast(Vector3 direction, float distance,
        out RaycastHit hit, int layer = Physics.DefaultRaycastLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        // 计算球形检测的有效距离，确保球形的检测范围符合预期
        var castDistance = Mathf.Abs(distance - radius);

        // 使用物理引擎进行球形碰撞检测
        return Physics.SphereCast(position, radius, direction,
            out hit, castDistance, layer, queryTriggerInteraction);
    }
    // 胶囊体检测（无返回检测信息，只返回是否检测到碰撞）
    public virtual bool CapsuleCast(Vector3 direction, float distance, int layer = Physics.DefaultRaycastLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        // 调用带有返回检测信息的方法，并忽略返回的hit信息
        return CapsuleCast(direction, distance, out _, layer, queryTriggerInteraction);
    }


    // 胶囊体检测（返回检测信息，检测是否与其他物体发生碰撞）
    public virtual bool CapsuleCast(Vector3 direction, float distance,
        out RaycastHit hit, int layer = Physics.DefaultRaycastLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        // 计算胶囊体的起始位置
        var origin = position - direction * radius + center;
        // 计算偏移量，调整胶囊体的上下半部分，使得碰撞器的中心处于正确位置
        var offset = transform.up * (height * 0.5f - radius);
        // 计算胶囊体的顶部和底部位置
        var top = origin + offset;
        var bottom = origin - offset;

        // 使用物理引擎进行胶囊体碰撞检测
        return Physics.CapsuleCast(top, bottom, radius, direction,
            out hit, distance + radius, layer, queryTriggerInteraction);
    }


    // 检测与其他实体的重叠，结果存储到传入的数组
    public virtual int OverlapEntity(Collider[] result, float skinOffset = 0)
    {
        // 计算接触偏移量（包括碰撞器的皮肤宽度和默认的接触偏移量）
        var contactOffset = skinOffset + controller.skinWidth + Physics.defaultContactOffset;
        // 计算重叠半径（包括胶囊碰撞器的半径和接触偏移量）
        var overlapsRadius = radius + contactOffset;
        // 计算碰撞器顶部和底部的偏移位置
        var offset = (height + contactOffset) * 0.5f - overlapsRadius;
        // 计算胶囊碰撞器的顶部位置（是球心位置）
        var top = position + Vector3.up * offset;
        // 计算胶囊碰撞器的底部位置（是球心位置）
        var bottom = position + Vector3.down * offset;
        // 使用Physics.OverlapCapsuleNonAlloc来检测与其他实体的重叠，返回重叠的实体数量
        return Physics.OverlapCapsuleNonAlloc(top, bottom, overlapsRadius, result);
    }
}

// 泛型版本实体类，T继承自Entity<T>
public abstract class Entity<T>:EntityBase where T : Entity<T>
{
    public EntityStateManager<T> states {  get; protected set; }

    public float accelerationMultiplier { get; set; } = 1f;       // 加速度倍率

    public float gravityMultiplier { get; set; } = 1f;            // 重力倍率

    public float topSpeedMultiplier { get; set; } = 1f;           // 最高速度倍率

    public float turningDragMultiplier { get; set; } = 1f;        // 转向阻力倍率

    public float decelerationMultiplier { get; set; } = 1f;       // 减速度倍率


    protected virtual void Awake()
    {
        InitializeStateManager();
        InitializeController();
    }
    // 初始化角色控制器组件（CharacterController）
    // 负责角色的基本移动、碰撞等物理交互
    protected virtual void InitializeController()
    {
        // 获取当前物体上的 CharacterController 组件
        controller = GetComponent<CharacterController>();
        // 如果没有，就动态添加一个 CharacterController
        if (!controller)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // skinWidth 表示碰撞器表面到实际碰撞检测边界的距离（防止卡住用的小偏移）
        controller.skinWidth = 0.005f;
        // minMoveDistance 为最小移动距离（设为 0 表示即使移动非常小也会被检测到）
        controller.minMoveDistance = 0;
        // 记录角色控制器的初始高度（用于后续复位或高度调整）
        originalHeight = controller.height;
    }

    // 初始化状态管理器
    protected virtual void InitializeStateManager() => states = GetComponent<EntityStateManager<T>>();
    // 根据输入的方向平滑加速移动
    public virtual void Accelerate(Vector3 direction, float turningDrag, float acceleration, float topSpeed)
    {
        // 判断方向是否有效（不为零向量）
        if (direction.sqrMagnitude > 0)
        {
            // 计算当前速度在目标方向上的投影速度（标量）
            var speed = Vector3.Dot(direction, lateralVelocity);
            // 计算当前速度在目标方向上的向量部分
            var velocity = direction * speed;
            // 计算当前速度中垂直于目标方向的部分（转向速度）
            var turningVelocity = lateralVelocity - velocity;
            // 计算转向阻力对应的速度变化量（根据转向阻力系数和时间增量）
            var turningDelta = turningDrag * turningDragMultiplier * Time.deltaTime;
            // 计算最大允许速度（考虑速度倍率）
            var targetTopSpeed = topSpeed * topSpeedMultiplier;

            // 如果当前速度未达最大速度，或当前速度与目标方向相反，则加速（这里恐怕有问题）
            if (lateralVelocity.magnitude < targetTopSpeed || speed < 0)
            {
                // 增加速度，受加速度倍率和时间影响
                speed += acceleration * accelerationMultiplier * Time.deltaTime;
                // 限制速度在[-最大速度, 最大速度]范围内
                speed = Mathf.Clamp(speed, -targetTopSpeed, targetTopSpeed);
            }

            // 重新计算目标方向速度向量
            velocity = direction * speed;
            // 将转向速度平滑减小到0，实现自然转向过渡
            turningVelocity = Vector3.MoveTowards(turningVelocity, Vector3.zero, turningDelta);
            // 更新横向速度为目标方向速度与转向速度之和
            lateralVelocity = velocity + turningVelocity;
        }
    }
    // 让角色按一定旋转速度朝向某个方向（平滑转向）
    public virtual void FaceDirection(Vector3 direction, float degreesPerSecond)
    {
        // 必须是有效的方向
        if (direction != Vector3.zero)
        {
            // 当前旋转
            var rotation = transform.rotation;
            // 本帧允许的最大旋转角度（受 Time.deltaTime 影响）
            var rotationDelta = degreesPerSecond * Time.deltaTime;
            // 目标旋转
            var target = Quaternion.LookRotation(direction, Vector3.up);
            // 按最大旋转速度逐渐逼近目标旋转
            transform.rotation = Quaternion.RotateTowards(rotation, target, rotationDelta);
        }
    }

    // 平滑减速，速度逐渐趋近于 0（水平速度减速）
    public virtual void Decelerate(float deceleration)
    {
        // 计算本帧的减速度（decelerationMultiplier 可用于调节全局减速效果）
        var delta = deceleration * decelerationMultiplier * Time.deltaTime;
        // 将 lateralVelocity（水平速度向量）逐渐插值到 Vector3.zero（完全停止）
        // 第三个参数是本帧允许的最大速度变化量
        lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, delta);
    }


    // 处理角色控制器的移动
    protected virtual void HandleController()
    {
        if (controller.enabled)
        {
            controller.Move(velocity * Time.deltaTime);
            return;
        }
        transform.position += velocity * Time.deltaTime;
    }
    // 处理状态机的步进逻辑
    protected virtual void HandleStates() => states.Step();

    protected virtual void Update()
    {
        HandleStates();
        HandleController();
    }

}
