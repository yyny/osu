// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;
using osu.Framework.Bindables;
using osu.Framework.Input;

namespace osu.Game.Screens.Edit.Components.Menus
{
    public class PlatformActionAccelerator : ConstBindable<string>, IAccelerator
    {
        public PlatformActionAccelerator(GameHost host, PlatformActionType action)
            : base(PlatformActionAcceleratorString(host, action))
        {
        }

        public static string PlatformActionAcceleratorString(GameHost host, PlatformActionType actionType)
        {
            foreach (var keybind in host.PlatformKeyBindings) {
                if (keybind.Action is PlatformAction action)
                {
                    if (action.ActionType == actionType)
                        return keybind.KeyCombination.ReadableString();
                }
            }
            return "";
        }
    }
}
