﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using Player;
    using Script;
    using State;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.PedalSwitch)]
    public class PedalSwitchObject : SceneObject
    {
        private const float DESCENDING_HEIGHT = 0.25f;
        private const float DESCENDING_ANIMATION_DURATION = 2f;

        private StandingPlatformController _platformController;

        private readonly PlayerManager _playerManager;
        private readonly SceneManager _sceneManager;
        private readonly GameStateManager _gameStateManager;
        private readonly ScriptManager _scriptManager;

        public PedalSwitchObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
            _gameStateManager = ServiceLocator.Instance.Get<GameStateManager>();
            _scriptManager = ServiceLocator.Instance.Get<ScriptManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetPolyModelRenderer().GetMeshBounds();

            // _h1.pol
            if (ObjectInfo.Name.Equals("_h1.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.2f, 0f),
                    size = new Vector3(3f, 0.5f, 3f),
                };
            }
            else if (ObjectInfo.Name.Equals("_c.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.2f, -0.5f),
                    size = new Vector3(3.5f, 0.5f, 6f),
                };
            }

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);
            _platformController.OnPlayerActorEntered += OnPlayerActorEntered;

            // Set to final position if it is already activated
            if (ObjectInfo.Times == 0)
            {
                Vector3 finalPosition = sceneGameObject.transform.position;
                finalPosition.y -= DESCENDING_HEIGHT;
                sceneGameObject.transform.position = finalPosition;
            }

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerActorGameObject)
        {
            // Prevent duplicate triggers
            if (_gameStateManager.GetCurrentState() != GameState.Gameplay) return;

            if (ObjectInfo.SwitchState == 1) return;

            if (!IsInteractableBasedOnTimesCount()) return;

            ToggleAndSaveSwitchState();

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));

            Pal3.Instance.StartCoroutine(Interact(true));
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            GameObject pedalSwitchGo = GetGameObject();
            var platformController = pedalSwitchGo.GetComponent<StandingPlatformController>();
            Vector3 platformCenterPosition = platformController.GetCollider().bounds.center;
            var actorStandingPosition = new Vector3(
                platformCenterPosition.x,
                platformController.GetPlatformHeight(),
                platformCenterPosition.z);

            var actorMovementController = _sceneManager.GetCurrentScene()
                .GetActorGameObject((int)_playerManager.GetPlayerActor())
                .GetComponent<ActorMovementController>();

            yield return actorMovementController.MoveDirectlyTo(actorStandingPosition, 0);

            // Play descending animation
            Vector3 finalPosition = pedalSwitchGo.transform.position;
            finalPosition.y -= DESCENDING_HEIGHT;

            yield return AnimationHelper.MoveTransform(pedalSwitchGo.transform,
                finalPosition,
                DESCENDING_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            ExecuteScriptIfAny();

            yield return ActivateOrInteractWithLinkedObjectIfAny();

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Gameplay));
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_platformController);
            }

            base.Deactivate();
        }
    }
}