using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    // 输入动作资源（在 Input System 中配置的 InputActionAsset）
    public InputActionAsset actions;
    
    // 用于锁定移动方向的时间戳（当前时间小于此值时，禁止移动输入）
    protected float m_movementDirectionUnlockTime;
    
    // 输入动作缓存
    protected InputAction m_movement;
    
    // 主摄像机引用，用于计算相对移动方向
    protected Camera m_camera;

    protected virtual void Awake() => CacheActions();

    protected virtual void Start()
    {
        m_camera = Camera.main;
        actions.Enable();
    }
    
    protected virtual void OnEnable() => actions?.Enable();
    protected virtual void OnDisable() => actions?.Disable();

    protected virtual void CacheActions()
    {
        m_movement = actions["Movement"];
    }
    
    /// <summary>
    /// 获取移动方向输入（带十字型死区判断）
    /// 如果在锁定时间内，则返回 Vector3.zero
    /// </summary>
    public virtual Vector3 GetMovementDirection()
    {
        if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;

        var value = m_movement.ReadValue<Vector2>();
        return GetAxisWithCrossDeadZone(value);
    }
    
    /// <summary>
    /// 根据十字形死区修正输入值（Input System 默认是圆形死区）
    /// </summary>
    /// <param name="axis">输入轴</param>
    public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis)
    {
        var deadzone = InputSystem.settings.defaultDeadzoneMin;
        axis.x = Mathf.Abs(axis.x) > deadzone ? RemapToDeadzone(axis.x, deadzone) : 0;
        axis.y = Mathf.Abs(axis.y) > deadzone ? RemapToDeadzone(axis.y, deadzone) : 0;
        return new Vector3(axis.x, 0, axis.y);
    }
    
    /// <summary>
    /// 将输入值按给定死区重新映射到 0-1
    /// </summary>
    //protected float RemapToDeadzone(float value,float deadzone)=>(value - deadzone) / (1-deadzone);
    protected float RemapToDeadzone(float value,float deadzone)=>(value - (value > 0 ? -deadzone : deadzone)) / (1-deadzone);

    
    /// <summary>
    /// 获取相机方向下的移动向量  
    /// 将输入方向映射到相机朝向（Y轴旋转）下
    /// </summary>
    public virtual Vector3 GetMovementCameraDirection()
    {
        // 1. 获取移动方向（通常是玩家输入的水平/垂直方向，比如 WSAD 或摇杆）
        var direction = GetMovementDirection();

        // 2. 如果有输入（不是零向量）
        if (direction.sqrMagnitude > 0)
        {
            // 3. 构建一个旋转：根据摄像机的 Y 轴角度（水平朝向）
            // Quaternion.AngleAxis(angle, axis) 表示绕某个轴旋转一个角度
            var rotation = Quaternion.AngleAxis(m_camera.transform.eulerAngles.y, Vector3.up);

            // 4. 把原始输入方向旋转到摄像机的朝向下
            direction = rotation * direction;

            // 5. 归一化，保持方向向量的长度为 1（只保留方向）
            direction = direction.normalized;
        }

        // 6. 返回最终的世界空间移动方向
        return direction;
    }
}