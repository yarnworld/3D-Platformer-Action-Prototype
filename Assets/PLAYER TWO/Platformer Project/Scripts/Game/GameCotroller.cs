using UnityEngine;

[AddComponentMenu("PLAYER TWO/Platformer Project/Game/Game Controller")]
public class GameController : MonoBehaviour
{
    protected Game m_game => Game.instance;

    public virtual void AddRetries(int amount) => m_game.retries += amount;
}