public class Player : Entity<Player> // 继承自通用的 Entity<Player> 基类
{
    /// <summary> 玩家输入管理器实例 </summary>
    public PlayerInputManager inputs { get; protected set; }

    protected override void Awake()
    {
        base.Awake();
        InitializeInputs();
    }

    // 初始化输入
    protected virtual void InitializeInputs() => inputs = GetComponent<PlayerInputManager>();
}