﻿using Nancy.Diagnostics;

namespace Nancy.Hosting.Aspnet
{
    using System;
    using System.Collections.Generic;

    using Bootstrapper;
    using Nancy.TinyIoc;

    /// <summary>
    /// TinyIoC ASP.Net Bootstrapper
    /// No child container support because per-request is handled by the AsPerRequestSingleton option
    /// </summary>
    public abstract class DefaultNancyAspNetBootstrapper : NancyBootstrapperBase<TinyIoCContainer>
    {
        /// <summary>
        /// Gets the diagnostics for initialisation
        /// </summary>
        /// <returns>IDiagnostics implementation</returns>
        protected override IDiagnostics GetDiagnostics()
        {
            return this.ApplicationContainer.Resolve<IDiagnostics>();
        }

        /// <summary>
        /// Gets all registered startup tasks
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> instance containing <see cref="IApplicationStartup"/> instances. </returns>
        protected override IEnumerable<IApplicationStartup> GetApplicationStartupTasks()
        {
            return this.ApplicationContainer.ResolveAll<IApplicationStartup>(false);
        }

        /// <summary>
        /// Gets all registered application registration tasks
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> instance containing <see cref="IRegistrations"/> instances.</returns>
        protected override IEnumerable<IRegistrations> GetApplicationRegistrationTasks()
        {
            return this.ApplicationContainer.ResolveAll<IRegistrations>(false);
        }

        /// <summary>
        /// Get all NancyModule implementation instances - should be multi-instance
        /// </summary>
        /// <param name="context">Current request context</param>
        /// <returns>IEnumerable of INancyModule</returns>
        public override sealed IEnumerable<INancyModule> GetAllModules(NancyContext context)
        {
            return this.ApplicationContainer.ResolveAll<INancyModule>(false);
        }

        /// <summary>
        /// Retrieves a specific <see cref="INancyModule"/> implementation - should be per-request lifetime
        /// </summary>
        /// <param name="moduleType">Module type</param>
        /// <param name="context">The current context</param>
        /// <returns>The <see cref="INancyModule"/> instance</returns>
        public override INancyModule GetModule(System.Type moduleType, NancyContext context)
        {
            return this.ApplicationContainer.Resolve<INancyModule>(moduleType.FullName);
        }

        /// <summary>
        /// Creates and initializes the request pipelines.
        /// </summary>
        /// <param name="context">The <see cref="NancyContext"/> used by the request.</param>
        /// <returns>An <see cref="IPipelines"/> instance.</returns>
        protected override sealed IPipelines InitializeRequestPipelines(NancyContext context)
        {
            return base.InitializeRequestPipelines(context);
        }

        /// <summary>
        /// Configures the container using AutoRegister followed by registration
        /// of default INancyModuleCatalog and IRouteResolver.
        /// </summary>
        /// <param name="container">Container instance</param>
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            container.AutoRegister();
            container.Register<INancyModuleCatalog>(this);
        }

        /// <summary>
        /// Resolve INancyEngine
        /// </summary>
        /// <returns>INancyEngine implementation</returns>
        protected override sealed INancyEngine GetEngineInternal()
        {
            return this.ApplicationContainer.Resolve<INancyEngine>();
        }

        /// <summary>
        /// Create a default, unconfigured, container
        /// </summary>
        /// <returns>Container instance</returns>
        protected override TinyIoCContainer GetApplicationContainer()
        {
            return new TinyIoCContainer();
        }

        /// <summary>
        /// Register the bootstrapper's implemented types into the container.
        /// This is necessary so a user can pass in a populated container but not have
        /// to take the responsibility of registering things like INancyModuleCatalog manually.
        /// </summary>
        /// <param name="applicationContainer">Application container to register into</param>
        protected override sealed void RegisterBootstrapperTypes(TinyIoCContainer applicationContainer)
        {
            applicationContainer.Register<INancyModuleCatalog>(this);
        }

        /// <summary>
        /// Register the default implementations of internally used types into the container as singletons
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="typeRegistrations">Type registrations to register</param>
        protected override sealed void RegisterTypes(TinyIoCContainer container, IEnumerable<TypeRegistration> typeRegistrations)
        {
            foreach (var typeRegistration in typeRegistrations)
            {
                switch (typeRegistration.Lifetime)
                {
                    case Lifetime.Transient:
                        container.Register(typeRegistration.RegistrationType, typeRegistration.ImplementationType).AsMultiInstance();
                        break;
                    case Lifetime.Singleton:
                        container.Register(typeRegistration.RegistrationType, typeRegistration.ImplementationType).AsSingleton();
                        break;
                    case Lifetime.PerRequest:
                        container.Register(typeRegistration.RegistrationType, typeRegistration.ImplementationType).AsPerRequestSingleton();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Register the various collections into the container as singletons to later be resolved
        /// by IEnumerable{Type} constructor dependencies.
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="collectionTypeRegistrationsn">Collection type registrations to register</param>
        protected override sealed void RegisterCollectionTypes(TinyIoCContainer container, IEnumerable<CollectionTypeRegistration> collectionTypeRegistrationsn)
        {
            foreach (var collectionTypeRegistration in collectionTypeRegistrationsn)
            {
                switch (collectionTypeRegistration.Lifetime)
                {
                    case Lifetime.Transient:
                        container.RegisterMultiple(collectionTypeRegistration.RegistrationType, collectionTypeRegistration.ImplementationTypes).AsMultiInstance();
                        break;
                    case Lifetime.Singleton:
                        container.RegisterMultiple(collectionTypeRegistration.RegistrationType, collectionTypeRegistration.ImplementationTypes).AsSingleton();
                        break;
                    case Lifetime.PerRequest:
                        container.RegisterMultiple(collectionTypeRegistration.RegistrationType, collectionTypeRegistration.ImplementationTypes).AsPerRequestSingleton();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Register the given module types into the container
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="moduleRegistrationTypes">NancyModule types</param>
        protected override sealed void RegisterModules(TinyIoCContainer container, IEnumerable<ModuleRegistration> moduleRegistrationTypes)
        {
            foreach (var registrationType in moduleRegistrationTypes)
            {
                container.Register(typeof(INancyModule), registrationType.ModuleType, registrationType.ModuleType.FullName).AsPerRequestSingleton();
            }
        }

        /// <summary>
        /// Register the given instances into the container
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="instanceRegistrations">Instance registration types</param>
        protected override void RegisterInstances(TinyIoCContainer container, IEnumerable<InstanceRegistration> instanceRegistrations)
        {
            foreach (var instanceRegistration in instanceRegistrations)
            {
                container.Register(
                    instanceRegistration.RegistrationType,
                    instanceRegistration.Implementation);
            }
        }
    }
}