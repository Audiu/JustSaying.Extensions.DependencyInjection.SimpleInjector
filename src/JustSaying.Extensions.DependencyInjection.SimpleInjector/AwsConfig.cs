namespace JustSaying.Extensions.DependencyInjection.SimpleInjector
{
    public class AwsConfig
    {
        public string AccessKey { get; protected set; }
        public string SecretKey { get; protected set; }
        public string RegionEndpoint { get; protected set; }
        public string ServiceUrl { get; protected set; }

        protected AwsConfig()
        {
        }

        public AwsConfig(string accessKey, string secretKey, string regionEndpoint, string serviceUrl = null)
        {
            AccessKey = accessKey;
            SecretKey = secretKey;
            RegionEndpoint = regionEndpoint;
            ServiceUrl = serviceUrl;
        }
    }
}
