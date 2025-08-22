using UnityEngine;

/// <summary>
/// 抽象基类，用于管理实体状态机，带有事件支持。
/// </summary>
public abstract class EntityStateManager : MonoBehaviour
{
    /// <summary>
    /// 状态管理相关事件集合（进入状态、退出状态、状态切换等）。
    /// 具体定义在 EntityStateManagerEvents 中。
    /// </summary>
    //public EntityStateManagerEvents events;
}

/// <summary>
/// 泛型抽象类，继承自 EntityStateManager，管理特定实体类型 T 的状态机。
/// T 必须继承自 Entity<T>。
/// </summary>
/// <typeparam name="T">实体类型，必须继承自 Entity<T>。</typeparam>
public abstract class EntityStateManager<T> : EntityStateManager where T : Entity<T>
{
    
}