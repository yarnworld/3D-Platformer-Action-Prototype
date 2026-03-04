using UnityEngine;

/// <summary>
/// 泛型抽象类，用于定义实体的属性数据。
/// 继承自 ScriptableObject，方便通过 Unity 编辑器创建和管理数据资产。
/// T 是具体的 ScriptableObject 类型，限定属性数据的具体实现。
/// </summary>
/// <typeparam name="T">继承自 ScriptableObject 的具体属性类型。</typeparam>
public abstract class EntityStats<T>:ScriptableObject where T : ScriptableObject
{
    // 这里可以定义一些通用的属性字段，例如生命值、移动速度等。
    // 具体的属性字段由继承类根据需要添加。
}