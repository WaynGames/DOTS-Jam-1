
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;

partial struct SceneLoaderSystem : ISystem
{
    private ComponentLookup<SceneSwitchComponent> _sceneSwitchComponentLookUp;
    private ComponentLookup<PlayerTag> _playerTagLookUp;
    private NativeQueue<int> _sceneLoadCommand;
    


    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _sceneSwitchComponentLookUp = SystemAPI.GetComponentLookup<SceneSwitchComponent>(true);
        _playerTagLookUp = SystemAPI.GetComponentLookup<PlayerTag>(true);
        _sceneLoadCommand = new NativeQueue<int>(Allocator.Persistent);

    }

    public void OnUpdate(ref SystemState state)
    {

        
        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        _playerTagLookUp.Update(ref state);
        _sceneSwitchComponentLookUp.Update(ref state);
        state.Dependency = new InteracitonJob()
        {
            PlayerTagLookUp = _playerTagLookUp,
            SceneSwitchComponentLookUp = _sceneSwitchComponentLookUp,
            SceneLoadCommand = _sceneLoadCommand
        }.Schedule(simulation, state.Dependency);

        state.Dependency.Complete();
        if (_sceneLoadCommand.TryDequeue(out int scenetoLoad))
        {
            GameObject ui = GameObject.FindGameObjectWithTag("LoadScreenCanvas");
            GameObject.DontDestroyOnLoad(ui);            
            ui.SetActive(false);
            ui.SetActiveRecursively(true);
            SceneManager.LoadScene(scenetoLoad,LoadSceneMode.Single);
            GameObject.Destroy(ui,4f);



        }


    }


    public struct InteracitonJob : ITriggerEventsJob
    {
        [ReadOnly]
        public ComponentLookup<SceneSwitchComponent> SceneSwitchComponentLookUp;
        [ReadOnly]
        public ComponentLookup<PlayerTag> PlayerTagLookUp;

        public NativeQueue<int> SceneLoadCommand;

        public void Execute(TriggerEvent triggerEvent)
        {

             var (player, sceneSwitch) =  IdentifyEntityPair(triggerEvent);
            Debug.Log($"{player.Index}:{player.Version}/{sceneSwitch.Index}:{sceneSwitch.Version}");

            if (!ShouldProcess(player, sceneSwitch)) return;

            Debug.Log($"Processing");


            SceneLoadCommand.Enqueue(SceneSwitchComponentLookUp[sceneSwitch].TargetScene);

        }

        private static bool ShouldProcess(Entity player, Entity sceneSwitch)
        {
            return !Entity.Null.Equals(player) && !Entity.Null.Equals(sceneSwitch);
        }


        private (Entity, Entity) IdentifyEntityPair(TriggerEvent triggerEvent)
        {
            Entity player = Entity.Null;
            Entity sceneSwitch = Entity.Null;

            // For each entity involved in the even, we check if it is available in one of the lookup 
            if (PlayerTagLookUp.HasComponent(triggerEvent.EntityA))
                player = triggerEvent.EntityA;
            if (PlayerTagLookUp.HasComponent(triggerEvent.EntityB))
                player = triggerEvent.EntityB;
            if (SceneSwitchComponentLookUp.HasComponent(triggerEvent.EntityA))
                sceneSwitch = triggerEvent.EntityA;
            if (SceneSwitchComponentLookUp.HasComponent(triggerEvent.EntityB))
                sceneSwitch = triggerEvent.EntityB;
            return (player, sceneSwitch);
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        _sceneLoadCommand.Dispose();
    }
}
