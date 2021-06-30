// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Configuration;
using osu.Game.Input.Bindings;

namespace osu.Game.Screens.Edit.Components.Menus
{
    public class GlobalActionAccelerator : IAccelerator
    {
        public string Representation { get; private set; }

        public GlobalActionAccelerator(OsuConfigManager config, GlobalAction action)
        {
            Representation = config.LookupKeyBindings != null ? config.LookupKeyBindings(action) : "";
        }
    }
}
