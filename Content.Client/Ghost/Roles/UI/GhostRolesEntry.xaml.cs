using System;
using Content.Shared.Ghost.Roles;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Ghost.Roles.UI
{
    [GenerateTypedNameReferences]
    public partial class GhostRolesEntry : BoxContainer
    {
        public GhostRolesEntry(GhostRoleInfo info, Action<BaseButton.ButtonEventArgs> requestAction)
        {
            RobustXamlLoader.Load(this);

            Title.SetMessage(info.Name);
            Description.SetMessage(info.Description);
            RequestButton.OnPressed += requestAction;
        }
    }
}