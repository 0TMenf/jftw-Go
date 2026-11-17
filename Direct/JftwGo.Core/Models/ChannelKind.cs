namespace JftwGo.Core.Models
{
    /// <summary>资源通道枚举。未知字符串归一化为 Unknown，保证容错。</summary>
    public enum ChannelKind
    {
        Unknown,
        Stable,
        Preview,
        Dev,
        Community,
        Game
    }
}
