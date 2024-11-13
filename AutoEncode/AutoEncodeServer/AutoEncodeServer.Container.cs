using AutoEncodeServer.Communication;
using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeServer.Factories;
using AutoEncodeServer.Managers;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Logger;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace AutoEncodeServer;

internal partial class AutoEncodeServer
{
    private static void RegisterContainerComponents(WindsorContainer container)
    {
        // Top Level Setup
        container.Install(FromAssembly.This());

        // Register Container to be accessed later
        container.Register(Component.For<IWindsorContainer>()
            .Instance(container));

        container.AddFacility<TypedFactoryFacility>();

        // Register Components
        container.Register(Component.For<ILogger>()
            .ImplementedBy<Logger>()
            .LifestyleSingleton());

        container.Register(Component.For<ICommunicationMessageHandler>()
            .ImplementedBy<CommunicationMessageHandler>()
            .LifestyleSingleton());

        container.Register(Component.For<IClientUpdatePublisher>()
            .ImplementedBy<ClientUpdatePublisher>()
            .LifestyleSingleton());

        container.Register(Component.For<ISourceFileModelFactory>()
            .AsFactory());

        container.Register(Component.For<ISourceFileModel>()
            .ImplementedBy<SourceFileModel>()
            .LifestyleTransient());

        container.Register(Component.For<ISourceFileManager>()
            .ImplementedBy<SourceFileManager>()
            .LifestyleSingleton());

        container.Register(Component.For<IEncodingJobModelFactory>()
            .AsFactory());

        container.Register(Component.For<IEncodingJobModel>()
            .ImplementedBy<EncodingJobModel>()
            .LifestyleTransient());

        container.Register(Component.For<IEncodingJobManager>()
            .ImplementedBy<EncodingJobManager>()
            .LifestyleSingleton());

        container.Register(Component.For<IAutoEncodeServerManager>()
            .ImplementedBy<AutoEncodeServerManager>()
            .LifestyleSingleton());
    }

    private static void ContainerCleanup(WindsorContainer container)
    {
        container?.Dispose();
    }
}
