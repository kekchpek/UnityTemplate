using UnityMVVM.DI;
using Zenject;

namespace SkillsSystem 
{
    public class SkillsSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.FastBind<ISkillsMutableModel, ISkillsModel, SkillsModel>();
            Container.FastBind<ISkillsService, SkillsService>();
        }
    }
}