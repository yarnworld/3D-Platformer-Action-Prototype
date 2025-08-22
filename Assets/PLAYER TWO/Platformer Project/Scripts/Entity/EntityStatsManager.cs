using UnityEngine;

// 泛型抽象类，用于管理指定类型的属性集，T 必须继承自 EntityStats<T>
public abstract class EntityStatsManager<T> : MonoBehaviour where T : EntityStats<T>
{
}