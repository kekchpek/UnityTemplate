using System.Globalization;
using BigInteger = System.Numerics.BigInteger;
using GMConsole;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources
{
    public class LargeResourcesService : BaseResourcesService<BigInteger>, ILargeResourcesService
    {
        private const string SetLargeResCommand = "SetLargeRes";

        public LargeResourcesService(
            ILargeResourcesMutableModel model,
            IGameMasterCommandRegistry gameMasterCommandRegistry)
            : base(
                model,
                gameMasterCommandRegistry,
                SetLargeResCommand,
                "Устанавливает значение large-ресурса. Пример: \"SetLargeRes Gold 999999999999999999\"")
        {
        }

        public override void Initialize()
        {
            Model.RegisterResource(ResourceId.Gold);
            Model.RegisterResource(ResourceId.Stone);
            Model.RegisterResource(ResourceId.Crystals);
            Model.RegisterResource(ResourceId.Wood);
        }

        protected override BigInteger Zero => BigInteger.Zero;

        protected override BigInteger Add(BigInteger left, BigInteger right) => left + right;

        protected override BigInteger Subtract(BigInteger left, BigInteger right) => left - right;

        protected override bool GreaterOrEqual(BigInteger left, BigInteger right) => left >= right;

        protected override bool Less(BigInteger left, BigInteger right) => left < right;

        protected override void SetResourceFromCommand(GMArgs args)
        {
            Model.SetResource(
                ResourceId.FromString(args.GetString()),
                BigInteger.Parse(args.GetString(), CultureInfo.InvariantCulture));
        }
    }
}
