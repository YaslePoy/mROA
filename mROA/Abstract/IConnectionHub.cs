namespace mROA.Abstract
{
    public interface IConnectionHub
    {
        void RegisterInteraction(IChannelInteractionModule interaction);
        IChannelInteractionModule GetInteraction(int id);
    }
}