namespace mROA.Implementation
{
    public class DistributionOptions
    {
        public EDistributionType DistributionType { get; set; }
    }

    public enum EDistributionType
    {
        Channeled,
        ExtractorFirst
    }
}