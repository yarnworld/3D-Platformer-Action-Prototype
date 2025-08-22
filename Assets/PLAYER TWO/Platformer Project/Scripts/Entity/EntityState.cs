/// <summary>
/// 泛型抽象类，代表某种实体(Entity)的状态机中的一个状态。
/// T 是继承自 Entity<T> 的实体类型。
/// </summary>
public abstract class EntityState<T> where T : Entity<T>
{
}