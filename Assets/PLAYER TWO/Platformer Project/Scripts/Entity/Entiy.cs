using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityBase : MonoBehaviour 
{
    public Vector3 unsizedPosition => transform.position;

    public bool isGrounded { get; protected set; } = true;        // 是否在地面上
    public CharacterController controller { get; protected set; } // 角色控制器组件

    public float originalHeight { get; protected set; }            // 初始碰撞器高度

    // 判断实体是否在斜坡上
    public virtual bool OnSlopingGround()
    {
        return false;
    }
}

// 泛型版本实体类，T继承自Entity<T>
public abstract class Entity<T>:EntityBase where T : Entity<T>
{
    public EntityStateManager<T> states {  get; protected set; }
    public Vector3 velocity { get; set; }                          // 当前速度

    public float accelerationMultiplier { get; set; } = 1f;       // 加速度倍率

    public float gravityMultiplier { get; set; } = 1f;            // 重力倍率

    public float topSpeedMultiplier { get; set; } = 1f;           // 最高速度倍率

    public float turningDragMultiplier { get; set; } = 1f;        // 转向阻力倍率

    public float decelerationMultiplier { get; set; } = 1f;       // 减速度倍率

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
