using UnityEngine;

public abstract class EntityBase : MonoBehaviour
{
}

// 泛型版本实体类，T继承自Entity<T>
public abstract class Entity<T> : EntityBase where T : Entity<T>
{
    public EntityStateManager<T> states { get; protected set; }    // 状态管理器

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