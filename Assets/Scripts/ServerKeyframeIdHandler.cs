public class ServerKeyframeIdHandler : IKeyframeMessageConsumer
{
    public int? recentServerKeyframeId { get; private set; } = null;

    public void ProcessMessage(Message message)
    {
        recentServerKeyframeId = message.serverKeyframeId;
    }

    public void Update() {}
}
