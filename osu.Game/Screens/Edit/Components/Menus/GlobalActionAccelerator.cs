// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Configuration;
using osu.Framework.Bindables;
using osu.Game.Input.Bindings;

namespace osu.Game.Screens.Edit.Components.Menus
{
    public class GlobalActionAccelerator : Bindable<string>, IAccelerator
    {
        public GlobalActionAccelerator(OsuConfigManager config, GlobalAction action)
            : base(config.LookupKeyBindings(action))
        {
        }
    }
}
