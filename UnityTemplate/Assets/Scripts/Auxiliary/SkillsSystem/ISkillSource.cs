using AsyncReactAwait.Bindable;
using kekchpek.Auxiliary.Collections;

namespace SkillsSystem 
{
    public interface ISkillSource 
    {

        string SkillId { get; }

        IBindable<string> CurrentValue { get; }

        IBindable<string> NextValue { get; }

        IBindable<int> Level { get; }
        
        int InitialLevel { get; }

        IBindable<int> MaxLevel { get; }

        IBindable<HyperListReadonlyToken<string>> FormattingArgs { get; }

        void SetLevel(int level);

    }
}