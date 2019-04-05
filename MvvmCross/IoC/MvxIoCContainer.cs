﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MS-PL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MvvmCross.Base;
using MvvmCross.Exceptions;
using MvvmCross.Logging;

namespace MvvmCross.IoC
{
    public class MvxIoCContainer
        : IMvxIoCProvider
    {
        private readonly Dictionary<Type, IResolver> _resolvers = new Dictionary<Type, IResolver>();
        private readonly Dictionary<Type, List<Action>> _waiters = new Dictionary<Type, List<Action>>();
        private readonly Dictionary<Type, bool> _circularTypeDetection = new Dictionary<Type, bool>();
        private readonly object _lockObject = new object();
        private readonly IMvxIocOptions _options;
        private readonly IMvxPropertyInjector _propertyInjector;
        private readonly IMvxIoCProvider _parentProvider;

        protected object LockObject => _lockObject;

        protected IMvxIocOptions Options => _options;

        public MvxIoCContainer(IMvxIocOptions options, IMvxIoCProvider parentProvider = null)
        {
            _options = options ?? new MvxIocOptions();
            if (_options.PropertyInjectorType != null)
            {
                _propertyInjector = Activator.CreateInstance(_options.PropertyInjectorType) as IMvxPropertyInjector;
            }
            if (_propertyInjector != null)
            {
                RegisterSingleton(typeof(IMvxPropertyInjector), _propertyInjector);
            }
            if (parentProvider != null)
            {
                _parentProvider = parentProvider;
            }
        }

        public MvxIoCContainer(IMvxIoCProvider parentProvider)
            : this(null, parentProvider)
        {
            if (parentProvider == null)
            {
                throw new ArgumentNullException(nameof(parentProvider), "Provide a parent ioc provider to this constructor");
            }
        }

        public interface IResolver
        {
            object Resolve();

            ResolverType ResolveType { get; }

            void SetGenericTypeParameters(Type[] genericTypeParameters);
        }

        public class ConstructingResolver : IResolver
        {
            private readonly Type _type;
            private readonly IMvxIoCProvider _parent;

            public ConstructingResolver(Type type, IMvxIoCProvider parent)
            {
                _type = type;
                _parent = parent;
            }

            public object Resolve()
            {
                return _parent.IoCConstruct(_type);
            }

            public void SetGenericTypeParameters(Type[] genericTypeParameters)
            {
                throw new InvalidOperationException("This Resolver does not set generic type parameters");
            }

            public ResolverType ResolveType => ResolverType.DynamicPerResolve;
        }

        public class FuncConstructingResolver : IResolver
        {
            private readonly Func<object> _constructor;

            public FuncConstructingResolver(Func<object> constructor)
            {
                _constructor = constructor;
            }

            public object Resolve()
            {
                return _constructor();
            }

            public void SetGenericTypeParameters(Type[] genericTypeParameters)
            {
                throw new InvalidOperationException("This Resolver does not set generic type parameters");
            }

            public ResolverType ResolveType => ResolverType.DynamicPerResolve;
        }

        public class TypedFuncConstructingResolver<TInterface, TParameter1> : IResolver
            where TInterface : class
            where TParameter1 : class
        {
            protected readonly MvxIoCContainer _provider;
            private readonly Func<TParameter1, TInterface> _constructor;

            protected TypedFuncConstructingResolver(MvxIoCContainer parent)
            {
                _provider = parent;
            }

            public TypedFuncConstructingResolver(Func<TParameter1, TInterface> typedConstructor, MvxIoCContainer parent) : this(parent)
            {
                _constructor = typedConstructor;
            }

            public virtual object Resolve()
            {
                _provider.InternalTryResolve(typeof(TParameter1), out var parameter1);
                return _constructor((TParameter1)parameter1);
            }

            public void SetGenericTypeParameters(Type[] genericTypeParameters)
            {
                throw new InvalidOperationException("This Resolver does not set generic type parameters");
            }

            public ResolverType ResolveType => ResolverType.DynamicPerResolve;
        }

        public class TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2> : TypedFuncConstructingResolver<TInterface, TParameter1>
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
        {
            private readonly Func<TParameter1, TParameter2, TInterface> _constructor;

            protected TypedFuncConstructingResolver(MvxIoCContainer parent) : base(parent)
            {
            }

            public TypedFuncConstructingResolver(Func<TParameter1, TParameter2, TInterface> typedConstructor, MvxIoCContainer parent) : this(parent)
            {
                _constructor = typedConstructor;
            }

            public override object Resolve()
            {
                _provider.InternalTryResolve(typeof(TParameter1), out var parameter1);
                _provider.InternalTryResolve(typeof(TParameter2), out var parameter2);
                return _constructor((TParameter1)parameter1, (TParameter2)parameter2);
            }
        }

        public class TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3> : TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2>
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
            where TParameter3 : class
        {
            private readonly Func<TParameter1, TParameter2, TParameter3, TInterface> _constructor;

            protected TypedFuncConstructingResolver(MvxIoCContainer parent) : base(parent)
            {
            }

            public TypedFuncConstructingResolver(Func<TParameter1, TParameter2, TParameter3, TInterface> typedConstructor, MvxIoCContainer parent) : this(parent)
            {
                _constructor = typedConstructor;
            }

            public override object Resolve()
            {
                _provider.InternalTryResolve(typeof(TParameter1), out var parameter1);
                _provider.InternalTryResolve(typeof(TParameter2), out var parameter2);
                _provider.InternalTryResolve(typeof(TParameter3), out var parameter3);
                return _constructor((TParameter1)parameter1, (TParameter2)parameter2, (TParameter3)parameter3);
            }
        }

        public class TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3, TParameter4> : TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3>
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
            where TParameter3 : class
            where TParameter4 : class
        {
            private readonly Func<TParameter1, TParameter2, TParameter3, TParameter4, TInterface> _constructor;

            protected TypedFuncConstructingResolver(MvxIoCContainer parent) : base(parent)
            {
            }

            public TypedFuncConstructingResolver(Func<TParameter1, TParameter2, TParameter3, TParameter4, TInterface> typedConstructor, MvxIoCContainer parent) : this(parent)
            {
                _constructor = typedConstructor;
            }

            public override object Resolve()
            {
                _provider.InternalTryResolve(typeof(TParameter1), out var parameter1);
                _provider.InternalTryResolve(typeof(TParameter2), out var parameter2);
                _provider.InternalTryResolve(typeof(TParameter3), out var parameter3);
                _provider.InternalTryResolve(typeof(TParameter4), out var parameter4);
                return _constructor((TParameter1)parameter1, (TParameter2)parameter2, (TParameter3)parameter3, (TParameter4)parameter4);
            }
        }

        public class TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5> : TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3, TParameter4>
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
            where TParameter3 : class
            where TParameter4 : class
            where TParameter5 : class
        {
            private readonly Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInterface> _constructor;

            protected TypedFuncConstructingResolver(MvxIoCContainer parent) : base(parent)
            {
            }

            public TypedFuncConstructingResolver(Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInterface> typedConstructor, MvxIoCContainer parent) : this(parent)
            {
                _constructor = typedConstructor;
            }

            public override object Resolve()
            {
                _provider.InternalTryResolve(typeof(TParameter1), out var parameter1);
                _provider.InternalTryResolve(typeof(TParameter2), out var parameter2);
                _provider.InternalTryResolve(typeof(TParameter3), out var parameter3);
                _provider.InternalTryResolve(typeof(TParameter4), out var parameter4);
                _provider.InternalTryResolve(typeof(TParameter5), out var parameter5);
                return _constructor((TParameter1)parameter1, (TParameter2)parameter2, (TParameter3)parameter3, (TParameter4)parameter4, (TParameter5)parameter5);
            }
        }

        public class ParameterFuncConstructingResolver : IResolver
        {
            private readonly Func<object> _constructor;

            public static ParameterFuncConstructingResolver CreateResolver<TInterface, TParameter1>(
                Func<TParameter1, TInterface> typedConstructor, MvxIoCContainer parent)
                where TInterface : class
                where TParameter1 : class
            {
                var resolver = new Func<object>(() =>
                {
                    parent.InternalTryResolve(typeof(TParameter1), out var parameter1);
                    return typedConstructor((TParameter1)parameter1);
                });

                return new ParameterFuncConstructingResolver(resolver);
            }

            public static ParameterFuncConstructingResolver CreateResolver<TInterface, TParameter1, TParameter2>(
                Func<TParameter1, TParameter2, TInterface> typedConstructor, MvxIoCContainer parent)
                where TInterface : class
                where TParameter1 : class
                where TParameter2 : class
            {
                var resolver = new Func<object>(() =>
                {
                    parent.InternalTryResolve(typeof(TParameter1), out var parameter1);
                    parent.InternalTryResolve(typeof(TParameter2), out var parameter2);
                    return typedConstructor((TParameter1)parameter1, (TParameter2)parameter2);
                });

                return new ParameterFuncConstructingResolver(resolver);
            }

            public static ParameterFuncConstructingResolver CreateResolver<TInterface, TParameter1, TParameter2, TParameter3>(
                Func<TParameter1, TParameter2, TParameter3, TInterface> typedConstructor, MvxIoCContainer parent)
                where TInterface : class
                where TParameter1 : class
                where TParameter2 : class
                where TParameter3 : class
            {
                var resolver = new Func<object>(() =>
                {
                    parent.InternalTryResolve(typeof(TParameter1), out var parameter1);
                    parent.InternalTryResolve(typeof(TParameter2), out var parameter2);
                    parent.InternalTryResolve(typeof(TParameter3), out var parameter3);
                    return typedConstructor((TParameter1)parameter1, (TParameter2)parameter2, (TParameter3)parameter3);
                });

                return new ParameterFuncConstructingResolver(resolver);
            }

            public static ParameterFuncConstructingResolver CreateResolver<TInterface, TParameter1, TParameter2, TParameter3, TParameter4>(
                Func<TParameter1, TParameter2, TParameter3, TParameter4, TInterface> typedConstructor, MvxIoCContainer parent)
                where TInterface : class
                where TParameter1 : class
                where TParameter2 : class
                where TParameter3 : class
                where TParameter4 : class
            {
                var resolver = new Func<object>(() =>
                {
                    parent.InternalTryResolve(typeof(TParameter1), out var parameter1);
                    parent.InternalTryResolve(typeof(TParameter2), out var parameter2);
                    parent.InternalTryResolve(typeof(TParameter3), out var parameter3);
                    parent.InternalTryResolve(typeof(TParameter4), out var parameter4);
                    return typedConstructor((TParameter1)parameter1, (TParameter2)parameter2, (TParameter3)parameter3, (TParameter4)parameter4);
                });

                return new ParameterFuncConstructingResolver(resolver);
            }

            public static ParameterFuncConstructingResolver CreateResolver<TInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(
                Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInterface> typedConstructor, MvxIoCContainer parent)
                where TInterface : class
                where TParameter1 : class
                where TParameter2 : class
                where TParameter3 : class
                where TParameter4 : class
                where TParameter5 : class
            {
                var resolver = new Func<object>(() =>
                {
                    parent.InternalTryResolve(typeof(TParameter1), out var parameter1);
                    parent.InternalTryResolve(typeof(TParameter2), out var parameter2);
                    parent.InternalTryResolve(typeof(TParameter3), out var parameter3);
                    parent.InternalTryResolve(typeof(TParameter4), out var parameter4);
                    parent.InternalTryResolve(typeof(TParameter5), out var parameter5);
                    return typedConstructor((TParameter1)parameter1, (TParameter2)parameter2, (TParameter3)parameter3, (TParameter4)parameter4, (TParameter5)parameter5);
                });

                return new ParameterFuncConstructingResolver(resolver);
            }

            private ParameterFuncConstructingResolver(Func<object> constructor)
            {
                _constructor = constructor;
            }

            public object Resolve()
            {
                return _constructor();
            }

            public void SetGenericTypeParameters(Type[] genericTypeParameters)
            {
                throw new InvalidOperationException("This Resolver does not set generic type parameters");
            }

            public ResolverType ResolveType => ResolverType.DynamicPerResolve;
        }

        public class SingletonResolver : IResolver
        {
            private readonly object _theObject;

            public SingletonResolver(object theObject)
            {
                _theObject = theObject;
            }

            public object Resolve()
            {
                return _theObject;
            }

            public void SetGenericTypeParameters(Type[] genericTypeParameters)
            {
                throw new InvalidOperationException("This Resolver does not set generic type parameters");
            }

            public ResolverType ResolveType => ResolverType.Singleton;
        }

        public class ConstructingSingletonResolver : IResolver
        {
            private readonly object _syncObject = new object();
            private readonly Func<object> _constructor;
            private object _theObject;

            public ConstructingSingletonResolver(Func<object> theConstructor)
            {
                _constructor = theConstructor;
            }

            public object Resolve()
            {
                if (_theObject != null)
                    return _theObject;

                lock (_syncObject)
                {
                    if (_theObject == null)
                        _theObject = _constructor();
                }

                return _theObject;
            }

            public void SetGenericTypeParameters(Type[] genericTypeParameters)
            {
                throw new InvalidOperationException("This Resolver does not set generic type parameters");
            }

            public ResolverType ResolveType => ResolverType.Singleton;
        }

        public class ConstructingOpenGenericResolver : IResolver
        {
            private readonly Type _genericTypeDefinition;
            private readonly IMvxIoCProvider _parent;

            private Type[] _genericTypeParameters;

            public ConstructingOpenGenericResolver(Type genericTypeDefinition, IMvxIoCProvider parent)
            {
                _genericTypeDefinition = genericTypeDefinition;
                _parent = parent;
            }

            public void SetGenericTypeParameters(Type[] genericTypeParameters)
            {
                _genericTypeParameters = genericTypeParameters;
            }

            public object Resolve()
            {
                return _parent.IoCConstruct(_genericTypeDefinition.MakeGenericType(_genericTypeParameters));
            }

            public ResolverType ResolveType => ResolverType.DynamicPerResolve;
        }

        public bool CanResolve<T>()
            where T : class
        {
            return CanResolve(typeof(T));
        }

        public bool CanResolve(Type t)
        {
            lock (_lockObject)
            {
                if (_resolvers.ContainsKey(t))
                {
                    return true;
                }
                if (_parentProvider != null && _parentProvider.CanResolve(t))
                {
                    return true;
                }
                return false;
            }
        }

        public bool TryResolve<T>(out T resolved)
            where T : class
        {
            try
            {
                object item;
                var toReturn = TryResolve(typeof(T), out item);
                resolved = (T)item;
                return toReturn;
            }
            catch (MvxIoCResolveException)
            {
                resolved = (T)typeof(T).CreateDefault();
                return false;
            }
        }

        public bool TryResolve(Type type, out object resolved)
        {
            lock (_lockObject)
            {
                return InternalTryResolve(type, out resolved);
            }
        }

        public T Resolve<T>()
            where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type t)
        {
            lock (_lockObject)
            {
                object resolved;
                if (!InternalTryResolve(t, out resolved))
                {
                    throw new MvxIoCResolveException("Failed to resolve type {0}", t.FullName);
                }
                return resolved;
            }
        }

        public T GetSingleton<T>()
            where T : class
        {
            return (T)GetSingleton(typeof(T));
        }

        public object GetSingleton(Type t)
        {
            lock (_lockObject)
            {
                object resolved;
                if (!InternalTryResolve(t, ResolverType.Singleton, out resolved))
                {
                    throw new MvxIoCResolveException("Failed to resolve type {0}", t.FullName);
                }
                return resolved;
            }
        }

        public T Create<T>()
            where T : class
        {
            return (T)Create(typeof(T));
        }

        public object Create(Type t)
        {
            lock (_lockObject)
            {
                object resolved;
                if (!InternalTryResolve(t, ResolverType.DynamicPerResolve, out resolved))
                {
                    throw new MvxIoCResolveException("Failed to resolve type {0}", t.FullName);
                }
                return resolved;
            }
        }

        public void RegisterType<TInterface, TToConstruct>()
            where TInterface : class
            where TToConstruct : class, TInterface
        {
            RegisterType(typeof(TInterface), typeof(TToConstruct));
        }

        public void RegisterType<TInterface>(Func<TInterface> constructor)
            where TInterface : class
        {
            var resolver = new FuncConstructingResolver(constructor);
            InternalSetResolver(typeof(TInterface), resolver);
        }

        public void RegisterType<TInterface, TParameter1>(Func<TParameter1, TInterface> constructor)
            where TInterface : class
            where TParameter1 : class
        {
            //var resolver = new TypedFuncConstructingResolver<TInterface, TParameter1>(constructor, this);

            var resolver = ParameterFuncConstructingResolver.CreateResolver(constructor, this);
            InternalSetResolver(typeof(TInterface), resolver);
        }

        public void RegisterType<TInterface, TParameter1, TParameter2>(Func<TParameter1, TParameter2, TInterface> constructor)
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
        {
            //var resolver = new TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2>(constructor, this);

            var resolver = ParameterFuncConstructingResolver.CreateResolver(constructor, this);
            InternalSetResolver(typeof(TInterface), resolver);
        }

        public void RegisterType<TInterface, TParameter1, TParameter2, TParameter3>(Func<TParameter1, TParameter2, TParameter3, TInterface> constructor)
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
            where TParameter3 : class
        {
            var resolver = ParameterFuncConstructingResolver.CreateResolver(constructor, this);

            //var resolver = new TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3>(constructor, this);
            InternalSetResolver(typeof(TInterface), resolver);
        }

        public void RegisterType<TInterface, TParameter1, TParameter2, TParameter3, TParameter4>(Func<TParameter1, TParameter2, TParameter3, TParameter4, TInterface> constructor)
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
            where TParameter3 : class
            where TParameter4 : class
        {
            //var resolver = new TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3, TParameter4>(constructor, this);

            var resolver = ParameterFuncConstructingResolver.CreateResolver(constructor, this);
            InternalSetResolver(typeof(TInterface), resolver);
        }

        public void RegisterType<TInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInterface> constructor)
            where TInterface : class
            where TParameter1 : class
            where TParameter2 : class
            where TParameter3 : class
            where TParameter4 : class
            where TParameter5 : class
        {
            //var resolver = new TypedFuncConstructingResolver<TInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(constructor, this);

            var resolver = ParameterFuncConstructingResolver.CreateResolver(constructor, this);
            InternalSetResolver(typeof(TInterface), resolver);
        }

        public void RegisterType(Type t, Func<object> constructor)
        {
            var resolver = new FuncConstructingResolver(() =>
            {
                var ret = constructor();
                if (ret != null && !t.IsInstanceOfType(ret))
                    throw new MvxIoCResolveException("Constructor failed to return a compatibly object for type {0}", t.FullName);

                return ret;
            });

            InternalSetResolver(t, resolver);
        }

        public void RegisterType(Type interfaceType, Type constructType)
        {
            IResolver resolver = null;
            if (interfaceType.GetTypeInfo().IsGenericTypeDefinition)
            {
                resolver = new ConstructingOpenGenericResolver(constructType, this);
            }
            else
            {
                resolver = new ConstructingResolver(constructType, this);
            }

            InternalSetResolver(interfaceType, resolver);
        }

        public void RegisterSingleton<TInterface>(TInterface theObject)
            where TInterface : class
        {
            RegisterSingleton(typeof(TInterface), theObject);
        }

        public void RegisterSingleton(Type interfaceType, object theObject)
        {
            InternalSetResolver(interfaceType, new SingletonResolver(theObject));
        }

        public void RegisterSingleton<TInterface>(Func<TInterface> theConstructor)
            where TInterface : class
        {
            RegisterSingleton(typeof(TInterface), theConstructor);
        }

        public void RegisterSingleton(Type interfaceType, Func<object> theConstructor)
        {
            InternalSetResolver(interfaceType, new ConstructingSingletonResolver(theConstructor));
        }

        public object IoCConstruct(Type type)
        {
            return IoCConstruct(type, default(IDictionary<string, object>));
        }

        public T IoCConstruct<T>()
            where T : class
        {
            return (T)IoCConstruct(typeof(T));
        }

        public virtual object IoCConstruct(Type type, object arguments)
        {
            return IoCConstruct(type, arguments.ToPropertyDictionary());
        }

        public virtual T IoCConstruct<T>(IDictionary<string, object> arguments)
            where T : class
        {
            return (T)IoCConstruct(typeof(T), arguments);
        }

        public virtual T IoCConstruct<T>(object arguments)
            where T : class
        {
            return (T)IoCConstruct(typeof(T), arguments.ToPropertyDictionary());
        }

        public virtual T IoCConstruct<T>(params object[] arguments) where T : class
        {
            return (T)IoCConstruct(typeof(T), arguments);
        }

        public virtual object IoCConstruct(Type type, params object[] arguments)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            var argumentTypes = arguments.Select(x => x.GetType());
            var selectedConstructor = constructors.FirstOrDefault(x => x.GetParameters().Select(q => q.ParameterType).SequenceEqual(argumentTypes));

            if (selectedConstructor == null)
                throw new MvxIoCResolveException($"Failed to find constructor for type { type.FullName } with arguments: { argumentTypes.Select(x => x.Name + ", ") }");

            return IoCConstruct(type, selectedConstructor, arguments);
        }

        public virtual object IoCConstruct(Type type, IDictionary<string, object> arguments)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            var firstConstructor = constructors.FirstOrDefault();

            if (firstConstructor == null)
                throw new MvxIoCResolveException("Failed to find constructor for type {0}", type.FullName);

            var parameters = GetIoCParameterValues(type, firstConstructor, arguments);
            return IoCConstruct(type, firstConstructor, parameters.ToArray());
        }

        protected virtual object IoCConstruct(Type type, ConstructorInfo constructor, object[] arguments)
        {
            object toReturn;
            try
            {
                toReturn = constructor.Invoke(arguments);
            }
            catch (TargetInvocationException invocation)
            {
                throw new MvxIoCResolveException(invocation, "Failed to construct {0}", type.Name);
            }

            try
            {
                InjectProperties(toReturn);
            }
            catch (Exception)
            {
                if (!Options.CheckDisposeIfPropertyInjectionFails)
                    throw;

                toReturn.DisposeIfDisposable();
                throw;
            }
            return toReturn;
        }

        public void CallbackWhenRegistered<T>(Action action)
        {
            CallbackWhenRegistered(typeof(T), action);
        }

        public void CallbackWhenRegistered(Type type, Action action)
        {
            lock (_lockObject)
            {
                if (!CanResolve(type))
                {
                    List<Action> actions;
                    if (_waiters.TryGetValue(type, out actions))
                    {
                        actions.Add(action);
                    }
                    else
                    {
                        actions = new List<Action> { action };
                        _waiters[type] = actions;
                    }
                    return;
                }
            }

            // if we get here then the type is already registered - so call the aciton immediately
            action();
        }

        public void CleanAllResolvers()
        {
            _resolvers.Clear();
            _waiters.Clear();
            _circularTypeDetection.Clear();
        }

        public enum ResolverType
        {
            DynamicPerResolve,
            Singleton,
            Unknown
        }

        public virtual IMvxIoCProvider CreateChildContainer() => new MvxIoCContainer(this);

        private static readonly ResolverType? ResolverTypeNoneSpecified = null;

        private bool Supports(IResolver resolver, ResolverType? requiredResolverType)
        {
            if (!requiredResolverType.HasValue)
                return true;
            return resolver.ResolveType == requiredResolverType.Value;
        }

        private bool InternalTryResolve(Type type, out object resolved)
        {
            return InternalTryResolve(type, ResolverTypeNoneSpecified, out resolved);
        }

        private bool InternalTryResolve(Type type, ResolverType? requiredResolverType, out object resolved)
        {
            IResolver resolver;
            if (!TryGetResolver(type, out resolver))
            {
                if (_parentProvider != null && _parentProvider.TryResolve(type, out resolved))
                {
                    return true;
                }

                resolved = type.CreateDefault();
                return false;
            }

            if (!Supports(resolver, requiredResolverType))
            {
                resolved = type.CreateDefault();
                return false;
            }

            return InternalTryResolve(type, resolver, out resolved);
        }

        private bool TryGetResolver(Type type, out IResolver resolver)
        {
            if (_resolvers.TryGetValue(type, out resolver))
            {
                return true;
            }

            if (!type.GetTypeInfo().IsGenericType)
            {
                return false;
            }

            return _resolvers.TryGetValue(type.GetTypeInfo().GetGenericTypeDefinition(), out resolver);
        }

        private bool ShouldDetectCircularReferencesFor(IResolver resolver)
        {
            switch (resolver.ResolveType)
            {
                case ResolverType.DynamicPerResolve:
                    return Options.TryToDetectDynamicCircularReferences;

                case ResolverType.Singleton:
                    return Options.TryToDetectSingletonCircularReferences;

                case ResolverType.Unknown:
                    throw new MvxException("A resolver must have a known type - error in {0}", resolver.GetType().Name);
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolver), "unknown resolveType of " + resolver.ResolveType);
            }
        }

        private bool InternalTryResolve(Type type, IResolver resolver, out object resolved)
        {
            var detectingCircular = ShouldDetectCircularReferencesFor(resolver);
            if (detectingCircular)
            {
                try
                {
                    _circularTypeDetection.Add(type, true);
                }
                catch (ArgumentException)
                {
                    // the item already exists in the lookup table
                    // - this is "game over" for the IoC lookup
                    // - see https://github.com/MvvmCross/MvvmCross/issues/553
                    MvxLog.Instance.Error("IoC circular reference detected - cannot currently resolve {0}", type.Name);
                    resolved = type.CreateDefault();
                    return false;
                }
            }

            try
            {
                if (resolver is ConstructingOpenGenericResolver)
                {
                    resolver.SetGenericTypeParameters(type.GetTypeInfo().GenericTypeArguments);
                }

                var raw = resolver.Resolve();
                if (!type.IsInstanceOfType(raw))
                {
                    throw new MvxException("Resolver returned object type {0} which does not support interface {1}",
                                           raw.GetType().FullName, type.FullName);
                }

                resolved = raw;
                return true;
            }
            finally
            {
                if (detectingCircular)
                {
                    _circularTypeDetection.Remove(type);
                }
            }
        }

        private void InternalSetResolver(Type interfaceType, IResolver resolver)
        {
            List<Action> actions;
            lock (_lockObject)
            {
                _resolvers[interfaceType] = resolver;
                if (_waiters.TryGetValue(interfaceType, out actions))
                {
                    _waiters.Remove(interfaceType);
                }
            }

            if (actions != null)
            {
                foreach (var action in actions)
                {
                    action();
                }
            }
        }

        protected virtual void InjectProperties(object toReturn)
        {
            _propertyInjector?.Inject(toReturn, _options.PropertyInjectorOptions);
        }

        protected virtual List<object> GetIoCParameterValues(Type type, ConstructorInfo firstConstructor, IDictionary<string, object> arguments)
        {
            var parameters = new List<object>();
            foreach (var parameterInfo in firstConstructor.GetParameters())
            {
                object parameterValue;
                if (arguments != null && arguments.ContainsKey(parameterInfo.Name))
                {
                    parameterValue = arguments[parameterInfo.Name];
                }
                else if (!TryResolve(parameterInfo.ParameterType, out parameterValue))
                {
                    if (parameterInfo.IsOptional)
                    {
                        parameterValue = Type.Missing;
                    }
                    else
                    {
                        throw new MvxIoCResolveException(
                            "Failed to resolve parameter for parameter {0} of type {1} when creating {2}. You may pass it as an argument",
                            parameterInfo.Name,
                            parameterInfo.ParameterType.Name,
                            type.FullName);
                    }
                }

                parameters.Add(parameterValue);
            }
            return parameters;
        }
    }
}
