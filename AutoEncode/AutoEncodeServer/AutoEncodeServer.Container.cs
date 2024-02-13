using AutoEncodeServer.Communication;
using AutoEncodeServer.EncodingJob;
using AutoEncodeServer.Interfaces;
using AutoEncodeServer.MainThread;
using AutoEncodeServer.WorkerThreads;
using AutoEncodeUtilities.Logger;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace AutoEncodeServer
{
    internal partial class AutoEncodeServer
    {
        private static void RegisterContainerComponents(WindsorContainer container)
        {
            // Top Level Setup
            container.Install(FromAssembly.This());

            // Register Container to be accessed later
            container.Register(Component.For<IWindsorContainer>()
                .Instance(container));

            // Register Components
            container.Register(Component.For<ILogger>()
                .ImplementedBy<Logger>()
                .LifestyleSingleton());

            container.Register(Component.For<ICommunicationManager>()
                .ImplementedBy<CommunicationManager>()
                .LifestyleSingleton());

            container.Register(Component.For<IClientUpdatePublisher>()
                .ImplementedBy<ClientUpdatePublisher>()
                .LifestyleSingleton());

            container.Register(Component.For<IEncodingJobManager>()
                .ImplementedBy<EncodingJobManager>()
                .LifestyleSingleton());

            container.Register(Component.For<IEncodingJobModel>()
                .ImplementedBy<EncodingJobModel>()
                .LifestyleTransient());

            container.Register(Component.For<IEncodingJobFinderThread>()
                .ImplementedBy<EncodingJobFinderThread>()
                .LifestyleSingleton());

            container.Register(Component.For<IAEServerMainThread>()
                .ImplementedBy<AEServerMainThread>()
                .LifestyleSingleton());
        }

        private static void ContainerCleanup(WindsorContainer container)
        {
            container?.Dispose();
        }
    }
}
