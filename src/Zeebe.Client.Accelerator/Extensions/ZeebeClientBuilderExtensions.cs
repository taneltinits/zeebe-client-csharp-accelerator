using System;
using Zeebe.Client.Api.Builder;
using Zeebe.Client.Impl.Builder;
using static Zeebe.Client.Accelerator.Options.ZeebeClientAcceleratorOptions;

namespace Zeebe.Client.Accelerator.Extensions
{
     public static class ZeebeClientBuilderExtensions 
     {
         public static IZeebeClient Build(this IZeebeClientBuilder builder, ClientOptions options)
         {
             return builder
                .BuildTransportBuilder(options)
                .BuildFinalStep(options)
                .BuildClient(options);
         }

        private static IZeebeClientTransportBuilder BuildTransportBuilder(this IZeebeClientBuilder builder, ClientOptions options)
        {
            if(String.IsNullOrEmpty(options.GatewayAddress))
                throw new ArgumentNullException(nameof(options.GatewayAddress));

            return builder.UseGatewayAddress(options.GatewayAddress);
        }

        private static IZeebeClientFinalBuildStep BuildFinalStep(this IZeebeClientTransportBuilder builder, ClientOptions options)
        {
            if(options.TransportEncryption == null && options.Cloud == null)
                return builder.UsePlainText();

            IZeebeSecureClientBuilder clientBuilder = null;
            
            if (options.Cloud != null)
            {
                clientBuilder = builder.UseTransportEncryption();
                var tokenSupplier = CamundaCloudTokenProvider.Builder().UseAuthServer(options.Cloud.AuthorizationServerUrl)
                    .UseClientId(options.Cloud.ClientId).UseClientSecret(options.Cloud.ClientSecret).UseAudience(options.Cloud.TokenAudience)
                    .Build();
                clientBuilder.UseAccessTokenSupplier(tokenSupplier);
                // The CamundaCloudTokenProvider maintains a local token cache, using a local file as a means to persist the token...
                // -> try to get token early in order to prevent errors when writing credential file in parallel upon startup
                try { tokenSupplier.GetAccessTokenForRequestAsync().Wait(); } catch (Exception) { /* NOOP */ }
                return clientBuilder;
            }
            
            if (!String.IsNullOrEmpty(options.TransportEncryption.RootCertificatePath))
                clientBuilder = builder.UseTransportEncryption(options.TransportEncryption.RootCertificatePath);
            else
                clientBuilder = builder.UseTransportEncryption();

            if(!string.IsNullOrEmpty(options.TransportEncryption.AccessToken))
                clientBuilder.UseAccessToken(options.TransportEncryption.AccessToken);
            else if(options.TransportEncryption.AccessTokenSupplier != null)
                clientBuilder.UseAccessTokenSupplier(options.TransportEncryption.AccessTokenSupplier);

            return clientBuilder;
        }

        private static IZeebeClient BuildClient(this IZeebeClientFinalBuildStep builder, ClientOptions options)
        {
            if(options.KeepAlive.HasValue)
                builder = builder.UseKeepAlive(options.KeepAlive.Value);

            if(options.RetrySleepDurationProvider != null)
                builder = builder.UseRetrySleepDurationProvider(options.RetrySleepDurationProvider);

            return builder.Build();
        }
     }
}
