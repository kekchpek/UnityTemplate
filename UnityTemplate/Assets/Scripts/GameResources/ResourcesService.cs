using GMConsole;
using GameResources.Domain;
using kekchpek.MVVM.Models.GameResources.Container;
using UnityEngine;

namespace kekchpek.MVVM.Models.GameResources
{
    public class ResourcesService : BaseResourcesService<float>, IFloatResourcesService
    {
        private const string SetResCommand = "SetRes";

        public ResourcesService(
            IResourcesMutableModel model,
            IGameMasterCommandRegistry gameMasterCommandRegistry)
            : base(
                model,
                gameMasterCommandRegistry,
                SetResCommand,
                "Устанавливает значение ресурса. Пример: \"SetRes Gold 99999\"")
        {
        }

        protected override float Zero => 0f;

        protected override float Add(float left, float right) => left + right;

        protected override float Subtract(float left, float right) => left - right;

        protected override bool GreaterOrEqual(float left, float right) => left >= right;

        protected override bool Less(float left, float right) => left < right;

        protected override void SetResourceFromCommand(GMArgs args)
        {
            Model.SetResource(ResourceId.FromString(args.GetString()), args.GetFloat());
        }
    }
}
