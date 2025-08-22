using UnityEngine;

public abstract class EntityBase : MonoBehaviour
{
}

// 泛型版本实体类，T继承自Entity<T>
public abstract class Entity<T> : EntityBase where T : Entity<T>
{
}