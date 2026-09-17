#if !BL_DISABLE_CREATE_EDITOR_WORLD
namespace BovineLabs.Core.Editor
{
    using System.Threading.Tasks;
    using Unity.Entities;
    using UnityEditor.Scripting.LifecycleManagement;

    internal static partial class CreateEditorWorld
    {
        [OnEnteringEditMode]
        private static void Initialize()
        {
            _ = InitializeInternal();
        }

        private static async Task InitializeInternal()
        {
            await Task.Yield();

            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
        }
    }
}
#endif
