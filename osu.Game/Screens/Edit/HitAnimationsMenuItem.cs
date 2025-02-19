// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Game.Screens.Edit.Components.Menus;

namespace osu.Game.Screens.Edit
{
    internal class HitAnimationsMenuItem : ToggleEditorMenuItem
    {
        [UsedImplicitly]
        private readonly Bindable<bool> hitAnimations;

        public HitAnimationsMenuItem(Bindable<bool> hitAnimations)
            : base("Hit animations")
        {
            State.BindTo(this.hitAnimations = hitAnimations);
        }
    }
}
