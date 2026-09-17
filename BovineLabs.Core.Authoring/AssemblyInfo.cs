using System.Runtime.CompilerServices;
using Unity.Entities;

[assembly: DisableAutoTypeRegistration]
[assembly: InternalsVisibleTo("BovineLabs.Core.Editor")]
[assembly: RegisterGenericComponentType(typeof(BovineLabs.Core.Authoring.Settings.SettingsPrefabIdentity))]

#if UNITY_PHYSICS
[assembly: RegisterUnityEngineComponentType(typeof(BovineLabs.Core.Authoring.Entities.RemovePhysicsVelocityAuthoring.RemovePhysicsVelocityBaking))]
#endif
