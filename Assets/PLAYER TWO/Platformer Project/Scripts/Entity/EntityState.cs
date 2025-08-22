using System.Collections.Generic;

/// <summary>
/// 泛型抽象类，代表某种实体(Entity)的状态机中的一个状态。
/// T 是继承自 Entity<T> 的实体类型。
/// </summary>
public abstract class EntityState<T> where T : Entity<T>
{
    /// <summary>
    /// 静态方法，通过类型名称字符串创建对应的状态实例。
    /// 例如传入"PLAYERTWO.PlatformerProject.IdleState" 返回该类型的实例。
    /// </summary>
    /// <param name="typeName">状态类的完全限定名称。</param>
    /// <returns>对应的状态实例。</returns>
    public static EntityState<T> CreateFromString(string typeName)
    {
        return (EntityState<T>)System.Activator
            .CreateInstance(System.Type.GetType(typeName));
    }
    
    /// <summary>
    /// 静态方法，根据字符串数组批量创建状态实例列表。
    /// </summary>
    /// <param name="array">包含多个状态类名的字符串数组。</param>
    /// <returns>包含对应状态实例的列表。</returns>
    public static List<EntityState<T>> CreateListFromStringArray(string[] array)
    {
        var list = new List<EntityState<T>>();

        foreach (var typeName in array)
        {
            list.Add(CreateFromString(typeName));
        }

        return list;
    }
}