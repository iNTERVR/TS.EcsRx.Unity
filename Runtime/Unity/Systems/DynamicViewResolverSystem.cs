﻿using EcsRx.Unity.Dependencies;
using EcsRx.Collections;
using EcsRx.Collections.Database;
using EcsRx.Entities;
using EcsRx.Events;
using EcsRx.Extensions;
using EcsRx.Groups;
using EcsRx.Plugins.ReactiveSystems.Systems;
using EcsRx.Unity.MonoBehaviours;
using EcsRx.Plugins.Views.Components;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace EcsRx.Unity.Systems
{
    public abstract class DynamicViewResolverSystem : ISetupSystem, ITeardownSystem
    {
        public IEventSystem EventSystem { get; }
        public IEntityDatabase EntityDatabase { get; }
        public IUnityInstantiator Instantiator { get; }

        public abstract IGroup Group { get; }

        protected DynamicViewResolverSystem(IEventSystem eventSystem, IEntityDatabase entityDatabase, IUnityInstantiator instantiator)
        {
            EventSystem = eventSystem;
            EntityDatabase = entityDatabase;
            Instantiator = instantiator;
        }

        public abstract GameObject CreateView(IEntity entity);
        protected virtual void OnViewCreated(IEntity entity, GameObject view) { }
        public abstract void DestroyView(IEntity entity, GameObject view);
        
        public void Setup(IEntity entity)
        {
            var viewComponent = entity.GetComponent<ViewComponent>();
            if (viewComponent.View != null) { return; }
            
            var viewGameObject = CreateView(entity);
            viewComponent.View = viewGameObject;

            OnViewCreated(entity, viewComponent.View as GameObject);
            
            var entityBinding = viewGameObject.GetComponent<EntityView>();
            if (entityBinding == null)
            {
                entityBinding = viewGameObject.AddComponent<EntityView>();
                entityBinding.Entity = entity;
                entityBinding.EntityCollection = EntityDatabase.GetCollectionFor(entity);
            }

            if (viewComponent.DestroyWithView)
            {
                viewGameObject.OnDestroyAsObservable()
                    .Subscribe(x => entityBinding.EntityCollection.RemoveEntity(entity.Id))
                    .AddTo(viewGameObject);
            }
        }
        
        public void Teardown(IEntity entity)
        {
            var component = entity.GetComponent<ViewComponent>();
            DestroyView(entity, component.View as GameObject);
        }
    }
}