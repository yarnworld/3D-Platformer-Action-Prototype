using UnityEngine;

public abstract class EntityBase : MonoBehaviour
{
}

// 泛型版本实体类，T继承自Entity<T>
public abstract class Entity<T> : EntityBase where T : Entity<T>
{
    public EntityStateManager<T> states { get; protected set; }    // 状态管理器
    
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

    protected virtual void Awake()
    {
        InitializeStateManager();
    }

    // 初始化状态管理器
    protected virtual void InitializeStateManager() => states = GetComponent<EntityStateManager<T>>();
    
    // 处理状态机的步进逻辑
    protected virtual void HandleStates() => states.Step();

    protected virtual void Update()
    {
        HandleStates();
    }
}